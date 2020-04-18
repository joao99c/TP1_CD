using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        public ObservableCollection<Aluno> Alunos { get; set; }
        public ObservableCollection<Professor> Professores { get; set; }
        public ObservableCollection<Aula> Aulas { get; set; }
        private ObservableCollection<Mensagem> Mensagens { set; get; }
        public ICommand AddAlunoTeste { get; set; }
        public ICommand AddMensagemTeste { get; set; }
        public delegate void AddMensagemAction(Mensagem mensagem);
        public event AddMensagemAction AddMensagemEvent;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            Alunos = new ObservableCollection<Aluno>();
            Aulas = new ObservableCollection<Aula>();
            Professores = new ObservableCollection<Professor>();
            Mensagens = new ObservableCollection<Mensagem>();

            AddMensagemTeste = new RelayCommand(AddMensagemRecebidaAction);
            AddAlunoTeste = new RelayCommand(AddAlunoTesteAction);

            Aulas.Add(new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "CD"}});
            Aulas.Add(new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "AEDII"}});
            Aulas.Add(new Aula() {UnidadeCurricular = new UnidadeCurricular() {Nome = "LPII"}});
        }

        /// <summary>
        /// Ação de conexão de utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador a conectar</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        public void ConnectAction<T>(T utilizador)
        {
            ServerConnectService = ServiceLocator.Current.GetInstance<ServerConnectService>();

            ServerConnectService.IpAddress = "tp1cd.ddns.net";
            // networkService.IpAddress = "192.168.1.4";

            ServerConnectService.Port = int.Parse("1000");
            Thread networkServiceThread = new Thread(() => ServerConnectService.Start(utilizador));
            networkServiceThread.Start();
        }

        /// <summary>
        /// Função que adiciona Alunos à lista de alunos Online
        /// </summary>
        /// <param name="alunoAdicionar">Aluno a adicionar à lista de alunos Online</param>
        public void AddAlunoLista(Aluno alunoAdicionar)
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
                Remetente = "teste@alunos.ipca.pt", Destinatario = "loby", Conteudo = "MSG Teste",
                DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm"), NomeRemetente = "Teste Teste"
            });
            AddMensagemEvent?.Invoke(Mensagens.Last());
        }

        private void AddAlunoTesteAction()
        {
            Alunos.Add(new Aluno() {Nome = "Nome Teste"});
        }
    }
}