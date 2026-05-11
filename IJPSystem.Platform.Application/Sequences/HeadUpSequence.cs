using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>
    /// PRINT HEAD UP — 프린트 헤드를 위로 이동 (글래스에서 분리).
    /// 유지보수 / 디버깅 / 수동 점검용 단독 시퀀스.
    /// </summary>
    public static class HeadUpSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_HeadUp_1",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadUp, ct)),

            new SequenceStepDef(2, "Step_HeadUp_2",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),
        };
    }
}
