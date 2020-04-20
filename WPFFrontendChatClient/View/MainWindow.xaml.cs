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
        private string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        private string[] scopes = {"user.read"};

        private MainViewModel MainViewModel { get; set; }
        private string NomeUtilizadorLigado { get; set; }
        private string EmailUtilizadorLigado { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;

            DataContext = new MainViewModel();
            MainViewModel = (MainViewModel) DataContext;
            MainViewModel.AddMensagemEvent += DisplayMensagem;
            MainViewModel.AddSeparadorEvent += AddTabItem;

            _tabItems = new List<TabItem>();
            TabItem tabItemAdd = new TabItem {Header = "+"};
            _tabItems.Add(tabItemAdd);
            AddTabItem();
            ChatTabControl.DataContext = _tabItems;
            ChatTabControl.SelectedIndex = 0;
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
                authResult = await app.AcquireTokenSilent(scopes, firstAccount).ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes).WithAccount(accounts.FirstOrDefault())
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
            string nomeTemp = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
            EmailUtilizadorLigado = authResult.Account.Username;
            TextBlockUtilizadorLogado.Text = "";
            TextBlockUtilizadorLogado.Text += nomeTemp + " (" + EmailUtilizadorLigado + ")";
            EntrarPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            Utilizador user;
            if (TextBlockUtilizadorLogado.Text.Contains("alunos"))
            {
                user = new Utilizador(nomeTemp, EmailUtilizadorLigado, Utilizador.UserType.aluno);
            }
            else
            {
                user = new Utilizador(nomeTemp, EmailUtilizadorLigado, Utilizador.UserType.prof);
                // TODO: Fazer o mesmo para professores
            }

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
            Mensagem mensagem = new Mensagem(EmailUtilizadorLigado, "0","Lobby", 
                TextBoxMensagem.Text, NomeUtilizadorLigado);

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
            // LobyChat.Children.Add(mensagemTextBlock);

            TextBlock dataHoraEnvioTextBlock = new TextBlock {FontSize = 9, Text = mensagem.DataHoraEnvio};
            // LobyChat.Children.Add(dataHoraEnvioTextBlock);

            // LobyScrollViewer.ScrollToBottom();
        }


        // ZONA EM CONSTRUÇÃO (Usar Capacete :D )

        private List<TabItem> _tabItems;
        private TabItem _tabAdd;

        private void ChatTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*TabItem tab = ChatTabControl.SelectedItem as TabItem;

            if (tab != null && tab.Header != null)
            {
                if (tab.Header.Equals("+"))
                {
                    AddTabItem();
                }
                else
                {
                    // your code here...
                }
            }*/
        }

        private void CloseTabButton_OnClick(object sender, RoutedEventArgs e)
        {
            string tabName = ((Button) sender).CommandParameter.ToString();
            TabItem item = ChatTabControl.Items.Cast<TabItem>().SingleOrDefault(i => i.Name.Equals(tabName));
            TabItem tab = item;
            if (tab == null) return;
            if (_tabItems.Count < 3)
            {
                MessageBox.Show("Cannot remove last tab.");
            }
            else if (MessageBox.Show($"Are you sure you want to remove the tab '{tab.Header}'?",
                "Remove Tab", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                TabItem selectedTab = ChatTabControl.SelectedItem as TabItem;
                ChatTabControl.DataContext = null;
                _tabItems.Remove(tab);
                ChatTabControl.DataContext = _tabItems;
                if (selectedTab == null || selectedTab.Equals(tab))
                {
                    selectedTab = _tabItems[0];
                }

                ChatTabControl.SelectedItem = selectedTab;
            }
        }

        private void AddTabItem()
        {
            ChatTabControl.DataContext = null;

            int count = _tabItems.Count;
            TabItem tab = new TabItem
            {
                Header = $"Tab {count}",
                Name = $"tab{count}",
                HeaderTemplate = ChatTabControl.FindResource("TabHeader") as DataTemplate
            };

            TextBox txt = new TextBox {Name = "txt"};
            tab.Content = txt;

            _tabItems.Insert(count - 1, tab);
            ChatTabControl.DataContext = _tabItems;
            ChatTabControl.SelectedItem = tab;
        }

        // FIM DE ZONA EM CONSTRUÇÃO
    }
}