using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SakilaWebMVC.Helpers;
using SakilaWebMVC.Models;

namespace SakilaWebMVC.Controllers
{
    public class AccesoController : Controller
    {
        private readonly string _connectionString;

        public AccesoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.Usuario() != null)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            Usuario? usuario = null;

            using (SqlConnection conexion = new(_connectionString))
            {
                conexion.Open();

                string sql = @"SELECT usuario_id, nombre_usuario, password_hash, rol, customer_id
                               FROM Usuarios
                               WHERE nombre_usuario = @usuario AND activo = 1";

                using SqlCommand comando = new(sql, conexion);
                comando.Parameters.AddWithValue("@usuario", modelo.Usuario.Trim());

                using SqlDataReader reader = comando.ExecuteReader();
                if (reader.Read())
                {
                    usuario = new Usuario
                    {
                        UsuarioId = Convert.ToInt32(reader["usuario_id"]),
                        NombreUsuario = Convert.ToString(reader["nombre_usuario"]) ?? "",
                        PasswordHash = Convert.ToString(reader["password_hash"]) ?? "",
                        Rol = Convert.ToString(reader["rol"]) ?? "",
                        CustomerId = reader["customer_id"] is DBNull ? null : Convert.ToInt32(reader["customer_id"])
                    };
                }
            }

            // Mismo mensaje si el usuario no existe o la clave es incorrecta
            if (usuario == null || !PasswordHasher.Verificar(modelo.Password, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(modelo);
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetString(SesionExtensions.KeyUsuario, usuario.NombreUsuario);
            HttpContext.Session.SetString(SesionExtensions.KeyRol, usuario.Rol);
            HttpContext.Session.SetInt32(SesionExtensions.KeyUsuarioId, usuario.UsuarioId);
            if (usuario.CustomerId.HasValue)
                HttpContext.Session.SetInt32(SesionExtensions.KeyCustomerId, usuario.CustomerId.Value);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
