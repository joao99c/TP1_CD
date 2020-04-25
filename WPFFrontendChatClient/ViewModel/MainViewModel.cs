using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Models;
using WPFFrontendChatClient.Service;

namespace WPFFrontendChatClient.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public Dispatcher MainDispatcher { get; set; }
        public ServerConnectService ServerConnectService { get; set; }
        public ObservableCollection<Utilizador> Alunos { get; set; }
        public ObservableCollection<Utilizador> Professores { get; set; }
        public ObservableCollection<Aula> Aulas { get; set; }
        private ObservableCollection<Mensagem> Mensagens { set; get; }
        public ICommand AddProfessorTeste { get; set; }
        public ICommand AddAulaTeste { get; set; }

        public delegate void AddSeparadorAction(Utilizador utilizador);

        public event AddSeparadorAction AddSeparadorEvent;

        public delegate void AddMensagemRecebidaActionMvm(Mensagem mensagem);

        public event AddMensagemRecebidaActionMvm AddMensagemRecebidaEventMvm;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            Alunos = new ObservableCollection<Utilizador>();
            Aulas = new ObservableCollection<Aula>();
            Professores = new ObservableCollection<Utilizador>();
            Mensagens = new ObservableCollection<Mensagem>();

            AddProfessorTeste = new RelayCommand(AddProfessorTesteAction);
            AddAulaTeste = new RelayCommand(AddAulaTesteAction);
        }

        /// <summary>
        /// Ação de conexão de utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador a conectar</param>
        public void ConnectAction(Utilizador utilizador)
        {
            ServerConnectService = ServiceLocator.Current.GetInstance<ServerConnectService>();
            ServerConnectService.AddAlunoEvent += AddAlunoLista;
            ServerConnectService.AddMensagemRecebidaEventScs += AddMensagemRecebidaChat;

            // ServerConnectService.IpAddress = "tp1cd.ddns.net";
            ServerConnectService.IpAddress = "192.168.1.4";

            ServerConnectService.Port = int.Parse("1000");
            Thread networkServiceThread = new Thread(() => ServerConnectService.Start(utilizador));
            networkServiceThread.Start();
        }

        /// <summary>
        /// Procedimento "intermediário" de ligação entre o "ServerConnectService" e a "MainWindow"
        /// <para>O "ServerConnectService" evoca um evento que chama este procedimento.</para>
        /// <para>Este procedimento evoca outro evento que executa o procedimento de "DisplayMensagemRecebida" na "MainWindow"</para>
        /// </summary>
        /// <param name="mensagem"></param>
        private void AddMensagemRecebidaChat(Mensagem mensagem)
        {
            AddMensagemRecebidaEventMvm?.Invoke(mensagem);
        }

        /// <summary>
        /// Adiciona Alunos à lista de alunos Online
        /// </summary>
        /// <param name="alunoAdicionar">Aluno a adicionar à lista de alunos Online</param>
        private void AddAlunoLista(Utilizador alunoAdicionar)
        {
            alunoAdicionar.AbrirSeparadorChatCommand = new RelayCommand<Utilizador>(CriarSeparadorChatPrivado);
            Alunos.Add(alunoAdicionar);
        }

        /// <summary>
        /// Cria um separador de chat privado com o utilizador escolhido
        /// </summary>
        /// <param name="utilizador">Utilizador para criar separador</param>
        private void CriarSeparadorChatPrivado(Utilizador utilizador)
        {
            AddSeparadorEvent?.Invoke(utilizador);
        }

        /// <summary>
        /// Cria um separador de chat da Aula/UC escolhida
        /// TODO: Colocar a funcionar.
        /// </summary>
        /// <param name="aula">Aula para criar separador</param>
        private void CriarSeparadorChatAula(Aula aula)
        {
            MessageBox.Show("Aula: " + aula.UnidadeCurricular.Nome, "Criar Separador Chat Aula");
        }

        // TEST STUFF

        private int _numAux;

        private void AddProfessorTesteAction()
        {
            Professores.Add(new Utilizador(++_numAux, "Professor " + _numAux, "professor" + _numAux,
                Utilizador.UserType.Prof,
                new RelayCommand<Utilizador>(CriarSeparadorChatPrivado)));
        }

        private void AddAulaTesteAction()
        {
            Aulas.Add(
                new Aula(new UnidadeCurricular("UC " + ++_numAux), new RelayCommand<Aula>(CriarSeparadorChatAula)));
        }
    }
}