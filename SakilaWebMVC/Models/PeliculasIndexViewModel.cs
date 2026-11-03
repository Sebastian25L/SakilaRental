namespace SakilaWebMVC.Models
{
    public class PeliculasIndexViewModel
    {
        public List<Pelicula> Peliculas { get; set; } = new();
        public string? Busqueda { get; set; }
        public string? Rating { get; set; }
        public bool VerInactivas { get; set; }
        public int Pagina { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalRegistros { get; set; }
    }
}
