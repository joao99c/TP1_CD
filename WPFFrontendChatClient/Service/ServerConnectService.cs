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

        public delegate void AddAlunoAction(Utilizador utilizador);

        public event AddAlunoAction AddAlunoEvent;

        public delegate void AddMensagemRecebidaActionScs(Mensagem mensagem);

        public event AddMensagemRecebidaActionScs AddMensagemRecebidaEventScs;

        public ServerConnectService()
        {
        }

        /// <summary>
        /// Inicia a conexão do Utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador que vai iniciar conexão</param>
        public void Start(Utilizador utilizador)
        {
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
            // _ipEndPoint = new IPEndPoint(Dns.GetHostEntry(IpAddress).AddressList[0], Port);

            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ipEndPoint);
            Response resLogin = new Response(Response.Operation.Login, utilizador);
            Helpers.SendSerializedMessage(_tcpClient, resLogin);
            // Espera pela mensagem do servidor com os dados do user.
            Boolean flagHaveUser = false;
            while (!flagHaveUser)
            {
                Response resGetUserInfo = Helpers.ReceiveSerializedMessage(_tcpClient);
                UtilizadorLigado = resGetUserInfo.Utilizador;
                flagHaveUser = true;
            }

            MessageHandler();
        }

        private void MessageHandler()
        {
            Thread messageHandlerThread = new Thread(() =>
            {
                while (true)
                {
                    Response response = Helpers.ReceiveSerializedMessage(_tcpClient);
                    switch (response.Operacão)
                    {
                        case Response.Operation.EntrarChat:
                        {
                            break;
                        }
                        case Response.Operation.SendMessage:
                        {
                            Application.Current.Dispatcher?.Invoke(delegate
                            {
                                AddMensagemRecebidaEventScs?.Invoke(response.Mensagem);
                            });
                            break;
                        }
                        case Response.Operation.LeaveChat:
                        {
                            break;
                        }
                        case Response.Operation.Login:
                        {
                            break;
                        }
                        case Response.Operation.GetUserInfo:
                        {
                            break;
                        }
                        case Response.Operation.NewUserOnline:
                        {
                            Application.Current.Dispatcher?.Invoke(delegate { AddAlunoEvent?.Invoke(response.Utilizador); });
                            break;
                        }
                        case Response.Operation.BlockLogin:
                        {
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            });
            messageHandlerThread.Start();
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