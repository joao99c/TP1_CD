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

            /// <summary>
            /// Construtor do servidor
            /// </summary>
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
                Directory.CreateDirectory(ProjectDir + "\\Ficheiros");
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

                        Thread connectionThread = new Thread(() => ListenForClientMessages(client));
                        connectionThread.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ChatServer: Program.AcceptTcpClient");
                        Console.WriteLine("\t" + ex.Message);
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
                Console.WriteLine("Utilizadores ligados: " + ClientesConectados.Count);
                Cliente clienteConectado = ClientesConectados.Last();
                while (true)
                {
                    try
                    {
                        MessageHandler(clienteConectado);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ChatServer: Program.ListenForClientMessages");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Um utilizador foi desconectado!");
                        ClientesConectados.Remove(clienteConectado);
                        Console.WriteLine("Utilizadores ligados: " + ClientesConectados.Count);
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
                if (utilizadorEncontrado != null)
                {
                    // Se for um Utilizador já conectado
                    connectedCliente.User = utilizadorEncontrado;
                    Response responseBlockedLogin = new Response(Response.Operation.BlockLogin, connectedCliente.User);
                    // TODO: Não deixar entrar porque já existe alguém online (IMPLEMENTAR BLOQUEIO NO WPF)
                    // Helpers.SendSerializedMessage(connectedCliente.TcpClient, responseBlockedLogin);
                    return connectedCliente;
                }

                utilizadorEncontrado = Helpers.GetRegisteredUser(utilizadorConectar);
                if (utilizadorEncontrado != null)
                {
                    // Se for um Utilizador já registado
                    connectedCliente.User = utilizadorEncontrado;
                }

                if (utilizadorEncontrado == null)
                {
                    // Se for um Utilizador novo
                    // TODO: Get User information (Curso, horário etc)
                    connectedCliente.User = Helpers.SaveUserInFile(utilizadorConectar);
                }

                connectedCliente.User.IsOnline = true;

                // FAKE INFO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                connectedCliente.User.Curso = new Curso("ESI", new List<UnidadeCurricular>
                {
                    new UnidadeCurricular(1, "CD"),
                    new UnidadeCurricular(2, "AEDII")
                });
                connectedCliente.User.UnidadesCurriculares = new List<UnidadeCurricular>
                    {new UnidadeCurricular(3, "LP2")};
                // FIM FAKE INFO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

                /*
                 * Novo Utilizador ligado:
                 *     1 - Envia a informação completa do Utilizador para ele próprio;
                 *     2 - Enviar para todos os Utilizadores o novo Utilizador online;
                 *     3 - Enviar para o novo Utilizador todos os outros Utilizadores online;
                 */

                // 1
                Response resUpdateUserInfo = new Response(Response.Operation.GetUserInfo, connectedCliente.User);
                Helpers.SendSerializedMessage(connectedCliente.TcpClient, resUpdateUserInfo);

                // 2
                Response novoUtilizadorParaClientesConectados =
                    new Response(Response.Operation.NewUserOnline, connectedCliente.User);
                ClientesConectados.ForEach(clienteConectado =>
                {
                    if (clienteConectado.User.Email == connectedCliente.User.Email) return;
                    // 2
                    Helpers.SendSerializedMessage(clienteConectado.TcpClient, novoUtilizadorParaClientesConectados);

                    //3
                    Response clientesConectadosParaNovoUtilizador =
                        new Response(Response.Operation.NewUserOnline, clienteConectado.User);
                    Helpers.SendSerializedMessage(connectedCliente.TcpClient, clientesConectadosParaNovoUtilizador);
                });
                return connectedCliente;
            }

            /// <summary>
            /// Tratador de conexão
            /// <para>Recebe mensagens e trata-as de acordo com o seu tipo</para>
            /// </summary>
            /// <param name="clienteConectado">Cliente a escutar</param>
            private void MessageHandler(Cliente clienteConectado)
            {
                // TODO: Quando o WPF é fechado a conexão não cai!

                // Obtém a "Response" a tratar
                Response response = Helpers.ReceiveSerializedMessage(clienteConectado.TcpClient);
                switch (response.Operacao)
                {
                    case Response.Operation.EntrarChat:
                    {
                        Helpers.SendChat(clienteConectado, response.Mensagem.Conteudo);
                        break;
                    }
                    case Response.Operation.SendMessage:
                    {
                        SendMessage(response.Mensagem, response.Utilizador);
                        break;
                    }
                    case Response.Operation.SendFile:
                    {
                        Helpers.ReceiveFile(clienteConectado.TcpClient, response.Mensagem,
                            out Mensagem mensagemModificada);

                        Response responseModificada = new Response(Response.Operation.SendMessageFile,
                            clienteConectado.User, mensagemModificada);

                        string filename = mensagemModificada.IdDestinatario + ".txt";
                        Helpers.SaveMessageInFile(mensagemModificada, filename);

                        ClientesConectados.ForEach(cliente =>
                        {
                            if (!cliente.User.IsOnline) return;
                            Helpers.SendSerializedMessage(cliente.TcpClient, responseModificada);
                        });
                        break;
                    }
                    case Response.Operation.PedirFile:
                    {
                        Helpers.SendFile(clienteConectado.TcpClient, null,
                            Helpers.FilesFolder + response.Mensagem.Conteudo, true);
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

                        clienteConectado.User = user;
                        // Antes do Login no Chat
                        clienteConectado.User.IsOnline = false;
                        // Login no Chat
                        clienteConectado = addNewUserOnline(clienteConectado, user);
                        Console.WriteLine("Login efetuado: " + clienteConectado.User.Nome);
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
                //     - Aula: uc10
                //     - MP: id_id (idMenor_idMaior)
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
    }
}