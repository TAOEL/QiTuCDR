using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Center
{
    public sealed class CenterObjectsCommand : IBridgeCommand
    {
        private readonly CenterObjectsService _service;

        public CenterObjectsCommand(CenterObjectsService service)
        {
            _service = service;
        }

        public string Action => Actions.CenterObjects;
        public bool RequiresReadyState => true;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token) => _service.ExecuteAsync(request, token);
    }
}
