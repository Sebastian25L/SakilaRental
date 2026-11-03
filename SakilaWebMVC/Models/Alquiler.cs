namespace SakilaWebMVC.Models
{
    public class Alquiler
    {
        public int RentalId { get; set; }
        public int InventoryId { get; set; }
        public int FilmId { get; set; }
        public string Title { get; set; } = "";
        public int CustomerId { get; set; }
        public string Cliente { get; set; } = "";
        public DateTime RentalDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public bool Activo => ReturnDate == null;
        public string Estado => Activo ? "Activo" : "Devuelto";
    }
}
