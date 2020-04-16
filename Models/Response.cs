using System;
using System.Net.Sockets;
using System.Text;
using Models;

namespace ClassLibrary
{
    [Serializable]
    public class Response<T>
    {
        public enum Operation
        {
            Login,
            EntrarChat,
            LeaveChat,
            SendMessage,
            GetUser,
            NewUserOnline
            // ....
        }

        public Operation Op { get; set; }
        public Mensagem Msg { get; set; }
        
        public T User { get; set; }

        public Response(Operation op, T user, Mensagem msg=null)
        {
            Op = op;
            Msg = msg;
            User = user;
            switch (op)
            {
                case Operation.EntrarChat:
                {
                    break;
                }
                case Operation.LeaveChat:
                {
                    break;
                }
                case Operation.SendMessage:
                {
                    Msg = msg;
                    break;
                }
                case Operation.GetUser:
                {
                    Msg = msg;
                    break;
                }
            }
        }

        public static void SendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.UTF8.GetBytes(msg), 0, Encoding.Unicode.GetBytes(msg).Length);
        }
    }
}