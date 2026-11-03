using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Filters;
using SakilaWebMVC.Helpers;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class AlquileresController : Controller
    {
        private readonly string _connectionString;

        public AlquileresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private const string SelectAlquiler = @"
            SELECT r.rental_id, r.inventory_id, i.film_id, f.title, r.customer_id,
                   c.first_name + ' ' + c.last_name AS cliente,
                   r.rental_date, r.return_date
            FROM rental r
            INNER JOIN inventory i ON i.inventory_id = r.inventory_id
            INNER JOIN film f ON f.film_id = i.film_id
            INNER JOIN customer c ON c.customer_id = r.customer_id";

        private static Alquiler LeerAlquiler(SqlDataReader reader)
        {
            return new Alquiler
            {
                RentalId = Convert.ToInt32(reader["rental_id"]),
                InventoryId = Convert.ToInt32(reader["inventory_id"]),
                FilmId = Convert.ToInt32(reader["film_id"]),
                Title = Convert.ToString(reader["title"]) ?? "",
                CustomerId = Convert.ToInt32(reader["customer_id"]),
                Cliente = Convert.ToString(reader["cliente"]) ?? "",
                RentalDate = Convert.ToDateTime(reader["rental_date"]),
                ReturnDate = reader["return_date"] is DBNull ? null : Convert.ToDateTime(reader["return_date"])
            };
        }

        // El cliente que alquila sale de Session, nunca de un formulario
        [RolRequerido(Roles.Cliente)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rentar(int filmId)
        {
            int? customerId = HttpContext.Session.CustomerId();
            if (customerId == null)
            {
                TempData["Error"] = "Tu usuario no tiene un cliente asociado.";
                return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
            }

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();
            using SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // Regla: la pelicula debe existir y estar activa
                using (SqlCommand comandoFilm = new("SELECT activo FROM film WHERE film_id = @film", conexion, transaccion))
                {
                    comandoFilm.Parameters.AddWithValue("@film", filmId);
                    object? activa = comandoFilm.ExecuteScalar();
                    if (activa == null || !Convert.ToBoolean(activa))
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "La película no está disponible en el catálogo.";
                        return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
                    }
                }

                // Regla: no puede tener dos alquileres abiertos de la misma pelicula
                string sqlDuplicado = @"SELECT COUNT(*)
                                        FROM rental r
                                        INNER JOIN inventory i ON i.inventory_id = r.inventory_id
                                        WHERE r.customer_id = @cliente AND i.film_id = @film AND r.return_date IS NULL";

                using (SqlCommand comandoDup = new(sqlDuplicado, conexion, transaccion))
                {
                    comandoDup.Parameters.AddWithValue("@cliente", customerId.Value);
                    comandoDup.Parameters.AddWithValue("@film", filmId);
                    if (Convert.ToInt32(comandoDup.ExecuteScalar()) > 0)
                    {
                        transaccion.Rollback();
                        TempData["Error"] = "Ya tienes esta película alquilada. Devuélvela antes de volver a alquilarla.";
                        return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
                    }
                }

                // Regla: debe existir una copia sin alquiler abierto.
                // UPDLOCK + HOLDLOCK evita que dos clientes tomen la misma copia a la vez.
                string sqlCopia = @"SELECT TOP 1 i.inventory_id
                                    FROM inventory i WITH (UPDLOCK, HOLDLOCK)
                                    WHERE i.film_id = @film
                                      AND NOT EXISTS (SELECT 1 FROM rental r
                                                      WHERE r.inventory_id = i.inventory_id
                                                        AND r.return_date IS NULL)
                                    ORDER BY i.inventory_id";

                object? copia;
                using (SqlCommand comandoCopia = new(sqlCopia, conexion, transaccion))
                {
                    comandoCopia.Parameters.AddWithValue("@film", filmId);
                    copia = comandoCopia.ExecuteScalar();
                }

                if (copia == null)
                {
                    transaccion.Rollback();
                    TempData["Error"] = "No hay copias disponibles de esta película en este momento.";
                    return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
                }

                // Registrar el alquiler (staff = primer empleado registrado)
                string sqlInsert = @"INSERT INTO rental (rental_date, inventory_id, customer_id, staff_id, last_update)
                                     VALUES (GETDATE(), @inv, @cliente,
                                             (SELECT TOP 1 staff_id FROM staff ORDER BY staff_id), GETDATE())";

                using (SqlCommand comandoInsert = new(sqlInsert, conexion, transaccion))
                {
                    comandoInsert.Parameters.AddWithValue("@inv", Convert.ToInt32(copia));
                    comandoInsert.Parameters.AddWithValue("@cliente", customerId.Value);
                    comandoInsert.ExecuteNonQuery();
                }

                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }

            TempData["Exito"] = "Alquiler registrado correctamente.";
            return RedirectToAction(nameof(MisAlquileres));
        }

        // Solo los alquileres del cliente autenticado
        [RolRequerido(Roles.Cliente)]
        public IActionResult MisAlquileres()
        {
            int? customerId = HttpContext.Session.CustomerId();
            if (customerId == null) return RedirectToAction("AccesoDenegado", "Acceso");

            List<Alquiler> alquileres = new();

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            using SqlCommand comando = new(SelectAlquiler + " WHERE r.customer_id = @cliente ORDER BY r.rental_date DESC", conexion);
            comando.Parameters.AddWithValue("@cliente", customerId.Value);

            using SqlDataReader reader = comando.ExecuteReader();
            while (reader.Read())
            {
                alquileres.Add(LeerAlquiler(reader));
            }

            return View(alquileres);
        }

        [RolRequerido(Roles.Administrador)]
        public IActionResult Todos()
        {
            List<Alquiler> alquileres = new();

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            string sql = SelectAlquiler.Replace("SELECT r.rental_id", "SELECT TOP (100) r.rental_id")
                         + " ORDER BY r.rental_date DESC, r.rental_id DESC";

            using SqlCommand comando = new(sql, conexion);
            using SqlDataReader reader = comando.ExecuteReader();
            while (reader.Read())
            {
                alquileres.Add(LeerAlquiler(reader));
            }

            return View(alquileres);
        }

        // Devolver: no borra el alquiler, solo registra return_date (historial intacto)
        [RolRequerido(Roles.Cliente, Roles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Devolver(int rentalId)
        {
            bool esAdmin = HttpContext.Session.EsAdministrador();
            int? customerId = HttpContext.Session.CustomerId();
            IActionResult destino = esAdmin
                ? RedirectToAction(nameof(Todos))
                : RedirectToAction(nameof(MisAlquileres));

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            int duenio;
            bool abierto;

            using (SqlCommand comandoBuscar = new("SELECT customer_id, return_date FROM rental WHERE rental_id = @id", conexion))
            {
                comandoBuscar.Parameters.AddWithValue("@id", rentalId);
                using SqlDataReader reader = comandoBuscar.ExecuteReader();
                if (!reader.Read())
                {
                    TempData["Error"] = "El alquiler no existe.";
                    return destino;
                }

                duenio = Convert.ToInt32(reader["customer_id"]);
                abierto = reader["return_date"] is DBNull;
            }

            // Regla de propiedad: un cliente solo devuelve sus propios alquileres
            if (!esAdmin && duenio != customerId)
            {
                TempData["Error"] = "No puedes devolver un alquiler que no es tuyo.";
                return destino;
            }

            // Regla de estado: solo se devuelve un alquiler activo
            if (!abierto)
            {
                TempData["Error"] = "Este alquiler ya fue devuelto.";
                return destino;
            }

            using SqlCommand comando = new(@"UPDATE rental SET return_date = GETDATE(), last_update = GETDATE()
                                             WHERE rental_id = @id AND return_date IS NULL", conexion);
            comando.Parameters.AddWithValue("@id", rentalId);

            if (comando.ExecuteNonQuery() > 0)
                TempData["Exito"] = "Devolución registrada. El historial se conserva.";
            else
                TempData["Error"] = "No se pudo registrar la devolución.";

            return destino;
        }
    }
}
