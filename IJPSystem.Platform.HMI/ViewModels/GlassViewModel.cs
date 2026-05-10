using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Vision;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class GlassViewModel : ViewModelBase, IDisposable
    {
        private const string CamId = "CAM_02";

        private readonly IVisionDriver _vision;
        private readonly MainViewModel _mainVM;
        private readonly DispatcherTimer _statusTimer;
        private readonly DispatcherTimer _liveTimer;

        private CancellationTokenSource? _liveCts;

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

        // ── 라이브 모드 ────────────────────────────────────────────────────────
        private bool _isLiveMode;
        public bool IsLiveMode
        {
            get => _isLiveMode;
            private set
            {
                if (SetProperty(ref _isLiveMode, value))
                {
                    OnPropertyChanged(nameof(IsNotLiveMode));
                    OnPropertyChanged(nameof(LiveStatusText));
                }
            }
        }
        public bool   IsNotLiveMode  => !IsLiveMode;
        public string LiveStatusText => IsLiveMode ? "LIVE" : "STOP";

        // ── FPS 표시 ──────────────────────────────────────────────────────────
        private int _liveIntervalMs = 200;
        public int LiveIntervalMs
        {
            get => _liveIntervalMs;
            set
            {
                if (SetProperty(ref _liveIntervalMs, Math.Clamp(value, 50, 2000)))
                {
                    _liveTimer.Interval = TimeSpan.FromMilliseconds(_liveIntervalMs);
                    OnPropertyChanged(nameof(FpsText));
                }
            }
        }
        public string FpsText => $"{1000.0 / LiveIntervalMs:F1} fps";

        // ── 현재 표시 이미지 경로 ──────────────────────────────────────────────
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

        // ── 총 캡쳐 카운트 ────────────────────────────────────────────────────
        private int _captureCount;
        public int CaptureCount
        {
            get => _captureCount;
            private set => SetProperty(ref _captureCount, value);
        }

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

        // ── 처리 중 상태 ──────────────────────────────────────────────────────
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        // ── 커맨드 ────────────────────────────────────────────────────────────
        public ICommand StartLiveCommand  { get; }
        public ICommand StopLiveCommand   { get; }
        public ICommand ToggleLiveCommand { get; }
        public ICommand CaptureCommand    { get; }
        public ICommand LightOnCommand    { get; }
        public ICommand LightOffCommand   { get; }
        public ICommand OpenImageCommand  { get; }

        public GlassViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _vision = mainVM.GetController().GetMachine().Vision;

            StartLiveCommand  = new RelayCommand(_ => StartLive(),              _ => !IsLiveMode && !IsBusy);
            StopLiveCommand   = new RelayCommand(_ => StopLive(),               _ => IsLiveMode);
            ToggleLiveCommand = new RelayCommand(_ => { if (IsLiveMode) StopLive(); else StartLive(); });
            CaptureCommand    = new RelayCommand(async _ => await ExecuteCaptureAsync(), _ => !IsLiveMode && !IsBusy);
            LightOnCommand   = new RelayCommand(_ => ExecuteLight(true),  _ => !IsBusy);
            LightOffCommand  = new RelayCommand(_ => ExecuteLight(false), _ => !IsBusy);
            OpenImageCommand = new RelayCommand(_ => ExecuteOpenImage(),  _ => !IsLiveMode);

            // 카메라 상태 폴링 (500ms)
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _statusTimer.Tick += (_, _) => CamStatus = _vision.GetStatus(CamId);
            _statusTimer.Start();

            // 라이브 캡쳐 타이머
            _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_liveIntervalMs) };
            _liveTimer.Tick += async (_, _) => await LiveTickAsync();

            CamStatus = _vision.GetStatus(CamId);
        }

        // ── 라이브 시작 / 정지 ────────────────────────────────────────────────
        private void StartLive()
        {
            _liveCts = new CancellationTokenSource();
            IsLiveMode = true;
            _liveTimer.Start();
            RaiseAllCanExecute();
            _mainVM.AddLog("[VISION] Glass: 라이브 모드 시작", LogLevel.Info);
        }

        private void StopLive()
        {
            _liveTimer.Stop();
            _liveCts?.Cancel();
            _liveCts = null;
            IsLiveMode = false;
            RaiseAllCanExecute();
            _mainVM.AddLog("[VISION] Glass: 라이브 모드 정지", LogLevel.Info);
        }

        private async Task LiveTickAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var image = await _vision.CaptureAsync(CamId);
                if (image.IsValid)
                {
                    CurrentImagePath = null;           // 강제 갱신 트리거
                    CurrentImagePath = image.FilePath;
                    CaptureCount++;
                }
            }
            catch (Exception ex)
            {
                // 라이브 중 오류는 화면 로그 노출 없이 파일에만 기록
                LoggerService.WriteToFile("DEBUG", $"[GLASS_LIVE] capture failed: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // ── 단일 캡쳐 ──────────────────────────────────────────────────────────
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
                    CaptureCount++;
                    _mainVM.AddLog($"[VISION] Glass: 캡쳐 완료 ({image.Width}×{image.Height})", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[VISION] Glass: 캡쳐 실패: {ex.Message}", LogLevel.Error);
            }
            finally { IsBusy = false; RaiseAllCanExecute(); }
        }

        // ── 조명 ON/OFF ───────────────────────────────────────────────────────
        private void ExecuteLight(bool on)
        {
            _vision.SetLight(CamId, on);
            if (on) _vision.SetLightIntensity(CamId, LightIntensity);
            CamStatus = _vision.GetStatus(CamId);
        }

        // ── 이미지 파일 열기 ──────────────────────────────────────────────────
        private void ExecuteOpenImage()
        {
            string defaultDir = Path.Combine(@"C:\Logs\Vision", CamId);
            if (!Directory.Exists(defaultDir)) defaultDir = @"C:\Logs\Vision";
            if (!Directory.Exists(defaultDir)) defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var dlg = new OpenFileDialog
            {
                Title            = "이미지 파일 선택",
                Filter           = "이미지 파일|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일|*.*",
                InitialDirectory = defaultDir,
                Multiselect      = false,
            };

            if (dlg.ShowDialog() == true)
            {
                CurrentImagePath = dlg.FileName;
                _mainVM.AddLog($"[VISION] Glass: 이미지 로드: {Path.GetFileName(dlg.FileName)}", LogLevel.Info);
            }
        }

        private void RaiseAllCanExecute()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ((RelayCommand)StartLiveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopLiveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ToggleLiveCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CaptureCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOnCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOffCommand).RaiseCanExecuteChanged();
                ((RelayCommand)OpenImageCommand).RaiseCanExecuteChanged();
            });
        }

        public void Dispose()
        {
            _statusTimer.Stop();
            _liveTimer.Stop();
            _liveCts?.Cancel();
            _liveCts?.Dispose();
            _liveCts = null;
        }
    }
}
