using System.Net.Sockets;

namespace Models
{
    public class Cliente
    {
        public Utilizador User { get; set; }
        public TcpClient TcpClient { get; set; }

        public Cliente(Utilizador user, TcpClient tcpClient)
        {
            User = user;
            TcpClient = tcpClient;
        }
    }
}