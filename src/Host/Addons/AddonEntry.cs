using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QiTuCDR.Host.Lifecycle;
using QiTuCDR.Host.NativePanels;

namespace QiTuCDR.Host.Addons
{
    public sealed class AddonEntry : UserControl, IDisposable
    {
        private static readonly object LifecycleSync = new object();
        private static PluginLifecycleManager? s_lifecycleManager;
        private static bool s_lifecycleStarted;

        private bool _started;
        private bool _disposed;

        public AddonEntry()
        {
            MinHeight = 30;
            MaxHeight = 30;
            Height = 30;
            MinWidth = 760;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            ClipToBounds = true;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            WriteAddonMarker("loaded");
            Content = CreateSafeShell();
            if (IsLifecycleExplicitlyEnabled())
            {
                StartLifecycle();
            }
            else
            {
                WriteAddonMarker("safe-shell");
            }
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteAddonMarker("unloaded");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
            WriteAddonMarker("disposed");
        }

        private static bool IsLifecycleExplicitlyEnabled()
        {
            return string.Equals(
                System.Environment.GetEnvironmentVariable("QITUCDR_ENABLE_ADDON_LIFECYCLE"),
                "1",
                StringComparison.Ordinal);
        }

        private UIElement CreateSafeShell()
        {
            var panel = new DockPanel
            {
                MinWidth = 760,
                MinHeight = 30,
                MaxHeight = 30,
                Height = 30,
                LastChildFill = false,
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            AddToolbarButton(panel, "企图插件", "/", true);
            AddSeparator(panel);
            AddToolbarMenu(panel, "工具面板", new[]
            {
                ToolbarMenuItem.Native("批量转曲", NativeToolWindowManager.ConvertText),
                ToolbarMenuItem.Native("一键居中", NativeToolWindowManager.CenterObjects),
                ToolbarMenuItem.Native("冗余清理", NativeToolWindowManager.CleanupRedundant),
                ToolbarMenuItem.Native("尺寸规整", NativeToolWindowManager.NormalizeSize)
            }, false);
            AddToolbarMenu(panel, "排版工具", new[]
            {
                ToolbarMenuItem.Native("一键居中", NativeToolWindowManager.CenterObjects),
                ToolbarMenuItem.Native("统一尺寸", NativeToolWindowManager.NormalizeSize),
                ToolbarMenuItem.Native("批量对齐", "batchAlign"),
                ToolbarMenuItem.Native("等距分布", "equalDistribution")
            }, false);
            AddToolbarMenu(panel, "文字工具", new[]
            {
                ToolbarMenuItem.Native("批量转曲", NativeToolWindowManager.ConvertText),
                ToolbarMenuItem.Native("空文本清理", "emptyTextCleanup"),
                ToolbarMenuItem.Native("文字统计", "textStatistics")
            }, false);
            AddToolbarMenu(panel, "清理工具", new[]
            {
                ToolbarMenuItem.Native("冗余清理", NativeToolWindowManager.CleanupRedundant),
                ToolbarMenuItem.Native("隐藏图层检查", "hiddenLayerCheck"),
                ToolbarMenuItem.Native("辅助线清理", "guidelineCleanup")
            }, false);

            return panel;
        }

        private void AddToolbarMenu(DockPanel panel, string text, ToolbarMenuItem[] menuItems, bool primary)
        {
            var button = CreateToolbarToggleButton(text, primary);
            var popup = CreateToolbarPopup(button, menuItems);
            button.Click += (sender, args) =>
            {
                popup.IsOpen = button.IsChecked == true;
                if (primary)
                {
                    StartLifecycle("/");
                }
            };
            popup.Closed += (sender, args) => button.IsChecked = false;
            DockPanel.SetDock(button, Dock.Left);
            panel.Children.Add(button);
        }

        private ToggleButton CreateToolbarToggleButton(string text, bool primary)
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            content.Children.Add(new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(new TextBlock
            {
                Text = "▼",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(178, 190, 195)),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            return new ToggleButton
            {
                Content = content,
                Height = 22,
                MinWidth = primary ? 90 : 82,
                Margin = new Thickness(2, 4, 0, 4),
                Padding = new Thickness(10, 0, 10, 1),
                Background = primary ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 52, 54)),
                BorderBrush = primary ? new SolidColorBrush(Color.FromRgb(129, 199, 132)) : Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private Popup CreateToolbarPopup(ToggleButton placementTarget, ToolbarMenuItem[] menuItems)
        {
            var stack = new StackPanel();
            foreach (var item in menuItems)
            {
                var button = new Button
                {
                    Content = item.Label,
                    Height = 34,
                    MinWidth = 150,
                    Padding = new Thickness(14, 0, 14, 0),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.White,
                    Foreground = new SolidColorBrush(Color.FromRgb(45, 52, 54)),
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                button.Click += (sender, args) => ExecuteToolbarItem(item);
                stack.Children.Add(button);
            }

            return new Popup
            {
                PlacementTarget = placementTarget,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                Child = new Border
                {
                    MinWidth = 150,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(232, 236, 239)),
                    BorderThickness = new Thickness(1),
                    Child = stack
                }
            };
        }

        private void AddToolbarButton(DockPanel panel, string text, string route, bool primary = false)
        {
            var button = new Button
            {
                Content = text,
                ToolTip = "打开 QiTuCDR 综合面板",
                Height = 22,
                MinWidth = primary ? 90 : 82,
                Margin = new Thickness(2, 4, 0, 4),
                Padding = new Thickness(10, 0, 10, 1),
                Background = primary ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 52, 54)),
                BorderBrush = primary ? new SolidColorBrush(Color.FromRgb(129, 199, 132)) : Brushes.Transparent,
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            };
            button.Click += (sender, args) => StartLifecycle(route);
            DockPanel.SetDock(button, Dock.Left);
            panel.Children.Add(button);
        }

        private static void AddSeparator(DockPanel panel)
        {
            var separator = new Border
            {
                Width = 1,
                Height = 16,
                Margin = new Thickness(8, 7, 6, 7),
                Background = new SolidColorBrush(Color.FromRgb(232, 236, 239))
            };
            DockPanel.SetDock(separator, Dock.Left);
            panel.Children.Add(separator);
        }

        private void StartLifecycle(string route = "/")
        {
            if (_disposed)
            {
                return;
            }

            lock (LifecycleSync)
            {
                if (!s_lifecycleStarted)
                {
                    s_lifecycleStarted = true;
                    WriteAddonMarker("lifecycle-starting");
                    s_lifecycleManager = new PluginLifecycleManager(new AddonDockPanelHostFactory());
                    s_lifecycleManager.Start(null);
                    WriteAddonMarker("lifecycle-started");
                }

                s_lifecycleManager?.ShowPanel(route);
            }
        }

        private void ExecuteToolbarItem(ToolbarMenuItem item)
        {
            if (item.IsNativeTool)
            {
                NativeToolWindowManager.OpenTool(item.Key, item.Label);
                return;
            }

            StartLifecycle(item.Route);
        }

        private sealed class ToolbarMenuItem
        {
            private ToolbarMenuItem(string label, string key, bool isNativeTool)
            {
                Label = label;
                Key = key;
                IsNativeTool = isNativeTool;
            }

            public string Label { get; }

            public string Key { get; }

            public string Route => Key;

            public bool IsNativeTool { get; }

            public static ToolbarMenuItem Web(string label, string route)
            {
                return new ToolbarMenuItem(label, route, false);
            }

            public static ToolbarMenuItem Native(string label, string key)
            {
                return new ToolbarMenuItem(label, key, true);
            }
        }

        private static void WriteAddonMarker(string state)
        {
            try
            {
                var root = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "QiTuCDR",
                    "Logs");
                Directory.CreateDirectory(root);
                var path = Path.Combine(root, "coreldraw-addon-entry.log");
                File.AppendAllText(path, DateTimeOffset.Now.ToString("O") + " " + state + System.Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
