using Newtonsoft.Json;
using QiTuCDR.Bridge.DTO;

namespace QiTuCDR.Bridge.Serialization
{
    public sealed class BridgeJsonSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public RequestDto DeserializeRequest(string json)
        {
            var dto = JsonConvert.DeserializeObject<RequestDto>(json, Settings);
            if (dto == null)
            {
                throw new JsonSerializationException("Request JSON was empty.");
            }

            return NormalizeRequest(dto);
        }

        public bool TryDeserializeRequest(string json, out RequestDto? dto, out string errorMessage)
        {
            try
            {
                dto = DeserializeRequest(json);
                errorMessage = string.Empty;
                return true;
            }
            catch (JsonException ex)
            {
                dto = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        public string SerializeResponse(ResponseDto dto)
        {
            return JsonConvert.SerializeObject(dto, Formatting.None, Settings);
        }

        public string SerializeEvent(EventDto dto)
        {
            return JsonConvert.SerializeObject(dto, Formatting.None, Settings);
        }

        private static RequestDto NormalizeRequest(RequestDto dto)
        {
            if (dto.Payload == null)
            {
                dto.Payload = new Newtonsoft.Json.Linq.JObject();
            }

            return dto;
        }
    }
}
