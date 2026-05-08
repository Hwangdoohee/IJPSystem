using IJPSystem.Platform.Domain.Common;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace IJPSystem.Platform.Domain.Models.IO
{
    public enum IOContactType { NO, NC }
    public enum IOMode { Digital, Analog }

    public class IOViewModel : INotifyPropertyChanged
    {
        #region Fields
        private bool _hardwareSignal;
        private bool _isOn;
        private string? _description;
        private string? _ioCategory;
        private IOContactType _contactType = IOContactType.NO;
        private double _analogValue;
        private double _analogSetValue; 
        #endregion

        #region Common Properties
        public string? Address { get; set; }
        public string? Index { get; set; }
        public string? IoCategory
        {
            get => _ioCategory;
            set { _ioCategory = value; OnPropertyChanged(); }
        }
        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public IOMode Mode { get; set; } = IOMode.Digital;
        #endregion

        #region Digital Properties
        public bool HardwareSignal
        {
            get => _hardwareSignal;
            set
            {
                if (_hardwareSignal == value) return;
                _hardwareSignal = value;
                OnPropertyChanged();
                UpdateDisplayStatus();
            }
        }

        public IOContactType ContactType
        {
            get => _contactType;
            set
            {
                if (_contactType == value) return;
                _contactType = value;
                OnPropertyChanged();
                UpdateDisplayStatus();
            }
        }

        public bool IsOn
        {
            get => _isOn;
            private set { _isOn = value; OnPropertyChanged(); }
        }
        #endregion

        #region Analog Properties
        /// <summary>현재 아날로그 값 (모니터링용, 드라이버에서 읽어옴)</summary>
        public double AnalogValue
        {
            get => _analogValue;
            set { _analogValue = value; OnPropertyChanged(); }
        }

        /// <summary>사용자가 설정할 아날로그 출력값 (AO 전용)</summary>
        public double AnalogSetValue
        {
            get => _analogSetValue;
            set { _analogSetValue = value; OnPropertyChanged(); }
        }

        public string Unit { get; set; } = "V";
        public double MinValue { get; set; } = 0.0;
        public double MaxValue { get; set; } = 100.0;
        #endregion

        #region Commands & Events

        // 디지털 출력 토글 이벤트
        public event EventHandler<bool>? RequestControl;

        // 아날로그 출력 설정 이벤트 (추가)
        public event EventHandler<double>? RequestAnalogControl;

        // 디지털 토글 커맨드
        public ICommand? ToggleCommand { get; set; }

        // 아날로그 출력 설정 커맨드 (추가)
        private ICommand? _setAnalogCommand;
        public ICommand SetAnalogCommand =>
            _setAnalogCommand ??= new RelayCommand(_ =>
            {
                double clamped = Math.Clamp(AnalogSetValue, MinValue, MaxValue);
                AnalogSetValue = clamped;
                RequestAnalogControl?.Invoke(this, clamped);
            });

        public IOViewModel()
        {
            ToggleCommand = new RelayCommand(_ => ExecuteToggle());
        }

        private void ExecuteToggle()
        {
            if (Address?.StartsWith("Y") == true)
                RequestControl?.Invoke(this, !HardwareSignal);
        }

        private void UpdateDisplayStatus()
        {
            if (Mode == IOMode.Digital)
                IsOn = ContactType == IOContactType.NO ? HardwareSignal : !HardwareSignal;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}