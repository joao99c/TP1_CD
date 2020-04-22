using System;

namespace Models
{
    public class Mensagem
    {
        public string IdRemetente { get; set; }
        public string NomeRemetente { get; set; }
        public string IdDestinatario { get; set; }
        public string NomeDestinatario { get; set; }
        public string Conteudo { get; set; }
        public string DataHoraEnvio { get; set; }
        
        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Mensagem(){}
        
        /// <summary>
        /// Construtor de uma Mensagem
        /// </summary>
        /// <param name="idRemetente">Id do Remetente</param>
        /// <param name="nomeRemetente">Nome do Remetente</param>
        /// <param name="idDestinatario">Id do Destinatário</param>
        /// <param name="nomeDestinatario">Nome do Destinatário</param>
        /// <param name="conteudo">Conteúdo da Mensagem</param>
        public Mensagem(string idRemetente, string nomeRemetente ,string idDestinatario, string nomeDestinatario, string conteudo)
        {
            IdRemetente = idRemetente;
            IdDestinatario = idDestinatario;
            NomeDestinatario = nomeDestinatario;
            Conteudo = conteudo;
            NomeRemetente = nomeRemetente;
            DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm");
        }
    }
}