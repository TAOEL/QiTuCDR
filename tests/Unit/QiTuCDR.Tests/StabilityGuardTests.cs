using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using QiTuCDR.Bridge.Commands;
using QiTuCDR.Bridge.DTO;
using QiTuCDR.Core.Bridge;
using QiTuCDR.Host.Docking;
using QiTuCDR.Host.Lifecycle;
using QiTuCDR.Host.WebView;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Infrastructure.Tasks;
using QiTuCDR.Shared;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class StabilityGuardTests
    {
        [TestMethod]
        public async Task RepeatedBusinessDispatchReturnsStateMachineToReady()
        {
            var stateMachine = CreateReadyStateMachine();
            var command = new CountingCommand();
            var dispatcher = new BridgeDispatcher(new[] { command }, stateMachine, new MemoryLogger());
            var tokenFactoryCalls = 0;

            for (var i = 0; i < 250; i++)
            {
                var response = await dispatcher.DispatchAsync(
                    new RequestDto { RequestId = "stress-" + i, Action = command.Action },
                    () =>
                    {
                        tokenFactoryCalls++;
                        return CancellationToken.None;
                    },
                    CancellationToken.None);

                Assert.IsTrue(response.Success);
                Assert.AreEqual(PluginState.Ready, stateMachine.Current);
            }

            Assert.AreEqual(250, command.ExecuteCalls);
            Assert.AreEqual(250, tokenFactoryCalls);
        }

        [TestMethod]
        public async Task FailingBusinessDispatchDoesNotLeaveStateMachineBusy()
        {
            var stateMachine = CreateReadyStateMachine();
            var command = new FailingCommand();
            var dispatcher = new BridgeDispatcher(new[] { command }, stateMachine, new MemoryLogger());

            var response = await dispatcher.DispatchAsync(
                new RequestDto { RequestId = "fail-1", Action = command.Action },
                () => CancellationToken.None,
                CancellationToken.None);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(ErrorCodes.Unknown, response.ErrorCode);
            Assert.AreEqual(PluginState.Ready, stateMachine.Current);
        }

        [TestMethod]
        public void TaskCancellationHubCanBeReusedAfterCancelCurrentTask()
        {
            using (var hub = new TaskCancellationHub())
            {
                var first = hub.ResetForNewTask(TimeSpan.FromMinutes(1));
                hub.CancelCurrentTask();

                Assert.IsTrue(first.IsCancellationRequested);

                var second = hub.ResetForNewTask(TimeSpan.FromMinutes(1));

                Assert.IsFalse(second.IsCancellationRequested);
                Assert.IsFalse(hub.CurrentToken.IsCancellationRequested);
            }
        }

        [TestMethod]
        public void ShowPanelReusesSingleDockPanelHost()
        {
            var factory = new CountingDockPanelHostFactory();
            using (var manager = new PluginLifecycleManager(factory))
            {
                manager.ShowPanel();
                manager.ShowPanel();
                manager.HidePanel();

                Assert.AreEqual(1, factory.CreateCalls);
                Assert.AreEqual(2, factory.Host.ShowCalls);
                Assert.AreEqual(1, factory.Host.HideCalls);
                Assert.AreEqual(2, factory.Host.ShowWebViewCalls);

                var snapshot = manager.GetStabilitySnapshot();
                Assert.AreEqual("Debug", snapshot.ConfiguredDockHostMode);
                Assert.AreEqual(nameof(CountingDockPanelHost), snapshot.ActiveDockPanelHostType);
                Assert.AreEqual("Counting", snapshot.ActiveDockPanelHostKind);
                Assert.AreEqual("CountingAdapter", snapshot.ActiveDockerAdapterType);
                Assert.IsTrue(snapshot.IsDockerAdapterAttached);
                Assert.AreEqual(0, snapshot.DockHostFallbackCount);
            }
        }

        [TestMethod]
        public void CorelDockPanelHostFactoryReturnsPlaceholderHost()
        {
            var factory = new CorelDockPanelHostFactory();

            var host = factory.Create(null, new MemoryLogger(), null);

            Assert.IsInstanceOfType(host, typeof(CorelDockPanelHost));
            Assert.AreEqual(nameof(PlaceholderCorelDockerAdapter), host.DiagnosticAdapterType);
        }

        [TestMethod]
        public void CorelDockPanelHostFactoryCanInjectDockerAdapterFactory()
        {
            var adapter = new RecordingDockerAdapter();
            var factory = new CorelDockPanelHostFactory(new StaticDockerAdapterFactory(adapter));

            using (var host = factory.Create(null, new MemoryLogger(), new object()))
            {
                host.ShowWebView();
                host.Show();

                Assert.AreEqual(nameof(RecordingDockerAdapter), host.DiagnosticAdapterType);
                Assert.AreEqual("CorelDocker", host.DiagnosticHostKind);
                Assert.IsTrue(host.DiagnosticIsAttached);
                Assert.AreEqual(1, adapter.CreateContainerCalls);
                Assert.AreEqual(1, adapter.AttachPanelCalls);
                Assert.AreEqual(1, adapter.ShowCalls);
            }
        }

        [TestMethod]
        public void CorelDockerAdapterFailsFastUntilOfficialApiIsBound()
        {
            using (var adapter = new CorelDockerAdapter())
            {
                var ex = Assert.ThrowsException<NotSupportedException>(() => adapter.CreateContainer(new object()));

                StringAssert.Contains(ex.Message, "CorelDRAW Docker official API adapter is not implemented yet");
                StringAssert.Contains(ex.Message, "CreateContainer");
            }
        }

        [TestMethod]
        public void CorelDockPanelHostFailsFastUntilOfficialDockerApiIsConfirmed()
        {
            using (var host = new CorelDockPanelHost(null, new MemoryLogger()))
            {
                var ex = Assert.ThrowsException<NotSupportedException>(() => host.Show());

                StringAssert.Contains(ex.Message, "CorelDRAW Docker adapter step is not implemented yet");
                StringAssert.Contains(ex.Message, "CreateContainer");
            }
        }

        [TestMethod]
        public void CorelDockPanelHostPlaceholderDocumentsRequiredDockerSteps()
        {
            using (var host = new CorelDockPanelHost(null, new MemoryLogger()))
            {
                var showWebView = Assert.ThrowsException<NotSupportedException>(() => host.ShowWebView());
                var showFallback = Assert.ThrowsException<NotSupportedException>(() => host.ShowFallback());
                var hide = Assert.ThrowsException<NotSupportedException>(() => host.Hide());

                StringAssert.Contains(showWebView.Message, "CreateContainer");
                StringAssert.Contains(showFallback.Message, "CreateContainer");
                StringAssert.Contains(hide.Message, "Hide");
            }
        }

        [TestMethod]
        public void DockPanelHostFactorySelectorFallsBackToDebugForUnknownMode()
        {
            var factory = DockPanelHostFactorySelector.Create("UnknownMode", new MemoryLogger());

            Assert.IsInstanceOfType(factory, typeof(DebugDockPanelHostFactory));
        }

        [TestMethod]
        public void DockPanelHostFactorySelectorCanSelectCorelDockerPlaceholder()
        {
            var factory = DockPanelHostFactorySelector.Create("CorelDocker", new MemoryLogger());

            Assert.IsInstanceOfType(factory, typeof(CorelDockPanelHostFactory));
            using (var host = factory.Create(null, new MemoryLogger(), null))
            {
                Assert.AreEqual(nameof(PlaceholderCorelDockerAdapter), host.DiagnosticAdapterType);
            }
        }

        [TestMethod]
        public void DockPanelHostFactorySelectorCanSelectOfficialDockerAdapterOnlyWhenAllowed()
        {
            var factory = DockPanelHostFactorySelector.Create("CorelDocker", new MemoryLogger(), allowOfficialCorelDockerAdapter: true);

            Assert.IsInstanceOfType(factory, typeof(CorelDockPanelHostFactory));
            using (var host = factory.Create(null, new MemoryLogger(), null))
            {
                Assert.AreEqual(nameof(CorelDockerAdapter), host.DiagnosticAdapterType);
            }
        }

        [TestMethod]
        public void HostEventNotificationsUpdateStabilitySnapshot()
        {
            using (var manager = new PluginLifecycleManager(new CountingDockPanelHostFactory()))
            {
                manager.NotifyDocumentActivated();
                manager.NotifySelectionChanged();
                manager.NotifyHostShuttingDown();

                var snapshot = manager.GetStabilitySnapshot();

                Assert.AreEqual(1, snapshot.DocumentActivatedCancelCount);
                Assert.AreEqual(1, snapshot.SelectionChangedEventCount);
                Assert.AreEqual(1, snapshot.HostShutdownCancelCount);
            }
        }

        [TestMethod]
        public void LifecycleCanUseInjectedConfigForDockHostModeDiagnostics()
        {
            var corelApplication = new object();
            var dockFactory = new CountingDockPanelHostFactory();
            var configStore = new StaticConfigStore(new PluginConfig
            {
                DockHostMode = DockHostModes.CorelDocker,
                WebViewPreheatDelayMs = 60000
            });

            using (var manager = new PluginLifecycleManager(dockFactory, configStore))
            {
                manager.Start(corelApplication);
                manager.ShowPanel();

                var snapshot = manager.GetStabilitySnapshot();

                Assert.AreEqual(DockHostModes.CorelDocker, snapshot.ConfiguredDockHostMode);
                Assert.AreSame(corelApplication, dockFactory.LastCorelApplication);
            }
        }

        private static PluginStateMachine CreateReadyStateMachine()
        {
            var stateMachine = new PluginStateMachine();
            stateMachine.TransitionTo(PluginState.Preheating);
            stateMachine.TransitionTo(PluginState.Ready);
            return stateMachine;
        }

        private sealed class CountingCommand : IBridgeCommand
        {
            public string Action => "counting";
            public bool RequiresReadyState => true;
            public int ExecuteCalls { get; private set; }

            public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
            {
                ExecuteCalls++;
                return Task.FromResult(ResponseDto.Ok(request.RequestId, new JObject
                {
                    ["executeCalls"] = ExecuteCalls
                }));
            }
        }

        private sealed class FailingCommand : IBridgeCommand
        {
            public string Action => "failing";
            public bool RequiresReadyState => true;

            public Task<ResponseDto> ExecuteAsync(RequestDto request, CancellationToken token)
            {
                throw new InvalidOperationException("Simulated command failure.");
            }
        }

        private sealed class CountingDockPanelHostFactory : IDockPanelHostFactory
        {
            public int CreateCalls { get; private set; }
            public CountingDockPanelHost Host { get; } = new CountingDockPanelHost();

            public object? LastCorelApplication { get; private set; }

            public IDockPanelHost Create(WebView2Manager? webView2Manager, ILogger logger, object? corelApplication)
            {
                CreateCalls++;
                LastCorelApplication = corelApplication;
                return Host;
            }
        }

        private sealed class CountingDockPanelHost : IDockPanelHost
        {
            public int ShowCalls { get; private set; }
            public int HideCalls { get; private set; }
            public int ShowWebViewCalls { get; private set; }
            public int ShowFallbackCalls { get; private set; }
            public int DisposeCalls { get; private set; }
            public bool IsVisible { get; private set; }
            public string DiagnosticHostKind => "Counting";
            public string? DiagnosticAdapterType => "CountingAdapter";
            public bool DiagnosticIsAttached => true;

            public void Show()
            {
                ShowCalls++;
                IsVisible = true;
            }

            public void Hide()
            {
                HideCalls++;
                IsVisible = false;
            }

            public void ShowWebView()
            {
                ShowWebViewCalls++;
            }

            public void ShowFallback()
            {
                ShowFallbackCalls++;
            }

            public void Dispose()
            {
                DisposeCalls++;
                IsVisible = false;
            }
        }

        private sealed class StaticDockerAdapterFactory : ICorelDockerAdapterFactory
        {
            private readonly ICorelDockerAdapter _adapter;

            public StaticDockerAdapterFactory(ICorelDockerAdapter adapter)
            {
                _adapter = adapter;
            }

            public ICorelDockerAdapter Create()
            {
                return _adapter;
            }
        }

        private sealed class RecordingDockerAdapter : ICorelDockerAdapter
        {
            public int CreateContainerCalls { get; private set; }
            public int AttachPanelCalls { get; private set; }
            public int ShowCalls { get; private set; }
            public int HideCalls { get; private set; }
            public int ReleaseCalls { get; private set; }
            public int DisposeCalls { get; private set; }
            public bool IsVisible { get; private set; }

            public void CreateContainer(object? corelApplication)
            {
                CreateContainerCalls++;
            }

            public void AttachPanel(QiTuDockPanel panel)
            {
                AttachPanelCalls++;
            }

            public void Show()
            {
                ShowCalls++;
                IsVisible = true;
            }

            public void Hide()
            {
                HideCalls++;
                IsVisible = false;
            }

            public void Release()
            {
                ReleaseCalls++;
                IsVisible = false;
            }

            public void Dispose()
            {
                DisposeCalls++;
            }
        }

        private sealed class StaticConfigStore : IPluginConfigStore
        {
            private readonly PluginConfig _config;

            public StaticConfigStore(PluginConfig config)
            {
                _config = config;
            }

            public PluginConfig Load()
            {
                return _config;
            }

            public bool Save(PluginConfig config)
            {
                return true;
            }
        }
    }
}
