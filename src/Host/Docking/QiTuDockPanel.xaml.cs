using System.Windows.Controls;
using QiTuCDR.Host.WebView;

namespace QiTuCDR.Host.Docking
{
    public partial class QiTuDockPanel : UserControl
    {
        private readonly WebView2Manager? _webView2Manager;

        public QiTuDockPanel(WebView2Manager? webView2Manager, bool showInitialContent = true)
        {
            InitializeComponent();
            _webView2Manager = webView2Manager;
            if (!showInitialContent)
            {
                return;
            }

            if (_webView2Manager == null)
            {
                ShowFallback();
            }
            else
            {
                ShowWebView();
            }
        }

        public void ShowWebView()
        {
            Root.Children.Clear();
            if (_webView2Manager == null)
            {
                Root.Children.Add(new FallbackPanel());
                return;
            }

            Root.Children.Add(_webView2Manager.AttachOrCreate());
        }

        public void ShowFallback()
        {
            Root.Children.Clear();
            Root.Children.Add(new FallbackPanel());
        }
    }
}
