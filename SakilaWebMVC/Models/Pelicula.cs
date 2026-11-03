using System.ComponentModel.DataAnnotations;

namespace SakilaWebMVC.Models
{
    public class Pelicula
    {
        public int FilmId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Máximo 255 caracteres")]
        [Display(Name = "Título")]
        public string Title { get; set; } = "";

        [StringLength(2000, ErrorMessage = "Máximo 2000 caracteres")]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100")]
        [Display(Name = "Año de estreno")]
        public int? ReleaseYear { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un idioma")]
        [Display(Name = "Idioma")]
        public int LanguageId { get; set; }

        public string? LanguageName { get; set; }

        [Range(1, 30, ErrorMessage = "La duración del alquiler va de 1 a 30 días")]
        [Display(Name = "Días de alquiler")]
        public int RentalDuration { get; set; } = 3;

        [Range(0.01, 99.99, ErrorMessage = "La tarifa debe estar entre 0.01 y 99.99")]
        [Display(Name = "Tarifa de alquiler")]
        public decimal RentalRate { get; set; } = 2.99m;

        [Range(1, 1000, ErrorMessage = "La duración debe estar entre 1 y 1000 minutos")]
        [Display(Name = "Duración (min)")]
        public int? Length { get; set; }

        [Range(0.01, 999.99, ErrorMessage = "El costo de reposición debe estar entre 0.01 y 999.99")]
        [Display(Name = "Costo de reposición")]
        public decimal ReplacementCost { get; set; } = 19.99m;

        [Required(ErrorMessage = "Seleccione la clasificación")]
        [RegularExpression("^(G|PG|PG-13|R|NC-17)$", ErrorMessage = "Clasificación no válida")]
        [Display(Name = "Clasificación")]
        public string Rating { get; set; } = "G";

        public bool Activo { get; set; } = true;

        // Datos calculados desde el inventario (no se editan en el formulario)
        public int Copias { get; set; }
        public int Disponibles { get; set; }
    }
}
