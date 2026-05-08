using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Motion;
using IJPSystem.Platform.HMI.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class MotorControlViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;

        public ObservableCollection<AxisViewModel> AxisList => _mainVM.SharedAxisList;
        private AxisViewModel? _selectedAxis;
        public AxisViewModel? SelectedAxis
        {
            get => _selectedAxis;
            set => SetProperty(ref _selectedAxis, value);
        }
       
        public ICommand AllHomeCommand    { get; }
        public ICommand AllServoOnCommand  { get; }
        public ICommand AllServoOffCommand { get; }
        public ICommand AllStopCommand     { get; }

        private bool _useSelectedAxisForJog;
        public bool UseSelectedAxisForJog
        {
            get => _useSelectedAxisForJog;
            set => SetProperty(ref _useSelectedAxisForJog, value);
        }

        private double _jogSpeedScale = 1.0;
        public double JogSpeedScale
        {
            get => _jogSpeedScale;
            set
            {
                if (SetProperty(ref _jogSpeedScale, value))
                {
                    OnPropertyChanged(nameof(IsJogSpeedSlow));
                    OnPropertyChanged(nameof(IsJogSpeedNormal));
                    OnPropertyChanged(nameof(IsJogSpeedFast));
                }
            }
        }
        public bool IsJogSpeedSlow   { get => JogSpeedScale == 0.25; set { if (value) JogSpeedScale = 0.25; } }
        public bool IsJogSpeedNormal { get => JogSpeedScale == 1.0;  set { if (value) JogSpeedScale = 1.0; } }
        public bool IsJogSpeedFast   { get => JogSpeedScale == 2.0;  set { if (value) JogSpeedScale = 2.0; } }

        public MotorControlViewModel(MainViewModel mainViewModel)
        {
            _mainVM = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            SelectedAxis = AxisList.FirstOrDefault();

            AllHomeCommand    = new RelayCommand(async _ => await ExecuteAllHome());
            AllServoOnCommand  = new RelayCommand(async _ => await ExecuteAllServoOn());
            AllServoOffCommand = new RelayCommand(async _ => await ExecuteAllServoOff());
            AllStopCommand     = new RelayCommand(async _ => await ExecuteAllStop());
        }
        
        private async Task ExecuteAllHome()
        {
            var result = System.Windows.MessageBox.Show(
                "모든 축의 홈 복귀를 실행합니다.\n주변 장애물을 확인하세요.",
                "Home 복귀 확인",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            _mainVM.AddLog("[MOTION] All axes homing sequence started.");
            try
            {
                await Task.WhenAll(AxisList.Select(a => a.HomeAsync()));
                _mainVM.AddLog("[MOTION] All axes home complete.");
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[MOTION] All Home failed: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ExecuteAllServoOn()
        {
            _mainVM.AddLog("[MOTION] All Axes Servo ON Command.");
            try
            {
                await Task.WhenAll(AxisList.Select(a => a.ForceServoOnAsync()));
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[MOTION] All Servo ON failed: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ExecuteAllServoOff()
        {
            _mainVM.AddLog("[MOTION] All Axes Servo OFF Command.");
            try
            {
                await Task.WhenAll(AxisList.Select(a => a.ForceServoOffAsync()));
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[MOTION] All Servo OFF failed: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ExecuteAllStop()
        {
            _mainVM.AddLog("[MOTION] Stop all axes!");
            try
            {
                await Task.WhenAll(AxisList.Select(a => a.StopAsync()));
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[MOTION] All Stop failed: {ex.Message}", LogLevel.Error);
            }
        }
        
    }
}