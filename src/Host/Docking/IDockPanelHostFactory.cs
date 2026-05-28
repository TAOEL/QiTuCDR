using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public interface IDockPanelHostFactory
    {
        IDockPanelHost Create(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication);
    }
}
