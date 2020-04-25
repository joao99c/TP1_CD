using System.Collections.Generic;

namespace Models
{
    public class Curso
    {
        public string Nome { get; set; }
        public List<UnidadeCurricular> UnidadesCurriculares { get; set; }

        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Curso()
        {
        }
        
        /// <summary>
        /// Construtor de um Curso
        /// </summary>
        /// <param name="nome">Nome do Curso</param>
        /// <param name="unidadesCurriculares">Unidades Curriculares (Array)</param>
        public Curso(string nome, List<UnidadeCurricular> unidadesCurriculares)
        {
            Nome = nome;
            UnidadesCurriculares = unidadesCurriculares;
        }
    }
}