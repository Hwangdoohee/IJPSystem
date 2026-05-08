using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Vision;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public enum NozzleState { Unknown, Good, Weak, Missing }

    public class NozzleStatusItem : ViewModelBase
    {
        public int Index { get; }

        private NozzleState _state = NozzleState.Unknown;
        public NozzleState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                    OnPropertyChanged(nameof(Color));
            }
        }

        public string Color => State switch
        {
            NozzleState.Good    => "#22C55E",
            NozzleState.Weak    => "#F59E0B",
            NozzleState.Missing => "#EF4444",
            _                   => "#334155"
        };

        public NozzleStatusItem(int index) => Index = index;
    }

    public class DropWatcherViewModel : ViewModelBase
    {
        private const string CamId = "CAM_DW";
        private const int NozzleCount = 128;

        private readonly IVisionDriver _vision;
        private readonly MainViewModel _mainVM;
        private readonly DispatcherTimer _pollTimer;

        // ── 카메라 상태 ────────────────────────────────────────────────────────
        private CameraStatus? _camStatus;
        public CameraStatus? CamStatus
        {
            get => _camStatus;
            private set
            {
                if (SetProperty(ref _camStatus, value))
                    OnPropertyChanged(nameof(CaptureTimeText));
            }
        }

        public string CaptureTimeText => CamStatus?.LastCaptureTime == null
            ? "-"
            : CamStatus.LastCaptureTime.Value.ToString("HH:mm:ss.fff");

        // ── 현재 이미지 경로 ───────────────────────────────────────────────────
        private string? _currentImagePath;
        public string? CurrentImagePath
        {
            get => _currentImagePath;
            private set
            {
                if (SetProperty(ref _currentImagePath, value))
                {
                    OnPropertyChanged(nameof(HasImage));
                    OnPropertyChanged(nameof(HasNoImage));
                }
            }
        }
        public bool HasImage   => !string.IsNullOrEmpty(CurrentImagePath);
        public bool HasNoImage => string.IsNullOrEmpty(CurrentImagePath);

        // ── 노즐 상태 목록 ────────────────────────────────────────────────────
        public ObservableCollection<NozzleStatusItem> Nozzles { get; } = new();

        // ── 통계 ──────────────────────────────────────────────────────────────
        private int _goodCount;
        public int GoodCount
        {
            get => _goodCount;
            private set
            {
                SetProperty(ref _goodCount, value);
                OnPropertyChanged(nameof(HealthRateText));
                OnPropertyChanged(nameof(HealthRatePct));
            }
        }

        private int _weakCount;
        public int WeakCount
        {
            get => _weakCount;
            private set => SetProperty(ref _weakCount, value);
        }

        private int _missingCount;
        public int MissingCount
        {
            get => _missingCount;
            private set => SetProperty(ref _missingCount, value);
        }

        public string HealthRateText => GoodCount + WeakCount + MissingCount == 0
            ? "- %"
            : $"{GoodCount * 100.0 / NozzleCount:F1} %";
        public double HealthRatePct => GoodCount * 100.0 / NozzleCount;

        // ── 조명 강도 ──────────────────────────────────────────────────────────
        private int _lightIntensity = 200;
        public int LightIntensity
        {
            get => _lightIntensity;
            set
            {
                if (SetProperty(ref _lightIntensity, value))
                    _vision.SetLightIntensity(CamId, value);
            }
        }

        // ── 스트로브 주파수 ────────────────────────────────────────────────────
        private int _strobeFreqHz = 3000;
        public int StrobeFreqHz
        {
            get => _strobeFreqHz;
            set => SetProperty(ref _strobeFreqHz, Math.Clamp(value, 100, 30000));
        }

        // ── 처리 중 ───────────────────────────────────────────────────────────
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        // ── 커맨드 ────────────────────────────────────────────────────────────
        public ICommand CaptureCommand     { get; }
        public ICommand InspectAllCommand  { get; }
        public ICommand ClearResultCommand { get; }
        public ICommand LightOnCommand     { get; }
        public ICommand LightOffCommand    { get; }

        public DropWatcherViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _vision = mainVM.GetController().GetMachine().Vision;

            for (int i = 1; i <= NozzleCount; i++)
                Nozzles.Add(new NozzleStatusItem(i));

            CaptureCommand     = new RelayCommand(async _ => await ExecuteCaptureAsync(),    _ => !IsBusy);
            InspectAllCommand  = new RelayCommand(async _ => await ExecuteInspectAllAsync(), _ => !IsBusy);
            ClearResultCommand = new RelayCommand(_ => ExecuteClearResult(),                 _ => !IsBusy);
            LightOnCommand     = new RelayCommand(_ => ExecuteLight(true),  _ => !IsBusy);
            LightOffCommand    = new RelayCommand(_ => ExecuteLight(false), _ => !IsBusy);

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _pollTimer.Tick += (_, _) => CamStatus = _vision.GetStatus(CamId);
            _pollTimer.Start();

            CamStatus = _vision.GetStatus(CamId);
        }

        private void ExecuteLight(bool on)
        {
            _vision.SetLight(CamId, on);
            if (on) _vision.SetLightIntensity(CamId, LightIntensity);
            CamStatus = _vision.GetStatus(CamId);
        }

        private async Task ExecuteCaptureAsync()
        {
            IsBusy = true;
            RaiseAllCanExecute();
            try
            {
                var image = await _vision.CaptureAsync(CamId);
                if (image.IsValid)
                {
                    CurrentImagePath = image.FilePath;
                    _mainVM.AddLog($"[VISION] DropWatcher: 캡쳐 완료 ({image.Width}×{image.Height})", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[VISION] DropWatcher: 캡쳐 실패: {ex.Message}", LogLevel.Error);
                _mainVM.AlarmVM.RaiseAlarm("IO-DW-CAPTURE");
            }
            finally { IsBusy = false; RaiseAllCanExecute(); }
        }

        private async Task ExecuteInspectAllAsync()
        {
            IsBusy = true;
            RaiseAllCanExecute();
            try
            {
                var result = await _vision.CaptureAndInspectAsync(CamId);
                CurrentImagePath = result.Image?.FilePath;

                UpdateNozzleStatuses(result.Score);

                string status = result.IsPass ? "PASS" : $"NG [{result.NgCode}]";
                _mainVM.AddLog(
                    $"[VISION] DropWatcher: 검사 완료 — {status}  Score={result.Score:F1}  Good={GoodCount} Weak={WeakCount} Missing={MissingCount}",
                    result.IsPass ? LogLevel.Success : LogLevel.Warning);

                if (!result.IsPass)
                    _mainVM.AlarmVM.RaiseAlarm("DW-NOZZLE-NG");
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[VISION] DropWatcher: 검사 실패: {ex.Message}", LogLevel.Error);
                _mainVM.AlarmVM.RaiseAlarm("IO-DW-INSPECT");
            }
            finally { IsBusy = false; RaiseAllCanExecute(); }
        }

        private void UpdateNozzleStatuses(double score)
        {
            double healthPct = Math.Clamp(score / 100.0, 0.0, 1.0);
            var rng = new Random((int)DateTime.Now.Ticks);

            foreach (var nozzle in Nozzles)
            {
                double r = rng.NextDouble();
                nozzle.State = r < healthPct        ? NozzleState.Good
                             : r < healthPct + 0.05 ? NozzleState.Weak
                                                    : NozzleState.Missing;
            }

            GoodCount    = Nozzles.Count(n => n.State == NozzleState.Good);
            WeakCount    = Nozzles.Count(n => n.State == NozzleState.Weak);
            MissingCount = Nozzles.Count(n => n.State == NozzleState.Missing);
        }

        private void ExecuteClearResult()
        {
            foreach (var nozzle in Nozzles)
                nozzle.State = NozzleState.Unknown;
            GoodCount       = 0;
            WeakCount       = 0;
            MissingCount    = 0;
            CurrentImagePath = null;
            _mainVM.AddLog("[VISION] DropWatcher: 결과 초기화", LogLevel.Info);
        }

        private void RaiseAllCanExecute()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ((RelayCommand)CaptureCommand).RaiseCanExecuteChanged();
                ((RelayCommand)InspectAllCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ClearResultCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOnCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOffCommand).RaiseCanExecuteChanged();
            });
        }
    }
}
