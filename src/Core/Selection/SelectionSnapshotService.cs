using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Core.Host;

namespace QiTuCDR.Core.Selection
{
    public sealed class SelectionSnapshotService : ISelectionSnapshotService
    {
        private readonly ICorelDocumentAdapter _documentAdapter;

        public SelectionSnapshotService(ICorelDocumentAdapter documentAdapter)
        {
            _documentAdapter = documentAdapter;
        }

        public Task<SelectionSnapshot> CaptureAsync(CancellationToken token)
        {
            return _documentAdapter.CaptureSelectionSnapshotAsync(token);
        }
    }
}
