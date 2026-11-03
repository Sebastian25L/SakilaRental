using System.ComponentModel.DataAnnotations;

namespace SakilaWebMVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su usuario")]
        [StringLength(50)]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; } = "";

        [Required(ErrorMessage = "Ingrese su contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";
    }
}
