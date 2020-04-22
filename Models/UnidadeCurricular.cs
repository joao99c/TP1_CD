namespace Models
{
    public class UnidadeCurricular
    {
        public string Nome { get; set; }
        
        /// <summary>
        /// Construtor de uma Unidade Curricular
        /// </summary>
        /// <param name="nome">Nome da Unidade Curricular</param>
        public UnidadeCurricular(string nome)
        {
            Nome = nome;
        }
    }
}