using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>
    /// PRINT HEAD DOWN — 프린트 헤드를 아래로 이동 (글래스 가까이).
    /// 유지보수 / 디버깅 / 수동 점검용 단독 시퀀스.
    /// </summary>
    public static class HeadDownSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_HeadDown_Move",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadDown, ct)),

            new SequenceStepDef(2, "Step_HeadDown_MoveDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),
        };
    }
}
