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

        public Operation op;
        public string msg;
        public T user;

        public Response(Operation op, T user, string msg = null)
        {
            this.op = op;
            this.msg = op.ToString();

            switch (op)
            {
                case Operation.EntrarChat: break;
                case Operation.SairChat: break;
                case Operation.EnviarMensagem:
                    this.msg = msg;
                    break;
                case Operation.GetUtilizador:
                    this.msg = msg;
                    break;
            }
        }

        public static void sendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.UTF8.GetBytes(msg), 0, Encoding.UTF8.GetBytes(msg).Length);
        }
    }
}