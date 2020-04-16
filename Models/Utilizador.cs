using System.Net.Sockets;

namespace Models
{
    public abstract class Utilizador
    {
        public string Nome { get; set; }
        public string Email { get; set; }

        public TcpClient TcpClient { get; set; }
    }
}