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
            // TabItem lobbyTabItem = new TabItem {Header = "Lobby", HeaderTemplate = ChatTabControl.FindResource("TabHeader") as DataTemplate};
            // TextBox txt = new TextBox {Name = "txt"};
            // lobbyTabItem.Content = txt;
            // _tabItems.Add(lobbyTabItem);
            AddSeparadorChat(new Aluno() {Nome = "Lobby", Email = "lobby@asdsa"});
            // ChatTabControl.DataContext = _tabItems;
            // ChatTabControl.SelectedItem = lobbyTabItem;
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
                        .WithParentActivityOrWindow(new WindowInteropHelper(this)
                            .Handle) // optional, used to center the browser on the window
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
            TextBlockUtilizadorLogado.Text = "";
            TextBlockUtilizadorLogado.Text += nomeTemp + " (" + EmailUtilizadorLigado + ")";
            EntrarPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            if (TextBlockUtilizadorLogado.Text.Contains("alunos"))
            {
                Aluno a1 = new Aluno {Nome = nomeTemp, Email = EmailUtilizadorLigado};
                MainViewModel.ConnectAction(a1);
            }
            else
            {
                Professor p1 = new Professor {Nome = nomeTemp, Email = EmailUtilizadorLigado};
                MainViewModel.ConnectAction(p1);

                // TODO: Fazer o mesmo para professores
            }
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
            if (accounts.Any())
            {
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
        }

        /// <summary>
        /// Adiciona a Mensagem ao chat e envia-a para a MainViewModel
        /// <para>TODO: Detetar o Destinatário</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnviarMensagem_OnClick(object sender, RoutedEventArgs e)
        {
            Mensagem mensagem = new Mensagem
            {
                Conteudo = TextBoxMensagem.Text, DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"),
                Destinatario = "loby", NomeRemetente = NomeUtilizadorLigado, Remetente = EmailUtilizadorLigado
            };
            DisplayMensagem(mensagem);
            MainViewModel.ServerConnectService.EnviarMensagem(mensagem);
            TextBoxMensagem.Text = "";
        }

        /// <summary>
        /// Recebe uma Mensagem e cria os TextBlock's para colocar no chat
        /// <para>TODO: Colocar a funcionar com vários separadores de chat dependendo do Destinatário</para>
        /// </summary>
        /// <param name="mensagem">Mensagem a mostrar</param>
        private void DisplayMensagem(Mensagem mensagem)
        {
            TextBlock mensagemTextBlock = new TextBlock {FontSize = 15};
            Thickness mensagemTextBlockThickness = mensagemTextBlock.Margin;
            mensagemTextBlockThickness.Top = 10;
            mensagemTextBlock.Margin = mensagemTextBlockThickness;
            if (EmailUtilizadorLigado == mensagem.Remetente)
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
            // LobyChat.Children.Add(mensagemTextBlock);

            TextBlock dataHoraEnvioTextBlock = new TextBlock {FontSize = 9, Text = mensagem.DataHoraEnvio};
            // LobyChat.Children.Add(dataHoraEnvioTextBlock);

            // LobyScrollViewer.ScrollToBottom();
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
            TabItem item = ChatTabControl.Items.Cast<TabItem>().SingleOrDefault(i => i.Name.Equals(tabName));
            TabItem tab = item;
            if (tab == null) return;
            if (TabItems.Count <= 1) return;
            TabItem selectedTabItem = ChatTabControl.SelectedItem as TabItem;
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
        /// </summary>
        /// <param name="utilizador">Utilizador do separador (Destinatário)</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        private void AddSeparadorChat<T>(T utilizador) where T : Utilizador
        {
            string tabName = utilizador.Email.Substring(0, utilizador.Email.IndexOf("@", StringComparison.Ordinal));
            string tabHeader = $"{utilizador.Nome} ({tabName})";
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
                Header = tabHeader, Name = tabName,
                HeaderTemplate = ChatTabControl.FindResource("TabHeader") as DataTemplate
            };

            // TODO: Layout do chat
            TextBox txt = new TextBox {Name = "txt"};
            novaTabItem.Content = txt;

            TabItems.Insert(count, novaTabItem);
            ChatTabControl.DataContext = TabItems;
            ChatTabControl.SelectedItem = novaTabItem;
        }
    }
}