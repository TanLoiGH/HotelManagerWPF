namespace QuanLyKhachSan_PhamTanLoi.Models;

public class CaiDat
{
    public string HotelName { get; set; } = "";
    public string HotelAddress { get; set; } = "";
    public string HotelPhone { get; set; } = "";
    public string HotelEmail { get; set; } = "";

    // Store as "HH:mm" for simplicity (easy to bind/display).
    public string DefaultCheckIn { get; set; } = "14:00";
    public string DefaultCheckOut { get; set; } = "12:00";

    public int VatPercent { get; set; } = 8;
}