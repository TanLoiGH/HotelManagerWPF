namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class MaHelper
{
    /// <summary>Sinh mã tăng dần: "DP" + lastMa "DP007" → "DP008"</summary>
    public static string Next(string prefix, string? lastMa)
    {
        if (string.IsNullOrEmpty(lastMa)) return $"{prefix}001";
        if (int.TryParse(lastMa[prefix.Length..], out int n))
            return $"{prefix}{(n + 1):D3}";
        return $"{prefix}001";
    }
}