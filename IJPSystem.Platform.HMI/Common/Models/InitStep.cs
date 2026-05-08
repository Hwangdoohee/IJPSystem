using IJPSystem.Platform.Domain.Common;

namespace IJPSystem.Platform.HMI.Common.Models
{
    public enum InitStepStatus
    {
        Pending,
        Running,
        Done,
        Failed,
    }

    // 초기 로딩 화면의 한 단계 — 이름/설명/상태/에러
    public class InitStep : ViewModelBase
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        private InitStepStatus _status = InitStepStatus.Pending;
        public InitStepStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(Icon));
                    OnPropertyChanged(nameof(IconColor));
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError    => !string.IsNullOrEmpty(_errorMessage);
        public bool IsRunning   => _status == InitStepStatus.Running;

        public string Icon => _status switch
        {
            InitStepStatus.Pending => "○",
            InitStepStatus.Running => "●",
            InitStepStatus.Done    => "✓",
            InitStepStatus.Failed  => "✗",
            _ => "?",
        };

        public string IconColor => _status switch
        {
            InitStepStatus.Pending => "#475569",
            InitStepStatus.Running => "#3B82F6",
            InitStepStatus.Done    => "#22C55E",
            InitStepStatus.Failed  => "#EF4444",
            _ => "#94A3B8",
        };
    }
}
