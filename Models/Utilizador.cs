using System.Windows.Input;

namespace Models
{
    public class Utilizador
    {
        public Utilizador(){}
        public Utilizador(string nome, string email, UserType tipoUtilizador)
        {
            Nome = nome;
            Email = email;
            TipoUtilizador = tipoUtilizador;
        }

        public Utilizador(Utilizador u)
        {
            Nome = u.Nome;
            Email = u.Email;
            Horario = u.Horario;
            AbrirSeparadorChatCommand = u.AbrirSeparadorChatCommand;
            UnidadeCurriculares = u.UnidadeCurriculares;
            Curso = u.Curso;
            TipoUtilizador = u.TipoUtilizador;
        }

        public int ID { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public Horario Horario { get; set; }
        public ICommand AbrirSeparadorChatCommand { get; set; }
        public UnidadeCurricular[] UnidadeCurriculares { get; set; }
        public Curso Curso { get; set; }
        private UserType TipoUtilizador;
        public enum UserType 
        {
            aluno,
            prof
        }
    }
}