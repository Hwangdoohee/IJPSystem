using IJPSystem.Machines.Inkjet5G;
using IJPSystem.Platform.Application.Sequences;
using IJPSystem.Platform.Common;
using IJPSystem.Platform.HMI.Services;
using IJPSystem.Platform.HMI.Simulation.Models;
using IJPSystem.Platform.HMI.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.HMI.Simulation
{
    // 시나리오를 실제 시퀀스로 실행하고 결과를 expect 와 비교.
    // Phase 1: 기존 가상 드라이버(VirtualIODriver) 그대로 사용 — 시퀀스 코드 무수정.
    public class ScenarioRunner
    {
        private readonly MainViewModel _mainVM;

        public ScenarioRunner(MainViewModel mainVM) => _mainVM = mainVM;

        public async Task<ScenarioRunResult> RunAsync(ScenarioDef scenario, CancellationToken ct = default)
        {
            var machine = _mainVM.GetController()?.GetMachine() as InkjetMachine;
            if (machine == null)
                return ScenarioRunResult.Failed("머신 미초기화");

            var def = SequenceRegistry.GetAll().FirstOrDefault(d => d.Id == scenario.Sequence);
            if (def == null)
                return ScenarioRunResult.Failed($"알 수 없는 시퀀스 ID: {scenario.Sequence}");

            var motion = new MotionServiceAdapter(_mainVM);
            var steps  = def.BuildSteps(machine, motion);

            // at_ms 기반 events 사전 등록 — IO.ScheduleInput 의 지연 트리거 활용
            foreach (var ev in scenario.Events.Where(e => e.AtMs.HasValue))
                ApplyEvent(machine, ev, ev.AtMs!.Value);

            var sw = Stopwatch.StartNew();
            string  result      = "completed";
            string? failedStep  = null;
            string? alarmCode   = null;

            // 시뮬 모드 ON — 가상 모션 즉시 도착, ScheduleInput 지연 무시, SimClock.DelayAsync 즉시 통과
            SimulationContext.FastForward = true;
            try
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var step = steps[i];

                    // on_step 기반 events — 해당 step 진입 직전에 주입
                    foreach (var ev in scenario.Events.Where(e => e.OnStep == step.Name))
                        ApplyEvent(machine, ev, ev.AfterMs ?? 0);

                    failedStep = step.Name;
                    await step.Action(ct).ConfigureAwait(false);
                }
                failedStep = null;
            }
            catch (OperationCanceledException)
            {
                result = "stopped";
            }
            catch (TimeoutException)
            {
                result    = "failed";
                alarmCode = "SEQ-MOTION-TIMEOUT";
            }
            catch (Exception)
            {
                result = "failed";
            }
            finally
            {
                SimulationContext.FastForward = false;
            }
            sw.Stop();

            return Validate(scenario, result, alarmCode, failedStep, sw.ElapsedMilliseconds);
        }

        private static void ApplyEvent(InkjetMachine machine, ScenarioEvent ev, int delayMs)
        {
            if (ev.SetDi != null)
                foreach (var kv in ev.SetDi)
                    machine.IO.ScheduleInput(kv.Key, kv.Value, delayMs);

            // AI 는 즉시 적용 (가상 입력에 지연 schedule 미지원). 향후 확장 포인트.
            if (ev.SetAi != null)
                foreach (var kv in ev.SetAi)
                    machine.IO.SetAnalogOutput(kv.Key, kv.Value);
        }

        private static ScenarioRunResult Validate(
            ScenarioDef s, string actualResult, string? alarm, string? failedStep, long elapsedMs)
        {
            if (!string.Equals(actualResult, s.Expect.Result, StringComparison.OrdinalIgnoreCase))
                return ScenarioRunResult.Failed(
                    $"결과 불일치: 기대 {s.Expect.Result}, 실제 {actualResult}", elapsedMs);

            if (!string.IsNullOrEmpty(s.Expect.Alarm) &&
                !string.Equals(s.Expect.Alarm, alarm, StringComparison.OrdinalIgnoreCase))
                return ScenarioRunResult.Failed(
                    $"알람 불일치: 기대 {s.Expect.Alarm}, 실제 {alarm ?? "(없음)"}", elapsedMs);

            if (!string.IsNullOrEmpty(s.Expect.FailedAtStep) &&
                !string.Equals(s.Expect.FailedAtStep, failedStep, StringComparison.OrdinalIgnoreCase))
                return ScenarioRunResult.Failed(
                    $"실패 step 불일치: 기대 {s.Expect.FailedAtStep}, 실제 {failedStep ?? "(없음)"}", elapsedMs);

            if (s.Expect.DurationMaxMs is int max && elapsedMs > max)
                return ScenarioRunResult.Failed(
                    $"시간 초과: 기대 ≤ {max}ms, 실제 {elapsedMs}ms", elapsedMs);

            return ScenarioRunResult.Passed(elapsedMs);
        }
    }

    public record ScenarioRunResult(bool IsPass, string Message, long ElapsedMs)
    {
        public static ScenarioRunResult Passed(long ms)            => new(true,  "PASS",   ms);
        public static ScenarioRunResult Failed(string msg, long ms = 0) => new(false, msg, ms);
    }
}
