using System;

namespace Models
{
    public class Mensagem 
    {
        public string conteudo { get; set; }
        public string remetente{ get; set; }
        public string destinatario{ get; set; }
        public DateTime hora { get; set; }
        
    }
}