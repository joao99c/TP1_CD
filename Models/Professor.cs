namespace Models
{
    public class Professor : Utilizador
    {
        public UnidadeCurricular[] UnidadeCurricularesLecionadas { get; set; }
        public Horario Horario { get; set; }
        
        public Professor(){}
        public Professor(Utilizador u)
        {
            Nome = u.Nome;
            Email = u.Email;
            UnidadeCurricularesLecionadas = null;
            Horario = null;
        }
    }
}