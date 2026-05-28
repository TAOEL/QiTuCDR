using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public sealed class DebugDockPanelHostFactory : IDockPanelHostFactory
    {
        public IDockPanelHost Create(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication)
        {
            return new DebugDockPanelHost(webView2Manager, logger);
        }
    }
}
