using MTCS.Service.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MTCS.Service.Helpers
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 8;
        private const int HashSize = 16;
        private const int Iterations = 1000;

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            byte[] combined = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

            return Convert.ToBase64String(combined);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                byte[] combined = Convert.FromBase64String(hashedPassword);

                if (combined.Length != SaltSize + HashSize)
                {
                    return false;
                }

                byte[] salt = new byte[SaltSize];
                byte[] hash = new byte[HashSize];
                Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(combined, SaltSize, hash, 0, HashSize);

                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize);

                return CryptographicOperations.FixedTimeEquals(hash, computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
