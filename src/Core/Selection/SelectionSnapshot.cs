using System.Collections.Generic;
using System.Linq;

namespace QiTuCDR.Core.Selection
{
    public sealed class SelectionSnapshot
    {
        public SelectionSnapshot(IEnumerable<int> shapeIds)
        {
            ShapeIds = shapeIds.ToList().AsReadOnly();
        }

        public IReadOnlyList<int> ShapeIds { get; }
    }
}
