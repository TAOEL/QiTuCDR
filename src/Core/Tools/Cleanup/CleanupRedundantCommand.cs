using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Cleanup
{
    public sealed class CleanupRedundantCommand : IBridgeCommand
    {
        private readonly CleanupRedundantService _service;

        public CleanupRedundantCommand(CleanupRedundantService service)
        {
            _service = service;
        }

        public string Action => Actions.CleanupRedundant;
        public bool RequiresReadyState => true;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token) => _service.ExecuteAsync(request, token);
    }
}
