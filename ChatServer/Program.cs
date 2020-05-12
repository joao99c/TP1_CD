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

            private List<Curso> Cursos { get; set; }
            private List<UnidadeCurricular> UnidadeCurriculares { get; set; }

            /// <summary>
            /// Construtor do servidor
            /// </summary>
            public Server()
            {
                TcpListener = new TcpListener(IPAddress.Parse("192.168.1.17"), 1000);
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

                Cursos = Helpers.GetDataFromFileToObjectT<Curso>("cursos.txt");
                UnidadeCurriculares = Helpers.GetDataFromFileToObjectT<UnidadeCurricular>("uniCurriculares.txt");


                if (!File.Exists(Helpers.UsersFilePath))
                {
                    using (File.CreateText(Helpers.UsersFilePath))
                    {
                    }
                }

                // List<UnidadeCurricular> ucsLesi1 = new List<UnidadeCurricular>();
                // ucsLesi1.Add(new UnidadeCurricular(1, "LP1"));
                // ucsLesi1.Add(new UnidadeCurricular(2, "AED1"));
                // ucsLesi1.Add(new UnidadeCurricular(3, "AM"));
                // ucsLesi1.Add(new UnidadeCurricular(4, "MDAL"));
                // ucsLesi1.Add(new UnidadeCurricular(5, "AC"));
                // ucsLesi1.Add(new UnidadeCurricular(6, "FF"));
                // ucsLesi1.Add(new UnidadeCurricular(7, "E"));
                // ucsLesi1.Add(new UnidadeCurricular(8, "MN"));
                // ucsLesi1.Add(new UnidadeCurricular(9, "AED2"));
                // ucsLesi1.Add(new UnidadeCurricular(10, "LP2"));
                //
                // Curso lesi1 = new Curso();
                // lesi1.Nome = "LESI 1ºano";
                // lesi1.UnidadesCurriculares = ucsLesi1;
                //
                // List<UnidadeCurricular> ucsLesi2 = new List<UnidadeCurricular>();
                // ucsLesi2.Add(new UnidadeCurricular(11, "RC"));
                // ucsLesi2.Add(new UnidadeCurricular(12, "SAD"));
                // ucsLesi2.Add(new UnidadeCurricular(13, "APS"));
                // ucsLesi2.Add(new UnidadeCurricular(14, "SOSD"));
                // ucsLesi2.Add(new UnidadeCurricular(15, "PL"));
                // ucsLesi2.Add(new UnidadeCurricular(16, "VC"));
                // ucsLesi2.Add(new UnidadeCurricular(17, "CD"));
                // ucsLesi2.Add(new UnidadeCurricular(18, "PS"));
                // ucsLesi2.Add(new UnidadeCurricular(19, "AD"));
                // ucsLesi2.Add(new UnidadeCurricular(20, "HM"));
                //
                // Curso lesi2 = new Curso();
                // lesi2.Nome = "LESI 2ºano";
                // lesi2.UnidadesCurriculares = ucsLesi2;
                //
                //
                // List<UnidadeCurricular> ucsLesi3 = new List<UnidadeCurricular>();
                // ucsLesi3.Add(new UnidadeCurricular(21, "ISI"));
                // ucsLesi3.Add(new UnidadeCurricular(22, "CSI"));
                // ucsLesi3.Add(new UnidadeCurricular(23, "IA"));
                // ucsLesi3.Add(new UnidadeCurricular(24, "ES"));
                // ucsLesi3.Add(new UnidadeCurricular(25, "GPE"));
                // ucsLesi3.Add(new UnidadeCurricular(26, "SETR"));
                // ucsLesi3.Add(new UnidadeCurricular(27, "ECE"));
                // ucsLesi3.Add(new UnidadeCurricular(28, "SAD"));
                // ucsLesi3.Add(new UnidadeCurricular(29, "MTW"));
                // ucsLesi3.Add(new UnidadeCurricular(30, "P/E"));
                //
                // Curso lesi3 = new Curso();
                // lesi3.Nome = "LESI 3ºano";
                // lesi3.UnidadesCurriculares = ucsLesi3;
                //
                // using (StreamWriter streamWriter =
                //     File.CreateText(Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName +
                //                     "\\uniCurriculares.txt"))
                // {
                //     List<UnidadeCurricular> teste = new List<UnidadeCurricular>();
                //     
                //     ucsLesi1.ForEach(curricular =>
                //     {
                //         streamWriter.WriteLine(JsonConvert.SerializeObject(curricular));
                //     });
                //     ucsLesi2.ForEach(curricular =>
                //     {
                //         streamWriter.WriteLine(JsonConvert.SerializeObject(curricular));
                //     });
                //     ucsLesi3.ForEach(curricular =>
                //     {
                //         streamWriter.WriteLine(JsonConvert.SerializeObject(curricular));
                //     });
                // }
                //
                // using (StreamWriter streamWriter = File.CreateText(Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName +
                //                                                   "\\cursos.txt"))
                // {
                //     streamWriter.WriteLine(JsonConvert.SerializeObject(lesi1));
                //     streamWriter.WriteLine(JsonConvert.SerializeObject(lesi2));
                //     streamWriter.WriteLine(JsonConvert.SerializeObject(lesi3));
                // }


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
                connectedCliente.User.Curso = Cursos.Find(curso => curso.Nome == "LESI 1ºano");
                List<UnidadeCurricular> fakeInfoUc = new List<UnidadeCurricular>
                {
                    UnidadeCurriculares.Find(uc => uc.Nome == "CD")
                };
                connectedCliente.User.UnidadesCurriculares = fakeInfoUc;
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

                        Response responseModificada = new Response(Response.Operation.SendMessage,
                            clienteConectado.User, mensagemModificada);

                        string filename = mensagemModificada.IdDestinatario.Contains("uc")
                            ? mensagemModificada.IdDestinatario + ".txt"
                            : int.Parse(mensagemModificada.IdDestinatario) > int.Parse(mensagemModificada.IdRemetente)
                                ? int.Parse(mensagemModificada.IdRemetente) + "_" +
                                  int.Parse(mensagemModificada.IdDestinatario) + ".txt"
                                : int.Parse(mensagemModificada.IdDestinatario) + "_" +
                                  int.Parse(mensagemModificada.IdRemetente) + ".txt";

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
                    // Todos os utilizadores na UC (do Curso) e online
                    ClientesConectados.FindAll(cliente =>
                        cliente.User.IsOnline &&
                        cliente.User.Curso.UnidadesCurriculares.Find(unidadeCurricular =>
                            unidadeCurricular.Id == idUc) != null).ForEach(alunoEmAula =>
                    {
                        if (alunoEmAula.User.Email == utilizador.Email) return;
                        Helpers.SendSerializedMessage(alunoEmAula.TcpClient, resMsgToDestinatario);
                    });

                    // Todos os utilizadores na UC (extra) e online
                    ClientesConectados.FindAll(cliente =>
                        cliente.User.IsOnline &&
                        cliente.User.UnidadesCurriculares.Find(unidadeCurricular =>
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