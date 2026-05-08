using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    public static class PurgeSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_Purge_1",
                ct =>
                {
                    machine.VacuumOff();
                    machine.IO.ScheduleInput("DI_VC_SENSOR_GLASS_STOP", false, 500);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_VC_SENSOR_GLASS_STOP",
                                                 expected: false, timeoutMs: 10_000, ct);
                }),

            new SequenceStepDef(2, "Step_Purge_2",
                ct => motion.MoveToPointAsync(PointNames.Purge, ct)),

            new SequenceStepDef(3, "Step_Purge_3",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),

            new SequenceStepDef(4, "Step_Purge_4",
                ct =>
                {
                    machine.IO.ScheduleInput("DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE", true, 3_000);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE",
                                                 expected: true, timeoutMs: 15_000, ct);
                }),

            new SequenceStepDef(5, "Step_Purge_5",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(6, "Step_Purge_6",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
