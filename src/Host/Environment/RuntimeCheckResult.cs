using System.Collections.Generic;
using System.Linq;

namespace QiTuCDR.Host.Environment
{
    public sealed class RuntimeCheckResult
    {
        public RuntimeCheckResult(IEnumerable<RuntimeCheckItem> items)
        {
            Items = items.ToList().AsReadOnly();
        }

        public IReadOnlyList<RuntimeCheckItem> Items { get; }

        public bool CanUseWebView => Items.All(x => x.Name != RuntimeCheckNames.WebView2Runtime || x.Passed);

        public bool CanUseCorelHost => Items.All(x => x.Name != RuntimeCheckNames.CorelHostObject || x.Passed);

        public bool HasFatalFailure => Items.Any(x => !x.Passed && x.Severity == RuntimeCheckSeverity.Fatal);
    }
}
