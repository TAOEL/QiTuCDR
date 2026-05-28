using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QiTuCDR.Core.Selection;
using QiTuCDR.Shared;

namespace QiTuCDR.Core.Host
{
    public interface ICorelDocumentAdapter
    {
        bool HasOpenDocument { get; }
        Task<SelectionSnapshot> CaptureSelectionSnapshotAsync(CancellationToken token);
        Task<IReadOnlyList<CorelShapeSnapshot>> GetShapeSnapshotsAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, CancellationToken token);
        Task<bool> CanResolveSelectionSnapshotAsync(SelectionSnapshot selectionSnapshot, CancellationToken token);
        Task<ConvertTextResult> ConvertTextAsync(ToolRange range, SelectionSnapshot? selectionSnapshot, bool includeHidden, int batchSize, Action<ConvertTextResult> progress, CancellationToken token);
        Task<int> CenterObjectsAsync(SelectionSnapshot selectionSnapshot, string mode, CancellationToken token);
        Task<int> NormalizeSizeAsync(SelectionSnapshot selectionSnapshot, NormalizeSizeOptions options, CancellationToken token);
        Task<int> CleanupRedundantAsync(CancellationToken token);
    }
}
