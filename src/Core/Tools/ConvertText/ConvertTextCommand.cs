using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Tools.ConvertText
{
    public sealed class ConvertTextCommand : IBridgeCommand
    {
        private readonly ConvertTextService _service;

        public ConvertTextCommand(ConvertTextService service)
        {
            _service = service;
        }

        public string Action => Actions.ConvertText;
        public bool RequiresReadyState => true;

        public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
        {
            return _service.ExecuteAsync(request, token);
        }
    }
}
