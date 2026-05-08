using IJPSystem.Platform.Common.Constants;
using System;

namespace IJPSystem.Platform.Common.Utilities
{
    /// <summary>날짜/시간 포맷 유틸리티</summary>
    public static class DateTimeUtils
    {
        /// <summary>HH:mm:ss — 로그 타임스탬프용</summary>
        public static string ToTimeStamp(this DateTime dt)
            => dt.ToString(AppConstants.FmtTime);

        /// <summary>HH:mm:ss.fff — 상세 로그용</summary>
        public static string ToTimeStampMs(this DateTime dt)
            => dt.ToString(AppConstants.FmtTimeMs);

        /// <summary>yyyy-MM-dd HH:mm:ss — DB 저장, 화면 표시용</summary>
        public static string ToDisplayDateTime(this DateTime dt)
            => dt.ToString(AppConstants.FmtDateTime);

        /// <summary>yyyyMMdd_HHmmss — 파일명 생성용</summary>
        public static string ToFileStamp(this DateTime dt)
            => dt.ToString(AppConstants.FmtDateTimeFile);

        /// <summary>경과 시간을 "X.Xs" 문자열로 반환</summary>
        public static string ToElapsedString(this TimeSpan ts)
            => $"{ts.TotalSeconds:F1}s";
    }
}
