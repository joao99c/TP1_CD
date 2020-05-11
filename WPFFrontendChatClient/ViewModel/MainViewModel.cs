using System;
using System.Collections.ObjectModel;
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
        public ICommand AddProfessorTeste { get; set; }

        public delegate void AddSeparadorAction(string displayName, string displayId, string idName);

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

            AddProfessorTeste = new RelayCommand(AddProfessorTesteAction);
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
            ServerConnectService.AddUnidadeCurricularEvent += AddUnidadeCurricularLista;
            ServerConnectService.Start(utilizador);
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
        /// Adiciona Unidades Curriculares à lista de Aulas
        /// </summary>
        /// <param name="unidadeCurricular">Unidade Curricular a adicionar</param>
        private void AddUnidadeCurricularLista(UnidadeCurricular unidadeCurricular)
        {
            Aulas.Add(new Aula(unidadeCurricular, new RelayCommand<Aula>(CriarSeparadorChatAula)));
        }

        /// <summary>
        /// Cria um separador de chat privado com o utilizador escolhido
        /// </summary>
        /// <param name="utilizador">Utilizador para criar separador</param>
        private void CriarSeparadorChatPrivado(Utilizador utilizador)
        {
            AddSeparadorEvent?.Invoke(utilizador.Nome,
                utilizador.Email.Substring(0, utilizador.Email.IndexOf("@", StringComparison.Ordinal)),
                utilizador.Id.ToString().Insert(0, "id"));
        }

        /// <summary>
        /// Cria um separador de chat da Aula/UC escolhida
        /// </summary>
        /// <param name="aula">Aula para criar separador</param>
        private void CriarSeparadorChatAula(Aula aula)
        {
            AddSeparadorEvent?.Invoke(aula.UnidadeCurricular.Nome, null,
                aula.UnidadeCurricular.Id.ToString().Insert(0, "uc"));
        }

        // TEST STUFF --------------------------------------------------------------------------------------------------

        private int _numAux;

        private void AddProfessorTesteAction()
        {
            Professores.Add(new Utilizador(++_numAux, "Professor " + _numAux, "professor" + _numAux + "@ipca.pt",
                Utilizador.UserType.Prof,
                new RelayCommand<Utilizador>(CriarSeparadorChatPrivado)));

            // ServerConnectService.EnviarMensagem(new Mensagem("50", "Professor", "professor@ipca.pt", "uc3", "LP2", "Teste Mensagem LP2"));
        }
    }
}