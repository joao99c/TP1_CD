using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Models;

namespace WPFFrontendChatClient.Service
{
    public class ServerConnectService
    {
        private TcpClient _tcpClient;
        private IPEndPoint _ipEndPoint;
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public Utilizador UtilizadorLigado { get; set; }

        public event AddAlunoAction AddAlunoEvent;

        public delegate void AddAlunoAction(Utilizador utilizador);

        public ServerConnectService()
        {
        }

        /// <summary>
        /// Inicia a conexão do Utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador que vai iniciar conexão</param>
        public void Start(Utilizador utilizador)
        {
            // _ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
            _ipEndPoint = new IPEndPoint(Dns.GetHostEntry(IpAddress).AddressList[0], Port);

            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ipEndPoint);
            Response resLogin = new Response(Response.Operation.Login, utilizador);
            Helpers.SendSerializedMessage(_tcpClient, resLogin);
            // Espera pela mensagem do servidor com os dados do user. (Curso, horario etc)
            Boolean flagHaveUser = true;
            while (flagHaveUser)
            {
                Response resGetUserInfo = Helpers.ReceiveSerializedMessage(_tcpClient);
                UtilizadorLigado = resGetUserInfo.User;
                flagHaveUser = false;
            }

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
        /// <returns>Utilizador Online (Aluno ou Professor)</returns>
        private Utilizador getOnlineUsers()
        {
            while (true)
            {
                if (_tcpClient == null) continue;
                Response response = Helpers.ReceiveSerializedMessage(_tcpClient);
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
                    Utilizador novoUser = getOnlineUsers();
                    Application.Current.Dispatcher?.Invoke(delegate { AddAlunoEvent?.Invoke(novoUser); });
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
            Response resp = new Response(Response.Operation.SendMessage, UtilizadorLigado, mensagem);
            Helpers.SendSerializedMessage(_tcpClient, resp);
        }
    }
}