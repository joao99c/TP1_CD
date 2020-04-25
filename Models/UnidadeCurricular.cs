﻿namespace Models
{
    public class UnidadeCurricular
    {
        public int Id { get; set; }
        public string Nome { get; set; }

        /// <summary>
        /// Construtor de uma Unidade Curricular
        /// </summary>
        /// <param name="nome">Nome da Unidade Curricular</param>
        public UnidadeCurricular(string nome)
        {
            Nome = nome;
        }

        /// <summary>
        /// Construtor de uma Unidade Curricular
        /// </summary>
        /// <param name="id">Id da Unidade Curricular</param>
        /// <param name="nome">Nome da Unidade Curricular</param>
        public UnidadeCurricular(int id, string nome)
        {
            Id = id;
            Nome = nome;
        }
    }
}