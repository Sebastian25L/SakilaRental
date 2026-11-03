using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Filters;
using SakilaWebMVC.Helpers;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class PeliculasController : Controller
    {
        private const int TamPagina = 8;
        private static readonly string[] Ratings = { "G", "PG", "PG-13", "R", "NC-17" };

        private readonly string _connectionString;

        public PeliculasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private const string SelectPelicula = @"
            SELECT f.film_id, f.title, f.description, f.release_year, f.language_id,
                   l.name AS idioma, f.rental_duration, f.rental_rate, f.length,
                   f.replacement_cost, f.rating, f.activo,
                   (SELECT COUNT(*) FROM vw_inventario_estado v
                     WHERE v.film_id = f.film_id) AS copias,
                   (SELECT COUNT(*) FROM vw_inventario_estado v
                     WHERE v.film_id = f.film_id AND v.disponible = 1) AS disponibles
            FROM film f
            INNER JOIN language l ON l.language_id = f.language_id";

        private static Pelicula LeerPelicula(SqlDataReader reader)
        {
            return new Pelicula
            {
                FilmId = Convert.ToInt32(reader["film_id"]),
                Title = Convert.ToString(reader["title"]) ?? "",
                Description = reader["description"] is DBNull ? null : Convert.ToString(reader["description"]),
                ReleaseYear = reader["release_year"] is DBNull ? null : Convert.ToInt32(reader["release_year"]),
                LanguageId = Convert.ToInt32(reader["language_id"]),
                LanguageName = (Convert.ToString(reader["idioma"]) ?? "").Trim(),
                RentalDuration = Convert.ToInt32(reader["rental_duration"]),
                RentalRate = Convert.ToDecimal(reader["rental_rate"]),
                Length = reader["length"] is DBNull ? null : Convert.ToInt32(reader["length"]),
                ReplacementCost = Convert.ToDecimal(reader["replacement_cost"]),
                Rating = reader["rating"] is DBNull ? "G" : (Convert.ToString(reader["rating"]) ?? "G").Trim(),
                Activo = Convert.ToBoolean(reader["activo"]),
                Copias = Convert.ToInt32(reader["copias"]),
                Disponibles = Convert.ToInt32(reader["disponibles"])
            };
        }

        // Catalogo (cliente y administrador) con busqueda, filtro y paginacion
        public IActionResult Index(string? busqueda, string? rating, bool verInactivas = false, int pagina = 1)
        {
            // Solo el administrador puede ver las peliculas dadas de baja
            bool incluirInactivas = verInactivas && HttpContext.Session.EsAdministrador();
            if (pagina < 1) pagina = 1;

            string? textoBusqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
            string? filtroRating = string.IsNullOrWhiteSpace(rating) ? null : rating;

            string where = @" WHERE (@busqueda IS NULL OR f.title LIKE '%' + @busqueda + '%')
                              AND (@rating IS NULL OR f.rating = @rating)
                              AND (@todas = 1 OR f.activo = 1)";

            List<Pelicula> peliculas = new();
            int total;

            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            using (SqlCommand comandoTotal = new("SELECT COUNT(*) FROM film f" + where, conexion))
            {
                comandoTotal.Parameters.AddWithValue("@busqueda", (object?)textoBusqueda ?? DBNull.Value);
                comandoTotal.Parameters.AddWithValue("@rating", (object?)filtroRating ?? DBNull.Value);
                comandoTotal.Parameters.AddWithValue("@todas", incluirInactivas ? 1 : 0);
                total = Convert.ToInt32(comandoTotal.ExecuteScalar());
            }

            int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamPagina));
            if (pagina > totalPaginas) pagina = totalPaginas;

            string sql = SelectPelicula + where + @"
                ORDER BY f.title
                OFFSET @offset ROWS FETCH NEXT @tam ROWS ONLY";

            using (SqlCommand comando = new(sql, conexion))
            {
                comando.Parameters.AddWithValue("@busqueda", (object?)textoBusqueda ?? DBNull.Value);
                comando.Parameters.AddWithValue("@rating", (object?)filtroRating ?? DBNull.Value);
                comando.Parameters.AddWithValue("@todas", incluirInactivas ? 1 : 0);
                comando.Parameters.AddWithValue("@offset", (pagina - 1) * TamPagina);
                comando.Parameters.AddWithValue("@tam", TamPagina);

                using SqlDataReader reader = comando.ExecuteReader();
                while (reader.Read())
                {
                    peliculas.Add(LeerPelicula(reader));
                }
            }

            ViewBag.Ratings = Ratings;

            return View(new PeliculasIndexViewModel
            {
                Peliculas = peliculas,
                Busqueda = busqueda,
                Rating = rating,
                VerInactivas = incluirInactivas,
                Pagina = pagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = total
            });
        }

        public IActionResult Detalle(int id)
        {
            Pelicula? pelicula = BuscarPelicula(id);
            if (pelicula == null || (!pelicula.Activo && !HttpContext.Session.EsAdministrador()))
                return NotFound();

            return View(pelicula);
        }

        // ---------- CRUD administrativo ----------

        [RolRequerido(Roles.Administrador)]
        [HttpGet]
        public IActionResult Crear()
        {
            CargarListas();
            return View(new Pelicula());
        }

        [RolRequerido(Roles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(Pelicula pelicula)
        {
            if (!ModelState.IsValid)
            {
                CargarListas();
                return View(pelicula);
            }

            int nuevoId;

            using (SqlConnection conexion = new(_connectionString))
            {
                conexion.Open();

                string sql = @"INSERT INTO film
                    (title, description, release_year, language_id, rental_duration,
                     rental_rate, length, replacement_cost, rating, last_update)
                    OUTPUT INSERTED.film_id
                    VALUES
                    (@title, @description, @year, @idioma, @duracion,
                     @tarifa, @largo, @reposicion, @rating, GETDATE())";

                using SqlCommand comando = new(sql, conexion);
                AgregarParametros(comando, pelicula);
                nuevoId = Convert.ToInt32(comando.ExecuteScalar());
            }

            TempData["Exito"] = "Película creada. Ahora agrega sus copias al inventario.";
            return RedirectToAction("Index", "Inventario", new { filmId = nuevoId });
        }

        [RolRequerido(Roles.Administrador)]
        [HttpGet]
        public IActionResult Editar(int id)
        {
            Pelicula? pelicula = BuscarPelicula(id);
            if (pelicula == null) return NotFound();

            CargarListas();
            return View(pelicula);
        }

        [RolRequerido(Roles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Pelicula pelicula)
        {
            if (!ModelState.IsValid)
            {
                CargarListas();
                return View(pelicula);
            }

            using (SqlConnection conexion = new(_connectionString))
            {
                conexion.Open();

                string sql = @"UPDATE film SET
                    title = @title, description = @description, release_year = @year,
                    language_id = @idioma, rental_duration = @duracion, rental_rate = @tarifa,
                    length = @largo, replacement_cost = @reposicion, rating = @rating,
                    last_update = GETDATE()
                    WHERE film_id = @id";

                using SqlCommand comando = new(sql, conexion);
                AgregarParametros(comando, pelicula);
                comando.Parameters.AddWithValue("@id", pelicula.FilmId);
                comando.ExecuteNonQuery();
            }

            TempData["Exito"] = "Película actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // Reto 2: "eliminar" = baja logica (film.activo = 0), el historial queda intacto
        [RolRequerido(Roles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Desactivar(int id)
        {
            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            // Regla: no se da de baja una pelicula con alquileres sin devolver
            string sqlAbiertos = @"SELECT COUNT(*)
                                   FROM rental r
                                   INNER JOIN inventory i ON i.inventory_id = r.inventory_id
                                   WHERE i.film_id = @id AND r.return_date IS NULL";

            using (SqlCommand comandoAbiertos = new(sqlAbiertos, conexion))
            {
                comandoAbiertos.Parameters.AddWithValue("@id", id);
                if (Convert.ToInt32(comandoAbiertos.ExecuteScalar()) > 0)
                {
                    TempData["Error"] = "No se puede dar de baja: la película tiene alquileres sin devolver.";
                    return RedirectToAction(nameof(Index));
                }
            }

            using SqlCommand comando = new("UPDATE film SET activo = 0, last_update = GETDATE() WHERE film_id = @id", conexion);
            comando.Parameters.AddWithValue("@id", id);

            if (comando.ExecuteNonQuery() > 0)
                TempData["Exito"] = "Película dada de baja correctamente.";
            else
                TempData["Error"] = "La película no existe.";

            return RedirectToAction(nameof(Index));
        }

        [RolRequerido(Roles.Administrador)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restaurar(int id)
        {
            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            using SqlCommand comando = new("UPDATE film SET activo = 1, last_update = GETDATE() WHERE film_id = @id", conexion);
            comando.Parameters.AddWithValue("@id", id);

            if (comando.ExecuteNonQuery() > 0)
                TempData["Exito"] = "Película restaurada al catálogo.";
            else
                TempData["Error"] = "La película no existe.";

            return RedirectToAction(nameof(Index), new { verInactivas = true });
        }

        // ---------- Metodos auxiliares ----------

        private Pelicula? BuscarPelicula(int id)
        {
            using SqlConnection conexion = new(_connectionString);
            conexion.Open();

            using SqlCommand comando = new(SelectPelicula + " WHERE f.film_id = @id", conexion);
            comando.Parameters.AddWithValue("@id", id);

            using SqlDataReader reader = comando.ExecuteReader();
            return reader.Read() ? LeerPelicula(reader) : null;
        }

        private static void AgregarParametros(SqlCommand comando, Pelicula p)
        {
            string? descripcion = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim();

            comando.Parameters.AddWithValue("@title", p.Title.Trim());
            comando.Parameters.AddWithValue("@description", (object?)descripcion ?? DBNull.Value);
            comando.Parameters.AddWithValue("@year", (object?)p.ReleaseYear ?? DBNull.Value);
            comando.Parameters.AddWithValue("@idioma", p.LanguageId);
            comando.Parameters.AddWithValue("@duracion", p.RentalDuration);
            comando.Parameters.AddWithValue("@tarifa", p.RentalRate);
            comando.Parameters.AddWithValue("@largo", (object?)p.Length ?? DBNull.Value);
            comando.Parameters.AddWithValue("@reposicion", p.ReplacementCost);
            comando.Parameters.AddWithValue("@rating", p.Rating);
        }

        private void CargarListas()
        {
            List<object> idiomas = new();

            using (SqlConnection conexion = new(_connectionString))
            {
                conexion.Open();

                using SqlCommand comando = new("SELECT language_id, name FROM language ORDER BY name", conexion);
                using SqlDataReader reader = comando.ExecuteReader();
                while (reader.Read())
                {
                    idiomas.Add(new
                    {
                        Id = Convert.ToInt32(reader["language_id"]),
                        Nombre = (Convert.ToString(reader["name"]) ?? "").Trim()
                    });
                }
            }

            ViewBag.Idiomas = new SelectList(idiomas, "Id", "Nombre");
            ViewBag.Ratings = new SelectList(Ratings);
        }
    }
}
