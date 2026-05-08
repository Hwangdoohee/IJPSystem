using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.IO;
using IJPSystem.Platform.Domain.Models.Motion; // AxisDeviceInfo 위치
using IJPSystem.Platform.HMI.Common;
using System;
using System.Windows;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class AxisViewModel : ViewModelBase
    {
        private readonly IMotionDriver _driver;
        private readonly MainViewModel _mainViewModel; 

        #region Properties (Config & Status)
        public AxisDeviceInfo Info { get; private set; }

        private double _targetPosition;
        public double TargetPosition
        {
            get => _targetPosition;
            set => SetProperty(ref _targetPosition, value);
        }
        // 3. 실시간 하드웨어 상태 (필드와 속성)
        private double _currentPos;
        public double CurrentPos 
        { 
            get => _currentPos;
            set
            {
                // 값이 거의 차이가 없으면 업데이트를 건너뜀 (Deadband 설정)
                if (Math.Abs(_currentPos - value) < 0.001) return;
                SetProperty(ref _currentPos, value);
            }
        }

        private bool _isServoOn;
        public bool IsServoOn
        {
            get => _isServoOn;
            set => SetProperty(ref _isServoOn, value); 
        }

        // AxisViewModel.cs에 아래 속성들을 추가하세요.
        private bool _isHomeDone;
        public bool IsHomeDone
        {
            get => _isHomeDone;
            set => SetProperty(ref _isHomeDone, value);
        }

        private bool _isInPosition;
        public bool IsInPosition
        {
            get => _isInPosition;
            set => SetProperty(ref _isInPosition, value);
        }

        private bool _isAlarm;
        public bool IsAlarm 
        { 
            get => _isAlarm; 
            set => SetProperty(ref _isAlarm, value); 
        }

        private bool _isMoving;
        public bool IsMoving 
        { 
            get => _isMoving; 
            set => SetProperty(ref _isMoving, value); 
        }
        private bool _isAbsMode = true; // 기본값: 절대좌표(ABS)
        public bool IsAbsMode
        {
            get => _isAbsMode;
            set
            {
                if (SetProperty(ref _isAbsMode, value))
                {
                    // 한쪽이 바뀌면 반대쪽(IsIncMode)도 바뀌었다고 UI에 알림
                    OnPropertyChanged(nameof(IsIncMode));
                }
            }
        }
        public bool IsUnitContinuity
        {
            get => JogUnit == 0;
            set { if (value) JogUnit = 0; }
        }

        public bool IsUnit10um
        {
            get => JogUnit == 0.01;
            set { if (value) JogUnit = 0.01; }
        }

        public bool IsUnit100um
        {
            get => JogUnit == 0.1;
            set { if (value) JogUnit = 0.1; }
        }

        private double _jogUnit = 0;
        public double JogUnit
        {
            get => _jogUnit;
            set
            {
                if (SetProperty(ref _jogUnit, value))
                {
                    OnPropertyChanged(nameof(IsUnitContinuity));
                    OnPropertyChanged(nameof(IsUnit10um));
                    OnPropertyChanged(nameof(IsUnit100um));
                    OnPropertyChanged(nameof(IsJogContinuity)); 
                }
            }
        }

        private bool _cwLimit;
        public bool CwLimit
        {
            get => _cwLimit;
            set => SetProperty(ref _cwLimit, value);
        }

        private bool _ccwLimit;
        public bool CcwLimit
        {
            get => _ccwLimit;
            set => SetProperty(ref _ccwLimit, value);
        }

        private bool _homeSensor;
        public bool HomeSensor
        {
            get => _homeSensor;
            set => SetProperty(ref _homeSensor, value);
        }

        public bool IsJogContinuity
        {
            get => JogUnit == 0;
            set
            {if (value) JogUnit = 0;}
        }

        public bool IsIncMode
        {
            get => !_isAbsMode;
            set => IsAbsMode = !value;
        }
        
        private AxisStatus? _status;
        public AxisStatus? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        #endregion

        #region Commands
        public ICommand MoveAbsCommand { get; }
        public ICommand JogForwardCommand { get; }
        public ICommand JogBackwardCommand { get; }
        public ICommand ServoCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand HomeCommand { get; }

        #endregion

        public AxisViewModel(IMotionDriver driver, AxisDeviceInfo info, MainViewModel mainViewModel)
        {
            _driver = driver;
            Info = info;
            _mainViewModel = mainViewModel;
            Status = new AxisStatus { AxisNo = info.AxisNo, Name = info.Name, Unit = info.Unit };
            

            // 커맨드 연결 (메서드를 참조하도록 수정)
            MoveAbsCommand = new RelayCommand(async _ => await MoveAsync());
            JogForwardCommand = new RelayCommand(async _ => await JogMoveAsync(true));
            JogBackwardCommand = new RelayCommand(async _ => await JogMoveAsync(false));
            ServoCommand = new RelayCommand(async _ => await ServoOnOffAsync());
            StopCommand = new RelayCommand(async _ => await StopAsync());
            HomeCommand = new RelayCommand(async _ => await HomeAsync());
        }

        public async Task ServoOnOffAsync()
        {
            if (Status == null) return;

            bool currentState = Status.IsServoOn;
            bool targetState = !currentState;
            string action = targetState ? "ON" : "OFF";
            _mainViewModel.AddLog($"[MOTION] {Info.Name} (Axis:{Info.AxisNo}) Servo {action}");

            await _driver.ServoOn(Info.AxisNo, targetState);

            Status.IsServoOn = targetState;
            OnPropertyChanged(nameof(Status));
        }

        public async Task ForceServoOnAsync()
        {
            if (Status == null) return;
            _mainViewModel.AddLog($"[MOTION] {Info.Name} Servo ON");
            await _driver.ServoOn(Info.AxisNo, true);
            Status.IsServoOn = true;
            OnPropertyChanged(nameof(Status));
        }

        public async Task ForceServoOffAsync()
        {
            if (Status == null) return;
            _mainViewModel.AddLog($"[MOTION] {Info.Name} Servo OFF");
            await _driver.ServoOn(Info.AxisNo, false);
            Status.IsServoOn = false;
            OnPropertyChanged(nameof(Status));
        }

        // 2. 절대 위치 이동 메서드
        // profileKind   : 사용할 속도/가감속 프로파일 (기본 Move). 인쇄 시 Printing 지정.
        // profileOverride: 적용된 레시피 snapshot의 프로파일 등을 외부에서 명시적으로 지정. 시퀀스 호출 시 사용.
        //                 null이면 Info.MotionConfig (편집 중인 값) 사용.
        public async Task MoveAsync(MotionProfileKind profileKind = MotionProfileKind.Move,
                                    Profile? profileOverride = null)
        {
            if (string.IsNullOrEmpty(Info.AxisNo))
            {
                MessageBox.Show("숫자를 입력하세요");
                return;
            }
            string modeName = IsAbsMode ? "ABS" : "INC";
            _mainViewModel.AddLog($"[MOTION] {Info.Name} -> Start {modeName} Move ({profileKind}): {TargetPosition:F3}mm");

            var profile = profileOverride ?? (profileKind switch
            {
                MotionProfileKind.Printing => Info.MotionConfig.Printing,
                MotionProfileKind.Jog      => Info.MotionConfig.Jog,
                _                          => Info.MotionConfig.Move,
            });

            try
            {
                if (IsAbsMode)
                {
                    // 절대 좌표 이동 (Absolute)
                    await _driver.MoveAbs(Info.AxisNo, TargetPosition,
                                         profile.Velocity, profile.Acceleration, profile.Deceleration);
                }
                else
                {
                    // 상대 좌표 이동 (Incremental / Relative)
                    await _driver.MoveRel(Info.AxisNo, TargetPosition,
                                         profile.Velocity, profile.Acceleration, profile.Deceleration);
                }
            }
            catch (Exception ex)
            {
                _mainViewModel.AddLog($"[MOTION] Move Failed: {ex.Message}", LogLevel.Error);
            }
        }
        private bool _isStepJogging = false;

        public async Task JogMoveAsync(bool isForward, double speedScale = 1.0)
        {
            var profile = Info.MotionConfig.Jog;
            double vel = profile.Velocity * Math.Max(0.01, speedScale);

            if (IsJogContinuity)
            {
                string direction = isForward ? "Forward(+)" : "Backward(-)";
                _mainViewModel.AddLog($"[MOTION] Jog: {Info.Name} {direction} ×{speedScale:F2}");
                await _driver.MoveJog(Info.AxisNo, isForward, vel, profile.Acceleration, profile.Deceleration);
            }
            else
            {
                if (_isStepJogging) return;  // step 이동 중 중복 방지
                _isStepJogging = true;
                try
                {
                    string direction = isForward ? "+" : "-";
                    _mainViewModel.AddLog($"[MOTION] Jog: {Info.Name} Step {direction}{JogUnit:F3}mm ×{speedScale:F2}");
                    double distance = isForward ? JogUnit : -JogUnit;
                    await _driver.MoveRel(Info.AxisNo, distance, vel, profile.Acceleration, profile.Deceleration);
                }
                finally
                {
                    _isStepJogging = false;
                }
            }
        }

        public async Task StopAsync()
        {
            _isStepJogging = false;
            _mainViewModel.AddLog($"[MOTION] {Info.Name} Motor Stop Command.");
            await _driver.Stop(Info.AxisNo);
        }

        // 원점 복귀 메서드
        public async Task HomeAsync()
        {
            _mainViewModel.AddLog($"[MOTION] {Info.Name} Home Searching Start.");
            await _driver.Home(Info.AxisNo);
        }

        // driver 의 실시간 IsInPosition (UpdateMotorStatus 100ms 캐시 우회)
        // 시퀀스 InPosition 폴링이 직전 step의 stale 값에 즉시 break되는 문제 방지용
        public bool IsDriverInPosition()
            => _driver?.GetStatus(Info.AxisNo)?.IsInPosition == true;

        public void UpdateMotorStatus()
        {
            try
            {
                if (_driver == null || Status == null) return;

                // 하드웨어로부터 최신 상태 가져오기
                var latest = _driver.GetStatus(Info.AxisNo);

                if (latest != null)
                {
                    // 🚨 [추가] 하드웨어 알람 감지 및 통합 알람 팝업 연동
                    if (latest.IsAlarm)
                    {
                        // 기존에 알람 상태가 아니었다가 새로 발생한 경우에만 팝업을 띄움
                        if (!this.IsAlarm)
                        {
                            // 1. 로그 기록
                            _mainViewModel.AddLog($"[ALARM] Axis {Info.AxisNo} ({Info.Name}) — hardware alarm detected", LogLevel.Error);

                            // 2. 통합 알람 팝업창 호출 — 축 식별자 치환 ({0} → "X" 등)
                            _mainViewModel.AlarmVM.RaiseAlarm("MOT-AXIS-ALM", Info.Name);
                        }
                    }

                    // 상태 동기화
                    this.IsAlarm = latest.IsAlarm;

                    // Status 객체 속성 갱신
                    Status.CurrentPos = latest.CurrentPos;
                    Status.IsServoOn = latest.IsServoOn;
                    Status.IsHomeDone = latest.IsHomeDone;
                    Status.IsInPosition = latest.IsInPosition;
                    Status.IsAlarm = latest.IsAlarm;
                    Status.IsMoving = latest.IsMoving;

                    // 센서 상태 동기화
                    Status.CwLimit = latest.CwLimit;
                    Status.CcwLimit = latest.CcwLimit;
                    Status.HomeSensor = latest.HomeSensor;

                    // ViewModel 직속 속성 동기화 (UI 바인딩 가속)
                    this.CurrentPos = latest.CurrentPos;
                    this.IsServoOn = latest.IsServoOn;

                    // UI에 상태 변화 알림
                    OnPropertyChanged(nameof(Status));
                }
            }
            catch (Exception ex)
            {
                /* 통신 에러 로그 출력 */
                _mainViewModel.AddLog($"[MOTION] Axis {Info.AxisNo} comm error: {ex.Message}", LogLevel.Warning);
            }
        }
    }
}