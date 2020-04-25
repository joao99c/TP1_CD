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

        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Aula()
        {
        }

        /// <summary>
        /// Construtor de uma Aula
        /// </summary>
        /// <param name="unidadeCurricular">Unidade Curricular da Aula</param>
        /// <param name="abrirSeparadorChatCommand">Comando de abertura separador de chat</param>
        public Aula(UnidadeCurricular unidadeCurricular, ICommand abrirSeparadorChatCommand)
        {
            UnidadeCurricular = unidadeCurricular;
            AbrirSeparadorChatCommand = abrirSeparadorChatCommand;
        }
    }
}