using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public record LoginResult
{
    public string MaNhanVien { get; init; } = "";
    public string TenNhanVien { get; init; } = "";
    public string ChucVu { get; init; } = "";
    public List<string> Quyen { get; init; } = [];

    public bool HasRole(string role) => Quyen.Contains(role);
}

