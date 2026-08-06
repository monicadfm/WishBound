using System.Security.Cryptography;

namespace WishBound.WebAPI.Services
{
    /// <summary>
    /// Criação e verificação de hashes de passwords com PBKDF2 (SHA-256).
    ///
    /// Porque é que não guardamos a password em texto simples?
    /// Se a base de dados fosse comprometida, todas as contas ficariam
    /// expostas. Em vez disso guarda-se um "hash": um valor calculado a
    /// partir da password que não permite recuperar a password original.
    ///
    /// Formato guardado na coluna PasswordHash:
    ///   {iterações}.{sal em Base64}.{hash em Base64}
    /// O "sal" é um valor aleatório único por utilizador que impede que
    /// duas passwords iguais produzam o mesmo hash.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iteracoes = 100_000;
        private const int TamanhoSal = 16;   // bytes
        private const int TamanhoHash = 32;  // bytes

        /// <summary>Gera o hash de uma password (usado no registo e na alteração de password).</summary>
        public static string GerarHash(string password)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(TamanhoSal);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, sal, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);

            return Iteracoes + "." + Convert.ToBase64String(sal) + "." + Convert.ToBase64String(hash);
        }

        /// <summary>Verifica se uma password corresponde ao hash guardado (usado no login).</summary>
        public static bool Verificar(string password, string? hashGuardado)
        {
            if (string.IsNullOrEmpty(hashGuardado))
            {
                return false; // conta sem password local (ex.: conta Google)
            }

            string[] partes = hashGuardado.Split('.');
            if (partes.Length != 3)
            {
                return false; // formato inesperado
            }

            try
            {
                int iteracoes = int.Parse(partes[0]);
                byte[] sal = Convert.FromBase64String(partes[1]);
                byte[] hashEsperado = Convert.FromBase64String(partes[2]);

                byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                    password, sal, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

                // Comparação em tempo constante (evita "timing attacks")
                return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
