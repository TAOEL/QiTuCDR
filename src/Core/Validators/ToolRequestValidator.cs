using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Validators
{
    public sealed class ToolRequestValidator
    {
        public string? ValidateRangePayload(RequestDto dto)
        {
            if (dto.Payload == null)
            {
                return ErrorCodes.InvalidPayload;
            }

            var range = dto.Payload.Value<string>("range");
            if (string.IsNullOrWhiteSpace(range))
            {
                return ErrorCodes.InvalidPayload;
            }

            if (!System.Enum.TryParse<ToolRange>(range, true, out _))
            {
                return ErrorCodes.InvalidPayload;
            }

            return null;
        }
    }
}
