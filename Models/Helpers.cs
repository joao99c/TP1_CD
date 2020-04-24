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

        public static void SendSerializedMessage(TcpClient tcpClient, object obj)
        {
            tcpClient.Client.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj)));
        }

        public static Response ReceiveSerializedMessage(TcpClient tcpClient, int dataSize = 1024)
        {
            byte[] data = new byte[dataSize];
            tcpClient.Client.Receive(data);
            return JsonConvert.DeserializeObject<Response>(Encoding.Unicode.GetString(data));
        }

        public static Utilizador GetUserConnected(List<Cliente> clientesConectados, Utilizador utilizadorASerProcurado)
        {
            Utilizador utilizadorEncontrado = null;
            clientesConectados.ForEach(clienteConectado =>
            {
                if (clienteConectado.User != null)
                {
                    if (clienteConectado.User.Email != utilizadorASerProcurado.Email) return;
                    if (clienteConectado.User.IsOnline)
                    {
                        // Utilizador já conectado e registado
                        // Console.WriteLine("O " + clienteConectado.User.Nome + " já estava online!");
                        utilizadorEncontrado = clienteConectado.User;
                    }
                }
            });
            return utilizadorEncontrado;
        }

        public static Utilizador SaveUserInFile(Utilizador utilizador)
        {
            // Get ID
            utilizador.Id = File.ReadLines(UsersFilePath).Count() + 1;
            using (StreamWriter sw = !File.Exists(UsersFilePath)
                ? File.CreateText(UsersFilePath)
                : File.AppendText(UsersFilePath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(utilizador));
            }

            return utilizador;
        }

        public static Utilizador GetUserRegisted(Utilizador utilizador)
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
    }
}