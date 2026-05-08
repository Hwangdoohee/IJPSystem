namespace IJPSystem.Platform.Domain.Models.Alarm
{
    public class AlarmMasterModel
    {
        public string? AlarmCode { get; set; }

        // 분류
        public int Category { get; set; }
        public string? CategoryName { get; set; }
        public string? Severity { get; set; }       // Fatal / Error / Warning / Info

        // 에러 명칭
        public string? AlarmName_KR { get; set; }
        public string? AlarmName_EN { get; set; }

        // 조치 방법
        public string? ActionGuide_KR { get; set; }
        public string? ActionGuide_EN { get; set; }

        // 운영 정보
        public string? TriggerCondition { get; set; }
        public bool AckRequired { get; set; } = true;
        public int? AutoResetDelayMs { get; set; }
        public string? FileLocation { get; set; }
    }
}
