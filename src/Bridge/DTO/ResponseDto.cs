using Newtonsoft.Json.Linq;

namespace QiTuCDR.Bridge.DTO
{
    public sealed class ResponseDto
    {
        public string Version { get; set; } = "1.0";
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public JObject Payload { get; set; } = new JObject();

        public static ResponseDto Ok(string requestId, JObject? payload = null, string message = "")
        {
            return new ResponseDto
            {
                RequestId = requestId,
                Success = true,
                Message = message,
                Payload = payload ?? new JObject()
            };
        }

        public static ResponseDto Fail(string requestId, string errorCode, string message)
        {
            return new ResponseDto
            {
                RequestId = requestId,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Payload = new JObject()
            };
        }
    }
}
