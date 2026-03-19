using System.Security.Cryptography;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class PasswordHasher
{
    private const int SaltSize = 16; // bytes
    private const int KeySize = 32;  // bytes
    private const int Iterations = 100_000;
    private const string Prefix = "HASH2";

    public static string Hash(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] salt = new byte[SaltSize];
        rng.GetBytes(salt);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{Prefix}:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 4 || parts[0] != Prefix) return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] key = Convert.FromBase64String(parts[3]);

        byte[] incoming = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            key.Length);

        return CryptographicOperations.FixedTimeEquals(incoming, key);
    }
}


