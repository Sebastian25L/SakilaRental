namespace SakilaWebMVC.Models
{
    public class Copia
    {
        public int InventoryId { get; set; }
        public int FilmId { get; set; }
        public int StoreId { get; set; }
        public bool Disponible { get; set; }
        public string? ClienteActual { get; set; }
    }
}
