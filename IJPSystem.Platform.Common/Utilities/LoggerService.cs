using IJPSystem.Platform.Common.Constants;
using System;
using System.IO;

namespace IJPSystem.Platform.Common.Utilities
{
    /// <summary>로그를 물리적 파일(.txt)로 기록하는 서비스</summary>
    public static class LoggerService
    {
        private static readonly string LogDirectory = AppConstants.LogFolder;

        public static void WriteToFile(string level, string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                string fileName = $"{DateTime.Now:yyyy-MM-dd}.txt";
                string path     = Path.Combine(LogDirectory, fileName);
                string logLine  = $"[{DateTime.Now.ToTimeStampMs()}] [{level}] {message}";

                File.AppendAllText(path, logLine + Environment.NewLine);
            }
            catch { /* 파일 기록 실패 시 무시 */ }
        }
    }
}
