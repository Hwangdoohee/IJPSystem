using IJPSystem.Platform.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    public static class PurgeSequence
    {
        // ── AO 인덱스 (IO.json 매핑) ──
        private const string AO_PURGE_PRESSURE_SV    = "AO_CH_1_TARGET_PURGE";
        private const string AO_MENISCUS_PRESSURE_SV = "AO_CH_1_TARGET_MENISCUS";
        private const int    PressureStabilizeMs     = 1_500;  // 압력 안정화 대기

        // Default 파라미터는 호출자가 값 미지정 시 폴백 (PNID 화면에서 SET 버튼으로 SV 입력)
        public static IReadOnlyList<SequenceStepDef> Build(
            IMachine machine,
            IMotionService motion,
            double purgePressureKpa    = 50.0,   // 양압 SV (kPa) — PNID 화면 PressureSV 로 주입
            double meniscusPressureKpa = -3.5)   // 음압 SV (kPa) — PNID 화면 VacuumSV 로 주입
            => new[]
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

            // ── 신규: 헤드 인렛/리턴 밸브 8개 OPEN (Y700~Y715) ──
            new SequenceStepDef(6, "Step_Purge_ValveOpen",
                ct =>
                {
                    for (int i = 1; i <= 8; i++)
                    {
                        machine.IO.SetOutput($"DO_PACK1_H{i}_IN",  true);
                        machine.IO.SetOutput($"DO_PACK1_H{i}_OUT", true);
                    }
                    return Task.Delay(300, ct);   // 밸브 응답 시간
                }),

            // ── 신규: 양압(Purge pressure) 인가 + 안정화 대기 ──
            new SequenceStepDef(7, "Step_Purge_PressurizeOn",
                ct =>
                {
                    machine.IO.SetAnalogOutput(AO_PURGE_PRESSURE_SV, purgePressureKpa);
                    return Task.Delay(PressureStabilizeMs, ct);
                }),

            new SequenceStepDef(8, "Step_Purge_4",
                ct =>
                {
                    machine.IO.ScheduleInput("DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE", true, 3_000);
                    return WaitHelper.ForIOSignal(machine.IO, "DI_PRESSURE_SW1_N2_IMS_AIR_KNIFE",
                                                 expected: true, timeoutMs: 15_000, ct);
                }),

            // ── 신규: 양압 OFF + 음압(Meniscus) 인가 — 노즐 메니스커스 복원 ──
            new SequenceStepDef(9, "Step_Purge_PressurizeOff",
                ct =>
                {
                    machine.IO.SetAnalogOutput(AO_PURGE_PRESSURE_SV, 0.0);
                    machine.IO.SetAnalogOutput(AO_MENISCUS_PRESSURE_SV, meniscusPressureKpa);
                    return Task.Delay(PressureStabilizeMs, ct);
                }),

            // ── 신규: 헤드 밸브 CLOSE (16개 일괄) ──
            new SequenceStepDef(10, "Step_Purge_ValveClose",
                ct =>
                {
                    for (int i = 1; i <= 8; i++)
                    {
                        machine.IO.SetOutput($"DO_PACK1_H{i}_IN",  false);
                        machine.IO.SetOutput($"DO_PACK1_H{i}_OUT", false);
                    }
                    return Task.Delay(300, ct);
                }),

            // ── 신규: 프린트 헤드 UP (퍼지 완료 후 떼기) ──
            new SequenceStepDef(11, "Step_Purge_HeadUp",
                ct => motion.MoveToPointAsync(PointNames.PrintHeadUp, ct)),

            new SequenceStepDef(12, "Step_Purge_HeadUpDone",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 10_000, ct)),

            new SequenceStepDef(13, "Step_Purge_5",
                ct => motion.MoveToPointAsync(PointNames.Ready, ct)),

            new SequenceStepDef(14, "Step_Purge_6",
                ct => WaitHelper.ForAllMotionDone(machine.Motion, timeoutMs: 20_000, ct)),
        };
    }
}
