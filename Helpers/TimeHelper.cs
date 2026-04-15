using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class TimeHelper
{
    /// <summary>
    /// Luôn luôn lấy thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
    /// Bất kể server đang chạy ở múi giờ nào.
    /// </summary>
    public static DateTime GetVietnamTime()
    {
        DateTime utcNow = DateTime.UtcNow;

        try
        {
            // Mã múi giờ của Việt Nam/Đông Nam Á trên Windows
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback an toàn nếu server chạy Linux/Docker (Mã múi giờ khác Windows)
            TimeZoneInfo linuxVnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, linuxVnTimeZone);
        }
    }
}
