using System.Collections.Generic;
using System.Threading.Tasks;
using IJPSystem.Platform.Domain.Models.Motion;

namespace IJPSystem.Platform.Domain.Interfaces
{
    public interface IMotionDriver
    {
        bool IsConnected { get; }

        // 1. 초기화 및 해제
        bool Connect();
        void Disconnect();
        void Initialize(List<AxisDeviceInfo> axisConfigs);
        void Terminate();

        // 2. 상태 조회 (Status)
        AxisStatus GetStatus(string axisNo);
        double GetActualPosition(string axisNo);
        List<AxisStatus> GetAllStatus();

        // 3. 구동 명령 (Motion)
        Task<bool> ServoOn(string axisNo, bool isOn);

        // [수정] 가속도(acc)와 감속도(dec) 파라미터 추가
        Task<bool> MoveAbs(string axisNo, double pos, double vel, double acc, double dec);
        Task<bool> MoveRel(string axisNo, double distance, double vel, double acc, double dec);

        // [수정] 조그 구동 시에도 가속도/감속도 적용 가능하도록 변경
        Task<bool> MoveJog(string axisNo, bool isForward, double vel, double acc, double dec);

        Task<bool> Stop(string axisNo);
        Task<bool> Home(string axisNo);

        // 4. 알람 관리
        Task<bool> ResetAlarm(string axisNo);
    }
}