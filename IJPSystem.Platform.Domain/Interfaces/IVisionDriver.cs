using IJPSystem.Platform.Domain.Models.Vision;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Domain.Interfaces
{
    public interface IVisionDriver
    {
        // ── 1. 연결 / 초기화 ──
        bool Connect();
        void Disconnect();
        bool IsConnected { get; }
        void Initialize(List<CameraDeviceInfo> configs);
        void Terminate();

        // ── 2. 상태 조회 ──
        CameraStatus GetStatus(string cameraId);
        List<CameraStatus> GetAllStatus();

        // ── 3. 촬영 ──
        Task<VisionImage> CaptureAsync(string cameraId);
        Task<VisionImage> WaitForHardwareTriggerAsync(string cameraId, CancellationToken ct);

        // ── 4. 검사 ──
        Task<InspectionResult> InspectAsync(string cameraId, VisionImage image);
        Task<InspectionResult> CaptureAndInspectAsync(string cameraId);

        // ── 5. 조명 제어 ──
        void SetLight(string cameraId, bool on);
        void SetLightIntensity(string cameraId, int intensity);   // 0 ~ 255

        // ── 6. 카메라 파라미터 ──
        void   SetExposure(string cameraId, double ms);
        void   SetGain(string cameraId, double gain);
        double GetExposure(string cameraId);
        double GetGain(string cameraId);
    }
}
