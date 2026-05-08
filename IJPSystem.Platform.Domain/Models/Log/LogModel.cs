using System;

namespace IJPSystem.Platform.Domain.Models.Log
{
    public class LogModel
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DisplayTime => Time.ToString("HH:mm:ss");
    }
}