using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class ReportesController : Controller
    {
        private readonly string _connectionString;

        public ReportesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        // Top 10 de peliculas mas alquiladas (cuenta todo el historial)
        public IActionResult PeliculasMasAlquiladas()
        {
            List<PeliculaTop> lista = new();

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            string sql = @"SELECT TOP 10 f.film_id, f.title, COUNT(r.rental_id) AS veces
                           FROM film f
                           INNER JOIN inventory i ON i.film_id = f.film_id
                           INNER JOIN rental r ON r.inventory_id = i.inventory_id
                           GROUP BY f.film_id, f.title
                           ORDER BY veces DESC, f.title";

            using SqlCommand comando = new(sql, conexion);
            using SqlDataReader reader = comando.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new PeliculaTop
                {
                    FilmId = Convert.ToInt32(reader["film_id"]),
                    Title = Convert.ToString(reader["title"]) ?? "",
                    VecesAlquilada = Convert.ToInt32(reader["veces"])
                });
            }

            return View(lista);
        }
    }
}
