using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Models;

namespace ChatServer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server();
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
            private List<Cliente> ClientesConectados { get; set; }

            public Server()
            {
                TcpListener = new TcpListener(IPAddress.Parse("192.168.1.4"), 1000);
            }

            /// <summary>
            /// Inicia o servidor
            /// </summary>
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
                Thread acceptTcpClientThread = new Thread(AcceptTcpClient);
                acceptTcpClientThread.Start();
            }

            /// <summary>
            /// Aceita conexões e inicia a escuta de mensagens
            /// </summary>
            private void AcceptTcpClient()
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("Waiting for connections...");
                        TcpClient client = TcpListener.AcceptTcpClient();
                        Console.WriteLine("Connected!");
                        // Console.WriteLine(GetState(client));

                        Console.WriteLine($"Utilizadores ligados: {ClientesConectados.Count}");
                        Thread connectionThread = new Thread(() => ListenForClientMessages(client));
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
            /// Adiciona o cliente à lista de Clientes conectados e inicia o tratamento de mensagens do mesmo
            /// </summary>
            /// <param name="connectedTcpClient">Cliente a escutar</param>
            private void ListenForClientMessages(TcpClient connectedTcpClient)
            {
                ClientesConectados.Add(new Cliente(connectedTcpClient));
                Cliente clienteConectado = ClientesConectados.Last();
                while (true)
                {
                    try
                    {
                        MessageHandler(clienteConectado);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Um utilizador foi desconectado!");
                        ClientesConectados.Remove(clienteConectado);
                        Console.WriteLine($"Utilizadores ligados: {ClientesConectados.Count}");
                        return;
                    }
                }
            }

            /// <summary>
            /// Adiciona o Utilizador à lista de Utilizadores conectados, caso já não esteja nessa lista.
            /// <para>Se não existir um Utilizador já registado, vai registar esse Utilizador.</para>
            /// Se o Utilizador já estiver registado, será apenas adicionado à lista.
            /// <para>No fim, o novo Utilizador ligado é enviado para os que já estão online e, os que já estão online
            /// são enviados para o novo Utilizador ligado</para>
            /// </summary>
            /// <param name="connectedCliente">Cliente a colocar como Online.</param>
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

                utilizadorEncontrado = Helpers.GetRegisteredUser(utilizadorConectar);
                // Se for um utilizador já registado
                if (utilizadorEncontrado != null)
                {
                    // Console.WriteLine("O utilizador já estava registado");
                    connectedCliente.User = utilizadorEncontrado;
                }

                // Regista e guarda o Utilizador
                // TODO: Get User information (Curso, horário etc)
                if (utilizadorEncontrado == null)
                {
                    connectedCliente.User = Helpers.SaveUserInFile(utilizadorConectar);
                }

                connectedCliente.User.IsOnline = true;

                // FAKE INFO
                connectedCliente.User.Curso = new Curso("ESI", new List<UnidadeCurricular>
                {
                    new UnidadeCurricular(1, "CD"),
                    new UnidadeCurricular(2, "AEDII")
                });
                connectedCliente.User.UnidadesCurriculares = new List<UnidadeCurricular>
                    {new UnidadeCurricular(3, "LP2")};
                // FIM FAKE INFO

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
            /// <param name="connectedClient">Cliente a escutar</param>
            private void MessageHandler(Cliente connectedClient)
            {
                // Obtém a "Response" a tratar
                Response response = Helpers.ReceiveSerializedMessage(connectedClient.TcpClient);
                switch (response.Operacao)
                {
                    case Response.Operation.EntrarChat:
                    {
                        break;
                    }
                    case Response.Operation.SendMessage:
                    {
                        SendMessage(response.Mensagem, response.Utilizador);
                        // Console.WriteLine(response.Mensagem.Conteudo);
                        break;
                    }
                    case Response.Operation.LeaveChat:
                    {
                        break;
                    }
                    case Response.Operation.Login:
                    {
                        Utilizador user = new Utilizador(response.Utilizador.Nome, response.Utilizador.Email,
                            Utilizador.UserType.Aluno);
                        // Verifica se não é um email de aluno
                        if (!response.Utilizador.Email.Contains("alunos"))
                        {
                            user.TipoUtilizador = Utilizador.UserType.Prof;
                        }

                        connectedClient.User = user;
                        // Antes do Login no Programa
                        connectedClient.User.IsOnline = false;
                        // New user online
                        connectedClient = addNewUserOnline(connectedClient, user);
                        Console.WriteLine($"Login efetuado: {connectedClient.User.Nome}");
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
                    {
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            /// <summary>
            /// Trata da Mensagem recebida de um Utilizador (mensagem que o servidor recebe)
            /// <para>Guarda a Mensagem no ficheiro e envia para o destinatário caso este esteja online</para>
            /// TODO: Ver o que se passa com as mensagens de UC's extra (não aparecem na outra pessoa??)
            /// </summary>
            /// <param name="mensagem">Mensagem a ser tratada</param>
            /// <param name="utilizador">Utilizador que a enviou</param>
            private void SendMessage(Mensagem mensagem, Utilizador utilizador)
            {
                // Filename:
                // Aula: uc10
                // MP: 15310_15315 
                // Lobby: idDestinatario = 0 

                string filename = null;
                Response resMsgToDestinatario = new Response(Response.Operation.SendMessage, utilizador, mensagem);

                if (mensagem.IdDestinatario.Contains("uc"))
                {
                    // Nome do ficheiro = idDestinatario = "uc1" - ex.: "uc1.txt" onde 1 é o Id da Unidade Curricular
                    filename += mensagem.IdDestinatario + ".txt";
                    int idUc = int.Parse(mensagem.IdDestinatario.Remove(0, 2));
                    // Todos os utilizadores na UC e online
                    ClientesConectados.FindAll(cliente =>
                        cliente.User.IsOnline &&
                        cliente.User.Curso.UnidadesCurriculares.Find(unidadeCurricular =>
                            unidadeCurricular.Id == idUc) != null).ForEach(alunoEmAula =>
                    {
                        if (alunoEmAula.User.Email == utilizador.Email) return;
                        Helpers.SendSerializedMessage(alunoEmAula.TcpClient, resMsgToDestinatario);
                    });
                    Helpers.SaveMessageInFile(mensagem, filename);
                }
                else if (int.Parse(mensagem.IdDestinatario) == 0)
                {
                    // Console.WriteLine("Mensagem: " + mensagem.Conteudo);
                    // Mensagem para o Lobby
                    ClientesConectados.ForEach(cliente =>
                    {
                        if (!cliente.User.IsOnline) return;
                        // Não envia para ele próprio
                        if (cliente.User.Email == utilizador.Email) return;
                        Helpers.SendSerializedMessage(cliente.TcpClient, resMsgToDestinatario);
                    });
                    // Não guarda em ficheiro
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

        /*private static TcpState GetState(TcpClient tcpClient)
        {
            TcpConnectionInformation foo = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo?.State ?? TcpState.Unknown;
        }*/
    }
}