using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Motion;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>Auto Print — 자동 인쇄 시퀀스</summary>
    /// <remarks>
    /// SequenceStepDef.Name 은 표시용 **번역 키** (Step_AutoPrint_N).
    /// HMI 의 ViewModel 이 Loc.T(key) 로 사용자 언어로 변환해서 화면에 표시.
    /// 키→번역은 Common/Resources/Languages/ko-KR.xaml, en-US.xaml 참조.
    /// </remarks>3
    public static class AutoPrintSequence
    {
        public static IReadOnlyList<SequenceStepDef> Build(IMachine machine, IMotionService motion) => new[]
        {
            new SequenceStepDef(1, "Step_AutoPrint_1",
                ct =>
                {
                    machine.IO.ScheduleInput("DI_VC_SENSOR_GLASS_STOP", true, 2_000);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_VC_SENSOR_GLASS_STOP",
                                                 expected: true, timeoutMs: 30_000, ct);
                }),

            new SequenceStepDef(2, "Step_AutoPrint_2",
                ct =>
                {
                    machine.VacuumOn();
                    machine.IO.ScheduleInput("DI_PRESSURE_SW4_VACUUM_CV_P", true, 500);
                    return Task.CompletedTask;
                }),

            new SequenceStepDef(3, "Step_AutoPrint_3",
                ct => WaitHelper.ForIOSignal(machine.IO, "DI_PRESSURE_SW4_VACUUM_CV_P",
                                             expected: true, timeoutMs: 5_000, ct)),

            new SequenceStepDef(4, "Step_AutoPrint_4",
                ct => Task.Delay(1_000, ct)),

            new SequenceStepDef(5, "Step_AutoPrint_5",
                ct => motion.MoveToPointAsync(PointNames.PrintStart, ct)),

            new SequenceStepDef(6, "Step_AutoPrint_6",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),

            // ── 신규: 프린트 헤드 DOWN (글래스 가까이) ──
            new SequenceStepDef(7, "Step_AutoPrint_HeadDown",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadDown, ct)),

            new SequenceStepDef(8, "Step_AutoPrint_HeadDownDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),

            new SequenceStepDef(9, "Step_AutoPrint_7",
                ct => motion.MoveToPointAsync(PointNames.PrintEnd, ct, MotionProfileKind.Printing)),

            new SequenceStepDef(10, "Step_AutoPrint_8",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 60_000, ct)),

            // ── 신규: 프린트 헤드 UP (글래스에서 떼기) ──
            new SequenceStepDef(11, "Step_AutoPrint_HeadUp",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadUp, ct)),

            new SequenceStepDef(12, "Step_AutoPrint_HeadUpDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),

            new SequenceStepDef(13, "Step_AutoPrint_9",
                ct =>
                {
                    machine.VacuumOff();
                    machine.IO.ScheduleInput("DI_PRESSURE_SW4_VACUUM_CV_P", false, 200);
                    machine.IO.ScheduleInput("DI_VC_SENSOR_GLASS_STOP", false, 500);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_VC_SENSOR_GLASS_STOP",
                                                 expected: false, timeoutMs: 10_000, ct);
                }),

            new SequenceStepDef(14, "Step_AutoPrint_10",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(15, "Step_AutoPrint_11",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
