using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Models;

namespace ChatServer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server();
            server.Start();
            while (true)
            {
            }
        }

        private class Server
        {
            private static readonly string ProjectDir =
                Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName;

            private TcpListener TcpListener { get; set; }
            public List<Cliente> ClientesConectados { get; set; }

            public Server()
            {
                TcpListener = new TcpListener(IPAddress.Parse("192.168.1.4"), 1000);
            }

            public void Start()
            {
                // Init
                ClientesConectados = new List<Cliente>();
                Directory.CreateDirectory(ProjectDir + "\\Chats");
                Directory.CreateDirectory(ProjectDir + "\\Utilizadores");
                if (!File.Exists(Helpers.UsersFilePath))
                {
                    using (File.CreateText(Helpers.UsersFilePath))
                    {
                    }
                }

                // Start Listener
                TcpListener.Start();
                var acceptTcpClientThread = new Thread(AcceptTcpClient);
                acceptTcpClientThread.Start();
            }

            private void AcceptTcpClient()
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Waiting for connections...");
                        TcpClient client = TcpListener.AcceptTcpClient();
                        Console.WriteLine("Connected!");
                        ClientesConectados.Add(new Cliente(client));
                        Console.WriteLine(GetState(client));

                        Console.WriteLine($"Utilizadores ligados: {ClientesConectados.Count}");
                        Thread connectionThread = new Thread(ListenForClientConnections);
                        connectionThread.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            /// <summary>
            /// Aceita conexão, verifica que tipo de utilizador é e inicia a Thread de interceção de Mensagens
            /// </summary>
            private void ListenForClientConnections()
            {
                while (true)
                {
                    Parallel.ForEach(ClientesConectados, clienteConectado =>
                    {
                        try
                        {
                            // Login user and Start communications
                            Response response = Helpers.ReceiveSerializedMessage(clienteConectado.TcpClient);
                            Thread.Sleep(1000); //idk
                            if (response.Op != Response.Operation.Login) return;

                            Utilizador user = new Utilizador(response.User.Nome, response.User.Email,
                                Utilizador.UserType.Aluno);
                            // Check if user or email using the email
                            if (!response.User.Email.Contains("alunos"))
                            {
                                user.TipoUtilizador = Utilizador.UserType.Prof;
                            }

                            clienteConectado.User = user;
                            // Antes do Login no Programa
                            clienteConectado.User.IsOnline = false;
                            // New user online
                            clienteConectado = addNewUserOnline(clienteConectado, user);
                            MessageHandler(clienteConectado.TcpClient);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Um utilizador foi desconectado!");
                            ClientesConectados.Remove(clienteConectado);
                            Console.WriteLine($"Utilizadores ligados: {ClientesConectados.Count}");
                        }
                    });
                    Thread.Sleep(1000);
                }
            }

            /// <summary>
            /// Adiciona o Utilizador à lista de Utilizadores conectados, caso não esteja ligado
            /// </summary>
            /// <param name="connectedCliente"></param>
            /// <param name="utilizadorConectar">Utilizador a adicionar</param>
            private Cliente addNewUserOnline(Cliente connectedCliente, Utilizador utilizadorConectar)
            {
                Utilizador utilizadorEncontrado = Helpers.GetUserConnected(ClientesConectados, utilizadorConectar);
                // Se for um cliente já conectado
                if (utilizadorEncontrado != null)
                {
                    // Console.WriteLine("O utilizador já estava online");
                    connectedCliente.User = utilizadorEncontrado;
                    Response responseBlockedLogin = new Response(Response.Operation.BlockLogin, connectedCliente.User);
                    // TODO: Não deixar entrar porque já existe alguém online (IMPLEMENTAR BLOQUEIO NO WPF)
                    // Helpers.SendSerializedMessage(connectedCliente.TcpClient, responseBlockedLogin);
                    return connectedCliente;
                }

                utilizadorEncontrado = Helpers.GetUserRegisted(utilizadorConectar);
                // Se for um utilizador já registado
                if (utilizadorEncontrado != null)
                {
                    // Console.WriteLine("O utilizador já estava registado");
                    connectedCliente.User = utilizadorEncontrado;
                }

                // Regista e guarda o Utilizador
                // TODO: Get User information (Curso, horario etc)
                if (utilizadorEncontrado == null)
                {
                    connectedCliente.User = Helpers.SaveUserInFile(utilizadorConectar);
                }

                connectedCliente.User.IsOnline = true;

                /*
                 * É um utilizador novo:
                 *     1 - Envia a informação completa do Utilizador para ele próprio;
                 *     2 - Enviar para todos os Utilizadores o novo Utilizador online;
                 *     3 - Enviar para o novo Utilizador todos os outros Utilizadores online;
                 */

                // 1
                Response resUpdateUserInfo = new Response(Response.Operation.GetUserInfo, connectedCliente.User);
                Helpers.SendSerializedMessage(connectedCliente.TcpClient, resUpdateUserInfo);

                // 2
                Response euParaClientesConectados =
                    new Response(Response.Operation.NewUserOnline, connectedCliente.User);
                ClientesConectados.ForEach(clienteConectado =>
                {
                    if (clienteConectado.User.Email == connectedCliente.User.Email) return;
                    // 2
                    Helpers.SendSerializedMessage(clienteConectado.TcpClient, euParaClientesConectados);
                    // Console.WriteLine($"Novo Utilizador Online enviado para {clienteConectado.User.Nome}!");

                    //3
                    Response outrosParaEu = new Response(Response.Operation.NewUserOnline, clienteConectado.User);
                    Helpers.SendSerializedMessage(connectedCliente.TcpClient, outrosParaEu);
                    // Console.WriteLine($"{clienteConectado.User.Nome} enviado para novo Utilizador Online!");
                });
                return connectedCliente;
            }

            /// <summary>
            /// Tratador de conexão
            /// <para>Recebe mensagens e trata-as de acordo com o seu tipo</para>
            /// </summary>
            /// <param name="connectedTcpClient">Cliente a escutar</param>
            private void MessageHandler(TcpClient connectedTcpClient)
            {
                // Get Response
                Response response = Helpers.ReceiveSerializedMessage(connectedTcpClient);
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
                    case Response.Operation.BlockLogin:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
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
            private void SendMessage(Mensagem mensagem, Utilizador utilizador)
            {
                // Filename:
                    // Aula: aula10
                    // MP: 15310_15315 
                    // Lobby: idDestinatario = 0 
                
                string filename = null;
                Response resMsgToDestinatario = new Response(Response.Operation.SendMessage, utilizador, mensagem);

                if (mensagem.IdDestinatario.Contains("aula"))
                {
                    // Nome do ficheiro = idDestinatario - ex.: aula1 onde 1 é o id da aula
                    filename += mensagem.IdDestinatario + ".txt";
                    int idAula = int.Parse(mensagem.IdDestinatario.Remove(0, 4));
                    // Todos os utilizadores na aula e online
                    ClientesConectados.FindAll(cliente =>
                        cliente.User.IsOnline &&
                        (cliente.User.Curso.UnidadesCurriculares.Find(uc => uc.Id == idAula) != null)
                    ).ForEach(alunoEmAula =>
                    {
                        if (alunoEmAula.User.Email == utilizador.Email) return;
                        Helpers.SendSerializedMessage(alunoEmAula.TcpClient, resMsgToDestinatario);
                    });
                    Helpers.SaveMessageInFile(mensagem, filename);
                }
                else if (int.Parse(mensagem.IdDestinatario) == 0) // LOBBY
                {
                    ClientesConectados.ForEach(cliente =>
                    {
                        if (cliente.User.IsOnline)
                        {
                            // Não envia para ele proprio
                            if (cliente.User.Email == utilizador.Email) return;
                            Helpers.SendSerializedMessage(cliente.TcpClient, resMsgToDestinatario);
                        }
                    });
                    // NAO GUARDA EM FICHEIRO
                }
                else
                {
                    if (int.Parse(mensagem.IdDestinatario) > int.Parse(mensagem.IdRemetente))
                    {
                        filename += int.Parse(mensagem.IdRemetente) + "_" + int.Parse(mensagem.IdDestinatario) + ".txt";
                    }
                    else
                    {
                        filename += int.Parse(mensagem.IdDestinatario) + "_" + int.Parse(mensagem.IdRemetente) + ".txt";
                    }

                    Cliente destinatario = ClientesConectados.Find(cliente =>
                        cliente.User.IsOnline && cliente.User.Id == int.Parse(mensagem.IdDestinatario));

                    Helpers.SendSerializedMessage(destinatario.TcpClient, resMsgToDestinatario);
                    Helpers.SaveMessageInFile(mensagem, filename);
                }
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