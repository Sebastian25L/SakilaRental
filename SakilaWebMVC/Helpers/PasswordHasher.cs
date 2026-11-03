using System.Security.Cryptography;

namespace SakilaWebMVC.Helpers
{
    // Reto 1: contrasenas con PBKDF2-SHA256 + salt aleatorio.
    // Formato guardado: PBKDF2-SHA256$iteraciones$salt(base64)$hash(base64)
    public static class PasswordHasher
    {
        private const string Prefijo = "PBKDF2-SHA256";
        private const int Iteraciones = 100000;
        private const int TamSalt = 16;
        private const int TamHash = 32;

        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(TamSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iteraciones, HashAlgorithmName.SHA256, TamHash);

            return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verificar(string password, string almacenado)
        {
            string[] partes = almacenado.Split('$');
            if (partes.Length != 4 || partes[0] != Prefijo || !int.TryParse(partes[1], out int iter))
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(partes[2]);
                byte[] esperado = Convert.FromBase64String(partes[3]);
                byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iter, HashAlgorithmName.SHA256, esperado.Length);

                return CryptographicOperations.FixedTimeEquals(calculado, esperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
