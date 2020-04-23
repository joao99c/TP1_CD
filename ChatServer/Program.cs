using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
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
            public static string projectDir = Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName;
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
                
                // Init
                ClientesConectados = new List<Cliente>();
                Directory.CreateDirectory(projectDir + "\\Chats");
                Directory.CreateDirectory(projectDir + "\\Utilizadores");
                if (!File.Exists(Helpers.UsersFilePath))
                {
                    using(File.CreateText(Helpers.UsersFilePath)){};
                }

                // Start Listener
                TcpListener.Start();
                Console.WriteLine("Waiting for a connection... ");
                Running = true;
                ConnectionThread = new Thread(() => ListenForClientConnections());
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
                    Response response = Helpers.ReceiveSerializedMessage(ConnectedTcpClient);
                    if (response.Op == Response.Operation.Login)
                    {
                        Utilizador user = new Utilizador(response.User.Nome, response.User.Email,
                            Utilizador.UserType.Aluno);
                        // Check if user or email using the email
                        if (!response.User.Email.Contains("alunos"))
                        {
                            user = new Utilizador(response.User.Nome, response.User.Email,
                                Utilizador.UserType.Prof);
                        }

                        // New user online
                        addNewUserOnline(user);
                        MessageThread = new Thread(MessageHandler);

                        MessageThread.Start();
                    }
                }
            }

            /// <summary>
            /// Adiciona o Utilizador à lista de Utilizadores conectados, caso não esteja ligado
            /// </summary>
            /// <param name="utilizadorConectar">Utilizador a adicionar</param>
            /// <typeparam name="T">Tipo de Utilizador</typeparam>
            private void addNewUserOnline(Utilizador utilizadorConectar)
            {
                Utilizador utilizadorEncontrado = Helpers.GetUserConnected(ClientesConectados, utilizadorConectar);

                ////////////////////////////////////////////////////
                // Se for um cliente já conectado
                if (utilizadorEncontrado != null) return;
                Console.WriteLine("Não está online");
                ////////////////////////////////////////////////////

                utilizadorEncontrado = Helpers.GetUserRegisted(utilizadorConectar);
                // Se for um utilizador já registado
                if (utilizadorEncontrado != null) return;
                Console.WriteLine("Não está registado");
                ////////////////////////////////////////////////////


                ////////////////////////////////////////////////////
                // É um utilizador novo:
                //     1 - Enviar para todos os utilizadores o novo utilizador online.
                //         1a-Enviar para todos os utilizadores o novo utilizador online.
                //         1b-Enviar para o novo Utilizador todos os outros utilizadores online;
                //     2 - Guardar a class do utilizador num ficheiro
                //     3 - Adicionar o utilizador á lista de utilizadores online;
                //     4 - Enviar a class do utilizador para o lado do cliente;


                // 1
                Response resParaClienteConectados =
                    new Response(Response.Operation.NewUserOnline, utilizadorConectar);
                ClientesConectados.ForEach(clienteConectado =>
                {
                    //1a
                    Helpers.SendSerializedMessage(clienteConectado.TcpClient, resParaClienteConectados);
                    Console.WriteLine($"Novo Utilizador Online enviado para {clienteConectado.User.Nome}!");

                    //2b
                    Response resParaClienteNovo =
                        new Response(Response.Operation.NewUserOnline, clienteConectado.User);
                    Helpers.SendSerializedMessage(ConnectedTcpClient, resParaClienteNovo);
                    Console.WriteLine($"{clienteConectado.User.Nome} enviado para novo Utilizador Online!");
                });

                //TODO: Get User information (Curso, horario etc)
                // 2 
                Helpers.SaveUserInFile(utilizadorConectar);


                // 3
                ClientesConectados.Add(new Cliente(utilizadorConectar, ConnectedTcpClient));
                Console.WriteLine("O " + utilizadorConectar.Nome + " está agora online!");

                // 4
                Response resUpdateUserInfo = new Response(Response.Operation.GetUserInfo, utilizadorConectar);
                Helpers.SendSerializedMessage(ConnectedTcpClient, resUpdateUserInfo);
            }

            /// <summary>
            /// Tratador de conexão
            /// <para>Recebe mensagens e trata-as de acordo com o seu tipo</para>
            /// </summary>
            /// <typeparam name="T">Tipo de Utilizador</typeparam>
            private void MessageHandler()
            {
                while (true)
                {
                    // Get Response
                    Response response = Helpers.ReceiveSerializedMessage(ConnectedTcpClient);
                    switch (response.Op)
                    {
                        case Response.Operation.EntrarChat:
                        {
                            Console.WriteLine("EntrarChat");


                            break;
                        }

                        case Response.Operation.SendMessage:
                        {
                            SendMessage(response.Msg, response.User);


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
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Trata da Mensagem recebida de um Utilizador (mensagem que o servidor recebe)
        /// <para>Guarda a Mensagem no ficheiro e envia para o destinatário caso este esteja online</para>
        /// TODO: NÃO FUNCIONA!
        /// TODO: É necessário colocar o Id do Utilizador a funcionar antes de modificar esta função
        /// TODO:  - Para isso é necessário guardar os Utilizadores e atribuir-lhes Id's quando entram pela primeira vez
        /// TODO: No fim disto tudo é necessário juntar os Id's e criar o ficheiro, guardar a Mensagem nesse ficheiro e
        /// TODO: enviar ao destinatário caso esteja online
        /// </summary>
        /// <param name="mensagem"></param>
        /// <param name="utilizador"></param>
        private static void SendMessage(Mensagem mensagem, Utilizador utilizador)
        {
            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName + $"\\Chats\\";

            mensagem.IdRemetente = "10";
            // Nome do ficheiro
            // aula10 = aula de cd
            // 15310_15315 = mensagem privada
            string filename = "";

            if (mensagem.IdDestinatario.Contains("aula"))
            {
                filename += mensagem.NomeDestinatario + ".txt";
            }
            else
            {
                int num1 = int.Parse(mensagem.IdDestinatario);
                int num2 = int.Parse(mensagem.IdRemetente);
                if (num1 > num2)
                {
                    filename += num2 + "_" + num1 + ".txt";
                }
                else
                {
                    filename += num1 + "_" + num2 + ".txt";
                }
            }

            Console.WriteLine(projectDirectory + filename);

            // Create a file to write to.
            using (StreamWriter sw = !File.Exists(projectDirectory + filename)
                ? File.CreateText(projectDirectory + filename)
                : File.AppendText(projectDirectory + filename))
            {
                // De: 
                sw.Write($"E:{mensagem.IdRemetente}");
                // Para:
                sw.Write($" R:{mensagem.IdDestinatario}");
                // Mensagem
                sw.Write($" \"{mensagem.Conteudo.Trim()}\" \"{mensagem.DataHoraEnvio}\"\n");
                // Horas
            }
        }


        public static TcpState GetState(TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo != null ? foo.State : TcpState.Unknown;
        }
    }
}

//TODO: Utilizadores em ficheiros
//TODO: Enviar mensagem para o WPF
//TODO: Usar o ID do utilizador nas tabs etc