using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Vision;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>
    /// 시퀀스 스텝에서 조건 대기를 처리하는 유틸리티.
    ///
    /// [Virtual 모드 동작 원리]
    /// - ForMotionDone : VirtualMotionDriver 타이머가 IsMoving=false, IsInPosition=true를 자동 세팅
    /// - ForIOSignal   : 시퀀스에서 machine.IO.ScheduleInput()으로 가상 신호를 예약한 뒤 호출
    ///                   실제 하드웨어에서는 ScheduleInput이 no-op이고 물리 신호가 자연 발생
    /// - ForVisionResult: CaptureAndInspectAsync() 실행 후 LastResult를 폴링
    /// </summary>
    public static class WaitHelper
    {
        // ────────────────────────────────────────────────
        // 1. 기본 조건 폴링 (모든 Wait의 핵심)
        // ────────────────────────────────────────────────

        /// <summary>
        /// condition이 true가 될 때까지 pollMs 간격으로 폴링합니다.
        /// timeoutMs 이내에 만족되지 않으면 TimeoutException을 던집니다.
        /// </summary>
        public static async Task ForCondition(
            Func<bool> condition,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 20)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                ct.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException($"조건 미충족 — 제한 시간 {timeoutMs}ms 초과");
                await Task.Delay(pollMs, ct);
            }
        }

        // ────────────────────────────────────────────────
        // 2. IO 신호 대기
        // ────────────────────────────────────────────────

        /// <summary>
        /// IO 입력 신호가 expected 값이 될 때까지 대기합니다.
        /// Virtual 모드: 호출 전에 machine.IO.ScheduleInput()으로 신호를 예약하세요.
        /// </summary>
        public static Task ForIOSignal(
            IIODriver io,
            string indexName,
            bool expected,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 20)
            => ForCondition(() => io.GetInput(indexName) == expected, timeoutMs, ct, pollMs);

        // ────────────────────────────────────────────────
        // 3. 모션 완료 대기
        // ────────────────────────────────────────────────

        /// <summary>단일 축의 IsMoving == false 대기</summary>
        public static Task ForMotionDone(
            IMotionDriver motion,
            string axisNo,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 50)
            => ForCondition(() => !motion.GetStatus(axisNo).IsMoving, timeoutMs, ct, pollMs);

        /// <summary>모든 축의 IsMoving == false 대기</summary>
        public static Task ForAllMotionDone(
            IMotionDriver motion,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 50)
            => ForCondition(() => motion.GetAllStatus().All(s => !s.IsMoving), timeoutMs, ct, pollMs);

        /// <summary>단일 축의 IsInPosition == true 대기</summary>
        public static Task ForInPosition(
            IMotionDriver motion,
            string axisNo,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 50)
            => ForCondition(() => motion.GetStatus(axisNo).IsInPosition, timeoutMs, ct, pollMs);

        // ────────────────────────────────────────────────
        // 4. 비전 검사 완료 대기
        // ────────────────────────────────────────────────

        /// <summary>
        /// 카메라의 LastResult가 null이 아닐 때까지 대기하고 결과를 반환합니다.
        /// 호출 전 CaptureAndInspectAsync()를 Fire-and-forget으로 실행하거나,
        /// 이 메서드가 직접 검사를 실행하는 오버로드를 사용하세요.
        /// </summary>
        public static async Task<InspectionResult?> ForVisionResult(
            IVisionDriver vision,
            string cameraId,
            int timeoutMs,
            CancellationToken ct,
            int pollMs = 50)
        {
            // LastResult를 null로 초기화한 뒤 새 결과가 들어올 때까지 대기
            var status = vision.GetStatus(cameraId);
            status.LastResult = null;

            await ForCondition(() => status.LastResult != null, timeoutMs, ct, pollMs);
            return status.LastResult;
        }

        /// <summary>촬영+검사를 실행하고 결과가 나올 때까지 대기합니다.</summary>
        public static async Task<InspectionResult?> CaptureAndWait(
            IVisionDriver vision,
            string cameraId,
            int timeoutMs,
            CancellationToken ct)
        {
            var status = vision.GetStatus(cameraId);
            status.LastResult = null;

            // 검사를 비동기로 시작
            _ = Task.Run(async () =>
            {
                var result = await vision.CaptureAndInspectAsync(cameraId);
                status.LastResult = result;
            }, ct);

            await ForCondition(() => status.LastResult != null, timeoutMs, ct, pollMs: 50);
            return status.LastResult;
        }
    }
}
