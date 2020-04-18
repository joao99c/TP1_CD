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
            Server server = new Server(1000);
            server.Start();
        }

        private class Server
        {
            private int Port { get; set; }
            private TcpListener TcpListener { get; set; }
            private bool Running { get; set; }
            private TcpClient ConnectedTcpClient { get; set; }
            private Thread ConnectionThread { get; set; }
            private Thread MessageThread { get; set; }
            private List<Cliente> ClientesConectados { get; set; }

            public Server(int port)
            {
                Port = port;
                TcpListener = new TcpListener(IPAddress.Parse("192.168.1.4"), port);
            }

            public void Start()
            {
                ClientesConectados = new List<Cliente>();
                TcpListener.Start();
                Console.WriteLine("Waiting for a connection... ");
                Running = true;
                ConnectionThread = new Thread(ListenForClientConnections);
                ConnectionThread.Start();
            }

            public void Stop()
            {
                if (!Running) return;
                TcpListener.Stop();
                Running = false;
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
                    Response<Aluno> response = Helpers.receiveSerializedMessage<Response<Aluno>>(ConnectedTcpClient);
                    if (response.Op != Response<Aluno>.Operation.Login) continue;
                    // Check if user or email using the email
                    if (response.User.Email.Contains("alunos"))
                    {
                        Aluno alunoRecebido = new Aluno(response.User);
                        Console.WriteLine("Aluno");
                        MessageThread = new Thread(MessageHandler<Aluno>);
                        // New user online
                        addNewUserOnline(alunoRecebido);
                    }
                    else
                    {
                        Professor profRecebido = new Professor(response.User);
                        Console.WriteLine("Professor");
                        MessageThread = new Thread(MessageHandler<Professor>);
                        // New user online
                        addNewUserOnline(profRecebido);
                    }

                    MessageThread.Start();
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

                // Se for um cliente já conectado
                ClientesConectados.ForEach(clienteConectado =>
                {
                    if (clienteConectado.user.Email != utilizadorConectar.Email) return;
                    // Encontrei!
                    Console.WriteLine("O " + clienteConectado.user.Nome + " já estava online!");
                    utilizadorEncontrado = clienteConectado.user;
                });

                // Se for um utilizador novo
                if (utilizadorEncontrado != null) return;
                /*
                 * TODO: Verificar se os utilizadores estão realmente a ser enviados em duas ocasiões:
                 * TODO: - Quando um novo utilizador entra todos os outros têm de o receber (acho que sim)
                 * TODO: - Quando um novo utilizador entra esse novo tem de receber todos os que já estavam online
                 */

                ClientesConectados.ForEach(clienteConectado =>
                {
                    Response<T> response = new Response<T>(Response<T>.Operation.NewUserOnline, utilizadorConectar);
                    Helpers.sendSerializedMessage(clienteConectado.TcpClient, response);
                    Console.WriteLine("Novo Utilizador Online enviado!");
                });
                ClientesConectados.Add(new Cliente(utilizadorConectar, ConnectedTcpClient));
                Console.WriteLine("O " + utilizadorConectar.Nome + " está agora online!");
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
                        case Response<T>.Operation.Login:
                        {
                            break;
                        }
                        case Response<T>.Operation.GetUser:
                        {
                            break;
                        }
                        case Response<T>.Operation.NewUserOnline:
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}