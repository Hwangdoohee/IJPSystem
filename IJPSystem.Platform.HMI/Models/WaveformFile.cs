using System.Collections.Generic;

namespace IJPSystem.Platform.HMI.Models
{
    public class WaveformFile
    {
        public string FilePath     { get; set; } = "";
        public string HeadType     { get; set; } = "";
        public string WaveformType { get; set; } = "";
        public int    Version      { get; set; }
        public List<WaveformPulse> Pulses { get; } = new();

        public bool   TempCompEnabled { get; set; }
        public double TCompLow        { get; set; }
        public double TCompHigh       { get; set; }
        public double VCompStart      { get; set; }
        public double VCompEnd        { get; set; }
        public double VTCoef          { get; set; }
    }
}
