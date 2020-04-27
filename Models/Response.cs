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

        public Operation Operacao { get; set; }
        public Mensagem Mensagem { get; set; }
        public Utilizador Utilizador { get; set; }

        public Response()
        {
        }
        
        /// <summary>
        /// Construtor de uma Response
        /// </summary>
        /// <param name="operacao">Operação</param>
        /// <param name="utilizador">Utilizador que a cria</param>
        /// <param name="mensagem">Mensagem da Response</param>
        public Response(Operation operacao, Utilizador utilizador, Mensagem mensagem = null)
        {
            Operacao = operacao;
            Mensagem = mensagem;
            Utilizador = utilizador;
            /*switch (operacao)
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
            }*/
        }

        /*public static void SendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.Unicode.GetBytes(msg), 0, Encoding.Unicode.GetBytes(msg).Length);
        }*/
    }
}