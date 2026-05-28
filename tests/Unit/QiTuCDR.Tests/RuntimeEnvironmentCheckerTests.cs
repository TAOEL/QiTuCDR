using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Host.Environment;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class RuntimeEnvironmentCheckerTests
    {
        [TestMethod]
        public void CheckReturnsExpectedRuntimeItems()
        {
            var checker = new RuntimeEnvironmentChecker();

            var result = checker.Check(corelApplication: null);

            Assert.IsTrue(result.Items.Any(x => x.Name == RuntimeCheckNames.CorelHostObject));
            Assert.IsTrue(result.Items.Any(x => x.Name == RuntimeCheckNames.WebView2Runtime));
            Assert.IsTrue(result.Items.Any(x => x.Name == RuntimeCheckNames.CorelDrawTypeLib));
            Assert.IsTrue(result.Items.Any(x => x.Name == RuntimeCheckNames.WebUiAssets));
            Assert.IsFalse(result.CanUseCorelHost);
        }
    }
}
