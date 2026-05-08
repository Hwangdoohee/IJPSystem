namespace IJPSystem.Platform.Domain.Enums
{
    // 액체 레벨 상태 — TANK 비주얼/알람 매핑용
    public enum LevelStatus
    {
        Unknown = 0,
        Empty,
        Low,
        Set,    // 정상 운전 레벨
        High,
        HH,     // High-High (알람)
    }
}
