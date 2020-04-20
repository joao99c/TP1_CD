using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Models;
using WPFFrontendChatClient.Service;
using MessageBox = System.Windows.MessageBox;

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
        public ICommand AddAlunoTeste { get; set; }
        public ICommand AddProfessorTeste { get; set; }
        public ICommand AddMensagemTeste { get; set; }
        public ICommand AddAulaTeste { get; set; }
        public event AddMensagemAction AddMensagemEvent;
        public event AddSeparadorAction AddSeparadorEvent;

        public delegate void AddMensagemAction(Mensagem mensagem);

        public delegate void AddSeparadorAction();

        public Utilizador userOnline { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            Alunos = new ObservableCollection<Utilizador>();
            Aulas = new ObservableCollection<Aula>();
            Professores = new ObservableCollection<Utilizador>();
            Mensagens = new ObservableCollection<Mensagem>();

            AddMensagemTeste = new RelayCommand(AddMensagemRecebidaAction);
            AddAlunoTeste = new RelayCommand(AddAlunoTesteAction);
            AddProfessorTeste = new RelayCommand(AddProfessorTesteAction);
            AddAulaTeste = new RelayCommand(AddAulaTesteAction);

            userOnline = null;
        }

        /// <summary>
        /// Ação de conexão de utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador a conectar</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        public void ConnectAction(Utilizador utilizador)
        {
            ServerConnectService = ServiceLocator.Current.GetInstance<ServerConnectService>();

            // ServerConnectService.IpAddress = "tp1cd.ddns.net";
            ServerConnectService.IpAddress = "192.168.1.4";

            ServerConnectService.Port = int.Parse("1000");
            Thread networkServiceThread = new Thread(() => ServerConnectService.Start(utilizador));
            networkServiceThread.Start();
        }

        /// <summary>
        /// Função que adiciona Alunos à lista de alunos Online
        /// </summary>
        /// <param name="alunoAdicionar">Aluno a adicionar à lista de alunos Online</param>
        public void AddAlunoLista(Utilizador alunoAdicionar)
        {
            Alunos.Add(alunoAdicionar);
        }

        // TODO: Usar na utilização real??
        // TODO: Trocar node da função, apagar ICommand e RelayCommand
        // TODO: Chamar esta função no ServerConnectService quando receber mensagens
        private void AddMensagemRecebidaAction()
        {
            Mensagens.Add(new Mensagem()
            {
                IdRemetente = "teste@alunos.ipca.pt", Destinatario = "loby", Conteudo = "MSG Teste",
                DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"), NomeRemetente = "Teste Teste"
            });
            AddMensagemEvent?.Invoke(Mensagens.Last());
        }

        /// <summary>
        /// Cria um separador de chat privado com o utilizador escolhido
        /// </summary>
        /// <param name="utilizador">Utilizador para criar separador</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        private void CriarSeparadorChatPrivado<T>(T utilizador) where T : Utilizador
        {
            /*MessageBox.Show("Nome: " + utilizador.Nome + "\nEmail: " + utilizador.Email,
                "Criar Separador Chat Privado");*/
            AddSeparadorEvent?.Invoke();
        }

        /// <summary>
        /// Cria um separador de chat da Aula/UC escolhida
        /// </summary>
        /// <param name="aula">Aula para criar separador</param>
        private void CriarSeparadorChatAula(Aula aula)
        {
            MessageBox.Show("Aula: " + aula.UnidadeCurricular.Nome, "Criar Separador Chat Aula");
        }

        private int _numAux = 0;

        private void AddAlunoTesteAction()
        {
            Alunos.Add(new Utilizador()
            {
                Nome = "Aluno " + ++_numAux, Email = "aluno" + _numAux + "@alunos.ipca.pt",
                AbrirSeparadorChatCommand = new RelayCommand<Utilizador>(CriarSeparadorChatPrivado)
            });
        }

        private void AddProfessorTesteAction()
        {
            Professores.Add(new Utilizador()
            {
                Nome = "Professor " + ++_numAux, Email = "professor" + _numAux + "@ipca.pt",
                AbrirSeparadorChatCommand = new RelayCommand<Utilizador>(CriarSeparadorChatPrivado)
            });
        }

        private void AddAulaTesteAction()
        {
            Aulas.Add(new Aula()
            {
                UnidadeCurricular = new UnidadeCurricular() {Nome = "UC " + ++_numAux},
                AbrirSeparadorChatCommand = new RelayCommand<Aula>(CriarSeparadorChatAula)
            });
        }
    }
}