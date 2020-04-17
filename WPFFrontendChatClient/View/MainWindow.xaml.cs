using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
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
        private ObservableCollection<Aluno> _alunos;
        private ObservableCollection<Professor> _professors;
        private ObservableCollection<Aula> _aulas;
        private ObservableCollection<Mensagem> _mensagens;

        private string _nomeUtilizadorLogado;
        private string _emailUtilizadorLogado;

        private int _numAlunosTeste;
        // FIM DE VARIÁVEIS DE TESTES (TEMPORÁRIAS)

        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;

            // ZONA DE TESTES (CÓDIGO TEMPORÁRIO)
            _alunos = new ObservableCollection<Aluno>()
            {
                // new Aluno() {Nome = "Nome Apelido 1"},
                // new Aluno() {Nome = "Nome Apelido 2"},
                // new Aluno() {Nome = "Nome Apelido 3"}
            };
            _numAlunosTeste = 3;

            _aulas = new ObservableCollection<Aula>()
            {
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "CD"}},
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "AEDII"}},
                new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "LPII"}}
            };

            _mensagens = new ObservableCollection<Mensagem>()
            {
                new Mensagem()
                {
                    Remetente = "a15310@alunos.ipca.pt", Destinatario = "a15314@alunos.ipca.pt", Conteudo = "MSG 1",
                    DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"), NomeRemetente = "Hélder Carvalho"
                },
                new Mensagem()
                {
                    Remetente = "a15310@alunos.ipca.pt", Destinatario = "a15314@alunos.ipca.pt", Conteudo = "MSG 2",
                    DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"), NomeRemetente = "Hélder Carvalho"
                },
                new Mensagem()
                {
                    Remetente = "a15314@alunos.ipca.pt", Destinatario = "a15310@alunos.ipca.pt", Conteudo = "MSG 3",
                    DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"), NomeRemetente = "João Carvalho"
                },
            };
            // FIM DE ZONA DE TESTES (CÓDIGO TEMPORÁRIO)
        }

        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                string nomeTemp = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                _emailUtilizadorLogado = authResult.Account.Username;
                TextBlockUtilizadorLogado.Text = "";
                TextBlockUtilizadorLogado.Text += nomeTemp + " (" + _emailUtilizadorLogado + ")";
                EntrarPanel.Visibility = Visibility.Collapsed;
                ChatPanel.Visibility = Visibility.Visible;

                Thread userListAutoRefresh;
                if (TextBlockUtilizadorLogado.Text.Contains("alunos"))
                {
                    Aluno a1 = new Aluno() {Nome = nomeTemp, Email = _emailUtilizadorLogado};
                    ServiceLocator.Current.GetInstance<MainViewModel>().ConnectAction(a1);
                    userListAutoRefresh = new Thread ( ( ) =>
                    {
                        a1 = ServiceLocator.Current.GetInstance<ServerConnectService>().getOnlineUsers(a1);
                        
                        // Fix Erro, collectionObservable só pode ser modificado dentro da sua thread 
                        Application.Current.Dispatcher.Invoke(delegate {
                            _alunos.Add(a1);
                        });
                    } );

                }
                else
                {
                    Professor p1 = new Professor() {Nome = nomeTemp, Email = _emailUtilizadorLogado};
                    ServiceLocator.Current.GetInstance<MainViewModel>().ConnectAction(p1);
                    userListAutoRefresh = new Thread ( ( ) =>
                    {
                        p1 = ServiceLocator.Current.GetInstance<ServerConnectService>().getOnlineUsers(p1);
                        _professors.Add(p1);
                    } );

                }
                userListAutoRefresh.Start ( );

                // PREENCHER INTERFACE
                UsersItemsControl.ItemsSource = _alunos;
                AulasItemsControl.ItemsSource = _aulas;
                foreach (Mensagem mensagem in _mensagens)
                {
                    TextBlock MensagemTextBlock = new TextBlock();
                    MensagemTextBlock.FontSize = 15;
                    Thickness thickness = MensagemTextBlock.Margin;
                    thickness.Top = 10;
                    MensagemTextBlock.Margin = thickness;
                    if (_emailUtilizadorLogado == mensagem.Remetente)
                    {
                        MensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                            {FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline});
                    }
                    else
                    {
                        MensagemTextBlock.Inlines.Add(new Run(mensagem.NomeRemetente + ":")
                            {FontWeight = FontWeights.Bold});
                    }

                    MensagemTextBlock.Inlines.Add(" " + mensagem.Conteudo);
                    LobyChat.Children.Add(MensagemTextBlock);

                    TextBlock DataHoraEnvioTextBlock = new TextBlock();
                    DataHoraEnvioTextBlock.FontSize = 9;
                    DataHoraEnvioTextBlock.Text = mensagem.DataHoraEnvio;
                    LobyChat.Children.Add(DataHoraEnvioTextBlock);
                }

                LobyScrollViewer.ScrollToBottom();
                // FIM PREENCHER INTERFACE
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>First and Last Name of the received User</returns>
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
                return _nomeUtilizadorLogado = contentJObject["givenName"] + " " + (string) contentJObject["surname"];
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Procedimento de TESTE para ver se o funcionamento de adição de Mensagens dinamicamente funciona
        /// USAR QUANDO RECEBER CONEXÃO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnviarMensagem_OnClick(object sender, RoutedEventArgs e)
        {
            TextBlock MensagemTextBlock = new TextBlock();
            MensagemTextBlock.FontSize = 15;
            Thickness thickness = MensagemTextBlock.Margin;
            thickness.Top = 10;
            MensagemTextBlock.Margin = thickness;
            MensagemTextBlock.Inlines.Add(new Run(_nomeUtilizadorLogado + ":")
                {FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline});
            MensagemTextBlock.Inlines.Add(" " + TextBoxMensagem.Text);
            LobyChat.Children.Add(MensagemTextBlock);

            TextBlock DataHoraEnvioTextBlock = new TextBlock();
            DataHoraEnvioTextBlock.FontSize = 9;
            DataHoraEnvioTextBlock.Text = DateTime.Now.ToString("dd/MM/yy HH:mm");
            LobyChat.Children.Add(DataHoraEnvioTextBlock);
            LobyScrollViewer.ScrollToBottom();
            TextBoxMensagem.Text = "";
        }

        /// <summary>
        /// Procedimento de TESTE para ver se o funcionamento de adição de Utilizadores dinamicamente funciona
        /// USAR QUANDO RECEBER CONEXÃO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdicionarUtilizadorTeste_OnClick(object sender, RoutedEventArgs e)
        {
            _alunos.Add(new Aluno() {Nome = "Nome Apelido " + ++_numAlunosTeste});
            UsersItemsControl.ItemsSource = _alunos;
        }
    }
}