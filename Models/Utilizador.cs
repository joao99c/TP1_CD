using System.Windows.Input;

namespace Models
{
    public class Utilizador
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public Horario Horario { get; set; }
        public ICommand AbrirSeparadorChatCommand { get; set; }
        public UnidadeCurricular[] UnidadeCurriculares { get; set; }
        public Curso Curso { get; set; }
        private readonly UserType _tipoUtilizador;
        public enum UserType 
        {
            Aluno, Prof
        }
        
        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Utilizador(){}

        /// <summary>
        /// Construtor de um Utilizador sem Tipo
        /// </summary>
        /// <param name="nome">Nome do Utilizador</param>
        /// <param name="email">Email do Utilizador</param>
        public Utilizador(string nome, string email)
        {
            Nome = nome;
            Email = email;
        }
        
        /// <summary>
        /// Construtor de um Utilizador
        /// </summary>
        /// <param name="nome">Nome do Utilizador</param>
        /// <param name="email">Email do Utilizador</param>
        /// <param name="tipoUtilizador">Tipo de Utilizador</param>
        public Utilizador(string nome, string email, UserType tipoUtilizador)
        {
            Nome = nome;
            Email = email;
            _tipoUtilizador = tipoUtilizador;
        }

        /// <summary>
        /// Construtor de um Utilizador com comando de abertura de chat
        /// </summary>
        /// <param name="nome">Nome do Utilizador</param>
        /// <param name="email">Email do Utilizador</param>
        /// <param name="tipoUtilizador">Tipo de Utilizador</param>
        /// <param name="abrirSeparadorChatCommand">Comando de abertura separador de chat</param>
        public Utilizador(string nome, string email, UserType tipoUtilizador, ICommand abrirSeparadorChatCommand)
        {
            Nome = nome;
            Email = email;
            AbrirSeparadorChatCommand = abrirSeparadorChatCommand;
            _tipoUtilizador = tipoUtilizador;
        }
    }
}