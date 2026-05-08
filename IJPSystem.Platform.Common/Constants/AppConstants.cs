namespace IJPSystem.Platform.Common.Constants
{
    /// <summary>애플리케이션 전역 상수</summary>
    public static class AppConstants
    {
        // ── 날짜/시간 포맷 ────────────────────────────────────────────────────
        public const string FmtTime          = "HH:mm:ss";
        public const string FmtTimeMs        = "HH:mm:ss.fff";
        public const string FmtDateTime      = "yyyy-MM-dd HH:mm:ss";
        public const string FmtDateTimeFile  = "yyyyMMdd_HHmmss";

        // ── 파일 경로 ─────────────────────────────────────────────────────────
        public const string ConfigFolder          = "Config";
        public const string LogFolder             = @"C:\Logs";
        public const string MotorConfigFile       = "MotorConfig.json";
        public const string IoConfigFile          = "IO.json";
        public const string VisionConfigFile      = "VisionConfig.json";
        public const string AlarmSystemDb         = "AlarmSystem.db";
        public const string AlarmHistoryDb        = "AlarmHistory.db";
        public const string SystemLogDb           = "SystemLog.db";

        // ── 로그/실행 제한 ────────────────────────────────────────────────────
        public const int MaxExecutionLogCount = 50;   // 시퀀스 실행 로그 최대 보관 수
        public const int MaxMainLogCount      = 500;  // 메인 로그 최대 보관 수

        // ── 타이머 주기 (ms) ──────────────────────────────────────────────────
        public const int TimerIntervalFastMs  = 50;   // 빠른 갱신 (모션 시뮬레이션, 애니메이션)
        public const int TimerIntervalSlowMs  = 500;  // 느린 갱신 (시스템 시간, I/O 상태)

        // ── 모션 ──────────────────────────────────────────────────────────────
        public const int MotionPollIntervalMs    = 100;  // 축 상태 폴링 주기
        public const int MotionInPositionTimeout = 200;  // InPosition 대기 최대 횟수 (× 100ms = 20s)
        public const double MaxJogVelocity       = 5000; // 조그 최대 속도 (pulse/s)
    }
}
