namespace SakilaWebMVC.Models
{
    public class InventarioViewModel
    {
        public Pelicula Pelicula { get; set; } = new();
        public List<Copia> Copias { get; set; } = new();
    }
}
