using System.Security.Cryptography;
using System.Text;

namespace VinhKhanhApi.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string plainText)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public static bool Verify(string plainText, string hash)
            => Hash(plainText).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}
