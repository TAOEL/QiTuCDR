using System;
using System.Windows;
using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public sealed class DebugDockPanelHost : IDockPanelHost
    {
        private readonly ILogger _logger;
        private readonly Window _window;
        private bool _disposed;

        public DebugDockPanelHost(WebView2Manager? webView2Manager, ILogger logger)
        {
            _logger = logger;
            Panel = new QiTuDockPanel(webView2Manager);
            _window = new Window
            {
                Title = "QiTuCDR",
                Content = Panel,
                Width = 420,
                Height = 720,
                MinWidth = 360,
                MinHeight = 560,
                ShowInTaskbar = false
            };
            _window.Closing += OnClosing;
        }

        public QiTuDockPanel Panel { get; }

        public bool IsVisible => _window.IsVisible;

        public string DiagnosticHostKind => "Debug";

        public string? DiagnosticAdapterType => null;

        public bool DiagnosticIsAttached => false;

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
            Panel.ShowWebView();
        }

        public void ShowFallback()
        {
            ThrowIfDisposed();
            Panel.ShowFallback();
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
                _logger.Error("Debug dock panel close during dispose failed.", ex);
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            e.Cancel = true;
            _logger.Info("Debug dock panel close intercepted; hiding panel.");
            Hide();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DebugDockPanelHost));
            }
        }
    }
}
