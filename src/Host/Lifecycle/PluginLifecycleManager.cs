using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Bridge.Events;
using QiTuCDR.Bridge.Serialization;
using QiTuCDR.Core.Bridge;
using QiTuCDR.Core.Composition;
using QiTuCDR.Host.COM;
using QiTuCDR.Host.Docking;
using QiTuCDR.Host.Environment;
using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Infrastructure.Tasks;
using QiTuCDR.Shared;

namespace QiTuCDR.Host.Lifecycle
{
    public sealed class PluginLifecycleManager : IDisposable
    {
        private readonly PluginStateMachine _stateMachine = new PluginStateMachine();
        private readonly TaskCancellationHub _cancellationHub = new TaskCancellationHub();
        private readonly EventBus _eventBus = new EventBus();
        private readonly BridgeJsonSerializer _serializer = new BridgeJsonSerializer();
        private readonly ILogger _logger = new FileLogger();
        private readonly IRuntimeEnvironmentChecker _environmentChecker = new RuntimeEnvironmentChecker();
        private readonly ICorelDocumentAdapterFactory _documentAdapterFactory = new CorelDocumentAdapterFactory();
        private readonly IDockPanelHostFactory? _dockPanelHostFactoryOverride;
        private readonly IPluginConfigStore? _configStoreOverride;
        private PluginConfig _config = new PluginConfig();
        private CancellationTokenSource _lifetime = new CancellationTokenSource();

        private RuntimeCheckResult? _runtimeCheckResult;
        private BridgeDispatcher? _bridgeDispatcher;
        private WebView2Manager? _webView2Manager;
        private IDockPanelHost? _dockPanelHost;
        private object? _corelApplication;
        private int _dockHostFallbackCount;
        private int _browserRecoveryCount;
        private int _documentCloseCancelCount;
        private int _documentActivatedCancelCount;
        private int _selectionChangedEventCount;
        private int _hostShutdownCancelCount;
        private bool _disposed;

        public PluginLifecycleManager()
            : this(null)
        {
        }

        public PluginLifecycleManager(IDockPanelHostFactory? dockPanelHostFactory)
            : this(dockPanelHostFactory, null)
        {
        }

        public PluginLifecycleManager(IDockPanelHostFactory? dockPanelHostFactory, IPluginConfigStore? configStore)
        {
            _dockPanelHostFactoryOverride = dockPanelHostFactory;
            _configStoreOverride = configStore;
        }

        public void Start(object? corelApplication)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PluginLifecycleManager));
            }

            try
            {
                _corelApplication = corelApplication;
                _config = (_configStoreOverride ?? new JsonPluginConfigStore(logger: _logger)).Load();
                _runtimeCheckResult = _environmentChecker.Check(corelApplication);
                LogRuntimeChecks(_runtimeCheckResult);

                var dispatcher = Application.Current?.Dispatcher ?? DispatcherProvider.Current;
                var comDispatcher = new ComDispatcher(dispatcher, _logger);
                var documentAdapter = _documentAdapterFactory.Create(corelApplication, comDispatcher, _config, _logger);
                var factory = new CoreServiceFactory(documentAdapter, _stateMachine, _cancellationHub, _eventBus, _config, _logger);

                _bridgeDispatcher = new BridgeDispatcher(factory.CreateCommands(), _stateMachine, _logger);
                if (_runtimeCheckResult.CanUseWebView)
                {
                    _webView2Manager = new WebView2Manager(_serializer, _eventBus, _logger);
                    _webView2Manager.MessageReceived += OnWebMessageReceived;
                    _webView2Manager.BrowserFailed += OnBrowserFailed;
                }

                _stateMachine.StateChanged += OnStateChanged;

                _stateMachine.TransitionTo(PluginState.Preheating);
                _ = DelayedPreheatAsync(_lifetime.Token);
            }
            catch (Exception ex)
            {
                _logger.Error("Plugin start failed.", ex);
                _stateMachine.TransitionTo(PluginState.Faulted);
            }
        }

        public void ShowPanel()
        {
            if (_webView2Manager == null && _runtimeCheckResult?.CanUseWebView == true)
            {
                return;
            }

            if (_dockPanelHost == null)
            {
                _dockPanelHost = CreateDockPanelHost();
            }

            try
            {
                ShowDockPanel(_dockPanelHost);
            }
            catch (Exception ex)
            {
                if (_dockPanelHostFactoryOverride != null)
                {
                    throw;
                }

                _logger.Error("Configured dock host failed during show. Falling back to Debug dock host.", ex);
                _dockHostFallbackCount++;
                TryDisposeStep("Dispose failed dock host", () => _dockPanelHost?.Dispose());
                _dockPanelHost = new DebugDockPanelHostFactory().Create(_webView2Manager, _logger, _corelApplication);
                ShowDockPanel(_dockPanelHost);
            }

            _ = EnsureWebViewInitializedForVisiblePanelAsync(_lifetime.Token);
        }

        public void ShowPanel(string route)
        {
            ShowPanel();
            _webView2Manager?.NavigateToRoute(route);
        }

        public void HidePanel()
        {
            _dockPanelHost?.Hide();
        }

        public PluginStabilitySnapshot GetStabilitySnapshot()
        {
            return new PluginStabilitySnapshot
            {
                State = _stateMachine.Current,
                ConfiguredDockHostMode = _config.DockHostMode,
                ActiveDockPanelHostType = _dockPanelHost?.GetType().Name,
                ActiveDockPanelHostKind = _dockPanelHost?.DiagnosticHostKind,
                ActiveDockerAdapterType = _dockPanelHost?.DiagnosticAdapterType,
                IsDockerAdapterAttached = _dockPanelHost?.DiagnosticIsAttached == true,
                HasDockPanel = _dockPanelHost != null,
                IsPanelVisible = _dockPanelHost?.IsVisible == true,
                HasWebViewManager = _webView2Manager != null,
                IsWebViewInitialized = _webView2Manager?.IsInitialized == true,
                WebViewCreateCount = _webView2Manager?.CreateCount ?? 0,
                WebViewAttachCallCount = _webView2Manager?.AttachCallCount ?? 0,
                DockHostFallbackCount = _dockHostFallbackCount,
                BrowserRecoveryCount = _browserRecoveryCount,
                DocumentCloseCancelCount = _documentCloseCancelCount,
                DocumentActivatedCancelCount = _documentActivatedCancelCount,
                SelectionChangedEventCount = _selectionChangedEventCount,
                HostShutdownCancelCount = _hostShutdownCancelCount
            };
        }

        public void NotifyDocumentClosing()
        {
            _documentCloseCancelCount++;
            _logger.Warn("CorelDRAW document closing detected; cancelling current task.");
            _cancellationHub.CancelCurrentTask();
            _eventBus.Publish(EventDto.Create(EventTypes.DocumentChanged, new Newtonsoft.Json.Linq.JObject
            {
                ["reason"] = "closing",
                ["cancelledCurrentTask"] = true
            }));
        }

        public void NotifyDocumentActivated()
        {
            _documentActivatedCancelCount++;
            _logger.Info("CorelDRAW document activated; cancelling current task to avoid cross-document execution.");
            _cancellationHub.CancelCurrentTask();
            _eventBus.Publish(EventDto.Create(EventTypes.DocumentChanged, new Newtonsoft.Json.Linq.JObject
            {
                ["reason"] = "activated",
                ["cancelledCurrentTask"] = true
            }));
        }

        public void NotifySelectionChanged()
        {
            _selectionChangedEventCount++;
            _logger.Info("CorelDRAW selection changed.");
            _eventBus.Publish(EventDto.Create(EventTypes.SelectionChanged, new Newtonsoft.Json.Linq.JObject
            {
                ["reason"] = "selectionChanged"
            }));
        }

        public void NotifyHostShuttingDown()
        {
            _hostShutdownCancelCount++;
            _logger.Warn("CorelDRAW host shutting down; cancelling all tasks.");
            _cancellationHub.CancelAll();
            _eventBus.Publish(EventDto.Create(EventTypes.DocumentChanged, new Newtonsoft.Json.Linq.JObject
            {
                ["reason"] = "hostShutdown",
                ["cancelledAllTasks"] = true
            }));
        }

        public void SimulateBrowserFailureForDiagnostics()
        {
            _logger.Warn("Simulating WebView2 browser failure for diagnostics.");
            OnBrowserFailed(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            TryDisposeStep("Transition to disposing", () => _stateMachine.TransitionTo(PluginState.Disposing));
            TryDisposeStep("Cancel lifetime", () => _lifetime.Cancel());
            TryDisposeStep("Cancel tasks", () => _cancellationHub.CancelAll());

            if (_webView2Manager != null)
            {
                TryDisposeStep("Dispose WebView2 manager", () =>
                {
                    _webView2Manager.MessageReceived -= OnWebMessageReceived;
                    _webView2Manager.BrowserFailed -= OnBrowserFailed;
                    _webView2Manager.Dispose();
                });
            }

            TryDisposeStep("Dispose dock panel", () => _dockPanelHost?.Dispose());
            TryDisposeStep("Detach state event", () => _stateMachine.StateChanged -= OnStateChanged);
            TryDisposeStep("Dispose cancellation hub", () => _cancellationHub.Dispose());
            TryDisposeStep("Dispose lifetime token", () => _lifetime.Dispose());
            TryDisposeStep("Transition to disposed", () => _stateMachine.TransitionTo(PluginState.Disposed));
        }

        private async Task DelayedPreheatAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(_config.WebViewPreheatDelayMs, token).ConfigureAwait(true);
                if (_runtimeCheckResult?.CanUseWebView == false)
                {
                    _stateMachine.TransitionTo(PluginState.Recovering);
                    _eventBus.Publish(QiTuCDR.Bridge.DTO.EventDto.Create(EventTypes.Recovery));
                    _stateMachine.TransitionTo(PluginState.Ready);
                    return;
                }

                if (_webView2Manager != null)
                {
                    _webView2Manager.AttachOrCreate();
                    await _webView2Manager.EnsureInitializedAsync(token).ConfigureAwait(true);
                }

                if (_stateMachine.Current == PluginState.Preheating)
                {
                    _stateMachine.TransitionTo(PluginState.Ready);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error("Delayed preheat failed.", ex);
                _stateMachine.TransitionTo(PluginState.Faulted);
            }
        }

        private async Task EnsureWebViewInitializedForVisiblePanelAsync(CancellationToken token)
        {
            if (_webView2Manager == null || _webView2Manager.IsInitialized)
            {
                return;
            }

            try
            {
                await Task.Delay(500, token).ConfigureAwait(true);
                _webView2Manager.AttachOrCreate();
                await _webView2Manager.EnsureInitializedAsync(token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error("Visible panel WebView2 initialization failed.", ex);
            }
        }

        private async void OnWebMessageReceived(object? sender, string message)
        {
            try
            {
                if (_bridgeDispatcher == null || _webView2Manager == null)
                {
                    return;
                }

                if (!_serializer.TryDeserializeRequest(message, out var request, out var errorMessage) || request == null)
                {
                    _logger.Warn("Invalid web message received: " + errorMessage);
                    TryPostFailureResponse(ErrorCodes.InvalidPayload, "Invalid request JSON.");
                    return;
                }

                var response = await _bridgeDispatcher.DispatchAsync(
                    request,
                    () => _cancellationHub.ResetForNewTask(TimeSpan.FromMilliseconds(_config.TaskTimeoutMs)),
                    _cancellationHub.CurrentToken).ConfigureAwait(true);
                _webView2Manager.PostResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Error("Handle web message failed.", ex);
                TryPostFailureResponse(ErrorCodes.Unknown, "Native message handling failed.");
            }
        }

        private void TryPostFailureResponse(string errorCode, string message)
        {
            try
            {
                _webView2Manager?.PostResponse(ResponseDto.Fail(string.Empty, errorCode, message));
            }
            catch (Exception ex)
            {
                _logger.Error("Post failure response failed.", ex);
            }
        }

        private void OnBrowserFailed(object? sender, EventArgs e)
        {
            _browserRecoveryCount++;
            _cancellationHub.CancelAll();
            _stateMachine.TransitionTo(PluginState.Faulted);
            _stateMachine.TransitionTo(PluginState.Recovering);
            _dockPanelHost?.ShowFallback();
            _eventBus.Publish(QiTuCDR.Bridge.DTO.EventDto.Create(EventTypes.Recovery));
            _stateMachine.TransitionTo(PluginState.Ready);
        }

        private void OnStateChanged(object? sender, PluginState state)
        {
            _eventBus.Publish(QiTuCDR.Bridge.DTO.EventDto.Create(EventTypes.StateChanged, new Newtonsoft.Json.Linq.JObject
            {
                ["state"] = state.ToString()
            }));
        }

        private IDockPanelHost CreateDockPanelHost()
        {
            var factory = _dockPanelHostFactoryOverride ?? DockPanelHostFactorySelector.Create(_config.DockHostMode, _logger, _config.AllowOfficialCorelDockerAdapter);
            try
            {
                return factory.Create(_webView2Manager, _logger, _corelApplication);
            }
            catch (Exception ex)
            {
                if (_dockPanelHostFactoryOverride != null)
                {
                    throw;
                }

                _logger.Error("Configured dock host creation failed. Falling back to Debug dock host.", ex);
                _dockHostFallbackCount++;
                return new DebugDockPanelHostFactory().Create(_webView2Manager, _logger, _corelApplication);
            }
        }

        private void ShowDockPanel(IDockPanelHost dockPanelHost)
        {
            if (_stateMachine.Current == PluginState.Faulted || _stateMachine.Current == PluginState.Recovering || _runtimeCheckResult?.CanUseWebView == false)
            {
                dockPanelHost.ShowFallback();
            }
            else
            {
                dockPanelHost.ShowWebView();
            }

            dockPanelHost.Show();
        }

        private void LogRuntimeChecks(RuntimeCheckResult result)
        {
            foreach (var item in result.Items)
            {
                var message = $"Runtime check {item.Name}: {(item.Passed ? "OK" : "FAILED")} - {item.Message}";
                if (item.Passed)
                {
                    _logger.Info(message);
                }
                else if (item.Severity == RuntimeCheckSeverity.Fatal)
                {
                    _logger.Error(message);
                }
                else
                {
                    _logger.Warn(message);
                }
            }
        }

        private void TryDisposeStep(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.Error("Lifecycle dispose step failed: " + name, ex);
            }
        }
    }
}
