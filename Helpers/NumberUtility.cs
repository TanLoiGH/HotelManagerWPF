using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public static class NumberUtility
    {
        // Hàm này được bứng ra từ ViewModel cũ, giờ nó có thể dùng ở mọi nơi!
        public static bool ThuParseSoTien(string? text, out decimal soTien)
        {
            soTien = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var digits = Regex.Replace(text, @"[^\d\-]", "");
            if (digits == "-" || string.IsNullOrWhiteSpace(digits)) return false;
            return decimal.TryParse(digits,
                NumberStyles.Integer | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out soTien);
        }
    }
}