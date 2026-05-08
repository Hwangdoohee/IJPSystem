using System;

namespace IJPSystem.Platform.Domain.Models.Vision
{
    public class VisionImage
    {
        public string   CameraId    { get; set; } = string.Empty;
        public DateTime CaptureTime { get; set; } = DateTime.Now;
        public int      Width       { get; set; }
        public int      Height      { get; set; }
        public string?  FilePath    { get; set; }   // 저장된 이미지 경로 (옵션)
        public bool     IsValid     { get; set; } = true;

        public static VisionImage Invalid(string cameraId) =>
            new() { CameraId = cameraId, IsValid = false };
    }
}
