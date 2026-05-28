using QiTuCDR.Host.Docking;
using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Addons
{
    internal sealed class AddonDockPanelHostFactory : IDockPanelHostFactory
    {
        public AddonDockPanelHostFactory()
        {
        }

        public IDockPanelHost Create(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication)
        {
            return new AddonDockPanelHost(webView2Manager, logger);
        }
    }
}
