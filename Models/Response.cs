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
            NewUserOnline,
            SendFile
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
        }

        /*public static void SendStringMessage(NetworkStream ns, string msg)
        {
            ns.Write(Encoding.Unicode.GetBytes(msg), 0, Encoding.Unicode.GetBytes(msg).Length);
        }*/
    }
}