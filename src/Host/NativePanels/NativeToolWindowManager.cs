using System;
using System.Windows;
using System.Windows.Interop;
using QiTuCDR.Host.NativePanels.Panels;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.Host.NativePanels
{
    public static class NativeToolWindowManager
    {
        public const string ConvertText = "convertText";
        public const string CenterObjects = "centerObjects";
        public const string CleanupRedundant = "cleanupRedundant";
        public const string NormalizeSize = "normalizeSize";

        private static readonly IPluginConfigStore s_configStore = new JsonPluginConfigStore();
        private static NativeToolWindow? s_activeWindow;
        private static string? s_activeKey;
        private static string? s_activeTitle;

        public static NativePanelThemeMode Theme => NativePanelResourceLoader.CurrentTheme;

        public static void SetTheme(NativePanelThemeMode theme)
        {
            if (Theme == theme)
            {
                return;
            }

            var activeKey = s_activeKey;
            var activeTitle = s_activeTitle;
            NativePanelResourceLoader.SetTheme(theme);

            if (s_activeWindow != null && activeKey != null && activeTitle != null)
            {
                CloseCurrent();
                OpenTool(activeKey, activeTitle);
            }
        }

        public static void OpenTool(string key, string title)
        {
            if (ActivateIfSame(key))
            {
                return;
            }

            CloseCurrent();
            var content = CreateContent(key, title);
            var config = s_configStore.Load();
            var window = new NativeToolWindow(
                key,
                title,
                content,
                "原生 WPF 独立窗口，下一步接入现有 Core 命令链路",
                config,
                s_configStore);
            window.Closed += OnWindowClosed;
            TryAttachOwner(window);
            window.Show();
            s_activeWindow = window;
            s_activeKey = key;
            s_activeTitle = title;
        }

        public static void CloseCurrent()
        {
            if (s_activeWindow == null)
            {
                return;
            }

            s_activeWindow.Closed -= OnWindowClosed;
            s_activeWindow.Close();
            s_activeWindow = null;
            s_activeKey = null;
            s_activeTitle = null;
        }

        private static bool ActivateIfSame(string key)
        {
            if (s_activeWindow == null || !string.Equals(s_activeKey, key, StringComparison.Ordinal))
            {
                return false;
            }

            if (s_activeWindow.WindowState == WindowState.Minimized)
            {
                s_activeWindow.WindowState = WindowState.Normal;
            }

            s_activeWindow.Activate();
            return true;
        }

        private static UIElement CreateContent(string key, string title)
        {
            switch (key)
            {
                case ConvertText:
                    return new ConvertTextPanel();
                case CenterObjects:
                    return new CenterPanel();
                case CleanupRedundant:
                    return new CleanupPanel();
                case NormalizeSize:
                    return new NormalizePanel();
                default:
                    return new PlaceholderToolPanel(title);
            }
        }

        private static void TryAttachOwner(Window window)
        {
            try
            {
                var owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (owner != IntPtr.Zero)
                {
                    new WindowInteropHelper(window).Owner = owner;
                }
            }
            catch
            {
            }
        }

        private static void OnWindowClosed(object? sender, EventArgs e)
        {
            s_activeWindow = null;
            s_activeKey = null;
            s_activeTitle = null;
        }
    }
}
