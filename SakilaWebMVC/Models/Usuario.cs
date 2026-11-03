namespace SakilaWebMVC.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Rol { get; set; } = "";
        public int? CustomerId { get; set; }
    }
}
