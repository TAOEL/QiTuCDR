using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Bridge.Serialization;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.WebView
{
    public sealed class WebView2Manager : IDisposable
    {
        private readonly BridgeJsonSerializer _serializer;
        private readonly EventBus _eventBus;
        private readonly ILogger _logger;
        private WebView2? _webView;
        private bool _initialized;
        private bool _navigationEventsAttached;
        private string _pendingRoute = "/";

        public WebView2Manager(BridgeJsonSerializer serializer, EventBus eventBus, ILogger logger)
        {
            _serializer = serializer;
            _eventBus = eventBus;
            _logger = logger;
            _eventBus.EventPublished += OnEventPublished;
        }

        public event EventHandler<string>? MessageReceived;
        public event EventHandler? BrowserFailed;

        public bool IsInitialized => _initialized;
        public int CreateCount { get; private set; }
        public int AttachCallCount { get; private set; }

        public WebView2 AttachOrCreate()
        {
            AttachCallCount++;
            if (_webView != null)
            {
                return _webView;
            }

            _webView = new WebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                        "QiTuCDR",
                        "WebView2")
                }
            };
            CreateCount++;
            _webView.WebMessageReceived += OnWebMessageReceived;
            return _webView;
        }

        public async Task EnsureInitializedAsync(CancellationToken token)
        {
            if (_initialized || _webView == null)
            {
                return;
            }

            var webView = _webView;
            if (!webView.Dispatcher.CheckAccess())
            {
                await webView.Dispatcher.InvokeAsync(() => EnsureInitializedAsync(token)).Task.Unwrap().ConfigureAwait(false);
                return;
            }

            try
            {
                await webView.EnsureCoreWebView2Async().ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                webView.CoreWebView2.ProcessFailed += OnProcessFailed;
                AttachNavigationDiagnostics(webView.CoreWebView2);
                NavigateToWebUi();
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error("WebView2 initialization failed.", ex);
                BrowserFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void PostResponse(ResponseDto response)
        {
            PostJson(_serializer.SerializeResponse(response));
        }

        public void NavigateToRoute(string route)
        {
            _pendingRoute = NormalizeRoute(route);
            var webView = _webView;
            if (webView == null)
            {
                return;
            }

            if (!webView.Dispatcher.CheckAccess())
            {
                webView.Dispatcher.BeginInvoke(new Action(() => NavigateToRoute(_pendingRoute)));
                return;
            }

            if (webView.CoreWebView2 == null)
            {
                return;
            }

            _logger.Info("Navigating WebView2 route: " + _pendingRoute);
            webView.CoreWebView2.Navigate(BuildWebUiUri(_pendingRoute));
        }

        public void Dispose()
        {
            _eventBus.EventPublished -= OnEventPublished;

            if (_webView != null)
            {
                _webView.WebMessageReceived -= OnWebMessageReceived;
                _webView.Dispose();
                _webView = null;
            }
        }

        private void NavigateToWebUi()
        {
            var index = FindWebUiIndex();

            if (File.Exists(index))
            {
                var webUiRoot = Path.GetDirectoryName(index) ?? AppDomain.CurrentDomain.BaseDirectory;
                const string hostName = "qitucdr.local";
                _logger.Info("Mapping WebView2 virtual host to WebUI: " + webUiRoot);
                _webView!.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    hostName,
                    webUiRoot,
                    CoreWebView2HostResourceAccessKind.Allow);
                _logger.Info("Navigating WebView2 to WebUI virtual host route: " + _pendingRoute);
                _webView.CoreWebView2.Navigate(BuildWebUiUri(_pendingRoute));
            }
            else
            {
                _logger.Warn("WebUI build output not found: " + index);
                _webView!.NavigateToString("<html><body><h1>QiTuCDR</h1><p>WebUI build output not found.</p></body></html>");
            }
        }

        private static string FindWebUiIndex()
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(WebView2Manager).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(assemblyDirectory, "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "WebUI", "index.html"),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.GetFullPath(candidates[0]);
        }

        private static string BuildWebUiUri(string route)
        {
            return "https://qitucdr.local/index.html#" + NormalizeRoute(route);
        }

        private static string NormalizeRoute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                return "/";
            }

            route = route.Trim();
            if (route.StartsWith("#", StringComparison.Ordinal))
            {
                route = route.Substring(1);
            }

            return route.StartsWith("/", StringComparison.Ordinal) ? route : "/" + route;
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e.WebMessageAsJson);
        }

        private void OnEventPublished(object? sender, EventDto e)
        {
            PostJson(_serializer.SerializeEvent(e));
        }

        private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            _logger.Error("WebView2 process failed: " + e.ProcessFailedKind);
            BrowserFailed?.Invoke(this, EventArgs.Empty);
        }

        private void AttachNavigationDiagnostics(CoreWebView2 coreWebView2)
        {
            if (_navigationEventsAttached)
            {
                return;
            }

            _navigationEventsAttached = true;
            coreWebView2.NavigationCompleted += (sender, args) =>
            {
                if (args.IsSuccess)
                {
                    _logger.Info("WebView2 navigation completed.");
                }
                else
                {
                    _logger.Error("WebView2 navigation failed: " + args.WebErrorStatus);
                    BrowserFailed?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private void PostJson(string json)
        {
            var webView = _webView;
            if (webView == null)
            {
                return;
            }

            try
            {
                if (!webView.Dispatcher.CheckAccess())
                {
                    webView.Dispatcher.BeginInvoke(new Action(() => PostJson(json)));
                    return;
                }

                if (webView.CoreWebView2 == null)
                {
                    return;
                }

                webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.Error("Post web message skipped because WebView2 is disposed.", ex);
            }
            catch (COMException ex)
            {
                _logger.Error("Post web message skipped because WebView2 COM object is not available.", ex);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error("Post web message skipped because WebView2 is not available.", ex);
            }
        }
    }
}
