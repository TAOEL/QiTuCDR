using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Core.Selection;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Host
{
    public sealed class NullCorelDocumentAdapter : ICorelDocumentAdapter
    {
        public bool HasOpenDocument => false;

        public Task<SelectionSnapshot> CaptureSelectionSnapshotAsync(CancellationToken token)
        {
            return Task.FromResult(new SelectionSnapshot(Array.Empty<int>()));
        }

        public Task<IReadOnlyList<CorelShapeSnapshot>> GetShapeSnapshotsAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, CancellationToken token)
        {
            return Task.FromResult<IReadOnlyList<CorelShapeSnapshot>>(Array.Empty<CorelShapeSnapshot>());
        }

        public Task<bool> CanResolveSelectionSnapshotAsync(SelectionSnapshot selectionSnapshot, CancellationToken token)
        {
            return Task.FromResult(false);
        }

        public Task<ConvertTextResult> ConvertTextAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, int batchSize, Action<ConvertTextResult> progress, CancellationToken token)
        {
            return Task.FromResult(new ConvertTextResult(0, 0, 0));
        }

        public Task<int> CenterObjectsAsync(SelectionSnapshot selectionSnapshot, string mode, CancellationToken token)
        {
            return Task.FromResult(0);
        }

        public Task<int> NormalizeSizeAsync(SelectionSnapshot selectionSnapshot, NormalizeSizeOptions options, CancellationToken token)
        {
            return Task.FromResult(0);
        }

        public Task<int> CleanupRedundantAsync(CancellationToken token)
        {
            return Task.FromResult(0);
        }
    }
}
