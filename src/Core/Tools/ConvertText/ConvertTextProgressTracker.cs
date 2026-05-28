using System;
using QiTuCDR.Core.Host;

namespace QiTuCDR.Core.Tools.ConvertText
{
    public sealed class ConvertTextProgressTracker
    {
        private readonly int _batchSize;
        private readonly int _total;
        private readonly Action<ConvertTextResult> _publish;
        private int _lastPublishedProcessed;

        public ConvertTextProgressTracker(int batchSize, int total, Action<ConvertTextResult> publish)
        {
            _batchSize = batchSize > 0 ? batchSize : 50;
            _total = Math.Max(0, total);
            _publish = publish;
        }

        public void ReportProcessed(int converted, int skipped)
        {
            var processed = Math.Min(converted + skipped, _total);
            if (processed <= 0)
            {
                return;
            }

            if (processed <= _lastPublishedProcessed)
            {
                return;
            }

            if (processed - _lastPublishedProcessed >= _batchSize || processed == _total)
            {
                _publish(new ConvertTextResult(converted, skipped, _total));
                _lastPublishedProcessed = processed;
            }
        }
    }
}
