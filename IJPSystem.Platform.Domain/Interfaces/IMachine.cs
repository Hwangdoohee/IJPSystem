using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Models.Motion;

namespace IJPSystem.Platform.Domain.Interfaces
{
    public interface IMachine
    {
        IIODriver IO { get; }
        IMotionDriver Motion { get; }
        IVisionDriver Vision { get; }
        MotionAxisRoot Config { get; set; }
        string MachineName { get; }

        void Initialize();
        void Terminate();

        // 시스템 상태 (램프)
        void SetSystemStatus(MachineState state);

        // 도어
        void OpenDoor();
        void CloseDoor();
        bool IsDoorLocked();

        // Vacuum
        void VacuumOn();
        void VacuumOff();

        // 센서
        bool IsGlassDetected();
        bool IsEmoActive();
        bool IsPressureOk(int swNo);

        // 시뮬레이션 전용 (가상 드라이버에서만 의미 있음)
        void SimulateDoorLockAfter(int delayMs);
    }
}