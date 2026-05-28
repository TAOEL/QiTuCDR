using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools
{
    public sealed class EchoCommand : IBridgeCommand
    {
        public string Action => Actions.Echo;
        public bool RequiresReadyState => false;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
        {
            var payload = new JObject
            {
                ["echo"] = request.Payload,
                ["native"] = "QiTuCDR bridge online"
            };

            return Task.FromResult(ResponseDto.Ok(request.RequestId, payload));
        }
    }
}
