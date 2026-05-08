using IJPSystem.Platform.Common.Enums;
using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Domain.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    /// <summary>
    /// Pattern Generator 화면 — 사각 영역(가로×세로)을 헤드팩으로 도장 인쇄하기 위한 파라미터를 입력받는다.
    /// 실제 인쇄 실행은 시퀀스/머신 레이어에서 수행하고 여기서는 사용자 입력값과 원점 캡처만 담당한다.
    /// </summary>
    public class PatternPrintViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;

        // ── 헤드팩 선택 ───────────────────────────────────────────────
        public ObservableCollection<string> HeadPacks { get; } = new()
        {
            "Head Pack 1", "Head Pack 2", "Head Pack 3", "Head Pack 4",
        };

        private string _selectedHeadPack = "Head Pack 1";
        public string SelectedHeadPack
        {
            get => _selectedHeadPack;
            set => SetProperty(ref _selectedHeadPack, value);
        }

        // ── 파라미터 ──────────────────────────────────────────────────
        private int _nOverlapNz = 30;
        public int NOverlapNz
        {
            get => _nOverlapNz;
            set => SetProperty(ref _nOverlapNz, Math.Max(0, value));
        }

        private double _widthMm = 150.0;
        public double WidthMm
        {
            get => _widthMm;
            set => SetProperty(ref _widthMm, Math.Max(0, value));
        }

        private double _lengthMm = 150.0;
        public double LengthMm
        {
            get => _lengthMm;
            set => SetProperty(ref _lengthMm, Math.Max(0, value));
        }

        private int _usingHead = 1;
        public int UsingHead
        {
            get => _usingHead;
            set => SetProperty(ref _usingHead, Math.Max(1, value));
        }

        // ── DPI / Drop Pitch ─────────────────────────────────────────
        private int _dpi = 600;
        public int Dpi
        {
            get => _dpi;
            set
            {
                if (SetProperty(ref _dpi, Math.Max(1, value)))
                    OnPropertyChanged(nameof(DropPitchMm));
            }
        }

        public double DropPitchMm => DpiConverter.DpiToPitchMm(_dpi);

        // ── 원점 (Set Print Origin 버튼으로 캡처) ─────────────────────
        private double _xOrigin;
        public double XOrigin { get => _xOrigin; private set => SetProperty(ref _xOrigin, value); }

        private double _yOrigin;
        public double YOrigin { get => _yOrigin; private set => SetProperty(ref _yOrigin, value); }

        private double _zOrigin;
        public double ZOrigin { get => _zOrigin; private set => SetProperty(ref _zOrigin, value); }

        private bool _isOriginSet;
        public bool IsOriginSet { get => _isOriginSet; private set => SetProperty(ref _isOriginSet, value); }

        // ── Print Velocity (활성 레시피의 X축 Print.Vel) ────────────────
        private double _printVelocity;
        public double PrintVelocity
        {
            get => _printVelocity;
            private set => SetProperty(ref _printVelocity, value);
        }

        // ── Commands ─────────────────────────────────────────────────
        public ICommand SetPrintOriginCommand { get; }
        public ICommand PrintCommand          { get; }
        public ICommand AbortCommand          { get; }

        public PatternPrintViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            SetPrintOriginCommand = new RelayCommand(_ => CaptureCurrentOrigin());
            PrintCommand          = new RelayCommand(_ => StartPrint(),  _ => IsOriginSet);
            AbortCommand          = new RelayCommand(_ => AbortPrint());

            RefreshPrintVelocity();
        }

        /// <summary>현재 X/Y/Z 축 위치를 인쇄 원점으로 캡처한다.</summary>
        private void CaptureCurrentOrigin()
        {
            XOrigin = FindAxisPos("X");
            YOrigin = FindAxisPos("Y");
            ZOrigin = FindAxisPos("Z");
            IsOriginSet = true;

            _mainVM.AddLog(
                $"[PATTERN] Print Origin set — X={XOrigin:F3}mm, Y={YOrigin:F3}mm, Z={ZOrigin:F3}mm",
                LogLevel.Info);
        }

        /// <summary>SharedAxisList 에서 축 이름(접두 매칭)으로 현재 위치를 찾는다.</summary>
        private double FindAxisPos(string namePrefix)
        {
            var ax = _mainVM.SharedAxisList.FirstOrDefault(a =>
                (a.Info?.Name ?? "").StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
            return ax?.CurrentPos ?? 0.0;
        }

        private void RefreshPrintVelocity()
        {
            // 활성 레시피의 X축 Print.Velocity 를 사용 (없으면 100 mm/s 기본값)
            var xAxis = _mainVM.SharedAxisList.FirstOrDefault(a =>
                (a.Info?.Name ?? "").StartsWith("X", StringComparison.OrdinalIgnoreCase));
            var cfg = xAxis == null ? null : _mainVM.RecipeVM?.GetActiveMotionConfig(xAxis.Info.AxisNo);
            PrintVelocity = cfg?.Printing?.Velocity ?? 200.0;
        }

        private void StartPrint()
        {
            // 실제 인쇄 시퀀스 트리거는 추후 연결.
            _mainVM.AddLog(
                $"[PATTERN] Print start — {SelectedHeadPack}, " +
                $"W={WidthMm:F2}mm × L={LengthMm:F2}mm, nz={NOverlapNz}, head={UsingHead}, " +
                $"{Dpi}dpi (pitch={DropPitchMm:F4}mm)",
                LogLevel.Info);
        }

        private void AbortPrint()
        {
            _mainVM.AddLog("[PATTERN] Print aborted", LogLevel.Warning);
        }
    }
}
