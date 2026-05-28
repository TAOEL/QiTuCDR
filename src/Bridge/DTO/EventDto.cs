using System;
using Newtonsoft.Json.Linq;

namespace QiTuCDR.Bridge.DTO
{
    public sealed class EventDto
    {
        public string Event { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public JObject Payload { get; set; } = new JObject();

        public static EventDto Create(string eventName, JObject? payload = null)
        {
            return new EventDto
            {
                Event = eventName,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Payload = payload ?? new JObject()
            };
        }
    }
}
