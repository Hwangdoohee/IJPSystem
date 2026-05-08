using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// IJPSystem.Platform.Domain/Enums/MachineState.cs
namespace IJPSystem.Platform.Domain.Enums
{
    public enum MachineState
    {
        Idle,       // 초기/정지 (소등)
        Standby,    // 대기 중   (노란등) ← 추가
        Running,    // 동작 중   (초록등)
        Alarm,      // 에러/알람 (빨간등)
        Emergency   // 비상 정지 (빨간등 + 부저 등 추가 가능)
    }
}
