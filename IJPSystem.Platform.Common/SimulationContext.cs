namespace IJPSystem.Platform.Common
{
    // 시뮬레이터 RUN 중에만 true. 가상 드라이버와 시퀀스 헬퍼가 이 플래그를 보고
    // 시간 가속(즉시 도착/즉시 IO/Delay 무시) 모드로 동작한다.
    public static class SimulationContext
    {
        public static bool FastForward { get; set; } = false;
    }
}
