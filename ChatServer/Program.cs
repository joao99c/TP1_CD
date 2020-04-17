using System;
using System.Collections.Generic;
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
                connectedClients = new List<Utilizador>();
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

            /// <summary>
            /// Aceita conexão, verifica que tipo de utilizador é e inicia a Thread de interceção de Mensagens
            /// </summary>
            private void ListenForClientConnections()
            {
                while (Running)
                {
                    ConnectedTcpClient = TcpListener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Login user and Start communications
                    Response<Aluno> _Response = Helpers.receiveSerializedMessage<Response<Aluno>>(ConnectedTcpClient);
                    if (_Response.Op == Response<Aluno>.Operation.Login)
                    {
                        // Check if user or email using the email
                        if (_Response.User.Email.Contains("alunos"))
                        {
                            Aluno alunoRecebido = new Aluno(_Response.User);
                            alunoRecebido.TcpClient = ConnectedTcpClient;
                            Console.WriteLine("Aluno");
                            MessageThread = new Thread(MessageHandler<Aluno>);
                            // New user online
                            addNewUserOnline(alunoRecebido);
                        }
                        else
                        {
                            Professor profRecebido = new Professor(_Response.User);
                            profRecebido.TcpClient = ConnectedTcpClient;
                            Console.WriteLine("Professor");
                            MessageThread = new Thread(MessageHandler<Professor>);
                            // New user online
                            addNewUserOnline(profRecebido);
                        }

                        MessageThread.Start();
                    }
                }
            }

            /// <summary>
            /// Adiciona o Utilizador à lista de Utilizadores conectados, caso não esteja ligado
            /// </summary>
            /// <param name="utilizadorConectar">Utilizador a adicionar</param>
            /// <typeparam name="T">Tipo de Utilizador</typeparam>
            private void addNewUserOnline<T>(T utilizadorConectar) where T : Utilizador
            {
                Utilizador utilizadorEncontrado = null;
                connectedClients.ForEach(utilizadorConectado =>
                {
                    if (utilizadorConectado.Email == utilizadorConectar.Email)
                    {
                        // Encontrei!
                        Console.WriteLine("O " + utilizadorConectado.Nome + " já estava online!");
                        utilizadorEncontrado = utilizadorConectado;
                    }
                });

                if (utilizadorEncontrado == null)
                {
                    connectedClients.ForEach(utilizadorConectado =>
                    {
                        Response<T> response = new Response<T>(Response<T>.Operation.NewUserOnline, utilizadorConectar);
                        Helpers.sendSerializedMessage(utilizadorConectado.TcpClient, response);
                    });
                    connectedClients.Add(utilizadorConectar);
                    Console.WriteLine("O " + utilizadorConectar.Nome + " está agora online!");
                }
            }

            /// <summary>
            /// Tratador de conexão
            /// <para>Recebe mensagens e trata-as de acordo com o seu tipo</para>
            /// </summary>
            /// <typeparam name="T">Tipo de Utilizador</typeparam>
            private void MessageHandler<T>() where T : Utilizador, new()
            {
                while (true)
                {
                    // Get Response
                    Response<T> response = Helpers.receiveSerializedMessage<Response<T>>(ConnectedTcpClient);
                    switch (response.Op)
                    {
                        case Response<T>.Operation.EntrarChat:
                        {
                            Console.WriteLine("EntrarChat");
                            break;
                        }

                        case Response<T>.Operation.SendMessage:
                        {
                            break;
                        }
                        case Response<T>.Operation.LeaveChat:
                        {
                            break;
                        }
                    }

                    response = null;
                }
            }
        }
    }
}