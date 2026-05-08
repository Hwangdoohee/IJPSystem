using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Motion;

namespace IJPSystem.Drivers.Motion
{
    public class VirtualMotionDriver : IMotionDriver
    {
        private readonly Dictionary<string, AxisStatus> _axisStates = new();
        private System.Timers.Timer? _statusTimer;

        public bool IsConnected { get; private set; } = false;

        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
            Terminate();
        }

        public void Initialize(List<AxisDeviceInfo> axisConfigs)
        {
            if (axisConfigs == null) return;

            _axisStates.Clear();

            foreach (var config in axisConfigs)
            {
                if (string.IsNullOrEmpty(config.AxisNo)) continue;

                _axisStates[config.AxisNo] = new AxisStatus
                {
                    AxisNo = config.AxisNo,
                    Name = config.Name ?? "Unknown Axis",
                    Unit = config.Unit ?? "mm",
                    CurrentPos = 0.0,
                    IsServoOn = false,
                    IsAlarm = false,
                    IsMoving = false,
                    IsHomeDone = false
                };
            }

            //Connect();

            // 가상 좌표 업데이트 타이머 (50ms 마다 실행하여 더 부드럽게 시뮬레이션)
            _statusTimer = new System.Timers.Timer(50);
            _statusTimer.Elapsed += (s, e) => UpdateSimulation();
            _statusTimer.AutoReset = true;
            _statusTimer.Start();

        }

        private void UpdateSimulation()
        {
            foreach (var state in _axisStates.Values.ToList())
            {
                if (state.IsMoving && state.IsServoOn)
                {
                    // [시뮬레이션 로직] CurrentVel(속도)에 따라 이동 거리 계산 (50ms 기준)
                    double step = state.CurrentVel * 0.05;
                    double distance = state.TargetPos - state.CurrentPos;

                    if (Math.Abs(distance) <= step || step <= 0)
                    {
                        state.CurrentPos  = state.TargetPos;
                        state.IsMoving    = false;
                        state.CurrentVel  = 0;
                        state.IsInPosition = true;  // WaitHelper.ForMotionDone / MotionServiceAdapter 폴링용
                    }
                    else
                    {
                        state.CurrentPos += (distance > 0) ? step : -step;
                    }
                }
            }
        }

        public AxisStatus GetStatus(string axisNo)
        {
            return _axisStates.TryGetValue(axisNo, out var status) ? status : new AxisStatus { AxisNo = axisNo };
        }

        public List<AxisStatus> GetAllStatus()
        {
            return _axisStates.Values.OrderBy(x => x.AxisNo).ToList();
        }

        public double GetActualPosition(string axisNo) => GetStatus(axisNo).CurrentPos;

        public Task<bool> ServoOn(string axisNo, bool isOn)
        {
            if (_axisStates.TryGetValue(axisNo, out var state))
            {
                state.IsServoOn = isOn;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // ★ MoveAbs: acc, dec 매개변수 추가
        public Task<bool> MoveAbs(string axisNo, double pos, double vel, double acc, double dec)
        {
            var state = GetStatus(axisNo);
            if (state.IsServoOn == false)
            {
                MessageBox.Show("Servo On 진행하세요");
                return Task.FromResult(false);
            }

            if (_axisStates.TryGetValue(axisNo, out state))
            {
                state.IsMoving     = true;
                state.IsInPosition = false;
                state.TargetPos    = pos;
                state.CurrentVel   = vel;

                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // ★ MoveRel: acc, dec 매개변수 추가
        public Task<bool> MoveRel(string axisNo, double distance, double vel, double acc, double dec)
        {
            var state = GetStatus(axisNo);
            if (state.IsServoOn == false)
            {
                MessageBox.Show("Servo On 진행하세요");
                return Task.FromResult(false);
            }

            if (_axisStates.TryGetValue(axisNo, out state))
            {
                state.IsMoving     = true;
                state.IsInPosition = false;
                state.TargetPos    = state.CurrentPos + distance;
                state.CurrentVel   = vel;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // ★ MoveJog: acc, dec 매개변수 추가
        public Task<bool> MoveJog(string axisNo, bool isForward, double vel, double acc, double dec)
        {
            var state = GetStatus(axisNo);
            if (state.IsServoOn == false)
            {
                MessageBox.Show("Servo On 진행하세요");
                return Task.FromResult(false);
            }

            if (_axisStates.TryGetValue(axisNo, out state))
            {
                state.IsMoving = true;
                state.CurrentVel = vel;
                // Jog는 목적지가 없으므로 아주 먼 곳으로 설정하거나 시뮬레이션 로직 분리 필요
                state.TargetPos = isForward ? 999999 : -999999;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> Stop(string axisNo)
        {
            if (_axisStates.TryGetValue(axisNo, out var state))
            {
                state.IsMoving = false;
                state.CurrentVel = 0;
                state.TargetPos = state.CurrentPos; // 현재 위치에서 멈춤
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // 원점복귀 시뮬레이션 속도(mm/s) — 50ms 주기에서 가시적으로 보이는 수준
        private const double HomingVelocity = 30.0;

        public async Task<bool> Home(string axisNo)
        {
            var state = GetStatus(axisNo);
            if (state.IsServoOn == false)
            {
                MessageBox.Show("Servo On 진행하세요");
                return false;
            }

            if (!_axisStates.TryGetValue(axisNo, out state))
                return false;

            // 이미 원점인 경우에도 한 번 더 스냅하고 종료
            if (Math.Abs(state.CurrentPos) < 1e-6)
            {
                state.CurrentPos = 0;
                state.IsHomeDone = true;
                state.IsMoving = false;
                state.CurrentVel = 0;
                state.TargetPos = 0;
                state.IsInPosition = true;
                return true;
            }

            // 실제 장비의 home seek 동작 흉내 — 0을 향해 일정 속도로 이동
            state.IsHomeDone   = false;
            state.IsInPosition = false;
            state.TargetPos    = 0.0;
            state.CurrentVel   = HomingVelocity;
            state.IsMoving     = true;

            // UpdateSimulation이 50ms 주기로 CurrentPos를 0에 수렴시킴
            while (state.IsMoving)
                await Task.Delay(20).ConfigureAwait(false);

            state.IsHomeDone = true;
            return true;
        }

        public void Terminate()
        {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _statusTimer = null;
        }

        public Task<bool> ResetAlarm(string axisNo) => Task.FromResult(true);
    }
}