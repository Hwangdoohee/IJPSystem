using IJPSystem.Platform.Common;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    // 시퀀스 안의 시간 대기를 시뮬 모드에서 즉시 통과시키는 단일 진입점.
    // 시뮬 모드가 아닐 때는 일반 Task.Delay 와 동일.
    public static class SimClock
    {
        public static Task DelayAsync(int ms, CancellationToken ct = default)
            => Task.Delay(SimulationContext.FastForward ? 0 : ms, ct);
    }
}
