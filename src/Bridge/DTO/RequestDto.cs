using Newtonsoft.Json.Linq;

namespace QiTuCDR.Bridge.DTO
{
    public sealed class RequestDto
    {
        public string Version { get; set; } = "1.0";
        public string RequestId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public JObject Payload { get; set; } = new JObject();
    }
}
