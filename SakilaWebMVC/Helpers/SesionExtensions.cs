namespace SakilaWebMVC.Helpers
{
    // Atajos para leer los datos de sesion guardados en el login
    public static class SesionExtensions
    {
        public const string KeyUsuario = "Usuario";
        public const string KeyRol = "Rol";
        public const string KeyUsuarioId = "UsuarioId";
        public const string KeyCustomerId = "CustomerId";

        public static string? Usuario(this ISession s) => s.GetString(KeyUsuario);
        public static string? Rol(this ISession s) => s.GetString(KeyRol);
        public static int? UsuarioId(this ISession s) => s.GetInt32(KeyUsuarioId);
        public static int? CustomerId(this ISession s) => s.GetInt32(KeyCustomerId);

        public static bool EsAdministrador(this ISession s) => s.Rol() == Roles.Administrador;
        public static bool EsCliente(this ISession s) => s.Rol() == Roles.Cliente;
    }
}
