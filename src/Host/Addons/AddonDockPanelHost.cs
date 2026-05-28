using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;
using System;
using System.ComponentModel;
using System.Windows;

namespace QiTuCDR.Host.Addons
{
    internal sealed class AddonDockPanelHost : Docking.IDockPanelHost
    {
        private readonly ILogger _logger;
        private readonly Docking.QiTuDockPanel _panel;
        private readonly Window _window;
        private bool _disposed;

        public AddonDockPanelHost(WebView2Manager? webView2Manager, ILogger logger)
        {
            _logger = logger;
            _panel = new Docking.QiTuDockPanel(webView2Manager, showInitialContent: false);
            _window = new Window
            {
                Title = "QiTuCDR 测试面板",
                Content = _panel,
                Width = 420,
                Height = 720,
                MinWidth = 360,
                MinHeight = 560,
                ShowInTaskbar = false
            };
            _window.Closing += OnClosing;
        }

        public bool IsVisible => !_disposed && _window.IsVisible;
        public string DiagnosticHostKind => "CorelAddonWpfHost";
        public string? DiagnosticAdapterType => null;
        public bool DiagnosticIsAttached => IsVisible;

        public void Show()
        {
            ThrowIfDisposed();
            if (!_window.IsVisible)
            {
                _window.Show();
            }

            _window.Activate();
        }

        public void Hide()
        {
            ThrowIfDisposed();
            _window.Hide();
        }

        public void ShowWebView()
        {
            ThrowIfDisposed();
            _panel.ShowWebView();
        }

        public void ShowFallback()
        {
            ThrowIfDisposed();
            _panel.ShowFallback();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _window.Closing -= OnClosing;
            _window.Content = null;

            try
            {
                _window.Close();
            }
            catch (Exception ex)
            {
                _logger.Error("Addon dock panel close during dispose failed.", ex);
            }
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            e.Cancel = true;
            _logger.Info("Addon dock panel close intercepted; hiding panel.");
            Hide();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AddonDockPanelHost));
            }
        }
    }
}
