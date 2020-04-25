using System;

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
        }

        public Operation Operacão { get; set; }
        public Mensagem Mensagem { get; set; }
        public Utilizador Utilizador { get; set; }

        /// <summary>
        /// Construtor de uma Response
        /// </summary>
        /// <param name="operacão">Operação</param>
        /// <param name="utilizador">Utilizador que a cria</param>
        /// <param name="mensagem">Mensagem da Response</param>
        public Response(Operation operacão, Utilizador utilizador, Mensagem mensagem = null)
        {
            Operacão = operacão;
            Mensagem = mensagem;
            Utilizador = utilizador;
            switch (operacão)
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
                    Mensagem = mensagem;
                    break;
                }
                case Operation.GetUserInfo:
                {
                    Mensagem = mensagem;
                    break;
                }
                case Operation.Login:
                {
                    break;
                }
                case Operation.BlockLogin:
                {
                    break;
                }
                case Operation.NewUserOnline:
                {
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /*public static void SendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.Unicode.GetBytes(msg), 0, Encoding.Unicode.GetBytes(msg).Length);
        }*/
    }
}