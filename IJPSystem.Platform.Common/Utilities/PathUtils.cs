using IJPSystem.Platform.Common.Constants;
using System;
using System.IO;

namespace IJPSystem.Platform.Common.Utilities
{
    /// <summary>Config 파일 경로 해석 유틸리티</summary>
    public static class PathUtils
    {
        /// <summary>
        /// Config 폴더 내 파일의 절대 경로를 반환합니다.
        /// DEBUG: 프로젝트 루트/Config → 없으면 실행 파일 옆 Config
        /// RELEASE: 실행 파일 옆 Config
        /// </summary>
        public static string GetConfigPath(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

#if DEBUG
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\"));
            string debugPath   = Path.Combine(projectRoot, AppConstants.ConfigFolder, fileName);
            if (File.Exists(debugPath)) return debugPath;
#endif
            return Path.Combine(baseDir, AppConstants.ConfigFolder, fileName);
        }
    }
}
