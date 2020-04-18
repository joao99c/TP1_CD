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
        
        private string _nomeUtilizadorLogado;
        private string _emailUtilizadorLogado;

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;

            DataContext = new MainViewModel();
            MainViewModel = (MainViewModel) DataContext;
            MainViewModel.AddMensagemEvent += DisplayMensagem;
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
            _emailUtilizadorLogado = authResult.Account.Username;
            TextBlockUtilizadorLogado.Text = "";
            TextBlockUtilizadorLogado.Text += nomeTemp + " (" + _emailUtilizadorLogado + ")";
            EntrarPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            if (TextBlockUtilizadorLogado.Text.Contains("alunos"))
            {
                Aluno a1 = new Aluno() {Nome = nomeTemp, Email = _emailUtilizadorLogado};
                MainViewModel.ConnectAction(a1);
            }
            else
            {
                Professor p1 = new Professor() {Nome = nomeTemp, Email = _emailUtilizadorLogado};
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
                return _nomeUtilizadorLogado = contentJObject["givenName"] + " " + (string) contentJObject["surname"];
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
            Mensagem mensagem = new Mensagem()
            {
                Conteudo = TextBoxMensagem.Text, DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"),
                Destinatario = "loby", NomeRemetente = _nomeUtilizadorLogado, Remetente = _emailUtilizadorLogado
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
            if (_emailUtilizadorLogado == mensagem.Remetente)
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
            LobyChat.Children.Add(mensagemTextBlock);

            TextBlock dataHoraEnvioTextBlock = new TextBlock {FontSize = 9, Text = mensagem.DataHoraEnvio};
            LobyChat.Children.Add(dataHoraEnvioTextBlock);

            LobyScrollViewer.ScrollToBottom();
        }
    }
}