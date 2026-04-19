using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string thaoTac, string chiTiet);
    }
}
