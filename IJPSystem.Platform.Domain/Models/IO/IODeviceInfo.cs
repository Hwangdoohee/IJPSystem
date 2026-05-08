using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization; // System.Text.Json 사용 시


namespace IJPSystem.Platform.Domain.Models.IO
{
    public class IODeviceInfo
    {
        public string? Address { get; set; }
        public string? Index { get; set; }
        public string? Description { get; set; }
        [JsonPropertyName("Type")]
        public string? ContactType { get; set; }
        public string? IoCategory { get; set; }
    }

    public class DigitalIORoot
    {
        // JSON의 "Digital_IO_List" 키와 이름을 맞춰야 자동으로 파싱됩니다.
        public List<IODeviceInfo>? Digital_IO_List { get; set; }
    }
}
