using System;
using System.Globalization;
using System.IO;
using IJPSystem.Platform.HMI.Models;

namespace IJPSystem.Platform.HMI.Common
{
    public static class WaveformParser
    {
        public static WaveformFile Parse(string filePath)
        {
            var file = new WaveformFile { FilePath = filePath };
            var lines = File.ReadAllLines(filePath);

            WaveformPulse? currentPulse = null;
            string section = "";

            foreach (var rawLine in lines)
            {
                // 주석 제거
                int ci = rawLine.IndexOf(';');
                string line = (ci >= 0 ? rawLine[..ci] : rawLine).Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 섹션 헤더 [xxx]
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    section = line[1..^1].Trim().ToLowerInvariant();
                    if (section.StartsWith("pulse"))
                    {
                        currentPulse = new WaveformPulse();
                        file.Pulses.Add(currentPulse);
                    }
                    continue;
                }

                // Key = Value
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim().Trim('"');

                if (section == "generic")
                {
                    if (key.Equals("HeadType", StringComparison.OrdinalIgnoreCase))
                        file.HeadType = val;
                    else if (key.Equals("WaveformType", StringComparison.OrdinalIgnoreCase))
                        file.WaveformType = val;
                    else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(val, out int v))
                        file.Version = v;
                }
                else if (section.StartsWith("pulse") && currentPulse != null)
                {
                    if (key.StartsWith("Seg", StringComparison.OrdinalIgnoreCase))
                    {
                        var p = val.Split(',');
                        if (p.Length >= 4 &&
                            TryParseD(p[0], out double sv) && TryParseD(p[1], out double sr) &&
                            TryParseD(p[2], out double ev) && TryParseD(p[3], out double ht))
                        {
                            currentPulse.Segments.Add(new WaveformSegment
                            {
                                StartVoltage = sv, SlewRate = sr,
                                EndVoltage   = ev, HoldTime = ht
                            });
                        }
                    }
                    else if (key.Equals("GLMask_A", StringComparison.OrdinalIgnoreCase))
                        currentPulse.GLMask_A = ParseHex(val);
                    else if (key.Equals("GLMask_B", StringComparison.OrdinalIgnoreCase))
                        currentPulse.GLMask_B = ParseHex(val);
                    else if (key.Equals("TempCompMask", StringComparison.OrdinalIgnoreCase))
                        currentPulse.TempCompMask = ParseHex(val);
                }
                else if (section == "temperaturecompensation")
                {
                    if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                        file.TempCompEnabled = val.Trim() == "1";
                    else if (key.Equals("TCompLow",  StringComparison.OrdinalIgnoreCase) && TryParseD(val, out double d)) file.TCompLow  = d;
                    else if (key.Equals("TCompHigh", StringComparison.OrdinalIgnoreCase) && TryParseD(val, out double d2)) file.TCompHigh = d2;
                    else if (key.Equals("VCompStart",StringComparison.OrdinalIgnoreCase) && TryParseD(val, out double d3)) file.VCompStart= d3;
                    else if (key.Equals("VCompEnd",  StringComparison.OrdinalIgnoreCase) && TryParseD(val, out double d4)) file.VCompEnd  = d4;
                    else if (key.Equals("VTCoef",    StringComparison.OrdinalIgnoreCase) && TryParseD(val, out double d5)) file.VTCoef    = d5;
                }
            }

            return file;
        }

        private static bool TryParseD(string s, out double v) =>
            double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v);

        private static int ParseHex(string val)
        {
            string h = val.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? val[2..] : val;
            return int.TryParse(h, NumberStyles.HexNumber, null, out int r) ? r : 0;
        }
    }
}
