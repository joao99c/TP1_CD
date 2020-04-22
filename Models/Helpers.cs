using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Models
{
    public static class Helpers
    {
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
    }
}