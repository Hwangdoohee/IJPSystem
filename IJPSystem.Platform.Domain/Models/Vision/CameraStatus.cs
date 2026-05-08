using IJPSystem.Platform.Domain.Common;
using System;

namespace IJPSystem.Platform.Domain.Models.Vision
{
    public class CameraStatus : ViewModelBase
    {
        public string CameraId { get; set; } = string.Empty;
        public string Name     { get; set; } = string.Empty;

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private bool _isCapturing;
        public bool IsCapturing
        {
            get => _isCapturing;
            set => SetProperty(ref _isCapturing, value);
        }

        private bool _isLightOn;
        public bool IsLightOn
        {
            get => _isLightOn;
            set => SetProperty(ref _isLightOn, value);
        }

        private int _lightIntensity;
        public int LightIntensity
        {
            get => _lightIntensity;
            set => SetProperty(ref _lightIntensity, value);
        }

        private double _exposureMs;
        public double ExposureMs
        {
            get => _exposureMs;
            set => SetProperty(ref _exposureMs, value);
        }

        private double _gain;
        public double Gain
        {
            get => _gain;
            set => SetProperty(ref _gain, value);
        }

        private long _totalCaptureCount;
        public long TotalCaptureCount
        {
            get => _totalCaptureCount;
            set => SetProperty(ref _totalCaptureCount, value);
        }

        private DateTime? _lastCaptureTime;
        public DateTime? LastCaptureTime
        {
            get => _lastCaptureTime;
            set => SetProperty(ref _lastCaptureTime, value);
        }

        private InspectionResult? _lastResult;
        public InspectionResult? LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }
    }
}
