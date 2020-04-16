namespace Models
{
    public class Aluno : Utilizador
    {
        public Curso Curso { get; set; }
        public UnidadeCurricular[] UnidadesCurricularesExtra { get; set; }
        public Horario Horario { get; set; }

        public Aluno(){}
        public Aluno(Utilizador u)
        {
            Nome = u.Nome;
            Email = u.Email;
            Curso = null;
            UnidadesCurricularesExtra = null;
            Horario = null;
        }
    }
}