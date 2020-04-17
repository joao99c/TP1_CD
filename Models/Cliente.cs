using System.Net.Sockets;

namespace Models
{
    public class Cliente
    {
        public Utilizador user { get; set; }
        public TcpClient TcpClient { get; set; }

        public Cliente(Utilizador user, TcpClient tcpClient)
        {
            this.user = user;
            TcpClient = tcpClient;
        }
    }
}