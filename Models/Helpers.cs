﻿using System.Net.Sockets;
using System.Text;
 using Models;
 using Newtonsoft.Json;

namespace ClassLibrary
{
    public class Helpers
    {
        public static void sendSerializedMessage(TcpClient tcpClient, object obj)
        {
            tcpClient.Client.Send(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj)));
        }
        
        public static T receiveSerializedMessage<T>(TcpClient tcpClient, int dataSize = 1024)
        {
            byte[] data = new byte[dataSize];
            tcpClient.Client.Receive(data);
            return JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(data));
        }
    }
}