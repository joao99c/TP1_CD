using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Models
{
    public static class Helpers
    {
        public static readonly string UsersFilePath =
            Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName + "\\Utilizadores\\users.txt";

        /// <summary>
        /// Envia mensagem serializada em Json
        /// <para>1º - envia o tamanho da mensagem;</para>
        /// 2º - envia a mensagem.
        /// </summary>
        /// <param name="tcpClient">Conexão TCP para onde enviar</param>
        /// <param name="obj">Dados a enviar</param>
        public static void SendSerializedMessage(TcpClient tcpClient, object obj)
        {
            byte[] enviar = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj));
            tcpClient.Client.Send(BitConverter.GetBytes(enviar.Length));
            tcpClient.Client.Send(enviar);
        }

        /// <summary>
        /// Recebe Mensagem serializada em Json
        /// <para>1º - recebe o tamanho da mensagem que vai receber;</para>
        ///     2º - cria um array de bytes com o tamanho recebido;
        /// <para>3º - recebe a mensagem</para>
        /// </summary>
        /// <param name="tcpClient">Conexão TCP que vai receber os dados</param>
        /// <returns>"Response" com os dados dentro (objeto des-serializado)</returns>
        public static Response ReceiveSerializedMessage(TcpClient tcpClient)
        {
            byte[] tamanho = new byte[4];
            tcpClient.Client.Receive(tamanho);
            byte[] mensagem = new byte[BitConverter.ToInt32(tamanho, 0)];
            tcpClient.Client.Receive(mensagem);
            return JsonConvert.DeserializeObject<Response>(Encoding.Unicode.GetString(mensagem));
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
        /// TODO: Modificar para guardar o Json Object da Mensagem
        /// </summary>
        /// <param name="mensagem">Mensagem a guardar</param>
        /// <param name="filename">Nome do ficheiro onde guardar</param>
        public static void SaveMessageInFile(Mensagem mensagem, string filename)
        {
            string projectDirectory = $"{Directory.GetParent(Environment.CurrentDirectory).Parent?.FullName}\\Chats\\";
            // Cria/Abre ficheiro para escrever
            using (StreamWriter sw = !File.Exists(projectDirectory + filename)
                ? File.CreateText(projectDirectory + filename)
                : File.AppendText(projectDirectory + filename))
            {
                sw.WriteLine(JsonConvert.SerializeObject(mensagem));
            }
        }
    }
}