using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public sealed class CorelDockPanelHostFactory : IDockPanelHostFactory
    {
        private readonly ICorelDockerAdapterFactory _adapterFactory;

        public CorelDockPanelHostFactory()
            : this(new PlaceholderCorelDockerAdapterFactory())
        {
        }

        public CorelDockPanelHostFactory(ICorelDockerAdapterFactory adapterFactory)
        {
            _adapterFactory = adapterFactory;
        }

        public IDockPanelHost Create(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication)
        {
            return new CorelDockPanelHost(webView2Manager, logger, corelApplication, _adapterFactory.Create());
        }
    }
}
