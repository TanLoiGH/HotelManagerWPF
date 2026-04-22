using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class PasswordHasher
{
    private const int SaltSize = 16; // bytes (128 bit)
    private const int KeySize = 32; // bytes (256 bit)
    private const int Iterations = 100_000;

    // Senior Fix: Chuyển thành public để các Service khác (như AuthService) 
    // có thể truy cập mà không phải hardcode chuỗi "HASH2:"
    public const string HashPrefix = "HASH2";

    public static string Hash(string password)
    {
        // Senior Fix: Validation đầu vào
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));

        // Tối ưu hóa: RandomNumberGenerator có sẵn hàm tĩnh GetBytes trong .NET hiện đại
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{HashPrefix}:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        var parts = hash.Split(':');
        if (parts.Length != 4 || parts[0] != HashPrefix) return false;

        if (!int.TryParse(parts[1], out var iterations)) return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] key = Convert.FromBase64String(parts[3]);

            byte[] incoming = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                key.Length);

            // Phòng chống Timing Attack
            return CryptographicOperations.FixedTimeEquals(incoming, key);
        }
        catch (FormatException)
        {
            // Senior Fix: Ngăn ứng dụng bị crash nếu DB bị lỗi lưu chuỗi Base64 không hợp lệ
            return false;
        }
    }

    /// <summary>
    /// Hàm tiện ích giúp kiểm tra nhanh xem chuỗi đã được Hash hay chưa.
    /// Giúp code bên AuthService sạch sẽ hơn rất nhiều.
    /// </summary>
    public static bool IsHashed(string input)
    {
        return !string.IsNullOrEmpty(input) && input.StartsWith($"{HashPrefix}:");
    }
}