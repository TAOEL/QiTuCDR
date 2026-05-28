using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Bridge.DTO;

namespace QiTuCDR.Core.Tools
{
    public interface IToolService
    {
        Task<ResponseDto> ExecuteAsync(RequestDto dto, CancellationToken token);
    }
}
