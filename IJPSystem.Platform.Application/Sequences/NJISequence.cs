using IJPSystem.Platform.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>Nozzle Jetting Inspection — 노즐 헤드 검사 시퀀스</summary>
    public static class NJISequence
    {
        private const string CamId = "CAM_01";

        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_NJI_MoveNji",
                ct => motion.MoveToPointAsync(PointNames.NJI, ct)),

            new SequenceStepDef(2, "Step_NJI_MoveNjiDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),

            new SequenceStepDef(3, "Step_NJI_LightOn",
                ct =>
                {
                    machine.Vision.SetLight(CamId, true);
                    machine.Vision.SetLightIntensity(CamId, 200);
                    return System.Threading.Tasks.Task.CompletedTask;
                }),

            new SequenceStepDef(4, "Step_NJI_Inspect",
                async ct =>
                {
                    var result = await WaitHelper.CaptureAndWait(machine.Vision, CamId,
                                                                  timeoutMs: 15_000, ct);
                    if (result == null || !result.IsPass)
                        throw new InvalidOperationException(
                            $"노즐 검사 NG — [{result?.NgCode}] {result?.NgDescription}  Score={result?.Score}");
                }),

            new SequenceStepDef(5, "Step_NJI_LightOff",
                ct =>
                {
                    machine.Vision.SetLight(CamId, false);
                    return System.Threading.Tasks.Task.CompletedTask;
                }),

            new SequenceStepDef(6, "Step_NJI_MoveReady",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(7, "Step_NJI_MoveReadyDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
