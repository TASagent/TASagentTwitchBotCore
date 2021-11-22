using System.Security.Cryptography;

namespace TASagentTwitchBot.Core;

public static class Cryptography
{
    /// <summary>
    /// Thank you stack overflow
    /// https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
    /// </summary>
    public static string HashPassword(string password, byte[]? salt = null)
    {
        byte[] hash;
        byte[] hashBytes;

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (salt is null)
        {
            salt = GenerateSalt();
        }

        using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
        {
            hash = pbkdf2.GetBytes(20);
        }

        hashBytes = new byte[36];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 20);

        return Convert.ToBase64String(hashBytes);
    }

    private static byte[] GenerateSalt() =>
        RandomNumberGenerator.GetBytes(16);

    /// <summary>
    /// Thank you stack overflow
    /// https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
    /// </summary>
    /// <exception cref="FormatException"> Throws <see cref="FormatException"/> if the passwordHash or password is invalid </exception>
    /// <exception cref="Exception"> Throws <see cref="Exception"/> if an exception is encountered in the password validation process </exception>
    public static bool ComparePassword(string password, string passwordHash)
    {
        if (passwordHash is null || passwordHash.Length != 48)
        {
            throw new FormatException("PasswordHash was invalid");
        }

        if (password is null)
        {
            throw new FormatException("Password was null");
        }

        try
        {
            byte[] hashBytes = Convert.FromBase64String(passwordHash);

            if (hashBytes.Length != 36)
            {
                throw new FormatException($"Received invalid passwordhash byte array of length {hashBytes.Length}");
            }

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
        catch (FormatException e)
        {
            throw new FormatException("Validation of password failed with exception", e);
        }
        catch (Exception e)
        {
            throw new Exception("Validation of password failed with exception", e);
        }
    }
}
