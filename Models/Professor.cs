namespace Models
{
    public class Professor : Utilizador
    {
        public UnidadeCurricular[] UnidadeCurricularesLecionadas { get; set; }
        public Horario Horario { get; set; }
    }
}