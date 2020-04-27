using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Models
{
    public static class Helpers
    {
        public static readonly string UsersFilePath =
            Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName + "\\Utilizadores\\users.txt";

        /// <summary>
        /// Envia mensagem serializada em Json
        /// </summary>
        /// <param name="tcpClient">Conexão TCP para onde enviar</param>
        /// <param name="obj">Dados a enviar</param>
        public static void SendSerializedMessage(TcpClient tcpClient, Response obj)
        {
            byte[] enviar = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj));
            try
            {
                Enviar(tcpClient.Client, enviar, 0, enviar.Length, 10000);
            }
            catch (Exception e)
            {
                // Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Recebe Mensagem serializada em Json
        /// <para>Assim que tiver conteúdo disponível para receber recebe e transforma em objeto</para>
        /// </summary>
        /// <param name="tcpClient">Conexão TCP que vai receber os dados</param>
        /// <returns>"Response" com os dados dentro (objeto des-serializado)</returns>
        public static Response ReceiveSerializedMessage(TcpClient tcpClient)
        {
            while (true)
            {
                if (tcpClient.Available <= 0) continue;
                byte[] buffer = new byte[tcpClient.Available];
                try
                {
                    Receber(tcpClient.Client, buffer, 0, tcpClient.Available, 10000);
                    string jsonString = Encoding.Unicode.GetString(buffer, 0, buffer.Length);
                    Response response = JsonConvert.DeserializeObject<Response>(jsonString);
                    return response;
                }
                catch (Exception e)
                {
                    // Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Envia bytes pelo TCP Client
        /// </summary>
        /// <param name="socket">Socket do TCP Client</param>
        /// <param name="buffer">Buffer de bytes a enviar</param>
        /// <param name="offset">Posição inicial de envio de bytes</param>
        /// <param name="size">Quantidade de bytes a enviar</param>
        /// <param name="timeout">Tempo máximo para o envio</param>
        /// <exception cref="Exception">Erro</exception>
        private static void Enviar(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount, bytesEnviados = 0;
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                {
                    throw new Exception("Timeout.");
                }

                try
                {
                    bytesEnviados += socket.Send(buffer, offset + bytesEnviados, size - bytesEnviados,
                        SocketFlags.None);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.IOPending ||
                        e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // Buffer cheio, esperar
                        Thread.Sleep(30);
                    }
                    else
                    {
                        // Erro real
                        throw;
                    }
                }
            } while (bytesEnviados < size);
        }

        /// <summary>
        /// Recebe bytes do TCP Client
        /// </summary>
        /// <param name="socket">Socket do TCP Client</param>
        /// <param name="buffer">Buffer onde guardar os bytes</param>
        /// <param name="offset">Posição inicial de receção de bytes</param>
        /// <param name="size">Quantidade de bytes a receber</param>
        /// <param name="timeout">Tempo máximo para a receção</param>
        /// <exception cref="Exception">Erro</exception>
        private static void Receber(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount, bytesRecebidos = 0;
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                {
                    throw new Exception("Timeout.");
                }

                try
                {
                    bytesRecebidos += socket.Receive(buffer, offset + bytesRecebidos, size - bytesRecebidos,
                        SocketFlags.None);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.IOPending ||
                        e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // Buffer vazio, esperar
                        Thread.Sleep(30);
                    }
                    else
                    {
                        // Erro real
                        throw;
                    }
                }
            } while (bytesRecebidos < size);
        }

        /// <summary>
        /// Verifica se um determinado Utilizador está online
        /// </summary>
        /// <param name="clientesConectados">Lista de Utilizadores conectados</param>
        /// <param name="utilizadorVerificar">Utilizador a verificar</param>
        /// <returns>
        ///     null -> Utilizador não está online;
        ///     Utilizador -> Utilizador está online.
        /// </returns>
        public static Utilizador GetUserConnected(List<Cliente> clientesConectados, Utilizador utilizadorVerificar)
        {
            Utilizador utilizadorEncontrado = null;
            clientesConectados.ForEach(clienteConectado =>
            {
                if (clienteConectado.User == null) return;
                if (clienteConectado.User.Email != utilizadorVerificar.Email) return;
                if (clienteConectado.User.IsOnline)
                {
                    // Utilizador já conectado e registado
                    // Console.WriteLine("O " + clienteConectado.User.Nome + " já estava online!");
                    utilizadorEncontrado = clienteConectado.User;
                }
            });
            return utilizadorEncontrado;
        }

        /// <summary>
        /// Guarda um Utilizador no ficheiro (registo)
        /// </summary>
        /// <param name="utilizador">Utilizador a guardar</param>
        /// <returns>Utilizador acabado de guardar</returns>
        public static Utilizador SaveUserInFile(Utilizador utilizador)
        {
            // Atribuir Id (+1 do que o último atribuído)
            utilizador.Id = File.ReadLines(UsersFilePath).Count() + 1;
            // Cria/Abre ficheiro para escrever
            using (StreamWriter sw = !File.Exists(UsersFilePath)
                ? File.CreateText(UsersFilePath)
                : File.AppendText(UsersFilePath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(utilizador));
            }

            return utilizador;
        }

        /// <summary>
        /// Verifica se um Utilizador está registado
        /// </summary>
        /// <param name="utilizador">Utilizador a verificar</param>
        /// <returns>
        ///     null -> Utilizador não está registado;
        ///     Utilizador -> Utilizador está registado.
        /// </returns>
        public static Utilizador GetRegisteredUser(Utilizador utilizador)
        {
            using (StreamReader sr = new StreamReader(UsersFilePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains(utilizador.Email))
                    {
                        // Console.WriteLine("Utilizador já registado!");
                        return JsonConvert.DeserializeObject<Utilizador>(line);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Guarda a Mensagem num ficheiro
        /// </summary>
        /// <param name="mensagem">Mensagem a guardar</param>
        /// <param name="filename">Nome do ficheiro onde guardar</param>
        public static void SaveMessageInFile(Mensagem mensagem, string filename)
        {
            string projectDirectory = $"{Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName}\\Chats\\";
            // Cria/Abre ficheiro para escrever
            using (StreamWriter streamWriter = !File.Exists(projectDirectory + filename)
                ? File.CreateText(projectDirectory + filename)
                : File.AppendText(projectDirectory + filename))
            {
                streamWriter.WriteLine(JsonConvert.SerializeObject(mensagem));
            }
        }
    }
}