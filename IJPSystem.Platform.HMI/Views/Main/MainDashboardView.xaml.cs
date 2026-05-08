using IJPSystem.Platform.HMI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace IJPSystem.Platform.HMI.Views
{
    public partial class MainDashboardView : UserControl
    {
        // ── 상태 ────────────────────────────────────────────────────
        private bool _isAnimating = false;
        private bool _isScanning  = false;
        private readonly List<Ellipse> _particles  = new();
        private readonly List<TranslateTransform> _nozzleXTransforms = new();
        // CompositionTarget.Rendering — V-sync 기반 프레임 콜백 (DispatcherTimer 대비 jitter 적음)
        private bool _renderingHooked;
        private DateTime _animStart;
        private readonly Random _rng = new();

        // 시퀀스 진행 상태 추적 — 잉크 분사 / 스캔라인 가시성 제어
        private int _currentStepNo;
        private double _maxScanT;                  // PrintedAreaScale 단조 증가용 (한 번 인쇄된 영역 유지)
        private const int PrintScanStepNo = 7;     // AutoPrintSequence step 7 = 인쇄 진행
        // 각 step 진입 시각 (animStart 기준 초) — 스크립트 모드 phase 애니메이션 기점
        private readonly Dictionary<int, double> _stepTimes = new();
        // 파티클 분사 throttle — V-sync ~60fps 환경에서 매 프레임 분사 시 GC 압력 큼
        private int _particleFrameSkip;

        // ── 진단용 ────────────────────────────────────────────────
        // 프레임 간격 / head 점프 / motor 점프 임계값 초과 시 Debug.WriteLine
        // VS 디버그 실행 시 [출력] 창의 디버그 출력에서 [DASH] 태그로 확인
        // static readonly로 둬서 if (DiagEnabled) 블록이 컴파일 타임에 unreachable로 잡히지 않게 함
        private static readonly bool DiagEnabled = false;   // 헤드 매핑 이슈 재발 시 true로 켜면 [DASH] 진단 로그 출력
        private const double FrameSpikeMs      = 50;     // 16ms 정상, 50ms 초과 = 프레임 스킵
        private const double HeadJumpPx        = 40;     // 1프레임에 40px 이상 점프 = 의심
        private const double MotorJumpMm       = 20;     // 1프레임에 20mm 이상 점프 = 의심
        private DateTime _lastFrameAt;
        private double   _lastHeadX = double.NaN;
        private double   _lastMotorX = double.NaN;

        // ── 레이아웃 상수 ────────────────────────────────────────────
        private const double HeadParkedX    = -250;
        private const double HeadScanStartX =    0;
        private const double HeadScanEndX   =  540;
        private const double PrintAreaMaxW  =  534;

        private const double GlassParkedL   = -600;
        private const double GlassCenter    =    0;
        private const double GlassParkedR   =  620;

        private const double NozzleCenterX = 130;
        private const double NozzleBaseY   = 222;

        // ── Phase 시간표 (초) ────────────────────────────────────────
        // Glass 반입/반출은 elapsed 기반 (시작/종료 이벤트가 별도로 없음)
        // Head 관련 phase는 step 진입 시각을 기점으로 한 duration만 사용
        private const double T_GlassLoadStart    = 0.0;
        private const double T_GlassLoadDur      = 1.5;
        private const double T_HeadPosDur        = 0.7;
        private const double T_ScanDur           = 2.5;   // 실제 모터 페이스에 가깝게 (이전 5.0초 → 2.5초)
        private const double T_HeadParkDur       = 0.7;
        private const double T_GlassUnloadStart  = 6.2;   // ScanDur 단축 반영 (1.7+0.7+2.5+0.7 + 여유 0.6)
        private const double T_GlassUnloadDur    = 1.5;
        private const double T_TotalCycle        = 7.7;   // GlassUnloadStart + GlassUnloadDur

        private MainDashboardViewModel? _vm;

        public MainDashboardView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateNozzleDots();
        }

        // ── ViewModel 이벤트 구독/해제 ──────────────────────────────
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromViewModel();
            if (e.NewValue is MainDashboardViewModel vm)
            {
                _vm = vm;
                _vm.AutoPrintStarted     += OnAutoPrintStarted;
                _vm.AutoPrintStepChanged += OnStepChanged;
                _vm.AutoPrintAborted     += StopAnimation;
                _vm.AutoPrintCompleted   += StopAnimation;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
            => UnsubscribeFromViewModel();

        private void UnsubscribeFromViewModel()
        {
            if (_vm == null) return;
            _vm.AutoPrintStarted     -= OnAutoPrintStarted;
            _vm.AutoPrintStepChanged -= OnStepChanged;
            _vm.AutoPrintAborted     -= StopAnimation;
            _vm.AutoPrintCompleted   -= StopAnimation;
            _vm = null;
        }

        // ── 노즐 점 생성 ────────────────────────────────────────────
        private void CreateNozzleDots()
        {
            const int    count    = 6;
            const double spacingX = 10;

            for (int i = 0; i < count; i++)
            {
                var tX = new TranslateTransform { X = HeadParkedX };
                var dot = new Ellipse
                {
                    Width = 5, Height = 5,
                    Fill = new SolidColorBrush(Color.FromRgb(167, 139, 250)),
                    Opacity = 0.85,
                    RenderTransform = tX,
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(124, 58, 237),
                        BlurRadius = 4, ShadowDepth = 0, Opacity = 0.8
                    }
                };
                Canvas.SetLeft(dot, NozzleCenterX - 25 + i * spacingX);
                Canvas.SetTop(dot, NozzleBaseY);
                MainCanvas.Children.Add(dot);
                _nozzleXTransforms.Add(tX);
            }
        }

        private void SyncNozzleX(double x)
        {
            foreach (var t in _nozzleXTransforms) t.X = x;
        }

        // ── 보간 / 이징 헬퍼 ────────────────────────────────────────
        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
        private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
        private static double EaseInCubic(double t) => t * t * t;

        // ── CompositionTarget.Rendering 후킹 ────────────────────────
        // V-sync에 맞춰 호출되어 DispatcherTimer보다 frame jitter가 적음.
        // 시그니처가 EventHandler(object?, EventArgs)로 OnFrameTick과 동일하므로 그대로 연결.
        private void HookRendering()
        {
            if (_renderingHooked) return;
            CompositionTarget.Rendering += OnFrameTick;
            _renderingHooked = true;
        }

        private void UnhookRendering()
        {
            if (!_renderingHooked) return;
            CompositionTarget.Rendering -= OnFrameTick;
            _renderingHooked = false;
        }

        // step 전환 시점의 기대 head 위치로 즉시 스냅 — 다음 OnFrameTick까지 점프 방지
        private void SnapHeadForStep(int stepNumber)
        {
            double snapX = stepNumber switch
            {
                3 or 4 => HeadParkedX,         // 진입 직전 — 파킹 위치에서 출발
                5      => HeadScanStartX,      // 스캔 시작 직전 — 스캔 시작점 정렬
                6 or 7 => HeadScanEndX,        // 스캔 직후 — 끝 위치 유지
                8 or 9 => HeadScanEndX,        // 파킹 복귀 직전 — 끝에서 출발
                _ => double.NaN
            };
            if (double.IsNaN(snapX)) return;

            // motor 모드는 라이브 모터 위치가 우선이므로 motor 매핑 결과로 덮어씀
            if (_vm != null && _vm.HasPrintRange)
                snapX = MapMotorToHeadPx(_vm.GetLiveMotorX());

            HeadXTransform.X     = snapX;
            HeadLabelTransform.X = snapX;
            SyncNozzleX(snapX);
        }

        // 모터 X(mm) → 헤드 X(px) piecewise 매핑
        // 키포인트: (READY, HeadParkedX) → (PRINT START, HeadScanStartX) → (PRINT END, HeadScanEndX)
        // - 스캔 영역(PRINT START~END) 안: 비례 보간
        // - 스캔 영역 밖 + READY 좌표가 PRINT START의 좌측: READY~PRINT START 구간 매핑
        // - 그 외: 가까운 키포인트 값으로 클램프 (음수/외삽 방지)
        // 호출 전 _vm != null && _vm.HasPrintRange 보장 필요
        private double MapMotorToHeadPx(double motorMm)
        {
            double s = _vm!.PrintStartXmm;
            double e = _vm.PrintEndXmm;

            if (motorMm >= s && motorMm <= e)
            {
                double t = (motorMm - s) / (e - s);
                return Lerp(HeadScanStartX, HeadScanEndX, t);
            }

            // 스캔 영역 밖 + READY가 PRINT START의 좌측에 있는 정상 디자인
            if (_vm.HasReadyMapping && _vm.ReadyXmm < s && motorMm < s)
            {
                double t = Math.Clamp((motorMm - _vm.ReadyXmm) / (s - _vm.ReadyXmm), 0, 1);
                return Lerp(HeadParkedX, HeadScanStartX, t);
            }

            // 안전 클램프
            return motorMm < s ? HeadScanStartX : HeadScanEndX;
        }

        // 스크립트 모드 헤드 X — step 이벤트 시각 기반 (elapsed 무시)
        // step 1-4 : 파킹 (글래스 감지 + 진공 ON + 센서 확인 + 안정화 대기)
        // step 5-6 : 스캔 시작 위치 진입 / step 7 : 스캔 / step 8-9 : 스캔 끝 유지 / step 10+ : 파킹 복귀
        private double ComputeScriptedHeadX(double now)
        {
            if (_currentStepNo < 5) return HeadParkedX;

            if (_currentStepNo < 7)   // 5, 6 — PRINT START 이동 + InPosition
            {
                double start = _stepTimes.TryGetValue(5, out var v5) ? v5 : now;
                double t = EaseOutCubic(Math.Clamp((now - start) / T_HeadPosDur, 0, 1));
                return Lerp(HeadParkedX, HeadScanStartX, t);
            }

            if (_currentStepNo == 7)  // 인쇄 스캔
            {
                double start = _stepTimes.TryGetValue(7, out var v7) ? v7 : now;
                double t = Math.Clamp((now - start) / T_ScanDur, 0, 1);
                return Lerp(HeadScanStartX, HeadScanEndX, t);
            }

            if (_currentStepNo < 10) return HeadScanEndX;   // 8, 9 — 인쇄 완료 후 우측 끝 유지

            // step 10+ : 파킹 복귀
            double start10 = _stepTimes.TryGetValue(10, out var v10) ? v10 : now;
            double tBack = EaseInCubic(Math.Clamp((now - start10) / T_HeadParkDur, 0, 1));
            return Lerp(HeadScanEndX, HeadParkedX, tBack);
        }

        // 한 phase 진행률(0~1) — 시작 전 0, 끝난 뒤 1
        private static double PhaseT(double elapsed, double start, double dur)
        {
            if (elapsed <= start) return 0;
            if (elapsed >= start + dur) return 1;
            return (elapsed - start) / dur;
        }

        // ── 절차적 애니메이션 메인 루프 ─────────────────────────────
        private void OnFrameTick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double elapsed = (now - _animStart).TotalSeconds;

            // [진단] 프레임 간격 스파이크 감지
            if (DiagEnabled && _lastFrameAt != default)
            {
                double frameMs = (now - _lastFrameAt).TotalMilliseconds;
                if (frameMs > FrameSpikeMs)
                    Debug.WriteLine($"[DASH] FRAME SPIKE  +{frameMs:F0}ms  step={_currentStepNo} elapsed={elapsed:F2}s");
            }
            _lastFrameAt = now;

            // ── Glass X (반입: 시작부터 elapsed 기반, 반출: STEP 9(vacuum off) 진입 시점부터) ──
            // 인쇄 프로파일/거리에 따라 STEP 7 길이가 달라지므로 unload 시점을 step 이벤트에 종속시킴
            const int VacuumOffStepNo = 9;
            bool unloadStarted = _stepTimes.TryGetValue(VacuumOffStepNo, out double unloadStart);
            if (!unloadStarted)
            {
                double t = EaseOutCubic(PhaseT(elapsed, T_GlassLoadStart, T_GlassLoadDur));
                GlassTransform.X = Lerp(GlassParkedL, GlassCenter, t);
            }
            else
            {
                double t = EaseInCubic(PhaseT(elapsed, unloadStart, T_GlassUnloadDur));
                GlassTransform.X = Lerp(GlassCenter, GlassParkedR, t);
            }

            // ── Head X ──
            // 우선순위:
            //   (1) HasPrintRange=true → 실제 모터 X(라이브) 매핑
            //   (2) HasPrintRange=false → step 이벤트 기반 스크립트 phase
            //       (스크립트도 step 진입 시각을 기점으로 동작 — elapsed 기준이 아님)
            double headX;
            double liveMotorX = _vm?.GetLiveMotorX() ?? 0.0;
            if (_vm != null && _vm.HasPrintRange)
                headX = MapMotorToHeadPx(liveMotorX);
            else
                headX = ComputeScriptedHeadX(elapsed);
            // [진단] 헤드/모터 점프 감지
            if (DiagEnabled && !double.IsNaN(_lastHeadX))
            {
                double headDelta  = Math.Abs(headX - _lastHeadX);
                double motorDelta = Math.Abs(liveMotorX - _lastMotorX);
                if (headDelta > HeadJumpPx || motorDelta > MotorJumpMm)
                    Debug.WriteLine(
                        $"[DASH] JUMP  head {_lastHeadX:F1}→{headX:F1} (Δ{headDelta:F1}px)  " +
                        $"motor {_lastMotorX:F2}→{liveMotorX:F2} (Δ{motorDelta:F2}mm)  " +
                        $"step={_currentStepNo} elapsed={elapsed:F2}s hasRange={_vm?.HasPrintRange}");
            }
            _lastHeadX  = headX;
            _lastMotorX = liveMotorX;

            HeadXTransform.X     = headX;
            HeadLabelTransform.X = headX;
            SyncNozzleX(headX);
            // GX 표시는 실제 모터 mm 값을 그대로 — bottom MOTOR POSITION X와 일치
            UpdateXDisplayMm(liveMotorX);

            // ── 스캔 라인 / 인쇄 영역 채움 ──
            // 잉크 분사·스캔라인은 인쇄 단계(step 5)에서만 활성. PrintedArea는 단조 증가.
            bool isPrintingNow = _currentStepNo == PrintScanStepNo;
            bool printAlreadyDone = _currentStepNo > PrintScanStepNo;

            if (_vm != null && _vm.HasPrintRange)
            {
                double rangeMm = _vm.PrintEndXmm - _vm.PrintStartXmm;
                double t = Math.Clamp((liveMotorX - _vm.PrintStartXmm) / rangeMm, 0, 1);

                // PrintedArea: 한 번 채워진 부분은 모터가 돌아와도 유지
                if (printAlreadyDone) _maxScanT = 1.0;       // 인쇄 단계 지나갔으면 100%로 잠금
                else if (isPrintingNow) _maxScanT = Math.Max(_maxScanT, t);
                PrintedAreaScale.ScaleX = _maxScanT;

                // 스캔 라인 + 잉크 분사: 인쇄 진행 중에만
                if (isPrintingNow)
                {
                    ScanLineTransform.X = t * PrintAreaMaxW;
                    bool inScanRange = t > 0.001 && t < 0.999;
                    ScanLine.Opacity = inScanRange ? 1.0 : 0.0;
                    _isScanning = inScanRange;
                }
                else
                {
                    ScanLine.Opacity = 0;
                    _isScanning = false;
                }
            }
            else
            {
                // 스크립트 폴백 — step 이벤트 기반 (head X와 동일한 기준)
                if (printAlreadyDone)
                {
                    PrintedAreaScale.ScaleX = 1.0;
                    ScanLineTransform.X     = PrintAreaMaxW;
                    ScanLine.Opacity        = 0;
                    _isScanning = false;
                }
                else if (isPrintingNow && _stepTimes.TryGetValue(PrintScanStepNo, out var t5))
                {
                    double t = Math.Clamp((elapsed - t5) / T_ScanDur, 0, 1);
                    PrintedAreaScale.ScaleX = t;
                    ScanLineTransform.X     = t * PrintAreaMaxW;
                    ScanLine.Opacity        = 1.0;
                    _isScanning = true;
                }
                else
                {
                    ScanLine.Opacity = 0;
                    _isScanning = false;
                }
            }

            // ── 파티클 업데이트 + 분사 ──
            UpdateParticles();
            if (_isScanning && (++_particleFrameSkip % 2 == 0))
                SpawnInkDrops();   // 30Hz로 throttle — GC 빈도 절반

            // 사이클 종료는 ViewModel의 AutoPrintCompleted/Aborted 이벤트 → StopAnimation()에서 처리.
            // 고정 시간(T_TotalCycle) 게이트는 사용하지 않음 — READY/PRINT 좌표 거리에 따라
            // 시퀀스 길이가 달라지므로 시각도 시퀀스 완료에 종속시킴.
        }

        // ── 파티클 시스템 ──────────────────────────────────────────
        private void UpdateParticles()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                Canvas.SetTop(p, Canvas.GetTop(p) + 2.2);
                p.Opacity -= 0.04;
                if (p.Opacity <= 0 || Canvas.GetTop(p) > 285)
                {
                    MainCanvas.Children.Remove(p);
                    _particles.RemoveAt(i);
                }
            }
        }

        private void SpawnInkDrops()
        {
            double headCenterX = NozzleCenterX + HeadXTransform.X;

            int count = _rng.Next(3, 6);
            for (int k = 0; k < count; k++)
            {
                double x = headCenterX + _rng.NextDouble() * 50 - 25;
                double y = NozzleBaseY + _rng.NextDouble() * 4;
                if (x < 133 || x > 667) continue;

                byte alpha = (byte)_rng.Next(140, 210);
                var drop = new Ellipse
                {
                    Width = _rng.Next(2, 5),
                    Height = _rng.Next(3, 6),
                    Fill = new SolidColorBrush(Color.FromArgb(alpha, 80, 50, 220)),
                    Opacity = 0.95
                };
                Canvas.SetLeft(drop, x);
                Canvas.SetTop(drop, y);
                MainCanvas.Children.Add(drop);
                _particles.Add(drop);
            }
        }

        // ── 위치·상태 표시 ─────────────────────────────────────────
        // 실제 모터 X mm 값을 그대로 표시 (bottom MOTOR POSITION 패널과 동일 소스)
        private void UpdateXDisplayMm(double motorMm)
        {
            YPosText.Text = $"GX : {motorMm,8:F3} mm";
        }

        private void SetStatus(string text, string hexColor)
        {
            StatusText.Text = text;
            StatusText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hexColor));
        }

        // ── 변환 초기화 ────────────────────────────────────────────
        private void ResetTransforms()
        {
            GlassTransform.X        = GlassParkedL;
            HeadXTransform.X        = HeadParkedX;
            HeadLabelTransform.X    = HeadParkedX;
            PrintedAreaScale.ScaleX = 0;
            ScanLineTransform.X     = 0;
            ScanLine.Opacity        = 0;
            SyncNozzleX(HeadParkedX);
        }

        // ── ViewModel.AutoPrintStarted ──────────────────────────────
        private void OnAutoPrintStarted()
        {
            Dispatcher.Invoke(() =>
            {
                _isAnimating    = true;
                _isScanning     = false;
                _currentStepNo  = 0;
                _maxScanT       = 0;
                _stepTimes.Clear();
                UnhookRendering();

                ResetTransforms();
                SetStatus("▶  STARTING ...", "#38BDF8");
                YPosText.Text = "GX :    0.000 mm";

                _animStart    = DateTime.Now;
                _lastFrameAt  = default;
                _lastHeadX    = double.NaN;
                _lastMotorX   = double.NaN;
                if (DiagEnabled) Debug.WriteLine("[DASH] === AUTO PRINT STARTED ===");
                HookRendering();
            });
        }

        // ── ViewModel.AutoPrintStepChanged — 상태 텍스트 + step 번호 추적 ──
        private void OnStepChanged(int stepNumber)
        {
            if (!_isAnimating) return;
            Dispatcher.Invoke(() =>
            {
                double elapsedAtStep = (DateTime.Now - _animStart).TotalSeconds;
                bool isReentry = _stepTimes.ContainsKey(stepNumber);
                _currentStepNo = stepNumber;
                // step 진입 시각은 사이클당 1회만 기록 — 알람 일시정지 후 재개로 인한
                // 재실행 시 시각이 덮어써져 스크립트 phase가 처음부터 재시작되는 문제 방지
                if (!isReentry)
                    _stepTimes[stepNumber] = elapsedAtStep;

                if (DiagEnabled)
                {
                    double motorX = _vm?.GetLiveMotorX() ?? 0.0;
                    Debug.WriteLine(
                        $"[DASH] STEP {stepNumber}{(isReentry ? " (RETRY)" : "")}  " +
                        $"elapsed={elapsedAtStep:F2}s  motor={motorX:F2}mm  " +
                        $"hasRange={_vm?.HasPrintRange} (start={_vm?.PrintStartXmm:F2} end={_vm?.PrintEndXmm:F2})");
                }

                // 디스패처 지연으로 OnFrameTick의 첫 반영이 지연되면 head가 점프해 보임 →
                // step 전환 시각의 기대 위치를 즉시 스냅해 첫 프레임의 시작점을 정렬
                SnapHeadForStep(stepNumber);

                switch (stepNumber)
                {
                    case 1: SetStatus("▶  LOADING  ·  GLASS SUBSTRATE ENTERING ...", "#38BDF8"); break;
                    case 2: SetStatus("⊙  VACUUM ON  ·  GLASS CLAMPED", "#22C55E"); break;
                    case 3:
                    case 4: SetStatus("⬇  PRINT HEAD  ·  POSITIONING TO SCAN START", "#60A5FA"); break;
                    case 5:
                    case 6: SetStatus("◉  PRINTING  ·  INKJET PRINTING IN PROGRESS ...", "#A78BFA"); break;
                    case 7: SetStatus("⊘  VACUUM OFF  ·  GLASS RELEASED", "#F59E0B"); break;
                    case 8:
                    case 9: SetStatus("◀  UNLOADING  ·  HEAD PARK + GLASS EXITING ...", "#38BDF8"); break;
                }
            });
        }

        // ── STOP / Completion ──────────────────────────────────────
        private void StopAnimation()
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isAnimating) return;
                _isAnimating = false;
                _isScanning  = false;
                UnhookRendering();

                foreach (var p in _particles)
                    MainCanvas.Children.Remove(p);
                _particles.Clear();

                SetStatus("⏹  STOPPED  ·  PRESS START TO RESTART", "#F59E0B");
            });
        }

        // ── 알람 팝업 테스트 ────────────────────────────────────────
        private void OpenAlarm_Click(object sender, RoutedEventArgs e)
        {
            // Application.Current.MainWindow 는 로그인 시점에 등록된 LoginWindow 일 수 있어
            // Windows 컬렉션에서 실제 MainWindow 타입을 직접 찾는다
            var mainWin = System.Windows.Application.Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();
            
            if (mainWin?.DataContext is MainViewModel mainVM)
                mainVM.AlarmVM.RaiseAlarm("SNS-EMO");
        }
    }
}
