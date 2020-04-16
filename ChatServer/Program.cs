using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ClassLibrary;
using Models;

namespace ChatServer
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
            public List<Utilizador> connectedClients { get; set; }

            public Server(int port)
            {
                Port = port;
                TcpListener = new TcpListener(IPAddress.Parse("192.168.1.4"), port);
            }

            public void Start()
            {
                this.connectedClients = new List<Utilizador>();
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
                    
                    // Login user and Start communciations
                    Response<Aluno> _Response = Helpers.receiveSerializedMessage<Response<Aluno>>(ConnectedTcpClient);
                    if (_Response.Op == Response<Aluno>.Operation.Login)
                    {
                        // Check if user or email using the email
                        if (!_Response.User.Email.Contains("alunos"))
                        {
                            Aluno alunoRecebido = new Aluno(_Response.User);
                            alunoRecebido.TcpClient = ConnectedTcpClient;
                            
                            Console.WriteLine("Professor");
                            MessageThread = new Thread(MessageHandler<Professor>);
                            // New user online
                            addNewUserOnline(alunoRecebido);
                        }
                        else
                        {
                            Professor profRecebido = new Professor(_Response.User);
                            profRecebido.TcpClient = ConnectedTcpClient;
                            Console.WriteLine("Aluno");
                            MessageThread = new Thread(MessageHandler<Aluno>);
                            // New user online
                            addNewUserOnline(profRecebido);
                        }

                        MessageThread.Start();
                    }
                    
                    
                }
            }

            private void addNewUserOnline<T>(T responseUser) where T : Utilizador
            {
                Utilizador searchedUser = null;
                connectedClients.ForEach(x =>
                {
                    if (x.Email == responseUser.Email)
                    {
                        // Encontrei!
                        Console.WriteLine("O " + x.Nome + " já estava online!");
                        searchedUser = x;
                    }
                });

                if (searchedUser == null)
                {
                    connectedClients.ForEach(x =>
                    {
                        Response<T> res = new Response<T>(Response<T>.Operation.NewUserOnline, responseUser); 
                        Helpers.sendSerializedMessage(x.TcpClient, res);
                    });
                    connectedClients.Add(responseUser);
                    Console.WriteLine("O " + responseUser.Nome + " está agora online!");
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
                        case Response<T>.Operation.SendMessage: break;
                        case Response<T>.Operation.LeaveChat: break;

                    }

                    response = null;
                }
            }
        }
    }
}