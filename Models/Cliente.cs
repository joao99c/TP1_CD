using System.Net.Sockets;

namespace Models
{
    public class Cliente
    {
        public Utilizador User { get; set; }
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Cliente()
        {
        }

        /// <summary>
        /// Construtor de um Cliente
        /// </summary>
        /// <param name="tcpClient">Conexão TCP</param>
        public Cliente(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }

        /// <summary>
        /// Construtor de um Cliente
        /// </summary>
        /// <param name="user">Utilizador</param>
        /// <param name="tcpClient">Conexão TCP</param>
        public Cliente(Utilizador user, TcpClient tcpClient)
        {
            User = user;
            TcpClient = tcpClient;
        }
    }
}