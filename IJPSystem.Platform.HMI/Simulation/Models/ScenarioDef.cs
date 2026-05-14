using System.Collections.Generic;

namespace IJPSystem.Platform.HMI.Simulation.Models
{
    // JSON 시나리오 파일과 1:1 매핑. 외부 정의(파일) 기반으로 시뮬레이션을 구동.
    public class ScenarioDef
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Sequence { get; set; } = "";   // SequenceRegistry.Id (예: PURGE, AUTO_PRINT)
        public List<ScenarioEvent> Events { get; set; } = new();
        public ExpectedResult Expect { get; set; } = new();
    }

    // 시간(at_ms) 또는 step 진입(on_step + after_ms) 기준으로 가상 입력을 주입.
    public class ScenarioEvent
    {
        public int?    AtMs    { get; set; }
        public string? OnStep  { get; set; }
        public int?    AfterMs { get; set; }
        public Dictionary<string, bool>?   SetDi { get; set; }
        public Dictionary<string, double>? SetAi { get; set; }
    }

    public class ExpectedResult
    {
        public string  Result         { get; set; } = "completed"; // completed | failed | stopped
        public string? Alarm          { get; set; }
        public string? FailedAtStep   { get; set; }
        public int?    DurationMaxMs  { get; set; }
    }
}
