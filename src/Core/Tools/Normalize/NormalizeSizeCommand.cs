using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.Normalize
{
    public sealed class NormalizeSizeCommand : IBridgeCommand
    {
        private readonly NormalizeSizeService _service;

        public NormalizeSizeCommand(NormalizeSizeService service)
        {
            _service = service;
        }

        public string Action => Actions.NormalizeSize;
        public bool RequiresReadyState => true;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token) => _service.ExecuteAsync(request, token);
    }
}
