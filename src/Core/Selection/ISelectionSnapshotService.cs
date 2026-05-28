using System.Threading;
using System.Threading.Tasks;

namespace QiTuCDR.Core.Selection
{
    public interface ISelectionSnapshotService
    {
        Task<SelectionSnapshot> CaptureAsync(CancellationToken token);
    }
}
