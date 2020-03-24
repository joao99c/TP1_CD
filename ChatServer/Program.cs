using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Classes;
using ClassLibrary;

namespace TestingSocketsAndShit
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Server s = new Server(1000);
            s.Start();
        }

        public class Server
        {
            private int _port;
            private TcpListener _tcpListener;
            private bool _running;
            private TcpClient _connectedTcpClient;
            private readonly BinaryFormatter _bFormatter;
            private Thread _connectionThread;
            private Thread _messageThread;
            private NetworkStream _netStream;
            private Utilizador[] _registedUtilizadors;


            public Server(int port)
            {
                _port = port;
                _tcpListener = new TcpListener(IPAddress.Loopback, port);
            }

            public void Start()
            {
                _tcpListener.Start();
                Console.WriteLine("Waiting for a connection... ");
                _running = true;
                _connectionThread = new Thread(ListenForClientConnections);
                _connectionThread.Start();
            }

            public void Stop()
            {
                if (_running)
                {
                    _tcpListener.Stop();
                    _running = false;
                }
            }

            private void ListenForClientConnections()
            {
                while (_running)
                {
                    _connectedTcpClient = _tcpListener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Ask username and email
                    Response<Aluno> _Response = Helpers.receiveSerializedMessage<Response<Aluno>>(_connectedTcpClient);
                    
                    // Check if user or email using the email
                    if (!_Response.user.email.Contains("alunos"))
                    {
                        Console.WriteLine("Professor");
                        // Return da class do utilizador ou de um novo utilizador
                        Professor prof = LogOrSignInUtilizador<Professor>(_Response.user.nome);
                        Helpers.sendSerializedMessage(_connectedTcpClient, prof);
                        _messageThread = new Thread(MessageHandler<Professor>);
                    }
                    else
                    {
                        Console.WriteLine("Aluno");
                        // Return da class do utilizador ou de um novo utilizador
                        Aluno aluno = LogOrSignInUtilizador<Aluno>(_Response.user.nome);
                        Helpers.sendSerializedMessage(_connectedTcpClient, aluno);
                        _messageThread = new Thread(MessageHandler<Aluno>);
                    }

                    _messageThread.Start();
                }
            }

            private void MessageHandler<T>() where T : Utilizador, new()
            {
                while (true)
                {
                    // Get Response
                    Response<T> _Response = Helpers.receiveSerializedMessage<Response<T>>(_connectedTcpClient);

                    switch (_Response.op)
                    {
                        case Response<T>.Operation.EntrarChat:
                            Console.WriteLine("EntrarChat");
                            break;
                        case Response<T>.Operation.EnviarMensagem: break;
                        case Response<T>.Operation.SairChat: break;
                    }

                    _Response = null;
                }
            }


            private T LogOrSignInUtilizador<T>(string nome) where T : Utilizador, new()
            {
                if (_registedUtilizadors != null)
                {
                    foreach (T user in _registedUtilizadors)
                    {
                        if (user.nome == nome)
                        {
                            return user;
                        }
                    }
                }

                return new T {nome = nome};
            }
        }
    }
}