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

            // ── 신규: 프린트 헤드 DOWN (퍼지 위치 가까이) ──
            new SequenceStepDef(4, "Step_Purge_HeadDown",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadDown, ct)),

            new SequenceStepDef(5, "Step_Purge_HeadDownDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),

            new SequenceStepDef(6, "Step_Purge_4",
                ct =>
                {
                    machine.IO.ScheduleInput("DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE", true, 3_000);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE",
                                                 expected: true, timeoutMs: 15_000, ct);
                }),

            // ── 신규: 프린트 헤드 UP (퍼지 완료 후 떼기) ──
            new SequenceStepDef(7, "Step_Purge_HeadUp",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadUp, ct)),

            new SequenceStepDef(8, "Step_Purge_HeadUpDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),

            new SequenceStepDef(9, "Step_Purge_5",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(10, "Step_Purge_6",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
