namespace Models
{
    public class Curso
    {
        public string Nome { get; set; }
        public UnidadeCurricular[] UnidadesCurriculares { get; set; }
        
        /// <summary>
        /// Construtor de um Curso
        /// </summary>
        /// <param name="nome">Nome do Curso</param>
        /// <param name="unidadesCurriculares">Unidades Curriculares (Array)</param>
        public Curso(string nome, UnidadeCurricular[] unidadesCurriculares)
        {
            Nome = nome;
            UnidadesCurriculares = unidadesCurriculares;
        }
    }
}