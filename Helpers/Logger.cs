using System;
using System.IO;
using System.Diagnostics;

namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        public static void LogError(string message, Exception ex = null)
        {
            string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {message}";
            if (ex != null) log += $"\nException: {ex.Message}\nStack: {ex.StackTrace}";
            log += "\n-----------------------------------\n";
            File.AppendAllText(LogPath, log);
            Debug.WriteLine(log);
        }
    }
}