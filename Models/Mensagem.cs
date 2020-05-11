using System;

namespace Models
{
    public class Mensagem
    {
        public string IdRemetente { get; set; }
        public string NomeRemetente { get; set; }
        public string EmailRemetente { get; set; }
        public string IdDestinatario { get; set; }
        public string NomeDestinatario { get; set; }

        // public string EmailDestinatario { get; set; }
        public string Conteudo { get; set; }
        public string DataHoraEnvio { get; set; }
        
        public bool IsFicheiro { get; set; }

        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Mensagem()
        {
        }

        /// <summary>
        /// Construtor de uma Mensagem
        /// </summary>
        /// <param name="idRemetente">Id do Remetente</param>
        /// <param name="nomeRemetente">Nome do Remetente</param>
        /// <param name="emailRemetente">Email do Remetente</param>
        /// <param name="idDestinatario">Id do Destinatário</param>
        /// <param name="nomeDestinatario">Nome do Destinatário</param>
        /// <param name="conteudo">Conteúdo da Mensagem</param>
        /// <param name="isFicheiro">Indica se a mensagem representa um upload de ficheiro</param>
        public Mensagem(string idRemetente, string nomeRemetente, string emailRemetente, string idDestinatario,
            string nomeDestinatario, string conteudo, bool isFicheiro=false)
        {
            IdRemetente = idRemetente;
            NomeRemetente = nomeRemetente;
            EmailRemetente = emailRemetente;
            IdDestinatario = idDestinatario;
            NomeDestinatario = nomeDestinatario;
            Conteudo = conteudo;
            IsFicheiro = isFicheiro;
            DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm");
        }
    }
}