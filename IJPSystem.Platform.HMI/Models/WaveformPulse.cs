using System.Collections.Generic;

namespace IJPSystem.Platform.HMI.Models
{
    public class WaveformPulse
    {
        public int GLMask_A     { get; set; }
        public int GLMask_B     { get; set; }
        public int TempCompMask { get; set; }
        public List<WaveformSegment> Segments { get; } = new();
    }
}
