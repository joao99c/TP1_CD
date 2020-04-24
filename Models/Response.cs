using System;
using System.Net.Sockets;
using System.Text;

namespace Models
{
    [Serializable]
    public class Response
    {
        public enum Operation
        {
            Login,
            BlockLogin,
            EntrarChat,
            LeaveChat,
            SendMessage,
            GetUserInfo,

            NewUserOnline
            // ....
        }

        public Operation Op { get; set; }
        public Mensagem Msg { get; set; }
        public Utilizador User { get; set; }

        public Response(Operation op, Utilizador user, Mensagem msg = null)
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
                case Operation.GetUserInfo:
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