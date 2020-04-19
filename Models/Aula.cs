using System;
using System.Windows.Input;

namespace Models
{
    public class Aula
    {
        public UnidadeCurricular UnidadeCurricular { get; set; }
        public DateTime HoraInicial { get; set; }
        public DateTime HoraFinal { get; set; }
        public ICommand AbrirSeparadorChatCommand { get; set; }
    }
}