using System.Collections.Generic;
using System.Text.Json.Serialization; // ← 이 부분이 다릅니다!

namespace IJPSystem.Platform.Domain.Models.IO
{
    public class IOConfig
    {
        // System.Text.Json에서는 JsonPropertyName을 사용합니다.
        [JsonPropertyName("Digital_IO_List")]
        public List<IODeviceInfo> DigitalList { get; set; } = new();

        [JsonPropertyName("Analog_IO_List")]
        public List<IODeviceInfo> AnalogList { get; set; } = new();

        public List<IODeviceInfo> GetAllDevices()
        {
            var all = new List<IODeviceInfo>();
            if (DigitalList != null) all.AddRange(DigitalList);
            if (AnalogList != null) all.AddRange(AnalogList);
            return all;
        }
    }
}