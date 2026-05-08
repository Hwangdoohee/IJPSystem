using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <remarks>
    /// SequenceStepDef.Name 은 표시용 **번역 키** (Step_Init_N).
    /// HMI 의 ViewModel 이 Loc.T(key) 로 사용자 언어로 변환.
    /// </remarks>
    public static class InitializeSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_Init_1",
                ct => motion.ServoOnAllAsync()),

            new SequenceStepDef(2, "Step_Init_2",
                ct => motion.HomeAllAsync(ct)),

            new SequenceStepDef(3, "Step_Init_3",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(4, "Step_Init_4",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 30_000, ct)),

            new SequenceStepDef(5, "Step_Init_5",
                ct =>
                {
                    machine.IO.ScheduleInput("DI_NC_SENSOR_GLASS_DETECT", true, 1_500);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_NC_SENSOR_GLASS_DETECT",
                                                 expected: true, timeoutMs: 30_000, ct);
                }),

            new SequenceStepDef(6, "Step_Init_6",
                ct => motion.MoveToPointAsync(PointNames.Load, ct)),

            new SequenceStepDef(7, "Step_Init_7",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
