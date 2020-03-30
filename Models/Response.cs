using System;
using System.Net.Sockets;
using System.Text;

namespace ClassLibrary
{
    [Serializable]
    public class Response<T>
    {
        public enum Operation
        {
            EntrarChat,
            SairChat,
            EnviarMensagem,

            GetUtilizador
            // ....
        }

        public Operation Op { get; set; }
        public string Msg { get; set; }
        public T User { get; set; }

        public Response(Operation op, T user, string msg = null)
        {
            Op = op;
            Msg = op.ToString();
            User = user;
            switch (op)
            {
                case Operation.EntrarChat:
                {
                    break;
                }
                case Operation.SairChat:
                {
                    break;
                }
                case Operation.EnviarMensagem:
                {
                    Msg = msg;
                    break;
                }
                case Operation.GetUtilizador:
                {
                    Msg = msg;
                    break;
                }
            }
        }

        public static void sendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.UTF8.GetBytes(msg), 0, Encoding.Unicode.GetBytes(msg).Length);
        }
    }
}