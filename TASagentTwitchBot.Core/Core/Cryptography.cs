using System;
using System.Security.Cryptography;

namespace TASagentTwitchBot.Core
{

    public static class Cryptography
    {
        /// <summary>
        /// Thank you stack overflow
        /// https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
        /// </summary>
        public static string HashPassword(string password, byte[] salt = null)
        {
            byte[] hash;
            byte[] hashBytes;

            if (salt == null)
            {
                salt = GenerateSalt();
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                hash = pbkdf2.GetBytes(20);
            }

            hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            return salt;
        }

        /// <summary>
        /// Thank you stack overflow
        /// https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
        /// </summary>
        public static bool ComparePassword(string password, string passwordHash)
        {
            byte[] hashBytes = Convert.FromBase64String(passwordHash);
            byte[] salt = new byte[16];
            byte[] hash;

            Array.Copy(hashBytes, 0, salt, 0, 16);
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                hash = pbkdf2.GetBytes(20);
            }

            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }

    }
}
