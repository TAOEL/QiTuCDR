using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class PluginConfigStoreTests
    {
        private string? _tempDirectory;
        private string? _settingsPath;

        [TestInitialize]
        public void Initialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "QiTuCDR.Tests." + Guid.NewGuid().ToString("N"));
            _settingsPath = Path.Combine(_tempDirectory, "settings.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            var tempDirectory = _tempDirectory ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tempDirectory))
            {
                return;
            }

            var directoryName = Path.GetFileName(tempDirectory);
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                return;
            }

            var tempRoot = Path.GetTempPath();
            if (!Directory.Exists(tempDirectory)
                || !tempDirectory.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                || !directoryName.StartsWith("QiTuCDR.Tests.", StringComparison.Ordinal))
            {
                return;
            }

            Directory.Delete(tempDirectory, recursive: true);
        }

        [TestMethod]
        public void LoadCreatesDefaultConfigWhenFileDoesNotExist()
        {
            var store = new JsonPluginConfigStore(_settingsPath);

            var config = store.Load();

            Assert.AreEqual(4000, config.WebViewPreheatDelayMs);
            Assert.AreEqual(50, config.BatchSize);
            Assert.AreEqual(120000, config.TaskTimeoutMs);
            Assert.IsFalse(config.PreferTypedCorelInterop);
            Assert.IsFalse(config.AllowOfficialCorelDockerAdapter);
            Assert.AreEqual(DockHostModes.Debug, config.DockHostMode);
            Assert.IsNotNull(config.NativePanel);
            Assert.IsFalse(config.NativePanel.WindowTopmost);
            Assert.IsTrue(config.NativePanel.SaveWindowPosition);
            Assert.IsTrue(config.NativePanel.SaveToolSettings);
            Assert.IsFalse(config.NativePanel.AutoBackupOriginalFile);
            Assert.IsTrue(config.NativePanel.ShowTaskCompletedToast);
            Assert.IsTrue(File.Exists(_settingsPath));
        }

        [TestMethod]
        public void LoadReadsExistingConfigFile()
        {
            Directory.CreateDirectory(_tempDirectory!);
            File.WriteAllText(_settingsPath!, "{\"webViewPreheatDelayMs\":1000,\"batchSize\":25,\"taskTimeoutMs\":30000,\"preferTypedCorelInterop\":true,\"allowOfficialCorelDockerAdapter\":true,\"dockHostMode\":\"CorelDocker\"}");
            var store = new JsonPluginConfigStore(_settingsPath);

            var config = store.Load();

            Assert.AreEqual(1000, config.WebViewPreheatDelayMs);
            Assert.AreEqual(25, config.BatchSize);
            Assert.AreEqual(30000, config.TaskTimeoutMs);
            Assert.IsTrue(config.PreferTypedCorelInterop);
            Assert.IsTrue(config.AllowOfficialCorelDockerAdapter);
            Assert.AreEqual(DockHostModes.CorelDocker, config.DockHostMode);
            Assert.IsNotNull(config.NativePanel);
            Assert.IsTrue(config.NativePanel.SaveWindowPosition);
        }

        [TestMethod]
        public void SaveAndLoadPersistsNativePanelSettings()
        {
            var store = new JsonPluginConfigStore(_settingsPath);
            var config = new PluginConfig
            {
                NativePanel =
                {
                    WindowTopmost = true,
                    SaveWindowPosition = true,
                    SaveToolSettings = false,
                    AutoBackupOriginalFile = true,
                    ShowTaskCompletedToast = false
                }
            };
            config.NativePanel.ToolWindowPositions["convertText"] = new WindowPositionConfig
            {
                Left = 101,
                Top = 202
            };
            config.NativePanel.PopupWindowPositions["popup"] = new WindowPositionConfig
            {
                Left = 303,
                Top = 404
            };

            Assert.IsTrue(store.Save(config));

            var loaded = store.Load();

            Assert.IsTrue(loaded.NativePanel.WindowTopmost);
            Assert.IsTrue(loaded.NativePanel.SaveWindowPosition);
            Assert.IsFalse(loaded.NativePanel.SaveToolSettings);
            Assert.IsTrue(loaded.NativePanel.AutoBackupOriginalFile);
            Assert.IsFalse(loaded.NativePanel.ShowTaskCompletedToast);
            Assert.AreEqual(101, loaded.NativePanel.ToolWindowPositions["convertText"].Left);
            Assert.AreEqual(202, loaded.NativePanel.ToolWindowPositions["convertText"].Top);
            Assert.AreEqual(303, loaded.NativePanel.PopupWindowPositions["popup"].Left);
            Assert.AreEqual(404, loaded.NativePanel.PopupWindowPositions["popup"].Top);
        }

        [TestMethod]
        public void LoadBacksUpBrokenConfigAndFallsBackToDefault()
        {
            Directory.CreateDirectory(_tempDirectory!);
            File.WriteAllText(_settingsPath!, "{ broken json");
            var store = new JsonPluginConfigStore(_settingsPath);

            var config = store.Load();

            Assert.AreEqual(4000, config.WebViewPreheatDelayMs);
            Assert.AreEqual(50, config.BatchSize);
            Assert.AreEqual(120000, config.TaskTimeoutMs);
            Assert.IsTrue(Directory.GetFiles(_tempDirectory!, "settings.json.bad.*").Any());
        }
    }
}
