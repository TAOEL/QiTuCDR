using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools
{
    public sealed class GetStateCommand : IBridgeCommand
    {
        private readonly PluginStateMachine _stateMachine;

        public GetStateCommand(PluginStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public string Action => Actions.GetState;
        public bool RequiresReadyState => false;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
        {
            return Task.FromResult(ResponseDto.Ok(request.RequestId, new JObject
            {
                ["state"] = _stateMachine.Current.ToString()
            }));
        }
    }
}
