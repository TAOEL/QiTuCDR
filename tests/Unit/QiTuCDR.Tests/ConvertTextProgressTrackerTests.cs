using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Core.Host;
using QiTuCDR.Core.Tools.ConvertText;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class ConvertTextProgressTrackerTests
    {
        [TestMethod]
        public void PublishesByProcessedObjectBatchAndFinalProgress()
        {
            var published = new List<ConvertTextResult>();
            var tracker = new ConvertTextProgressTracker(50, 120, published.Add);

            for (var processed = 1; processed <= 120; processed++)
            {
                tracker.ReportProcessed(converted: processed, skipped: 0);
            }

            Assert.AreEqual(3, published.Count);
            Assert.AreEqual(50, published[0].Converted);
            Assert.AreEqual(100, published[1].Converted);
            Assert.AreEqual(120, published[2].Converted);
        }

        [TestMethod]
        public void SkippedOnlyShapesPublishFinalProgressOnce()
        {
            var published = new List<ConvertTextResult>();
            var tracker = new ConvertTextProgressTracker(50, 3, published.Add);

            tracker.ReportProcessed(converted: 0, skipped: 1);
            tracker.ReportProcessed(converted: 0, skipped: 2);
            tracker.ReportProcessed(converted: 0, skipped: 3);
            tracker.ReportProcessed(converted: 0, skipped: 3);

            Assert.AreEqual(1, published.Count);
            Assert.AreEqual(0, published[0].Converted);
            Assert.AreEqual(3, published[0].Skipped);
            Assert.AreEqual(3, published[0].Total);
        }

        [TestMethod]
        public void InvalidBatchSizeFallsBackToDefaultBatchSize()
        {
            var published = new List<ConvertTextResult>();
            var tracker = new ConvertTextProgressTracker(0, 51, published.Add);

            for (var processed = 1; processed <= 51; processed++)
            {
                tracker.ReportProcessed(converted: processed, skipped: 0);
            }

            Assert.AreEqual(2, published.Count);
            Assert.AreEqual(50, published[0].Converted);
            Assert.AreEqual(51, published[1].Converted);
        }
    }
}
