using QiTuCDR.Shared;

namespace QiTuCDR.Host.Lifecycle
{
    public sealed class PluginStabilitySnapshot
    {
        public PluginState State { get; set; }
        public string? ConfiguredDockHostMode { get; set; }
        public string? ActiveDockPanelHostType { get; set; }
        public string? ActiveDockPanelHostKind { get; set; }
        public string? ActiveDockerAdapterType { get; set; }
        public bool IsDockerAdapterAttached { get; set; }
        public bool HasDockPanel { get; set; }
        public bool IsPanelVisible { get; set; }
        public bool HasWebViewManager { get; set; }
        public bool IsWebViewInitialized { get; set; }
        public int WebViewCreateCount { get; set; }
        public int WebViewAttachCallCount { get; set; }
        public int DockHostFallbackCount { get; set; }
        public int BrowserRecoveryCount { get; set; }
        public int DocumentCloseCancelCount { get; set; }
        public int DocumentActivatedCancelCount { get; set; }
        public int SelectionChangedEventCount { get; set; }
        public int HostShutdownCancelCount { get; set; }
    }
}
