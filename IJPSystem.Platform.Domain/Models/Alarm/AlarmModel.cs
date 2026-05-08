using System;
using IJPSystem.Platform.Domain.Common;

namespace IJPSystem.Platform.Domain.Models.Alarm
{
    public class AlarmModel : BindableBase
    {
        public long DbId { get; set; }              // DB 레코드 ID (LogAlarmEnd 호출 시 사용)
        public DateTime OccurredTime { get; set; }  // 발생 시각 (재발 시 갱신)
        public string AlarmCode { get; set; } = ""; // 에러 코드 (예: E101)
        public string AlarmName { get; set; } = "";
        public string AlarmGuide { get; set; } = "";
        public string Severity { get; set; } = "";  // 심각도 (Fatal/Error/Warning/Info)

        private bool _isCleared;
        public bool IsCleared
        {
            get => _isCleared;
            set => SetProperty(ref _isCleared, value);
        }

        private DateTime? _resolvedTime;
        public DateTime? ResolvedTime
        {
            get => _resolvedTime;
            set => SetProperty(ref _resolvedTime, value);
        }

        // 같은 코드 알람이 미해제 상태에서 재발한 횟수 (1=최초 발생).
        // 메모리 전용. DB에는 첫 발생 1행만 기록 — 재발은 카운트만 증가.
        private int _repeatCount = 1;
        public int RepeatCount
        {
            get => _repeatCount;
            set => SetProperty(ref _repeatCount, value);
        }
    }
}