namespace Models
{
    public class Curso
    {
        public string Nome { get; set; }
        public UnidadeCurricular[] UnidadesCurriculares { get; set; }
    }
}