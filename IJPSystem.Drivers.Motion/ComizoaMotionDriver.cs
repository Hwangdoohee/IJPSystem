using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Motion;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IJPSystem.Drivers.Motion
{
    // 코미조아(Comizoa) 모션 드라이버 스켈레톤.
    //
    // SDK: ComizoaIF.dll 등 C 함수를 P/Invoke 로 호출 (czm_init / czm_move_a ...).
    // 모든 메서드 본문은 placeholder — 실제 SDK 호출은 TODO 표시 지점에 채운다.
    //
    // 참고 — Comizoa 일반 호출 예:
    //   czm_init();
    //   czm_set_servo_on(axis, 1);
    //   czm_set_max_velo(axis, vel);
    //   czm_set_acc(axis, acc);
    //   czm_set_dec(axis, dec);
    //   czm_move_a(axis, pos);
    //   czm_get_pos(axis, out current);
    public class ComizoaMotionDriver : IMotionDriver
    {
        private readonly Dictionary<string, AxisStatus> _axisStates = new();
        // string AxisNo → ushort Comizoa axis index 매핑 (Initialize 단계에서 채움)
        private readonly Dictionary<string, ushort> _axisIndex = new();

        public bool IsConnected { get; private set; }

        public bool Connect()
        {
            // TODO: czm_init() 호출 + 보드 enumerate. 실패 시 false 반환.
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            // TODO: czm_exit() — DLL 자원 해제
            IsConnected = false;
        }

        public void Initialize(List<AxisDeviceInfo> axisConfigs)
        {
            if (axisConfigs == null) return;
            _axisStates.Clear();
            _axisIndex.Clear();

            ushort idx = 0;
            foreach (var cfg in axisConfigs)
            {
                if (string.IsNullOrEmpty(cfg.AxisNo)) continue;

                _axisStates[cfg.AxisNo] = new AxisStatus
                {
                    AxisNo = cfg.AxisNo,
                    Name   = cfg.Name ?? "Unknown Axis",
                    Unit   = cfg.Unit ?? "mm",
                };
                _axisIndex[cfg.AxisNo] = idx++;
            }
            // TODO: czm_set_pulse_out_mode / czm_set_enc_input_mode 등 펄스/엔코더 모드 셋업
        }

        public AxisStatus GetStatus(string axisNo)
        {
            // TODO:
            //   czm_get_motion_status(idx, ...) → IsMoving / IsInPosition / IsAlarm
            //   czm_get_pos(idx, out pos)       → CurrentPos
            //   czm_is_home_done(idx, ...)      → IsHomeDone
            return _axisStates.TryGetValue(axisNo, out var s) ? s : new AxisStatus { AxisNo = axisNo };
        }

        public double GetActualPosition(string axisNo)
        {
            // TODO: czm_get_pos(MapAxis(axisNo), out double pos); return pos;
            return GetStatus(axisNo).CurrentPos;
        }

        public List<AxisStatus> GetAllStatus()
            => _axisStates.Values.OrderBy(s => s.AxisNo).ToList();

        public Task<bool> ServoOn(string axisNo, bool isOn)
        {
            // TODO: czm_set_servo_on(MapAxis(axisNo), isOn ? 1 : 0);
            if (_axisStates.TryGetValue(axisNo, out var s))
                s.IsServoOn = isOn;
            return Task.FromResult(true);
        }

        public Task<bool> MoveAbs(string axisNo, double pos, double vel, double acc, double dec)
        {
            // TODO:
            //   czm_set_max_velo(idx, vel);
            //   czm_set_acc(idx, acc);
            //   czm_set_dec(idx, dec);
            //   czm_move_a(idx, pos);
            return Task.FromResult(false);
        }

        public Task<bool> MoveRel(string axisNo, double distance, double vel, double acc, double dec)
        {
            // TODO: czm_move_r(idx, distance) — 속도/가감속은 MoveAbs 와 동일 셋업
            return Task.FromResult(false);
        }

        public Task<bool> MoveJog(string axisNo, bool isForward, double vel, double acc, double dec)
        {
            // TODO: czm_jog_start(idx, isForward ? +1 : -1) — Stop 으로 종료
            return Task.FromResult(false);
        }

        public Task<bool> Stop(string axisNo)
        {
            // TODO: czm_stop(idx) — 부드러운 감속 정지. 비상정지는 czm_emergency_stop.
            return Task.FromResult(false);
        }

        public Task<bool> Home(string axisNo)
        {
            // TODO: czm_home_start(idx) 호출 + czm_is_home_done 폴링 (또는 ManualResetEvent)
            return Task.FromResult(false);
        }

        public Task<bool> ResetAlarm(string axisNo)
        {
            // TODO: czm_reset_alarm(idx)
            if (_axisStates.TryGetValue(axisNo, out var s))
                s.IsAlarm = false;
            return Task.FromResult(true);
        }
    }
}
