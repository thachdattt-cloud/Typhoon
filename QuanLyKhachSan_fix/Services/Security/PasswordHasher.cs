using System;
using System;
using System.Security.Cryptography;

namespace QuanLyKhachSan_fix.Services.Security
{
    // Dung Rfc2898DeriveBytes (PBKDF2) co san trong .NET, khong can cai them thu vien.
    // Chuoi hash luu vao users.password_hash co dang: "{salt_base64}.{hash_base64}"
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            using var rfc = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256);
            byte[] salt = rfc.Salt;
            byte[] hash = rfc.GetBytes(HashSize);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            using var rfc = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] actualHash = rfc.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}