using System;

namespace IJPSystem.Platform.Domain.Models.Vision
{
    public class InspectionResult
    {
        public string   CameraId      { get; set; } = string.Empty;
        public bool     IsPass        { get; set; }
        public string   NgCode        { get; set; } = string.Empty;
        public string   NgDescription { get; set; } = string.Empty;
        public double   Score         { get; set; }       // 0 ~ 100 (100 = 완벽)
        public int      DefectCount   { get; set; }
        public DateTime Timestamp     { get; set; } = DateTime.Now;
        public VisionImage? Image     { get; set; }

        public static InspectionResult Pass(string cameraId, double score = 100.0) =>
            new() { CameraId = cameraId, IsPass = true, Score = score };

        public static InspectionResult Fail(string cameraId, string code, string description, double score = 0.0, int defectCount = 1) =>
            new() { CameraId = cameraId, IsPass = false, NgCode = code, NgDescription = description, Score = score, DefectCount = defectCount };
    }
}
