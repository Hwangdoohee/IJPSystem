using IJPSystem.Platform.Application.Sequences;
using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Interfaces;
using static IJPSystem.Platform.HMI.Common.Loc;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class MainDashboardViewModel : ViewModelBase
    {
        // ── 기존 필드 유지 ──
        private double _tactTime;
        private string _currentStepName = "IDLE";
        private double _processProgress;
        private DispatcherTimer _procTimer;
        private readonly Action<string, LogLevel> _logAction;
        private readonly Action<bool> _onAlarmChanged;
        private readonly Action<string>? _raiseAlarm;
        // (pointName, axisName) → mm. 활성 레시피의 티칭 좌표 조회용.
        private readonly Func<string, string, double?>? _getPointAxisMm;
        // 미해제 알람 존재 여부 — 시작 전 체크용
        private readonly Func<bool>? _hasActiveAlarm;

        private readonly IMachine _machine;
        private readonly IMotionService _motion;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource? _stepCts;   // 스텝 단위 취소 (일시정지 시 사용)

        // AUTO PRINT 절차 — 우측 패널의 단계별 진행 표시용
        public ObservableCollection<SequenceStep> Steps { get; } = new();

        // 활성 레시피의 X축 티칭 좌표(mm) — 시퀀스 시작 시 캐싱.
        // View는 이 값과 GetLiveMotorX()로 헤드 픽셀 위치를 piecewise 매핑한다:
        //   motor ∈ [Ready, PrintStart] → head ∈ [HeadParkedX, HeadScanStartX]   (파킹↔스캔시작)
        //   motor ∈ [PrintStart, PrintEnd] → head ∈ [HeadScanStartX, HeadScanEndX] (스캔)
        public double ReadyXmm      { get; private set; } = double.NaN;
        public double PrintStartXmm { get; private set; } = double.NaN;
        public double PrintEndXmm   { get; private set; } = double.NaN;
        public bool   HasPrintRange => !double.IsNaN(PrintStartXmm)
                                    && !double.IsNaN(PrintEndXmm)
                                    && Math.Abs(PrintEndXmm - PrintStartXmm) > 0.001;
        public bool   HasReadyMapping => !double.IsNaN(ReadyXmm)
                                      && !double.IsNaN(PrintStartXmm)
                                      && Math.Abs(PrintStartXmm - ReadyXmm) > 0.001;

        // View 60fps 프레임 타이머용 — MotorXPosition 캐시(100ms 주기)와 별도로 매 호출마다 실측치 반환.
        // 모터 드라이버 자체는 50ms 시뮬레이션이므로, 60fps 호출 시 픽셀 동기는 사실상 부드러워짐.
        // 인쇄 속도 vs Move 속도 차이도 자연스럽게 헤드 속도에 반영됨 (모터 vel을 그대로 따름).
        public double GetLiveMotorX() => _machine.Motion?.GetActualPosition("X") ?? 0.0;

        private void CachePrintRange()
        {
            ReadyXmm      = _getPointAxisMm?.Invoke(PointNames.Ready,      "X") ?? double.NaN;
            PrintStartXmm = _getPointAxisMm?.Invoke(PointNames.PrintStart, "X") ?? double.NaN;
            PrintEndXmm   = _getPointAxisMm?.Invoke(PointNames.PrintEnd,   "X") ?? double.NaN;
            OnPropertyChanged(nameof(ReadyXmm));
            OnPropertyChanged(nameof(PrintStartXmm));
            OnPropertyChanged(nameof(PrintEndXmm));
            OnPropertyChanged(nameof(HasPrintRange));
            OnPropertyChanged(nameof(HasReadyMapping));
        }

        // ── RECENT CYCLES — 최근 사이클 통계 (최대 10개 유지) ──
        private const int MaxCycleHistory = 10;
        public ObservableCollection<CycleRecord> RecentCycles { get; } = new();

        public double MaxTact     => RecentCycles.Count == 0 ? 1.0 : RecentCycles.Max(c => c.TactTime);
        public double MinTact     => RecentCycles.Count == 0 ? 0.0 : RecentCycles.Min(c => c.TactTime);
        public double AverageTact => RecentCycles.Count == 0 ? 0.0 : RecentCycles.Average(c => c.TactTime);

        private void RegisterCycle(double tactSeconds)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                RecentCycles.Insert(0, new CycleRecord
                {
                    Number      = TotalCount,   // 누적 생산 카운트와 일치 — 큐에서 빠진 번호와 충돌 없음
                    TactTime    = tactSeconds,
                    CompletedAt = DateTime.Now,
                });
                while (RecentCycles.Count > MaxCycleHistory)
                    RecentCycles.RemoveAt(RecentCycles.Count - 1);

                OnPropertyChanged(nameof(MaxTact));
                OnPropertyChanged(nameof(MinTact));
                OnPropertyChanged(nameof(AverageTact));
            });
        }

        // 알람/STOP 일시정지 게이트 — 폴링 방식으로 OCE 없이 대기
        // - 알람: 모터 즉시 정지 + step 취소 → 재개 시 같은 step 재실행
        // - STOP: 현재 step은 그대로 완료, 다음 step 진입 전에 정지 → START로 재개 시 다음 step부터 진행
        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    // START는 (가동 중이 아닐 때) 또는 (가동 중이고 일시정지 상태일 때 재개용으로) 활성화
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand  as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // MainViewModel이 AlarmVM.HasActiveAlarm 변경을 감지하면 호출
        public void OnAlarmActiveChanged(bool isAlarmActive)
        {
            if (!IsRunning) return;

            if (isAlarmActive && !IsPaused)
            {
                IsPaused = true;
                StopAllMotion();        // 진행 중인 Move를 즉시 정지
                _stepCts?.Cancel();     // 진행 중인 스텝의 await도 즉시 깨움 → 외부 루프가 게이트에서 대기 후 재실행
                _logAction?.Invoke(T("Log_AutoPrintAlarmPause"), LogLevel.Warning);
            }
            else if (!isAlarmActive && IsPaused)
            {
                IsPaused = false;
                _logAction?.Invoke(T("Log_AutoPrintAlarmResume"), LogLevel.Info);
            }
        }

        // 전체 축 즉시 정지 (감속 정지)
        private void StopAllMotion()
        {
            try
            {
                var allAxes = _machine.Motion?.GetAllStatus();
                if (allAxes == null) return;
                foreach (var ax in allAxes)
                    _ = _machine.Motion!.Stop(ax.AxisNo);
            }
            catch (Exception ex)
            {
                _logAction?.Invoke(T("Log_AutoPrintStopMotionError", ex.Message), LogLevel.Error);
            }
        }

        private string _selectedRecipeName = "None";
        public string SelectedRecipe
        {
            get => _selectedRecipeName;
            set => SetProperty(ref _selectedRecipeName, value);
        }
        
        #region Properties (기존 유지)
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public double TactTime
        {
            get => _tactTime;
            set => SetProperty(ref _tactTime, value);
        }

        public string CurrentStepName
        {
            get => _currentStepName;
            set => SetProperty(ref _currentStepName, value);
        }

        public double ProcessProgress
        {
            get => _processProgress;
            set => SetProperty(ref _processProgress, value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand  as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private string _activeRecipeName = string.Empty;
        public string ActiveRecipeName
        {
            get => _activeRecipeName;
            set => SetProperty(ref _activeRecipeName, value);
        }

        #endregion

        #region Commands
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ResetCommand { get; }

        public ICommand OpenDoorCommand { get; }
        public ICommand CloseDoorCommand { get; }
        public ICommand VacuumOnCommand { get; }
        public ICommand VacuumOffCommand { get; }
        #endregion

        #region View 동기화 이벤트
        // View가 구독해서 시각 애니메이션을 시작/정지/완료 처리.
        // 시각 사이클 길이 = 시퀀스 길이 (View는 AutoPrintCompleted/Aborted에서 렌더링 종료).
        public event Action? AutoPrintStarted;
        public event Action? AutoPrintAborted;
        public event Action? AutoPrintCompleted;
        // 각 스텝 진입 시점에 호출 (View가 스텝 번호로 매칭하여 해당 phase 처리)
        public event Action<int>? AutoPrintStepChanged;
        #endregion

        #region 추가 상태 프로퍼티

        private bool _isDoorLocked;
        public bool IsDoorLocked
        {
            get => _isDoorLocked;
            set => SetProperty(ref _isDoorLocked, value);
        }

        private bool _isVacuumOn;
        public bool IsVacuumOn
        {
            get => _isVacuumOn;
            set => SetProperty(ref _isVacuumOn, value);
        }

        private bool _isGlassDetected;
        public bool IsGlassDetected
        {
            get => _isGlassDetected;
            set => SetProperty(ref _isGlassDetected, value);
        }

        private bool _isEmoActive;
        public bool IsEmoActive
        {
            get => _isEmoActive;
            set => SetProperty(ref _isEmoActive, value);
        }

        // ── 모터 위치 (X / Y / Z / Q) ──
        private double _motorXPosition;
        public double MotorXPosition
        {
            get => _motorXPosition;
            set => SetProperty(ref _motorXPosition, value);
        }

        private double _motorYPosition;
        public double MotorYPosition
        {
            get => _motorYPosition;
            set => SetProperty(ref _motorYPosition, value);
        }

        private double _motorZPosition;
        public double MotorZPosition
        {
            get => _motorZPosition;
            set => SetProperty(ref _motorZPosition, value);
        }

        private double _motorQPosition;
        public double MotorQPosition
        {
            get => _motorQPosition;
            set => SetProperty(ref _motorQPosition, value);
        }
        #endregion

        // ── 생성자: IMachine + IMotionService + 알람/티칭 좌표 조회 콜백 ──
        public MainDashboardViewModel(
            Action<string, LogLevel> logAction,
            Action<bool> onAlarmChanged,
            IMachine machine,
            string initialActiveRecipe,
            IMotionService motion,
            Action<string>? raiseAlarm = null,
            Func<string, string, double?>? getPointAxisMm = null,
            Func<bool>? hasActiveAlarm = null)
        {
            _logAction       = logAction;
            _onAlarmChanged  = onAlarmChanged;
            _raiseAlarm      = raiseAlarm;
            _getPointAxisMm  = getPointAxisMm;
            _hasActiveAlarm  = hasActiveAlarm;
            _machine = machine;
            _motion = motion;

            ActiveRecipeName = initialActiveRecipe;

            // START: 정지 상태면 새 시퀀스 시작, 일시정지 상태면 재개 (남은 step부터 이어서)
            StartCommand = new RelayCommand(async _ =>
            {
                if (IsRunning && IsPaused)
                {
                    IsPaused = false;
                    _logAction?.Invoke(T("Log_AutoPrintResume"), LogLevel.Info);
                    return;
                }
                if (!IsRunning)
                    await RunAutoPrintAsync();
            }, _ => !IsRunning || IsPaused);

            // STOP: 즉시 취소가 아니라 일시정지 — 진행 중인 step은 끝까지 마무리되고 다음 step 진입 전에 멈춤
            // 재시작은 START 버튼으로 (재개)
            StopCommand = new RelayCommand(_ =>
            {
                if (IsRunning && !IsPaused)
                {
                    IsPaused = true;
                    _logAction?.Invoke(T("Log_AutoPrintStopPause"), LogLevel.Warning);
                }
            }, _ => IsRunning && !IsPaused);

            ResetCommand = new RelayCommand(_ =>
            {
                IsError = false;
                _onAlarmChanged?.Invoke(false);
                _machine.SetSystemStatus(MachineState.Standby);
                _logAction?.Invoke(T("Log_ErrorReset"), LogLevel.Info);
            });

            // ── 추가 커맨드 ──
            OpenDoorCommand = new RelayCommand(_ =>
            {
                // 가동 중 도어 오픈 차단
                if (IsRunning)
                {
                    _logAction?.Invoke(T("Log_DoorOpenBlocked"), LogLevel.Warning);
                    return;
                }
                _machine.OpenDoor();
                IsDoorLocked = false;
                _logAction?.Invoke(T("Log_DoorOpen"), LogLevel.Info);
            });

            CloseDoorCommand = new RelayCommand(_ =>
            {
                _machine.CloseDoor();
                IsDoorLocked = true;
                _logAction?.Invoke(T("Log_DoorClose"), LogLevel.Info);
            });

            VacuumOnCommand = new RelayCommand(_ =>
            {
                _machine.VacuumOn();
                IsVacuumOn = true;
                _logAction?.Invoke(T("Log_VacuumOn"), LogLevel.Info);
            });

            VacuumOffCommand = new RelayCommand(_ =>
            {
                _machine.VacuumOff();
                IsVacuumOn = false;
                _logAction?.Invoke(T("Log_VacuumOff"), LogLevel.Info);
            });

            // 시작 전에도 절차 미리 표시
            BuildSteps();

            // 100ms 주기 센서 폴링 (타이머의 유일한 책임)
            _procTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _procTimer.Tick += (_, _) => UpdateSensorStatus();
            _procTimer.Start();
        }

        // 시퀀스 시작 직전 호출 — Steps 컬렉션을 sequence 정의로 다시 채우고 모두 Waiting으로 초기화.
        // def.Name 은 번역 키 (Step_AutoPrint_N) → 현재 언어로 변환해서 표시.
        // NameKey 도 보존해 언어 변경 시 RefreshStepNames() 로 재번역.
        private void BuildSteps()
        {
            Steps.Clear();
            foreach (var def in AutoPrintSequence.Build(_machine, _motion))
            {
                Steps.Add(new SequenceStep
                {
                    Number  = def.Number,
                    NameKey = def.Name,
                    Name    = Common.Loc.T(def.Name),
                    Action  = def.Action,
                });
            }
        }

        /// <summary>언어 변경 시 호출 — 진행 중이어도 표시명만 갱신</summary>
        public void RefreshStepNames()
        {
            foreach (var s in Steps)
                s.Name = Common.Loc.T(s.NameKey);
        }

        // 예외 발생 시 현재 Running 스텝의 상태를 갱신
        private void MarkRunningStepAs(StepStatus status)
        {
            var running = Steps.FirstOrDefault(s => s.Status == StepStatus.Running);
            if (running != null) running.Status = status;
        }

        /// <summary>
        /// AUTO PRINT 시퀀스 실행 — Application/Sequences/AutoPrintSequence와 연결
        /// </summary>
        private async Task RunAutoPrintAsync()
        {
            // 진행 중에는 CanExecute에서 차단됨 — 사전 조건만 확인
            if (!CheckSafetyBeforeStart()) return;

            IsRunning  = true;
            IsError    = false;
            ProcessProgress = 0;
            CurrentStepName = "STARTING";
            CachePrintRange();   // 시각 헤드 X 매핑용 좌표 캐싱
            _machine.SetSystemStatus(MachineState.Running);
            _logAction?.Invoke(T("Log_Start"), LogLevel.Success);

            // View 시각 애니메이션 시작 — 시퀀스 길이 = 시각 사이클 길이
            AutoPrintStarted?.Invoke();

            _cts = new CancellationTokenSource();
            var startTime = DateTime.Now;
            bool success = false;

            try
            {
                BuildSteps();
                int total = Steps.Count;

                for (int i = 0; i < Steps.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var step = Steps[i];
                    bool stepCompleted = false;

                    // 일시정지로 인해 스텝이 중단되면 IsPaused가 풀린 후 같은 스텝을 재실행
                    while (!stepCompleted)
                    {
                        // 폴링 대기 — OCE 없이 IsPaused 또는 cts 취소를 감지
                        if (IsPaused)
                        {
                            CurrentStepName = $"[{step.Number}/{total}] {step.Name}  (PAUSED)";
                            while (IsPaused && !_cts.Token.IsCancellationRequested)
                                await Task.Delay(100);
                        }
                        _cts.Token.ThrowIfCancellationRequested();   // STOP시 외부 catch로

                        CurrentStepName = $"[{step.Number}/{total}] {step.Name}";
                        ProcessProgress = (double)i / total * 100;
                        AutoPrintStepChanged?.Invoke(step.Number);   // View 애니메이션 sync

                        step.Status  = StepStatus.Running;
                        step.Elapsed = "-";

                        _stepCts?.Dispose();
                        _stepCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            await step.Action(_stepCts.Token);
                            sw.Stop();
                            step.Elapsed = $"{sw.Elapsed.TotalSeconds:F1}s";
                            step.Status  = StepStatus.Done;
                            stepCompleted = true;
                        }
                        catch (OperationCanceledException)
                        {
                            // STOP을 누른 경우(메인 cts 취소)는 외부 catch로 던짐
                            _cts.Token.ThrowIfCancellationRequested();
                            // 그 외에는 알람 일시정지로 인한 취소 — 재개 후 같은 스텝 재시도
                            step.Status = StepStatus.Aborted;
                            _logAction?.Invoke(T("Log_AutoPrintStepAborted", step.Number), LogLevel.Warning);
                        }
                    }
                }

                // 모든 step 완료 = 시퀀스 사이클 종료 (시각 애니메이션도 여기서 같이 정지됨)
                ProcessProgress = 100;
                CurrentStepName = "COMPLETED";
                TotalCount++;
                TactTime = Math.Round((DateTime.Now - startTime).TotalSeconds, 1);
                _machine.SetSystemStatus(MachineState.Standby);
                RegisterCycle(TactTime);   // RECENT CYCLES 통계에 등록
                _logAction?.Invoke(T("Log_AutoPrintCompleted", TactTime), LogLevel.Success);
                success = true;
            }
            catch (OperationCanceledException)
            {
                MarkRunningStepAs(StepStatus.Aborted);
                CurrentStepName = "ABORTED";
                _machine.SetSystemStatus(MachineState.Standby);
            }
            catch (TimeoutException ex)
            {
                MarkRunningStepAs(StepStatus.Failed);
                IsError = true;
                CurrentStepName = "TIMEOUT";
                _machine.SetSystemStatus(MachineState.Alarm);
                _logAction?.Invoke(T("Log_AutoPrintTimeoutMsg", ex.Message), LogLevel.Error);
                _raiseAlarm?.Invoke("SEQ-MOTION-TIMEOUT"); 
            }
            catch (Exception ex)
            {
                MarkRunningStepAs(StepStatus.Failed);
                IsError = true;
                CurrentStepName = "ERROR";
                _machine.SetSystemStatus(MachineState.Alarm);
                _logAction?.Invoke(T("Log_AutoPrintFailureMsg", ex.Message), LogLevel.Error);
                _raiseAlarm?.Invoke("SEQ-AUTO-PRINT-FAIL");  
            }
            finally
            {
                IsRunning = false;
                IsPaused  = false;     // 게이트 열기 (다음 런 대비)
                IsVacuumOn = _machine.IsGlassDetected();
                _stepCts?.Dispose();
                _stepCts = null;
                _cts?.Dispose();
                _cts = null;

                // View 데모 애니메이션 종료 신호
                if (success) AutoPrintCompleted?.Invoke();
                else         AutoPrintAborted?.Invoke();
            }
        }

        /// <summary>
        /// 가동 전 사전 조건 체크 (디버그/릴리즈 공통)
        /// - 모든 축이 원점복귀 완료 상태 (= INITIAL 시퀀스 수행 완료)
        /// - 모든 축이 서보 ON 상태
        /// </summary>
        private bool CheckPrerequisites()
        {
            // 미해제 알람이 있으면 시퀀스 시작 거부 (알람 클리어 후 재시도)
            if (_hasActiveAlarm?.Invoke() == true)
            {
                _logAction?.Invoke("[AUTO PRINT] 미해제 알람 존재 — 시작 거부", LogLevel.Warning);
                System.Windows.MessageBox.Show(
                    "미해제 알람이 있습니다.\n알람 화면에서 알람을 모두 해제(Clear)한 뒤 다시 시도하세요.",
                    "AUTO PRINT 시작 불가",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            var allAxes = _machine.Motion?.GetAllStatus();
            if (allAxes == null || allAxes.Count == 0)
            {
                _logAction?.Invoke(T("Log_AxisInfoMissing"), LogLevel.Error);
                return false;
            }

            // INITIAL 시퀀스 수행 여부 (전체 축 원점복귀 완료)
            var notHomed = allAxes.Where(ax => !ax.IsHomeDone)
                                  .Select(ax => ax.AxisNo).ToList();
            if (notHomed.Count > 0)
            {
                string msg = T("Log_PrereqNotHomed", string.Join(", ", notHomed));
                _logAction?.Invoke(msg.Replace("\n\n", " — "), LogLevel.Error);
                System.Windows.MessageBox.Show(msg, T("Log_PrereqDialogTitle"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            // 서보 ON 확인
            var notServoOn = allAxes.Where(ax => !ax.IsServoOn)
                                    .Select(ax => ax.AxisNo).ToList();
            if (notServoOn.Count > 0)
            {
                string msg = T("Log_PrereqNotServoOn", string.Join(", ", notServoOn));
                _logAction?.Invoke(msg.Replace("\n\n", " — "), LogLevel.Error);
                System.Windows.MessageBox.Show(msg, T("Log_PrereqDialogTitle"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 가동 전 안전 조건 체크
        /// </summary>
        private bool CheckSafetyBeforeStart()
        {
            // 사전 조건은 디버그/릴리즈 무관하게 항상 체크
            if (!CheckPrerequisites()) return false;

        #if DEBUG
                    // 개발 중에는 안전 조건 무시하고 바로 통과
            _logAction?.Invoke(T("Log_SafetyBypass"), LogLevel.Warning);
            return true;
        #else
            // EMO 감지
            if (_machine.IsEmoActive())
            {
                _machine.SetSystemStatus(MachineState.Emergency);
                _onAlarmChanged?.Invoke(true);
                _logAction?.Invoke(T("Log_EmoDetected"), LogLevel.Fatal);
                _raiseAlarm?.Invoke("SNS-EMO");
                return false;
            }

            // 도어 잠금 확인
            if (!_machine.IsDoorLocked())
            {
                _machine.SetSystemStatus(MachineState.Alarm);
                _onAlarmChanged?.Invoke(true);
                _logAction?.Invoke(T("Log_DoorLockFail"), LogLevel.Error);
                _raiseAlarm?.Invoke("SNS-DOOR-OPEN");
                return false;
            }

            // 압력 스위치 확인 (1번~3번)
            for (int i = 1; i <= 3; i++)
            {
                if (!_machine.IsPressureOk(i))
                {
                    _onAlarmChanged?.Invoke(true);
                    _logAction?.Invoke(T("Log_PressureFail", i), LogLevel.Error);
                    _raiseAlarm?.Invoke("SNS-PRESSURE-NG");
                    return false;
                }
            }

            return true;
        #endif
        }

        /// <summary>
        /// 센서 상태 주기적 업데이트 (SlowTimer에서 호출 또는 내부 타이머 활용)
        /// </summary>
        public void UpdateSensorStatus()
        {
            if (_machine == null) return;

            IsGlassDetected = _machine.IsGlassDetected();
            IsDoorLocked = _machine.IsDoorLocked();
            IsEmoActive = _machine.IsEmoActive();

            // 모터 위치 갱신 — HMI는 X/Y/Z/Q로 표기, 모션 드라이버는 회전축을 "T"로 사용
            if (_machine.Motion != null)
            {
                MotorXPosition = _machine.Motion.GetActualPosition("X");
                MotorYPosition = _machine.Motion.GetActualPosition("Y");
                MotorZPosition = _machine.Motion.GetActualPosition("Z");
                MotorQPosition = _machine.Motion.GetActualPosition("T");
            }

            // EMO 실시간 감지 — 시퀀스 진행 중이면 즉시 취소
            if (IsEmoActive && IsRunning)
            {
                _cts?.Cancel();
                _machine.VacuumOff();
                _machine.SetSystemStatus(MachineState.Emergency);
                _onAlarmChanged?.Invoke(true);
                _logAction?.Invoke(T("Log_EmoStopped"), LogLevel.Fatal);
                _raiseAlarm?.Invoke("SNS-EMO");
            }
        }

    }

    /// <summary>완료된 한 사이클의 통계 레코드 — RECENT CYCLES 패널에 바인딩</summary>
    public class CycleRecord
    {
        public int      Number      { get; init; }
        public double   TactTime    { get; init; }
        public DateTime CompletedAt { get; init; }
        public string   Label       => $"#{Number}";
        public string   TimeText    => $"{TactTime:F2}s";
    }
}