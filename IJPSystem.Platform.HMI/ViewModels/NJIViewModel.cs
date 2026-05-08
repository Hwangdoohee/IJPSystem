using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Vision;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class NJIViewModel : ViewModelBase
    {
        private const string CamId = "CAM_01";

        private readonly IVisionDriver _vision;
        private readonly MainViewModel _mainVM;
        private readonly DispatcherTimer _pollTimer;

        // ── 카메라 상태 (500ms 폴링) ─────────────────────────────────────────
        private CameraStatus? _camStatus;
        public CameraStatus? CamStatus
        {
            get => _camStatus;
            private set => SetProperty(ref _camStatus, value);
        }

        // ── 마지막 검사 결과 ──────────────────────────────────────────────────
        private InspectionResult? _lastResult;
        public InspectionResult? LastResult
        {
            get => _lastResult;
            private set
            {
                if (SetProperty(ref _lastResult, value))
                {
                    OnPropertyChanged(nameof(HasResult));
                    OnPropertyChanged(nameof(HasNoResult));
                    OnPropertyChanged(nameof(ResultIsPass));
                    OnPropertyChanged(nameof(ResultIsFail));
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(ScoreText));
                    OnPropertyChanged(nameof(NgDescription));
                }
            }
        }

        public bool   HasResult    => LastResult != null;
        public bool   HasNoResult  => LastResult == null;
        public bool   ResultIsPass => LastResult?.IsPass == true;
        public bool   ResultIsFail => LastResult?.IsPass == false;
        public string ResultText   => LastResult == null     ? "-"
                                    : LastResult.IsPass      ? "PASS"
                                                             : $"NG  [{LastResult.NgCode}]";
        public string NgDescription => LastResult?.NgDescription ?? "";
        public string ScoreText     => LastResult == null ? "-" : $"{LastResult.Score:F1}";

        public string CaptureTimeText => CamStatus?.LastCaptureTime == null
            ? "-"
            : CamStatus.LastCaptureTime.Value.ToString("HH:mm:ss.fff");

        // ── 마지막 저장 이미지 경로 ───────────────────────────────────────────
        private string? _lastImagePath;
        public string? LastImagePath
        {
            get => _lastImagePath;
            private set
            {
                if (SetProperty(ref _lastImagePath, value))
                {
                    OnPropertyChanged(nameof(HasImagePath));
                    OnPropertyChanged(nameof(HasNoImagePath));
                }
            }
        }

        public bool HasImagePath   => !string.IsNullOrEmpty(LastImagePath);
        public bool HasNoImagePath => string.IsNullOrEmpty(LastImagePath);

        // ── 통계 ──────────────────────────────────────────────────────────────
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                SetProperty(ref _totalCount, value);
                OnPropertyChanged(nameof(PassCount));
                OnPropertyChanged(nameof(PassRateText));
                OnPropertyChanged(nameof(PassRatePct));
            }
        }

        private int _ngCount;
        public int NgCount
        {
            get => _ngCount;
            private set
            {
                SetProperty(ref _ngCount, value);
                OnPropertyChanged(nameof(PassCount));
                OnPropertyChanged(nameof(PassRateText));
                OnPropertyChanged(nameof(PassRatePct));
            }
        }

        public int    PassCount    => TotalCount - NgCount;
        public string PassRateText => TotalCount == 0 ? "- %" : $"{PassCount * 100.0 / TotalCount:F1} %";
        public double PassRatePct  => TotalCount == 0 ? 0     : PassCount * 100.0 / TotalCount;

        // ── 조명 강도 (슬라이더 바인딩) ──────────────────────────────────────
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
        public ICommand CaptureCommand   { get; }
        public ICommand InspectCommand   { get; }
        public ICommand LightOnCommand   { get; }
        public ICommand LightOffCommand  { get; }
        public ICommand OpenImageCommand { get; }

        public NJIViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _vision = mainVM.GetController().GetMachine().Vision;

            CaptureCommand  = new RelayCommand(async _ => await ExecuteCaptureAsync(),  _ => !IsBusy);
            InspectCommand  = new RelayCommand(async _ => await ExecuteInspectAsync(),  _ => !IsBusy);
            LightOnCommand   = new RelayCommand(_ => ExecuteLight(true),    _ => !IsBusy);
            LightOffCommand  = new RelayCommand(_ => ExecuteLight(false),   _ => !IsBusy);
            OpenImageCommand = new RelayCommand(_ => ExecuteOpenImage());

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _pollTimer.Tick += (_, _) =>
            {
                CamStatus = _vision.GetStatus(CamId);
                OnPropertyChanged(nameof(CaptureTimeText));
            };
            _pollTimer.Start();

            CamStatus = _vision.GetStatus(CamId);
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
            if (!Directory.Exists(defaultDir))
                defaultDir = @"C:\Logs\Vision";
            if (!Directory.Exists(defaultDir))
                defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var dlg = new OpenFileDialog
            {
                Title            = "이미지 파일 선택",
                Filter           = "이미지 파일|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일|*.*",
                InitialDirectory = defaultDir,
                Multiselect      = false,
            };

            if (dlg.ShowDialog() == true)
            {
                LastImagePath = dlg.FileName;
                _mainVM.AddLog($"[VISION] NJI: 이미지 로드: {Path.GetFileName(dlg.FileName)}", LogLevel.Info);
            }
        }

        // ── 촬영 ──────────────────────────────────────────────────────────────
        private async Task ExecuteCaptureAsync()
        {
            IsBusy = true;
            RaiseAllCanExecute();
            try
            {
                var image = await _vision.CaptureAsync(CamId);
                LastImagePath = image.FilePath;
                _mainVM.AddLog($"[VISION] NJI: 촬영 완료 ({image.Width}×{image.Height})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[VISION] NJI: 촬영 실패: {ex.Message}", LogLevel.Error);
                _mainVM.AlarmVM.RaiseAlarm("IO-NJI-CAPTURE");
            }
            finally { IsBusy = false; RaiseAllCanExecute(); }
        }

        // ── 검사 ──────────────────────────────────────────────────────────────
        private async Task ExecuteInspectAsync()
        {
            IsBusy = true;
            RaiseAllCanExecute();
            try
            {
                var result = await _vision.CaptureAndInspectAsync(CamId);
                TotalCount++; 
                if (!result.IsPass) NgCount++;
                LastResult    = result;
                LastImagePath = result.Image?.FilePath;

                string log = result.IsPass
                    ? $"[VISION] NJI: PASS  Score={result.Score:F1}"
                    : $"[VISION] NJI: NG [{result.NgCode}] {result.NgDescription}  Score={result.Score:F1}";

                _mainVM.AddLog(log, result.IsPass ? LogLevel.Success : LogLevel.Error);

                if (!result.IsPass)
                    _mainVM.AlarmVM.RaiseAlarm("LOG-NJI-NG");
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[VISION] NJI: 검사 실패: {ex.Message}", LogLevel.Error);
                _mainVM.AlarmVM.RaiseAlarm("IO-NJI-INSPECT");
            }
            finally { IsBusy = false; RaiseAllCanExecute(); }
        }

        private void RaiseAllCanExecute()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ((RelayCommand)CaptureCommand).RaiseCanExecuteChanged();
                ((RelayCommand)InspectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOnCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LightOffCommand).RaiseCanExecuteChanged();
            });
        }
    }
}
