using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Infrastructure.Tasks;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools
{
    public sealed class CancelCurrentTaskCommand : IBridgeCommand
    {
        private readonly TaskCancellationHub _cancellationHub;

        public CancelCurrentTaskCommand(TaskCancellationHub cancellationHub)
        {
            _cancellationHub = cancellationHub;
        }

        public string Action => Actions.CancelCurrentTask;
        public bool RequiresReadyState => false;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
        {
            _cancellationHub.CancelCurrentTask();
            return Task.FromResult(ResponseDto.Ok(request.RequestId, new JObject
            {
                ["cancelled"] = true
            }));
        }
    }
}
