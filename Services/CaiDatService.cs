using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public static class CaiDatService
{
    private const string ROOT_KEY = "CaiDat";

    // Khóa an toàn đa luồng (Thread-safety lock)
    private static readonly object _lock = new();

    // In-Memory Cache: Lưu cấu hình vào RAM để tránh đọc file ổ cứng liên tục
    private static CaiDat? _cachedSettings;

    // Các giá trị mặc định chuẩn
    private const string DEFAULT_CHECK_IN = "14:00";
    private const string DEFAULT_CHECK_OUT = "12:00";
    private const int DEFAULT_VAT = 8;

    private static string GetConfigPath() => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    /// <summary>
    /// Lấy cấu hình hệ thống. (Siêu tốc do sử dụng Cache RAM)
    /// </summary>
    public static CaiDat Load()
    {
        // 1. Trả về ngay nếu Cache đã có (99.9% trường hợp sẽ rơi vào đây -> Không tốn I/O Disk)
        if (_cachedSettings != null) return _cachedSettings;

        // 2. Lock để đảm bảo chỉ có 1 thread được phép đọc file nếu Cache đang rỗng
        lock (_lock)
        {
            // Double-check locking pattern
            if (_cachedSettings != null) return _cachedSettings;

            var path = GetConfigPath();
            if (!File.Exists(path))
            {
                _cachedSettings = CreateDefaultSettings();
                return _cachedSettings;
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var root = JsonNode.Parse(json) as JsonObject;
                var sysNode = root?[ROOT_KEY];

                if (sysNode != null)
                {
                    // Tận dụng sức mạnh Deserialize của System.Text.Json thay vì Parse tay từng trường
                    _cachedSettings = sysNode.Deserialize<CaiDat>() ?? CreateDefaultSettings();
                }
                else
                {
                    _cachedSettings = CreateDefaultSettings();
                }
            }
            catch (Exception)
            {
                // Fallback an toàn nếu file JSON bị người dùng sửa sai cú pháp
                _cachedSettings = CreateDefaultSettings();
            }

            return _cachedSettings;
        }
    }

    /// <summary>
    /// Lưu cấu hình hệ thống và cập nhật Cache.
    /// </summary>
    public static void Save(CaiDat settings)
    {
        lock (_lock)
        {
            var path = GetConfigPath();
            JsonObject root;

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    root = (JsonNode.Parse(json) as JsonObject) ?? new JsonObject();
                }
                catch
                {
                    root = new JsonObject(); // Ghi đè nếu file hỏng
                }
            }
            else
            {
                root = new JsonObject();
            }

            // Ghi đè cấu hình mới vào Node "CaiDat", giữ nguyên "ConnectionStrings"
            root[ROOT_KEY] = JsonSerializer.SerializeToNode(settings);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options), Encoding.UTF8);

            // Xóa Cache hoặc cập nhật Cache trực tiếp để các hàm Load tiếp theo lấy dữ liệu mới
            _cachedSettings = settings;
        }
    }

    /// <summary>
    /// Tạo cấu hình mặc định an toàn.
    /// </summary>
    private static CaiDat CreateDefaultSettings()
    {
        return new CaiDat
        {
            HotelName = "Tên Khách Sạn",
            HotelAddress = "",
            HotelPhone = "",
            HotelEmail = "",
            DefaultCheckIn = DEFAULT_CHECK_IN,
            DefaultCheckOut = DEFAULT_CHECK_OUT,
            VatPercent = DEFAULT_VAT
        };
    }
}