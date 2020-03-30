using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ClassLibrary;
using Models;

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
            public int Port { get; set; }
            public TcpListener TcpListener { get; set; }
            public bool Running { get; private set; }
            public TcpClient ConnectedTcpClient { get; private set; }
            public BinaryFormatter BFormatter { get; set; }
            public Thread ConnectionThread { get; private set; }
            public Thread MessageThread { get; private set; }
            public NetworkStream NetStream { get; set; }
            public Utilizador[] RegistedUtilizadors { get; set; }

            public Server(int port)
            {
                Port = port;
                TcpListener = new TcpListener(IPAddress.Loopback, port);
            }

            public void Start()
            {
                TcpListener.Start();
                Console.WriteLine("Waiting for a connection... ");
                Running = true;
                ConnectionThread = new Thread(ListenForClientConnections);
                ConnectionThread.Start();
            }

            public void Stop()
            {
                if (Running)
                {
                    TcpListener.Stop();
                    Running = false;
                }
            }

            private void ListenForClientConnections()
            {
                while (Running)
                {
                    ConnectedTcpClient = TcpListener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Ask username and email
                    Response<Aluno> _Response = Helpers.receiveSerializedMessage<Response<Aluno>>(ConnectedTcpClient);

                    // Check if user or email using the email
                    if (!_Response.User.Email.Contains("alunos"))
                    {
                        Console.WriteLine("Professor");
                        // Return da class do utilizador ou de um novo utilizador
                        Professor prof = LogOrSignInUtilizador<Professor>(_Response.User.Nome);
                        Helpers.sendSerializedMessage(ConnectedTcpClient, prof);
                        MessageThread = new Thread(MessageHandler<Professor>);
                    }
                    else
                    {
                        Console.WriteLine("Aluno");
                        // Return da class do utilizador ou de um novo utilizador
                        Aluno aluno = LogOrSignInUtilizador<Aluno>(_Response.User.Nome);
                        Helpers.sendSerializedMessage(ConnectedTcpClient, aluno);
                        MessageThread = new Thread(MessageHandler<Aluno>);
                    }

                    MessageThread.Start();
                }
            }

            private void MessageHandler<T>() where T : Utilizador, new()
            {
                while (true)
                {
                    // Get Response
                    Response<T> response = Helpers.receiveSerializedMessage<Response<T>>(ConnectedTcpClient);
                    switch (response.Op)
                    {
                        case Response<T>.Operation.EntrarChat:
                            Console.WriteLine("EntrarChat");
                            break;
                        case Response<T>.Operation.EnviarMensagem: break;
                        case Response<T>.Operation.SairChat: break;
                    }

                    response = null;
                }
            }

            private T LogOrSignInUtilizador<T>(string nome) where T : Utilizador, new()
            {
                if (RegistedUtilizadors != null)
                {
                    foreach (T user in RegistedUtilizadors)
                    {
                        if (user.Nome == nome)
                        {
                            return user;
                        }
                    }
                }

                return new T {Nome = nome};
            }
        }
    }
}