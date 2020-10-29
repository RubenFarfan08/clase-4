namespace Netflix.Data
{
    public class Historial{
        public int Id { get; set; }
        
        public int IdPelicula { get; set; }
        public int IdSerie { get; set; }
        public int Minuto { get; set; }
        public int Segundo { get; set; }
        public string User { get; set; }
    }
}