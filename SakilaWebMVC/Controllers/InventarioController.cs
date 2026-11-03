using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Filters;
using SakilaWebMVC.Helpers;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    [RolRequerido(Roles.Administrador)]
    public class InventarioController : Controller
    {
        private readonly string _connectionString;

        public InventarioController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public IActionResult Index(int filmId)
        {
            InventarioViewModel modelo = new();

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            // Datos de la pelicula y resumen de copias
            string sqlPelicula = @"SELECT f.film_id, f.title, f.activo,
                                          (SELECT COUNT(*) FROM vw_inventario_estado v WHERE v.film_id = f.film_id) AS copias,
                                          (SELECT COUNT(*) FROM vw_inventario_estado v WHERE v.film_id = f.film_id AND v.disponible = 1) AS disponibles
                                   FROM film f
                                   WHERE f.film_id = @film";

            using (SqlCommand comandoPelicula = new(sqlPelicula, conexion))
            {
                comandoPelicula.Parameters.AddWithValue("@film", filmId);
                using SqlDataReader reader = comandoPelicula.ExecuteReader();
                if (!reader.Read()) return NotFound();

                modelo.Pelicula = new Pelicula
                {
                    FilmId = Convert.ToInt32(reader["film_id"]),
                    Title = Convert.ToString(reader["title"]) ?? "",
                    Activo = Convert.ToBoolean(reader["activo"]),
                    Copias = Convert.ToInt32(reader["copias"]),
                    Disponibles = Convert.ToInt32(reader["disponibles"])
                };
            }

            // Copias fisicas y quien las tiene alquiladas
            string sqlCopias = @"SELECT v.inventory_id, v.film_id, v.store_id, v.disponible,
                                        c.first_name + ' ' + c.last_name AS cliente
                                 FROM vw_inventario_estado v
                                 LEFT JOIN rental r ON r.inventory_id = v.inventory_id AND r.return_date IS NULL
                                 LEFT JOIN customer c ON c.customer_id = r.customer_id
                                 WHERE v.film_id = @film
                                 ORDER BY v.inventory_id";

            using (SqlCommand comandoCopias = new(sqlCopias, conexion))
            {
                comandoCopias.Parameters.AddWithValue("@film", filmId);
                using SqlDataReader reader = comandoCopias.ExecuteReader();
                while (reader.Read())
                {
                    modelo.Copias.Add(new Copia
                    {
                        InventoryId = Convert.ToInt32(reader["inventory_id"]),
                        FilmId = Convert.ToInt32(reader["film_id"]),
                        StoreId = Convert.ToInt32(reader["store_id"]),
                        Disponible = Convert.ToInt32(reader["disponible"]) == 1,
                        ClienteActual = reader["cliente"] is DBNull ? null : Convert.ToString(reader["cliente"])
                    });
                }
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Agregar(int filmId)
        {
            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            // Regla: solo peliculas activas reciben copias nuevas
            using (SqlCommand comandoActiva = new("SELECT activo FROM film WHERE film_id = @film", conexion))
            {
                comandoActiva.Parameters.AddWithValue("@film", filmId);
                object? activa = comandoActiva.ExecuteScalar();

                if (activa == null)
                {
                    TempData["Error"] = "La película no existe.";
                    return RedirectToAction(nameof(Index), new { filmId });
                }
                if (!Convert.ToBoolean(activa))
                {
                    TempData["Error"] = "La película está dada de baja.";
                    return RedirectToAction(nameof(Index), new { filmId });
                }
            }

            string sql = @"INSERT INTO inventory (film_id, store_id, last_update)
                           VALUES (@film, (SELECT TOP 1 store_id FROM store ORDER BY store_id), GETDATE())";

            using SqlCommand comando = new(sql, conexion);
            comando.Parameters.AddWithValue("@film", filmId);
            comando.ExecuteNonQuery();

            TempData["Exito"] = "Copia agregada al inventario.";
            return RedirectToAction(nameof(Index), new { filmId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int inventoryId, int filmId)
        {
            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            // Regla: una copia con historial de alquileres no se borra
            using (SqlCommand comandoHistorial = new("SELECT COUNT(*) FROM rental WHERE inventory_id = @inv", conexion))
            {
                comandoHistorial.Parameters.AddWithValue("@inv", inventoryId);
                if (Convert.ToInt32(comandoHistorial.ExecuteScalar()) > 0)
                {
                    TempData["Error"] = "No se puede eliminar: la copia tiene historial de alquileres.";
                    return RedirectToAction(nameof(Index), new { filmId });
                }
            }

            using SqlCommand comando = new("DELETE FROM inventory WHERE inventory_id = @inv", conexion);
            comando.Parameters.AddWithValue("@inv", inventoryId);

            if (comando.ExecuteNonQuery() > 0)
                TempData["Exito"] = "Copia eliminada del inventario.";
            else
                TempData["Error"] = "La copia no existe.";

            return RedirectToAction(nameof(Index), new { filmId });
        }
    }
}
