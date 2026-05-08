using System;

namespace IJPSystem.Platform.Common.Utilities
{
    /// <summary>DPI ↔ Drop Pitch(mm) 변환. 25.4mm/inch 기준.</summary>
    public static class DpiConverter
    {
        public const double InchToMm = 25.4;

        /// <summary>DPI → drop pitch(mm). 예) 600 → 0.04233…</summary>
        public static double DpiToPitchMm(int dpi)
        {
            if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
            return InchToMm / dpi;
        }

        /// <summary>drop pitch(mm) → DPI. 예) 0.0423 → ≈600</summary>
        public static int PitchMmToDpi(double pitchMm)
        {
            if (pitchMm <= 0) throw new ArgumentOutOfRangeException(nameof(pitchMm));
            return (int)Math.Round(InchToMm / pitchMm);
        }
    }
}
