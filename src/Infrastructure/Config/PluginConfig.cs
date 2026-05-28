namespace QiTuCDR.Infrastructure.Config
{
    using System.Collections.Generic;

    public sealed class PluginConfig
    {
        public int WebViewPreheatDelayMs { get; set; } = 4000;
        public int BatchSize { get; set; } = 50;
        public int TaskTimeoutMs { get; set; } = 120000;
        public bool PreferTypedCorelInterop { get; set; }
        public bool AllowOfficialCorelDockerAdapter { get; set; }
        public string DockHostMode { get; set; } = DockHostModes.Debug;
        public NativePanelConfig NativePanel { get; set; } = new NativePanelConfig();

        public void Normalize()
        {
            if (NativePanel == null)
            {
                NativePanel = new NativePanelConfig();
            }

            NativePanel.Normalize();
        }
    }

    public sealed class NativePanelConfig
    {
        public bool WindowTopmost { get; set; }
        public bool SaveWindowPosition { get; set; } = true;
        public bool SaveToolSettings { get; set; } = true;
        public bool AutoBackupOriginalFile { get; set; }
        public bool ShowTaskCompletedToast { get; set; } = true;
        public Dictionary<string, WindowPositionConfig> ToolWindowPositions { get; set; } = new Dictionary<string, WindowPositionConfig>();
        public Dictionary<string, WindowPositionConfig> PopupWindowPositions { get; set; } = new Dictionary<string, WindowPositionConfig>();

        public void Normalize()
        {
            if (ToolWindowPositions == null)
            {
                ToolWindowPositions = new Dictionary<string, WindowPositionConfig>();
            }

            if (PopupWindowPositions == null)
            {
                PopupWindowPositions = new Dictionary<string, WindowPositionConfig>();
            }
        }
    }

    public sealed class WindowPositionConfig
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
