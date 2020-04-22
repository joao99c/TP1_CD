using System;

namespace Models
{
    public class Mensagem
    {
        public Mensagem(string idRemetente,string idDestinatario, string destinatario, string conteudo, string nomeRemetente)
        {
            IdRemetente = idRemetente;
            IdDestinatario = idDestinatario;
            Destinatario = destinatario;
            Conteudo = conteudo;
            NomeRemetente = nomeRemetente;
            DataHoraEnvio = DateTime.Now.ToString("dd/MM/yy HH:mm");
        }
        public Mensagem(){}
        public string IdRemetente { get; set; }
        public string IdDestinatario { get; set; }
        public string Destinatario { get; set; }
        public string Conteudo { get; set; }
        public string DataHoraEnvio { get; set; }
        public string NomeRemetente { get; set; }
    }
}