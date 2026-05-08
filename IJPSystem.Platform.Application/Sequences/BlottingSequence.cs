using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    public static class BlottingSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_Blotting_1",
                ct =>
                {
                    machine.VacuumOff();
                    machine.IO.ScheduleInput("DI_VC_SENSOR_GLASS_STOP", false, 500);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_VC_SENSOR_GLASS_STOP",
                                                 expected: false, timeoutMs: 10_000, ct);
                }),

            new SequenceStepDef(2, "Step_Blotting_2",
                ct => motion.MoveToPointAsync(PointNames.Blotting, ct)),

            new SequenceStepDef(3, "Step_Blotting_3",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),

            new SequenceStepDef(4, "Step_Blotting_4",
                ct => Task.Delay(3_000, ct)),

            new SequenceStepDef(5, "Step_Blotting_5",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(6, "Step_Blotting_6",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
