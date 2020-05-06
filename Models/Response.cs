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
            LeaveChat,                      // Operação desnecessária???
            SendMessage,                    // TODO: Clarificar as operações, ex.: SendMessageServerCliente
                                            // TODO:                               SendMessageClientServer
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
    }
}