using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.DTO;

namespace QiTuCDR.Bridge.Commands
{
    public interface IBridgeCommand
    {
        string Action { get; }
        bool RequiresReadyState { get; }
        Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token);
    }
}
