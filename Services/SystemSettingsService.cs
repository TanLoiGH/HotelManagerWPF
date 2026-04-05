using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public static class SystemSettingsService
{
    private const string RootKey = "SystemSettings";

    private static string GetConfigPath()
        => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static SystemSettings Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return new SystemSettings();

        var json = File.ReadAllText(path, Encoding.UTF8);
        var node = JsonNode.Parse(json) as JsonObject;
        if (node == null) return new SystemSettings();

        var sys = node[RootKey] as JsonObject;
        if (sys == null) return new SystemSettings();

        return new SystemSettings
        {
            HotelName = sys["HotelName"]?.GetValue<string>() ?? "",
            HotelAddress = sys["HotelAddress"]?.GetValue<string>() ?? "",
            HotelPhone = sys["HotelPhone"]?.GetValue<string>() ?? "",
            HotelEmail = sys["HotelEmail"]?.GetValue<string>() ?? "",
            DefaultCheckIn = sys["DefaultCheckIn"]?.GetValue<string>() ?? "14:00",
            DefaultCheckOut = sys["DefaultCheckOut"]?.GetValue<string>() ?? "12:00",
            VatPercent = sys["VatPercent"]?.GetValue<int?>() ?? 8,
        };
    }

    public static void Save(SystemSettings settings)
    {
        var path = GetConfigPath();

        JsonObject root;
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            root = (JsonNode.Parse(json) as JsonObject) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        root[RootKey] = new JsonObject
        {
            ["HotelName"] = settings.HotelName ?? "",
            ["HotelAddress"] = settings.HotelAddress ?? "",
            ["HotelPhone"] = settings.HotelPhone ?? "",
            ["HotelEmail"] = settings.HotelEmail ?? "",
            ["DefaultCheckIn"] = settings.DefaultCheckIn ?? "14:00",
            ["DefaultCheckOut"] = settings.DefaultCheckOut ?? "12:00",
            ["VatPercent"] = settings.VatPercent,
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, root.ToJsonString(options), Encoding.UTF8);
    }
}

