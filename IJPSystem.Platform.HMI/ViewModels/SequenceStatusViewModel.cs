using IJPSystem.Platform.Domain.Common;

namespace IJPSystem.Platform.HMI.ViewModels
{
    // 자동 시퀀스 진행 상태 — PnidView 의 SEQUENCE STATUS 패널 바인딩 단일 소스.
    // 여러 흩어진 프로퍼티 대신 하나의 객체로 노출해 XAML 바인딩 구조와 setter 알림 체인을 단순화한다.
    public class SequenceStatusViewModel : ViewModelBase
    {
        private string _sequenceName    = string.Empty;
        private string _currentStepName = string.Empty;
        private int    _currentStepIndex;
        private int    _totalSteps;
        private bool   _isRunning;

        public string SequenceName    { get => _sequenceName;    private set => SetProperty(ref _sequenceName, value); }
        public string CurrentStepName { get => _currentStepName; private set => SetProperty(ref _currentStepName, value); }

        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            private set
            {
                if (SetProperty(ref _currentStepIndex, value))
                {
                    OnPropertyChanged(nameof(StepProgressPct));
                    OnPropertyChanged(nameof(StepIndexText));
                }
            }
        }

        public int TotalSteps
        {
            get => _totalSteps;
            private set
            {
                if (SetProperty(ref _totalSteps, value))
                {
                    OnPropertyChanged(nameof(StepProgressPct));
                    OnPropertyChanged(nameof(StepIndexText));
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                    OnPropertyChanged(nameof(IsVisible));
            }
        }

        public double StepProgressPct => _totalSteps == 0 ? 0.0 : (_currentStepIndex * 100.0 / _totalSteps);
        public string StepIndexText   => _totalSteps == 0 ? string.Empty : $"{_currentStepIndex}/{_totalSteps}";
        public bool   IsVisible       => _isRunning;

        public void Begin(string sequenceName, int totalSteps)
        {
            SequenceName     = sequenceName;
            TotalSteps       = totalSteps;
            CurrentStepIndex = 0;
            CurrentStepName  = string.Empty;
        }

        public void Advance(int index, string stepName)
        {
            CurrentStepIndex = index;
            CurrentStepName  = stepName;
        }

        public void Reset()
        {
            SequenceName     = string.Empty;
            CurrentStepName  = string.Empty;
            CurrentStepIndex = 0;
            TotalSteps       = 0;
        }
    }
}
