using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using CommonServiceLocator;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Identity.Client;
using Microsoft.Win32;
using Models;
using Newtonsoft.Json.Linq;
using WPFFrontendChatClient.ViewModel;

namespace WPFFrontendChatClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // API Endpoint to MS Graph
        private const string GraphApiEndpoint = "https://graph.microsoft.com/v1.0/me";
        private readonly string[] _scopes = {"user.read"};

        private MainViewModel MainViewModel { get; set; }
        private List<TabItem> TabItems { get; set; }

        private ICommand PedirFicheiroCommand
        {
            get
            {
                return new RelayCommand<string>((nomeFicheiro) =>
                {
                    if (nomeFicheiro == null) return;
                    PedirFicheiro(nomeFicheiro);
                });
            }
        }

        /// <summary>
        /// Construtor da MainWindow
        /// <para>Inicializa o componente de "ativa" a escuta de eventos</para>
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;

            DataContext = new MainViewModel();
            MainViewModel = (MainViewModel) DataContext;
            MainViewModel.AddMensagemRecebidaEventMvm += DisplayMensagem;
            MainViewModel.AddSeparadorEvent += AddSeparadorChat;
            TabItems = new List<TabItem>();
            AddSeparadorChat("Lobby", "lobby", "id0");
        }

        /// <summary>
        /// Inicia sessão de um Utilizador
        /// <para>Chama AcquireToken para obter o Token de inicio de sessão</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonEntrar_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            IPublicClientApplication app = App.PublicClientApp;
            IEnumerable<IAccount> accounts = (await app.GetAccountsAsync()).ToList();
            IAccount firstAccount = accounts.FirstOrDefault();
            try
            {
                authResult = await app.AcquireTokenSilent(_scopes, firstAccount).ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                Console.WriteLine(@"MsalUiRequiredException: " + ex.Message);
                try
                {
                    authResult = await app.AcquireTokenInteractive(_scopes).WithAccount(accounts.FirstOrDefault())
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle)
                        .WithPrompt(Prompt.SelectAccount).ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    // Erro ao adquirir Token
                    MessageBox.Show("Erro ao adquirir Token: " + msalex,
                        "WPFFrontendChatClient: MainWindow.ButtonEntrar_Click");
                }
            }
            catch (Exception ex)
            {
                // Erro ao adquirir Token Silenciosamente
                MessageBox.Show("Erro ao adquirir Token Silenciosamente: " + ex,
                    "WPFFrontendChatClient: MainWindow.ButtonEntrar_Click");
            }

            if (authResult == null) return;
            string nomeTemp = await GetHttpContentWithToken(GraphApiEndpoint, authResult.AccessToken);
            string emailUtilizadorLigadoTemp = authResult.Account.Username;
            TextBlockUtilizadorLigado.Text = "";
            TextBlockUtilizadorLigado.Text += nomeTemp + " (" + emailUtilizadorLigadoTemp + ")";
            EntrarPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            var user = TextBlockUtilizadorLigado.Text.Contains("alunos")
                ? new Utilizador(nomeTemp, emailUtilizadorLigadoTemp, Utilizador.UserType.Aluno)
                : new Utilizador(nomeTemp, emailUtilizadorLigadoTemp, Utilizador.UserType.Prof);
            MainViewModel.ConnectAction(user);
        }

        /// <summary>
        /// Realiza pedido HTTP GET para obter a informação relativa à conta que iniciou sessão
        /// </summary>
        /// <param name="url">API URL</param>
        /// <param name="token">Token de acesso</param>
        /// <returns>Primeiro e último nome do Utilizador</returns>
        private static async Task<string> GetHttpContentWithToken(string url, string token)
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();
                JObject contentJObject = JObject.Parse(content);
                return contentJObject["givenName"] + " " + (string) contentJObject["surname"];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "WPFFrontendChatClient: MainWindow.GetHttpContentWithToken");
                return ex.Message;
            }
        }

        /// <summary>
        /// Faz Logout do Utilizador atual
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSair_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = (await App.PublicClientApp.GetAccountsAsync()).ToList();
            if (!accounts.Any()) return;
            try
            {
                await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                ChatPanel.Visibility = Visibility.Collapsed;
                EntrarPanel.Visibility = Visibility.Visible;
            }
            catch (MsalException ex)
            {
                // Erro ao Sair
                MessageBox.Show(ex.Message, "WPFFrontendChatClient: MainWindow.ButtonSair_Click");
            }
        }

        /// <summary>
        /// Cria a Mensagem.
        /// <para>Chama a função que a coloca no chat.</para>
        /// Chama a função que a envia para o servidor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnviarMensagem_OnClick(object sender, RoutedEventArgs e)
        {
            Mensagem mensagem = ConstruirMensagem(TextBoxMensagem.Text);
            DisplayMensagem(mensagem);
            MainViewModel.ServerConnectService.EnviarMensagem(mensagem);
            TextBoxMensagem.Text = "";
            TextBoxMensagem.Focus();
        }

        /// <summary>
        /// Cria a mensagem.
        /// <para>Abre a janela de seleção de ficheiro (se o separador selecionado não for o do "Lobby").</para>
        /// Chama a função que envia o ficheiro para o servidor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnviarFicheiro_OnClick(object sender, RoutedEventArgs e)
        {
            if (((TabItem) ChatTabControl.SelectedItem).Name.Contains("id0")) return;
            OpenFileDialog fileDialog = new OpenFileDialog();
            bool? resultado = fileDialog.ShowDialog();
            if (resultado == false) return;
            string caminhoFicheiro = fileDialog.FileName;
            Mensagem mensagem = ConstruirMensagem("ficheiro");
            mensagem.IsFicheiro = true;
            MainViewModel.ServerConnectService.EnviarFicheiro(caminhoFicheiro, mensagem);
        }

        /// <summary>
        /// Constrói uma Mensagem com um determinado conteúdo
        /// </summary>
        /// <param name="conteudo">Conteúdo da Mensagem</param>
        /// <returns></returns>
        private Mensagem ConstruirMensagem(string conteudo)
        {
            TabItem destinatarioTabItem = (TabItem) ChatTabControl.SelectedItem;
            if (destinatarioTabItem.Header.ToString().Contains(" ("))
            {
                // Se for uma mensagem privada
                return new Mensagem(MainViewModel.ServerConnectService.UtilizadorLigado.Id.ToString(),
                    MainViewModel.ServerConnectService.UtilizadorLigado.Nome,
                    MainViewModel.ServerConnectService.UtilizadorLigado.Email, destinatarioTabItem.Name.Remove(0, 2),
                    destinatarioTabItem.Header.ToString().Substring(0,
                        destinatarioTabItem.Header.ToString().IndexOf(" (", StringComparison.Ordinal)), conteudo);
            }

            // Se for uma mensagem para uma Aula
            return new Mensagem(MainViewModel.ServerConnectService.UtilizadorLigado.Id.ToString(),
                MainViewModel.ServerConnectService.UtilizadorLigado.Nome,
                MainViewModel.ServerConnectService.UtilizadorLigado.Email, destinatarioTabItem.Name,
                destinatarioTabItem.Header.ToString(), conteudo);
        }

        /// <summary>
        /// Recebe uma Mensagem e cria os TextBlock's para colocar no chat.
        /// <para>
        ///     Se a mensagem for criada pelo envio de um ficheiro, esta irá ter um comando ativado por Click para pedir
        ///     o ficheiro (download) ao servidor.
        /// </para>
        /// </summary>
        /// <param name="mensagem">Mensagem a mostrar</param>
        private void DisplayMensagem(Mensagem mensagem)
        {
            TabItem mensagemTabItem;
            if (mensagem.IdDestinatario.Contains("uc"))
            {
                // Se for uma mensagem para uma Aula
                mensagemTabItem = TabItems.Find(tabItem => tabItem.Name == mensagem.IdDestinatario);
            }
            else
            {
                // Se for uma mensagem privada
                mensagemTabItem =
                    mensagem.IdDestinatario == MainViewModel.ServerConnectService.UtilizadorLigado.Id.ToString()
                        ? TabItems.Find(tabItem => tabItem.Name == "id" + mensagem.IdRemetente)
                        : TabItems.Find(tabItem => tabItem.Name == "id" + mensagem.IdDestinatario);
            }

            if (mensagemTabItem == null)
            {
                // Se não existir separador de chat aberto
                if (mensagem.IdDestinatario.Contains("uc"))
                {
                    // Vai abrir um separador da UC
                    AddSeparadorChat(mensagem.NomeDestinatario, null, mensagem.IdDestinatario);
                    // Termina a execução da função porque ao fazer "AddSeparadorChat" todas as mensagens vão ser
                    // apresentadas logo a mensagem recebida não pode ser mostrada (senão fica duplicada)
                    return;
                }

                // OU Vai abrir um separador do Utilizador
                AddSeparadorChat(mensagem.NomeRemetente,
                    mensagem.EmailRemetente.Substring(0,
                        mensagem.EmailRemetente.IndexOf("@", StringComparison.Ordinal)),
                    mensagem.IdRemetente.Insert(0, "id"));
                // Termina a execução da função porque ao fazer "AddSeparadorChat" todas as mensagens vão ser
                // apresentadas logo a mensagem recebida não pode ser mostrada (senão fica duplicada)
                return;
            }

            ScrollViewer destinatarioScrollViewer = (ScrollViewer) mensagemTabItem?.Content;
            ItemsControl destinatarioItemsControl = (ItemsControl) destinatarioScrollViewer?.Content;
            StackPanel destinatarioStackPanel = (StackPanel) destinatarioItemsControl?.Items.GetItemAt(0);

            TextBlock mensagemTextBlock = new TextBlock {FontSize = 15, TextWrapping = TextWrapping.Wrap};
            Thickness mensagemTextBlockThickness = mensagemTextBlock.Margin;
            mensagemTextBlockThickness.Top = 10;
            mensagemTextBlock.Margin = mensagemTextBlockThickness;
            if (MainViewModel.ServerConnectService.UtilizadorLigado.Id.ToString() == mensagem.IdRemetente)
            {
                mensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                    {FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline});
            }
            else
            {
                mensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                    {FontWeight = FontWeights.Bold});
            }

            mensagemTextBlock.Inlines.Add(" ");

            if (!mensagem.IsFicheiro)
            {
                mensagemTextBlock.Inlines.Add(mensagem.Conteudo);
            }
            else
            {
                // Adiciona o Comando (ativado por Click) para pedir o ficheiro
                mensagemTextBlock.Inlines.Add(new Run(mensagem.Conteudo)
                    {TextDecorations = TextDecorations.Underline, Foreground = Brushes.Blue});
                mensagemTextBlock.Cursor = Cursors.Hand;
                mensagemTextBlock.InputBindings.Add(new MouseBinding
                {
                    Gesture = new MouseGesture(MouseAction.LeftClick), Command = PedirFicheiroCommand,
                    CommandParameter = mensagem.Conteudo
                });
            }

            destinatarioStackPanel?.Children.Add(mensagemTextBlock);

            TextBlock dataHoraEnvioTextBlock = new TextBlock {FontSize = 9, Text = mensagem.DataHoraEnvio};
            destinatarioStackPanel?.Children.Add(dataHoraEnvioTextBlock);

            destinatarioScrollViewer?.ScrollToBottom();
        }

        /// <summary>
        /// Fecha o separador de Chat.
        /// <para>Não deixa fechar o separador do "Lobby".</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseTabButton_OnClick(object sender, RoutedEventArgs e)
        {
            string tabName = ((Button) sender).CommandParameter.ToString();
            if (tabName == "id0") return; // Se for o Lobby
            TabItem tab = ChatTabControl.Items.Cast<TabItem>().SingleOrDefault(i => i.Name.Equals(tabName));
            if (tab == null) return;
            if (TabItems.Count <= 1) return;
            TabItem selectedTabItem = (TabItem) ChatTabControl.SelectedItem;
            ChatTabControl.DataContext = null;
            TabItems.Remove(tab);
            ChatTabControl.DataContext = TabItems;
            if (selectedTabItem == null || selectedTabItem.Equals(tab))
            {
                selectedTabItem = TabItems[0];
            }

            ChatTabControl.SelectedItem = selectedTabItem;
        }

        /// <summary>
        /// Adiciona um separador de chat com todas as suas mensagens
        /// <para>Se o separador já existir não adiciona.</para>
        /// </summary>
        /// <param name="displayName">Nome a mostrar no título do separador</param>
        /// <param name="displayId">Id a mostrar no titulo do separador</param>
        /// <param name="idName">Id a colocar no Name (identificador) do separador</param>
        private void AddSeparadorChat(string displayName, string displayId, string idName)
        {
            string tabHeader = displayId == null ? displayName : $"{displayName} ({displayId})";
            bool existeTabIgual = false;
            TabItems.ForEach(tab =>
            {
                if ((string) tab.Header != tabHeader) return;
                existeTabIgual = true;
            });
            if (existeTabIgual) return;
            ChatTabControl.DataContext = null;
            int count = TabItems.Count;
            TabItem novaTabItem = new TabItem
            {
                Header = tabHeader, Name = idName,
                HeaderTemplate = ChatTabControl.FindResource("TabHeader") as DataTemplate
            };
            ScrollViewer chatScrollViewer = new ScrollViewer {Name = $"{idName}ScrollViewer"};
            ItemsControl chatItemsControl = new ItemsControl();
            StackPanel chatStackPanel = new StackPanel {Name = $"{idName}StackPanel"};
            chatItemsControl.Items.Add(chatStackPanel);
            chatScrollViewer.Content = chatItemsControl;
            novaTabItem.Content = chatScrollViewer;
            TabItems.Insert(count, novaTabItem);
            ChatTabControl.DataContext = TabItems;
            ChatTabControl.SelectedItem = novaTabItem;
            if (idName == "id0") return;
            MainViewModel.ServerConnectService.EntrarChat(idName);
        }

        /// <summary>
        /// Executa o procedimento que pede o ficheiro ao servidor
        /// </summary>
        /// <param name="nomeFicheiro">Nome do ficheiro a pedir</param>
        private void PedirFicheiro(string nomeFicheiro)
        {
            MainViewModel.ServerConnectService.PedirFicheiro(nomeFicheiro);
        }
    }
}