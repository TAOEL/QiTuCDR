using System;
using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public sealed class CorelDockPanelHost : IDockPanelHost
    {
        private readonly object? _corelApplication;
        private readonly ICorelDockerAdapter _dockerAdapter;
        private readonly ILogger _logger;
        private readonly QiTuDockPanel _panel;
        private bool _isAttached;
        private bool _disposed;

        public CorelDockPanelHost(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication = null, ICorelDockerAdapter? dockerAdapter = null)
        {
            WebView2Manager = webView2Manager;
            _logger = logger;
            _corelApplication = corelApplication;
            _dockerAdapter = dockerAdapter ?? new PlaceholderCorelDockerAdapter();
            _panel = new QiTuDockPanel(webView2Manager, showInitialContent: false);
        }

        public WebView2Manager? WebView2Manager { get; }

        public bool IsVisible => _dockerAdapter.IsVisible;

        public string DiagnosticHostKind => "CorelDocker";

        public string? DiagnosticAdapterType => _dockerAdapter.GetType().Name;

        public bool DiagnosticIsAttached => _isAttached;

        public void Show()
        {
            ThrowIfDisposed();
            EnsureAttached();
            _dockerAdapter.Show();
        }

        public void Hide()
        {
            ThrowIfDisposed();
            _dockerAdapter.Hide();
        }

        public void ShowWebView()
        {
            ThrowIfDisposed();
            EnsureAttached();
            _panel.ShowWebView();
        }

        public void ShowFallback()
        {
            ThrowIfDisposed();
            EnsureAttached();
            _panel.ShowFallback();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _dockerAdapter.Release();
            }
            catch (NotSupportedException ex)
            {
                _logger.Warn("CorelDRAW Docker host placeholder release skipped: " + ex.Message);
            }
            finally
            {
                _dockerAdapter.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CorelDockPanelHost));
            }
        }

        private void EnsureAttached()
        {
            if (_isAttached)
            {
                return;
            }

            _dockerAdapter.CreateContainer(_corelApplication);
            _dockerAdapter.AttachPanel(_panel);
            _isAttached = true;
        }
    }
}
