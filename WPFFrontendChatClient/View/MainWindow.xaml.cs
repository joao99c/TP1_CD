using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using CommonServiceLocator;
using Microsoft.Identity.Client;
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
        // Set the API Endpoint to Graph 'me' endpoint. 
        // To change from Microsoft public cloud to a national cloud, use another value of graphAPIEndpoint.
        // Reference with Graph endpoints here: https://docs.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
        private const string GraphApiEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        private readonly string[] _scopes = {"user.read"};

        private MainViewModel MainViewModel { get; set; }
        private List<TabItem> TabItems { get; set; }

        // TODO: Utilizar Id (int) do objeto Utilizador recebido do servidor
        private string IdUtilizadorLigado { get; set; }
        private string NomeUtilizadorLigado { get; set; }
        private string EmailUtilizadorLigado { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;

            DataContext = new MainViewModel();
            MainViewModel = (MainViewModel) DataContext;
            MainViewModel.AddMensagemEvent += DisplayMensagem;
            MainViewModel.AddSeparadorEvent += AddSeparadorChat;
            TabItems = new List<TabItem>();
            AddSeparadorChat(new Utilizador("Lobby", "lobby"));
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
            IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
            IAccount firstAccount = accounts.FirstOrDefault();
            try
            {
                authResult = await app.AcquireTokenSilent(_scopes, firstAccount).ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
                try
                {
                    authResult = await app.AcquireTokenInteractive(_scopes).WithAccount(accounts.FirstOrDefault())
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle)
                        .WithPrompt(Prompt.SelectAccount).ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    // Erro ao adquirir Token
                    MessageBox.Show("Erro ao adquirir Token: " + msalex);
                    // ResultText.Text = $"Error Acquiring Token:{Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                // Erro ao adquirir Token Silenciosamente
                MessageBox.Show("Erro ao adquirir Token Silenciosamente: " + ex);
                // ResultText.Text = $"Error Acquiring Token Silently:{Environment.NewLine}{ex}";
                return;
            }

            if (authResult == null) return;
            string nomeTemp = await GetHttpContentWithToken(GraphApiEndpoint, authResult.AccessToken);
            EmailUtilizadorLigado = authResult.Account.Username;
            IdUtilizadorLigado =
                EmailUtilizadorLigado.Substring(0, EmailUtilizadorLigado.IndexOf("@", StringComparison.Ordinal));
            TextBlockUtilizadorLogado.Text = "";
            TextBlockUtilizadorLogado.Text += nomeTemp + " (" + EmailUtilizadorLigado + ")";
            EntrarPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            var user = TextBlockUtilizadorLogado.Text.Contains("alunos")
                ? new Utilizador(nomeTemp, EmailUtilizadorLigado, Utilizador.UserType.Aluno)
                : new Utilizador(nomeTemp, EmailUtilizadorLigado, Utilizador.UserType.Prof);
            MainViewModel.ConnectAction(user);
        }

        /// <summary>
        /// Realiza pedido HTTP GET para obter a informação relativa à conta que iniciou sessão
        /// </summary>
        /// <param name="url">API URL</param>
        /// <param name="token">Token de acesso</param>
        /// <returns>Primeiro e último nome do Utilizador</returns>
        private async Task<string> GetHttpContentWithToken(string url, string token)
        {
            HttpClient httpClient = new HttpClient();
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();
                JObject contentJObject = JObject.Parse(content);
                return NomeUtilizadorLigado = contentJObject["givenName"] + " " + (string) contentJObject["surname"];
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Faz Logout do Utilizador atual
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSair_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await App.PublicClientApp.GetAccountsAsync();
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
                MessageBox.Show("Erro ao Terminar Sessão: " + ex.Message);
                // ResultText.Text = $"Error signing-out user: {ex.Message}";
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
            TabItem destinatarioTabItem = (TabItem) ChatTabControl.SelectedItem;
            string nomeDestinatarioTemp = destinatarioTabItem.Header.ToString().Substring(0,
                destinatarioTabItem.Header.ToString().IndexOf(" (", StringComparison.Ordinal));
            Mensagem mensagem = new Mensagem(IdUtilizadorLigado, NomeUtilizadorLigado,
                destinatarioTabItem.Name, nomeDestinatarioTemp, TextBoxMensagem.Text);
            DisplayMensagem(mensagem);
            MainViewModel.ServerConnectService.EnviarMensagem(mensagem);
            TextBoxMensagem.Text = "";
        }

        /// <summary>
        /// Recebe uma Mensagem e cria os TextBlock's para colocar no chat
        /// </summary>
        /// <param name="mensagem">Mensagem a mostrar</param>
        private void DisplayMensagem(Mensagem mensagem)
        {
            // TODO: Utilizar o Utilizador recebido do servidor
            // Utilizador utilizador = MainViewModel.ServerConnectService.UtilizadorLigado;
            TabItem mensagemTabItem = mensagem.IdDestinatario == IdUtilizadorLigado
                ? TabItems.Find(tabItem => tabItem.Name == mensagem.IdRemetente)
                : TabItems.Find(tabItem => tabItem.Name == mensagem.IdDestinatario);
            if (mensagemTabItem == null)
            {
                AddSeparadorChat(new Utilizador(mensagem.NomeRemetente, mensagem.IdRemetente));
                mensagemTabItem = TabItems.Last();
            }

            ScrollViewer destinatarioScrollViewer = (ScrollViewer) mensagemTabItem?.Content;
            ItemsControl destinatarioItemsControl = (ItemsControl) destinatarioScrollViewer?.Content;
            StackPanel destinatarioStackPanel = (StackPanel) destinatarioItemsControl?.Items.GetItemAt(0);

            TextBlock mensagemTextBlock = new TextBlock {FontSize = 15, TextWrapping = TextWrapping.Wrap};
            Thickness mensagemTextBlockThickness = mensagemTextBlock.Margin;
            mensagemTextBlockThickness.Top = 10;
            mensagemTextBlock.Margin = mensagemTextBlockThickness;
            if (EmailUtilizadorLigado == mensagem.IdRemetente)
            {
                mensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                    {FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline});
            }
            else
            {
                mensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                    {FontWeight = FontWeights.Bold});
            }

            mensagemTextBlock.Inlines.Add(" " + mensagem.Conteudo);
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
            if (tabName == "lobby") return;
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
        /// Adiciona um separador de chat.
        /// <para>Se o separador já existir não adiciona.</para>
        /// TODO: Utilizar Id do Utilizador recebido do servidor (em vez do email abaixo)
        /// </summary>
        /// <param name="utilizador">Utilizador do separador (Destinatário)</param>
        private void AddSeparadorChat(Utilizador utilizador)
        {
            string tabHeader = $"{utilizador.Nome} ({utilizador.Email})";
            bool existeTabIgual = false;
            TabItems.ForEach(tab =>
            {
                if ((string) tab.Header == tabHeader)
                {
                    existeTabIgual = true;
                }
            });
            if (existeTabIgual) return;
            ChatTabControl.DataContext = null;
            int count = TabItems.Count;
            TabItem novaTabItem = new TabItem
            {
                Header = tabHeader, Name = utilizador.Email,
                HeaderTemplate = ChatTabControl.FindResource("TabHeader") as DataTemplate
            };
            ScrollViewer chatScrollViewer = new ScrollViewer {Name = $"{utilizador.Email}ScrollViewer"};
            ItemsControl chatItemsControl = new ItemsControl();
            StackPanel chatStackPanel = new StackPanel {Name = $"{utilizador.Email}StackPanel"};
            chatItemsControl.Items.Add(chatStackPanel);
            chatScrollViewer.Content = chatItemsControl;
            novaTabItem.Content = chatScrollViewer;
            TabItems.Insert(count, novaTabItem);
            ChatTabControl.DataContext = TabItems;
            ChatTabControl.SelectedItem = novaTabItem;
        }
    }
}