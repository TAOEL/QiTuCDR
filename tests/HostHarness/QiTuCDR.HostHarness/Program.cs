using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using QiTuCDR.Host.Lifecycle;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.HostHarness
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var app = new Application
            {
                ShutdownMode = HasAutomationArgument(args) ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose
            };

            PluginLifecycleManager? lifecycleManager = null;

            try
            {
                lifecycleManager = new PluginLifecycleManager(null, CreateConfigStore(args));
                lifecycleManager.Start(corelApplication: null);

                if (TryReadPanelStressCount(args, out var count))
                {
                    var delayMs = ReadDelayMs(args);
                    var reportPath = ReadArgumentValue(args, "--report");
                    var manager = lifecycleManager;
                    app.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await RunPanelStressAsync(manager, count, delayMs, reportPath).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                            Environment.ExitCode = 1;
                        }
                        finally
                        {
                            app.Shutdown();
                        }
                    }));

                    app.Run();
                    return;
                }

                if (TryReadRecoveryStressCount(args, out var recoveryCount))
                {
                    var delayMs = ReadDelayMs(args);
                    var reportPath = ReadArgumentValue(args, "--report");
                    var manager = lifecycleManager;
                    app.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await RunRecoveryStressAsync(manager, recoveryCount, delayMs, reportPath).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                            Environment.ExitCode = 1;
                        }
                        finally
                        {
                            app.Shutdown();
                        }
                    }));

                    app.Run();
                    return;
                }

                if (TryReadDocumentCloseStressCount(args, out var documentCloseCount))
                {
                    var delayMs = ReadDelayMs(args);
                    var reportPath = ReadArgumentValue(args, "--report");
                    var manager = lifecycleManager;
                    app.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await RunDocumentCloseStressAsync(manager, documentCloseCount, delayMs, reportPath).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                            Environment.ExitCode = 1;
                        }
                        finally
                        {
                            app.Shutdown();
                        }
                    }));

                    app.Run();
                    return;
                }

                if (TryReadHostEventStressCount(args, out var hostEventCount))
                {
                    var delayMs = ReadDelayMs(args);
                    var reportPath = ReadArgumentValue(args, "--report");
                    var manager = lifecycleManager;
                    app.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await RunHostEventStressAsync(manager, hostEventCount, delayMs, reportPath).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                            Environment.ExitCode = 1;
                        }
                        finally
                        {
                            app.Shutdown();
                        }
                    }));

                    app.Run();
                    return;
                }

                var window = new HostHarnessWindow(lifecycleManager);
                app.Run(window);
            }
            finally
            {
                lifecycleManager?.Dispose();
            }
        }

        private static async Task RunPanelStressAsync(PluginLifecycleManager lifecycleManager, int count, int delayMs, string? reportPath)
        {
            var lines = new List<string>
            {
                "# QiTuCDR HostHarness Panel Stress Report",
                string.Empty,
                "- StartedAt: " + DateTimeOffset.Now.ToString("o"),
                "- Iterations: " + count,
                "- DelayMs: " + delayMs
            };

            for (var i = 0; i < count; i++)
            {
                lifecycleManager.ShowPanel();
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.HidePanel();
                await Task.Delay(delayMs).ConfigureAwait(true);
            }

            await WaitUntilReadyAsync(lifecycleManager, TimeSpan.FromSeconds(8), delayMs).ConfigureAwait(true);

            var snapshot = lifecycleManager.GetStabilitySnapshot();
            lines.Add("- CompletedAt: " + DateTimeOffset.Now.ToString("o"));
            lines.Add(string.Empty);
            lines.Add("## Snapshot");
            lines.Add(string.Empty);
            lines.Add("- State: " + snapshot.State);
            lines.Add("- ConfiguredDockHostMode: " + snapshot.ConfiguredDockHostMode);
            lines.Add("- ActiveDockPanelHostType: " + snapshot.ActiveDockPanelHostType);
            lines.Add("- ActiveDockPanelHostKind: " + snapshot.ActiveDockPanelHostKind);
            lines.Add("- ActiveDockerAdapterType: " + snapshot.ActiveDockerAdapterType);
            lines.Add("- IsDockerAdapterAttached: " + snapshot.IsDockerAdapterAttached);
            lines.Add("- DockHostFallbackCount: " + snapshot.DockHostFallbackCount);
            lines.Add("- HasDockPanel: " + snapshot.HasDockPanel);
            lines.Add("- IsPanelVisible: " + snapshot.IsPanelVisible);
            lines.Add("- HasWebViewManager: " + snapshot.HasWebViewManager);
            lines.Add("- IsWebViewInitialized: " + snapshot.IsWebViewInitialized);
            lines.Add("- WebViewCreateCount: " + snapshot.WebViewCreateCount);
            lines.Add("- WebViewAttachCallCount: " + snapshot.WebViewAttachCallCount);
            lines.Add("- BrowserRecoveryCount: " + snapshot.BrowserRecoveryCount);
            lines.Add("- DocumentCloseCancelCount: " + snapshot.DocumentCloseCancelCount);
            lines.Add("- DocumentActivatedCancelCount: " + snapshot.DocumentActivatedCancelCount);
            lines.Add("- SelectionChangedEventCount: " + snapshot.SelectionChangedEventCount);
            lines.Add("- HostShutdownCancelCount: " + snapshot.HostShutdownCancelCount);
            lines.Add(string.Empty);
            lines.Add("## Result");
            lines.Add(string.Empty);

            var passed = snapshot.State == QiTuCDR.Shared.PluginState.Ready && snapshot.WebViewCreateCount <= 1;
            if (string.Equals(snapshot.ConfiguredDockHostMode, DockHostModes.CorelDocker, StringComparison.OrdinalIgnoreCase))
            {
                passed = passed
                    && string.Equals(snapshot.ActiveDockPanelHostType, nameof(QiTuCDR.Host.Docking.DebugDockPanelHost), StringComparison.Ordinal)
                    && snapshot.DockHostFallbackCount > 0;
            }

            lines.Add(passed ? "PASSED" : "FAILED");

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(reportPath, lines);
            }

            if (!passed)
            {
                throw new InvalidOperationException("Panel stress did not return to Ready or more than one WebView2 control was created.");
            }
        }

        private static async Task RunDocumentCloseStressAsync(PluginLifecycleManager lifecycleManager, int count, int delayMs, string? reportPath)
        {
            var lines = new List<string>
            {
                "# QiTuCDR HostHarness Document Close Stress Report",
                string.Empty,
                "- StartedAt: " + DateTimeOffset.Now.ToString("o"),
                "- Iterations: " + count,
                "- DelayMs: " + delayMs
            };

            lifecycleManager.ShowPanel();

            for (var i = 0; i < count; i++)
            {
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.NotifyDocumentClosing();
                await Task.Delay(delayMs).ConfigureAwait(true);
            }

            await WaitUntilReadyAsync(lifecycleManager, TimeSpan.FromSeconds(8), delayMs).ConfigureAwait(true);

            var snapshot = lifecycleManager.GetStabilitySnapshot();
            lines.Add("- CompletedAt: " + DateTimeOffset.Now.ToString("o"));
            lines.Add(string.Empty);
            lines.Add("## Snapshot");
            lines.Add(string.Empty);
            lines.Add("- State: " + snapshot.State);
            lines.Add("- ConfiguredDockHostMode: " + snapshot.ConfiguredDockHostMode);
            lines.Add("- ActiveDockPanelHostType: " + snapshot.ActiveDockPanelHostType);
            lines.Add("- ActiveDockPanelHostKind: " + snapshot.ActiveDockPanelHostKind);
            lines.Add("- ActiveDockerAdapterType: " + snapshot.ActiveDockerAdapterType);
            lines.Add("- IsDockerAdapterAttached: " + snapshot.IsDockerAdapterAttached);
            lines.Add("- DockHostFallbackCount: " + snapshot.DockHostFallbackCount);
            lines.Add("- HasDockPanel: " + snapshot.HasDockPanel);
            lines.Add("- IsPanelVisible: " + snapshot.IsPanelVisible);
            lines.Add("- HasWebViewManager: " + snapshot.HasWebViewManager);
            lines.Add("- IsWebViewInitialized: " + snapshot.IsWebViewInitialized);
            lines.Add("- WebViewCreateCount: " + snapshot.WebViewCreateCount);
            lines.Add("- WebViewAttachCallCount: " + snapshot.WebViewAttachCallCount);
            lines.Add("- BrowserRecoveryCount: " + snapshot.BrowserRecoveryCount);
            lines.Add("- DocumentCloseCancelCount: " + snapshot.DocumentCloseCancelCount);
            lines.Add("- DocumentActivatedCancelCount: " + snapshot.DocumentActivatedCancelCount);
            lines.Add("- SelectionChangedEventCount: " + snapshot.SelectionChangedEventCount);
            lines.Add("- HostShutdownCancelCount: " + snapshot.HostShutdownCancelCount);
            lines.Add(string.Empty);
            lines.Add("## Result");
            lines.Add(string.Empty);

            var passed = snapshot.State == QiTuCDR.Shared.PluginState.Ready && snapshot.DocumentCloseCancelCount >= count;
            lines.Add(passed ? "PASSED" : "FAILED");

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(reportPath, lines);
            }

            if (!passed)
            {
                throw new InvalidOperationException("Document close stress did not return to Ready or did not cancel expected tasks.");
            }
        }

        private static async Task RunRecoveryStressAsync(PluginLifecycleManager lifecycleManager, int count, int delayMs, string? reportPath)
        {
            var lines = new List<string>
            {
                "# QiTuCDR HostHarness Recovery Stress Report",
                string.Empty,
                "- StartedAt: " + DateTimeOffset.Now.ToString("o"),
                "- Iterations: " + count,
                "- DelayMs: " + delayMs
            };

            lifecycleManager.ShowPanel();

            for (var i = 0; i < count; i++)
            {
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.SimulateBrowserFailureForDiagnostics();
                await Task.Delay(delayMs).ConfigureAwait(true);
            }

            var snapshot = lifecycleManager.GetStabilitySnapshot();
            lines.Add("- CompletedAt: " + DateTimeOffset.Now.ToString("o"));
            lines.Add(string.Empty);
            lines.Add("## Snapshot");
            lines.Add(string.Empty);
            lines.Add("- State: " + snapshot.State);
            lines.Add("- ConfiguredDockHostMode: " + snapshot.ConfiguredDockHostMode);
            lines.Add("- ActiveDockPanelHostType: " + snapshot.ActiveDockPanelHostType);
            lines.Add("- ActiveDockPanelHostKind: " + snapshot.ActiveDockPanelHostKind);
            lines.Add("- ActiveDockerAdapterType: " + snapshot.ActiveDockerAdapterType);
            lines.Add("- IsDockerAdapterAttached: " + snapshot.IsDockerAdapterAttached);
            lines.Add("- DockHostFallbackCount: " + snapshot.DockHostFallbackCount);
            lines.Add("- HasDockPanel: " + snapshot.HasDockPanel);
            lines.Add("- IsPanelVisible: " + snapshot.IsPanelVisible);
            lines.Add("- HasWebViewManager: " + snapshot.HasWebViewManager);
            lines.Add("- IsWebViewInitialized: " + snapshot.IsWebViewInitialized);
            lines.Add("- WebViewCreateCount: " + snapshot.WebViewCreateCount);
            lines.Add("- WebViewAttachCallCount: " + snapshot.WebViewAttachCallCount);
            lines.Add("- BrowserRecoveryCount: " + snapshot.BrowserRecoveryCount);
            lines.Add("- DocumentCloseCancelCount: " + snapshot.DocumentCloseCancelCount);
            lines.Add("- DocumentActivatedCancelCount: " + snapshot.DocumentActivatedCancelCount);
            lines.Add("- SelectionChangedEventCount: " + snapshot.SelectionChangedEventCount);
            lines.Add("- HostShutdownCancelCount: " + snapshot.HostShutdownCancelCount);
            lines.Add(string.Empty);
            lines.Add("## Result");
            lines.Add(string.Empty);

            var passed = snapshot.State == QiTuCDR.Shared.PluginState.Ready && snapshot.BrowserRecoveryCount >= count;
            lines.Add(passed ? "PASSED" : "FAILED");

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(reportPath, lines);
            }

            if (!passed)
            {
                throw new InvalidOperationException("Browser recovery stress did not return to Ready.");
            }
        }

        private static async Task RunHostEventStressAsync(PluginLifecycleManager lifecycleManager, int count, int delayMs, string? reportPath)
        {
            var lines = new List<string>
            {
                "# QiTuCDR HostHarness Host Event Stress Report",
                string.Empty,
                "- StartedAt: " + DateTimeOffset.Now.ToString("o"),
                "- Iterations: " + count,
                "- DelayMs: " + delayMs
            };

            lifecycleManager.ShowPanel();

            for (var i = 0; i < count; i++)
            {
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.NotifyDocumentActivated();
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.NotifySelectionChanged();
                await Task.Delay(delayMs).ConfigureAwait(true);
                lifecycleManager.NotifyDocumentClosing();
            }

            await Task.Delay(delayMs).ConfigureAwait(true);
            lifecycleManager.NotifyHostShuttingDown();
            await WaitUntilReadyAsync(lifecycleManager, TimeSpan.FromSeconds(8), delayMs).ConfigureAwait(true);

            var snapshot = lifecycleManager.GetStabilitySnapshot();
            lines.Add("- CompletedAt: " + DateTimeOffset.Now.ToString("o"));
            lines.Add(string.Empty);
            lines.Add("## Snapshot");
            lines.Add(string.Empty);
            lines.Add("- State: " + snapshot.State);
            lines.Add("- ConfiguredDockHostMode: " + snapshot.ConfiguredDockHostMode);
            lines.Add("- ActiveDockPanelHostType: " + snapshot.ActiveDockPanelHostType);
            lines.Add("- ActiveDockPanelHostKind: " + snapshot.ActiveDockPanelHostKind);
            lines.Add("- ActiveDockerAdapterType: " + snapshot.ActiveDockerAdapterType);
            lines.Add("- IsDockerAdapterAttached: " + snapshot.IsDockerAdapterAttached);
            lines.Add("- DockHostFallbackCount: " + snapshot.DockHostFallbackCount);
            lines.Add("- HasDockPanel: " + snapshot.HasDockPanel);
            lines.Add("- IsPanelVisible: " + snapshot.IsPanelVisible);
            lines.Add("- HasWebViewManager: " + snapshot.HasWebViewManager);
            lines.Add("- IsWebViewInitialized: " + snapshot.IsWebViewInitialized);
            lines.Add("- WebViewCreateCount: " + snapshot.WebViewCreateCount);
            lines.Add("- WebViewAttachCallCount: " + snapshot.WebViewAttachCallCount);
            lines.Add("- BrowserRecoveryCount: " + snapshot.BrowserRecoveryCount);
            lines.Add("- DocumentCloseCancelCount: " + snapshot.DocumentCloseCancelCount);
            lines.Add("- DocumentActivatedCancelCount: " + snapshot.DocumentActivatedCancelCount);
            lines.Add("- SelectionChangedEventCount: " + snapshot.SelectionChangedEventCount);
            lines.Add("- HostShutdownCancelCount: " + snapshot.HostShutdownCancelCount);
            lines.Add(string.Empty);
            lines.Add("## Result");
            lines.Add(string.Empty);

            var passed = snapshot.State == QiTuCDR.Shared.PluginState.Ready
                && snapshot.DocumentActivatedCancelCount >= count
                && snapshot.SelectionChangedEventCount >= count
                && snapshot.DocumentCloseCancelCount >= count
                && snapshot.HostShutdownCancelCount >= 1;
            lines.Add(passed ? "PASSED" : "FAILED");

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(reportPath, lines);
            }

            if (!passed)
            {
                throw new InvalidOperationException("Host event stress did not return expected stability counters.");
            }
        }

        private static async Task WaitUntilReadyAsync(PluginLifecycleManager lifecycleManager, TimeSpan timeout, int delayMs)
        {
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            var waitDelayMs = Math.Max(25, delayMs);

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (lifecycleManager.GetStabilitySnapshot().State == QiTuCDR.Shared.PluginState.Ready)
                {
                    return;
                }

                await Task.Delay(waitDelayMs).ConfigureAwait(true);
            }
        }

        private static bool HasAutomationArgument(string[] args)
        {
            return HasPanelStressArgument(args)
                || Array.IndexOf(args, "--recovery-stress") >= 0
                || Array.IndexOf(args, "--document-close-stress") >= 0
                || Array.IndexOf(args, "--host-event-stress") >= 0;
        }

        private static IPluginConfigStore? CreateConfigStore(string[] args)
        {
            var dockHostMode = ReadArgumentValue(args, "--dock-host-mode");
            if (string.IsNullOrWhiteSpace(dockHostMode))
            {
                return null;
            }

            var normalized = dockHostMode!.Trim();
            if (!string.Equals(normalized, DockHostModes.Debug, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(normalized, DockHostModes.CorelDocker, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("--dock-host-mode must be Debug or CorelDocker.");
            }

            return new StaticConfigStore(new PluginConfig
            {
                DockHostMode = string.Equals(normalized, DockHostModes.CorelDocker, StringComparison.OrdinalIgnoreCase)
                    ? DockHostModes.CorelDocker
                    : DockHostModes.Debug
            });
        }

        private static bool HasPanelStressArgument(string[] args)
        {
            return Array.IndexOf(args, "--panel-stress") >= 0;
        }

        private static bool TryReadPanelStressCount(string[] args, out int count)
        {
            count = 0;
            var index = Array.IndexOf(args, "--panel-stress");
            if (index < 0)
            {
                return false;
            }

            count = 100;
            if (index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed > 0)
            {
                count = parsed;
            }

            return true;
        }

        private static bool TryReadDocumentCloseStressCount(string[] args, out int count)
        {
            count = 0;
            var index = Array.IndexOf(args, "--document-close-stress");
            if (index < 0)
            {
                return false;
            }

            count = 1;
            if (index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed > 0)
            {
                count = parsed;
            }

            return true;
        }

        private static bool TryReadRecoveryStressCount(string[] args, out int count)
        {
            count = 0;
            var index = Array.IndexOf(args, "--recovery-stress");
            if (index < 0)
            {
                return false;
            }

            count = 1;
            if (index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed > 0)
            {
                count = parsed;
            }

            return true;
        }

        private static bool TryReadHostEventStressCount(string[] args, out int count)
        {
            count = 0;
            var index = Array.IndexOf(args, "--host-event-stress");
            if (index < 0)
            {
                return false;
            }

            count = 1;
            if (index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed > 0)
            {
                count = parsed;
            }

            return true;
        }

        private static int ReadDelayMs(string[] args)
        {
            var index = Array.IndexOf(args, "--delay-ms");
            if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed >= 0)
            {
                return parsed;
            }

            return 10;
        }

        private static string? ReadArgumentValue(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);
            if (index >= 0 && index + 1 < args.Length)
            {
                return args[index + 1];
            }

            return null;
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
