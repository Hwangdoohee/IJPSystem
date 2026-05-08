namespace IJPSystem.Platform.HMI.Models
{
    public class WaveformSegment
    {
        public double StartVoltage { get; init; }
        public double SlewRate     { get; init; }   // V/μs (음수 = 하강)
        public double EndVoltage   { get; init; }
        public double HoldTime     { get; init; }   // μs
    }
}
