namespace IJPSystem.Platform.Common.Enums
{
    /// <summary>로그의 심각도 또는 유형을 정의합니다.</summary>
    public enum LogLevel
    {
        Info,    // 일반 정보
        Success, // 작업 성공
        Warning, // 주의가 필요한 상황
        Error,   // 예외 발생
        Fatal    // 시스템 중단 수준의 치명적 오류
    }
}
