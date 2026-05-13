using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    public static class MaintenanceSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_Maint_DoorUnlock",
                ct => WaitHelper.ForCondition(
                    () => !machine.IsDoorLocked(),
                    timeoutMs: 10_000, ct)),

            new SequenceStepDef(2, "Step_Maint_MoveMt",
                ct => motion.MoveToPointAsync(PointNames.Maintenance, ct)),

            new SequenceStepDef(3, "Step_Maint_MoveMtDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),

            new SequenceStepDef(4, "Step_Maint_WaitDoorClose",
                ct =>
                {
                    machine.SimulateDoorLockAfter(5_000);
                    return WaitHelper.ForCondition(
                        () => machine.IsDoorLocked(),
                        timeoutMs: 120_000, ct);
                }),

            new SequenceStepDef(5, "Step_Maint_MoveReady",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(6, "Step_Maint_MoveReadyDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
