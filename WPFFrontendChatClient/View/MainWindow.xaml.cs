using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ClassLibrary;
using CommonServiceLocator;
using Microsoft.Identity.Client;
using Models;
using Newtonsoft.Json.Linq;
using WPFFrontendChatClient.Service;
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
        string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        string[] scopes = {"user.read"};

        // VARIÁVEIS DE TESTES (TEMPORÁRIAS)
        ObservableCollection<Aluno> alunos;
        ObservableCollection<Aula> aulas;

        int numAlunosTeste;

        // FIM DE VARIÁVEIS DE TESTES (TEMPORÁRIAS)
        private char tipoUtilizador; // a = aluno // p = professor

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher =
                Application.Current.Dispatcher;

            // ZONA DE TESTES (CÓDIGO TEMPORÁRIO)
            alunos = new ObservableCollection<Aluno>()
            {
                new Aluno() {Nome = "Nome Apelido 1"},
                new Aluno() {Nome = "Nome Apelido 2"},
                new Aluno() {Nome = "Nome Apelido 3"}
            };
            numAlunosTeste = 3;
            UsersItemsControl.ItemsSource = alunos;

            aulas = new ObservableCollection<Aula>()
            {
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "CD"}},
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "AEDII"}},
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "LPII"}}
            };
            AulasItemsControl.ItemsSource = aulas;
            // FIM DE ZONA DE TESTES (CÓDIGO TEMPORÁRIO)
        }

        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        private async void ButtonEntrar_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

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

            if (authResult != null)
            {
                TextBlockUtilizadorLogado.Text = "";
                TextBlockUtilizadorLogado.Text +=
                    await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken) + " (" +
                    authResult.Account.Username + ")";
                EntrarPanel.Visibility = Visibility.Collapsed;
                ChatPanel.Visibility = Visibility.Visible;

                if (TextBlockUtilizadorLogado.Text.Contains("alunos"))
                {
                    Aluno a1 = new Aluno();
                    a1.Nome = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                    a1.Email = authResult.Account.Username;
                    
                    ServiceLocator.Current.GetInstance<MainViewModel>().ConnectAction(a1);
                }
                else
                {
                    Professor p1 = new Professor();
                    p1.Nome = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                    p1.Email = authResult.Account.Username;
                    
                    ServiceLocator.Current.GetInstance<MainViewModel>().ConnectAction(p1);
                }
            }
        }


        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new HttpClient();

            HttpResponseMessage response;
            try

            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                JObject contentJObject = JObject.Parse(content);
                return (string) contentJObject["givenName"] + " " + (string) contentJObject["surname"];
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        private async void ButtonSair_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();
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
        /// Procedimento de TESTE para ver se o funcionamento de adição de Utilizadores dinamicamente funciona
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdicionarUtilizadorTeste_OnClick(object sender, RoutedEventArgs e)
        {
            alunos.Add(new Aluno()
            {
                Nome = "Nome Apelido " + ++numAlunosTeste
            });

            UsersItemsControl.ItemsSource = alunos;
        }
    }
}