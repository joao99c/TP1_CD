namespace Models
{
    public class Aluno : Utilizador
    {
        public Curso Curso { get; set; }
        public UnidadeCurricular[] UnidadesCurricularesExtra { get; set; }
        public Horario Horario { get; set; }
    }
}