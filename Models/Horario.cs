namespace Models
{
    public class Horario
    {
        public Aula[] SegundaFeira { get; set; }
        public Aula[] TercaFeira { get; set; }
        public Aula[] QuartaFeira { get; set; }
        public Aula[] QuintaFeira { get; set; }
        public Aula[] SextaFeira { get; set; }
        public Aula[] Sabado { get; set; }

        /// <summary>
        /// Construtor utilizado pelo Deserialize
        /// </summary>
        public Horario()
        {
        }

        /// <summary>
        /// Construtor de um Horário
        /// </summary>
        /// <param name="segundaFeira">Aulas da Segunda-feira (Array)</param>
        /// <param name="tercaFeira">Aulas da Terça-feira (Array)</param>
        /// <param name="quartaFeira">Aulas da Quarta-feira (Array)</param>
        /// <param name="quintaFeira">Aulas da Quinta-feira (Array)</param>
        /// <param name="sextaFeira">Aulas da Sexta-feira (Array)</param>
        /// <param name="sabado">Aulas da Sábado (Array)</param>
        public Horario(Aula[] segundaFeira, Aula[] tercaFeira, Aula[] quartaFeira, Aula[] quintaFeira,
            Aula[] sextaFeira, Aula[] sabado)
        {
            SegundaFeira = segundaFeira;
            TercaFeira = tercaFeira;
            QuartaFeira = quartaFeira;
            QuintaFeira = quintaFeira;
            SextaFeira = sextaFeira;
            Sabado = sabado;
        }
    }
}