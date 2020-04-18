using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using ClassLibrary;
using CommonServiceLocator;
using Models;
using WPFFrontendChatClient.ViewModel;

namespace WPFFrontendChatClient.Service
{
    public class ServerConnectService
    {
        private MainViewModel MainViewModel { get; set; }
        private TcpClient _tcpClient;
        private IPEndPoint _ipEndPoint;
        public string IpAddress { get; set; }
        public int Port { get; set; }

        public ServerConnectService()
        {
            MainViewModel = ServiceLocator.Current.GetInstance<MainViewModel>();
        }

        /// <summary>
        /// Inicia a conexão do Utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador que vai iniciar conexão</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        public void Start<T>(T utilizador)
        {
            // _ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
            _ipEndPoint = new IPEndPoint(Dns.GetHostEntry(IpAddress).AddressList[0], Port);

            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ipEndPoint);
            Response<T> response = new Response<T>(Response<T>.Operation.Login, utilizador);
            Helpers.sendSerializedMessage(_tcpClient, response);
            UpdaterAlunos();
        }

        private static void Receive(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int received = 0; // how many bytes is already received
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably empty, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex; // any serious error occurr
                }
            } while (received < size);
        }

        /// <summary>
        /// Obtém os Utilizadores Online (1 de cada vez)
        /// </summary>
        /// <typeparam name="T">Tipo de Utilizador que vai receber</typeparam>
        /// <returns>Utilizador Online (Aluno ou Professor)</returns>
        private T getOnlineUsers<T>() where T : Utilizador
        {
            while (true)
            {
                if (_tcpClient == null) continue;
                Response<T> response = Helpers.receiveSerializedMessage<Response<T>>(_tcpClient);
                return response.User;
            }
        }

        /// <summary>
        /// Thread que fica à escuta de novos Alunos
        /// <para>Assim que um Aluno se conectar ao servidor, este envia-o para ser adicionado à lista de Alunos Online</para>
        /// </summary>
        private void UpdaterAlunos()
        {
            Thread userListAutoRefresh = new Thread(() =>
            {
                while (true)
                {
                    Aluno novoAluno = getOnlineUsers<Aluno>();
                    if (novoAluno != null)
                    {
                        Aluno aluno = novoAluno;
                        Application.Current.Dispatcher?.Invoke(delegate { MainViewModel.AddAlunoLista(aluno); });
                    }
                }
            });
            userListAutoRefresh.Start();
        }

        /// <summary>
        /// Envia a Mensagem para o servidor
        /// </summary>
        /// <param name="mensagem">Mensagem a enviar</param>
        public void EnviarMensagem(Mensagem mensagem)
        {
            MessageBox.Show(mensagem.NomeRemetente + " - " + mensagem.Remetente + " - " + mensagem.Conteudo + " - " +
                            mensagem.Destinatario + " - " + mensagem.DataHoraEnvio, "ServerConnectService");
        }
    }
}