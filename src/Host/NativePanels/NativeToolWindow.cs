using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.Host.NativePanels
{
    internal sealed class NativeToolWindow : Window
    {
        private const string DisplayVersion = "1.0.0";

        private readonly string _toolKey;
        private readonly string _title;
        private readonly PluginConfig _config;
        private readonly IPluginConfigStore? _configStore;
        private Border? _bodyHost;
        private UIElement? _statusBar;
        private Button? _pinButton;
        private Button? _collapseButton;
        private NativeToolPopupWindow? _activePopup;
        private bool _isCollapsed;
        private double _expandedHeight;

        public NativeToolWindow(
            string toolKey,
            string title,
            UIElement content,
            string statusText,
            PluginConfig? config = null,
            IPluginConfigStore? configStore = null)
        {
            _toolKey = toolKey;
            _title = title;
            _config = config ?? new PluginConfig();
            _config.Normalize();
            _configStore = configStore;
            Title = title + " - QiTuCDR";
            Width = 436;
            MinWidth = 416;
            MaxHeight = 720;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            NativePanelResourceLoader.MergeInto(this);
            Background = Brushes.Transparent;
            ApplyConfigBeforeShow();
            Content = CreateLayout(title, content, statusText);
        }

        private UIElement CreateLayout(string title, UIElement content, string statusText)
        {
            var outer = new Border();
            outer.SetResourceReference(StyleProperty, "QiTuToolWindowOuterBorder");

            var root = new Grid { ClipToBounds = true };
            root.SizeChanged += (sender, args) =>
            {
                root.Clip = new RectangleGeometry(
                    new Rect(0, 0, root.ActualWidth, root.ActualHeight),
                    6,
                    6);
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBar = CreateTitleBar(title);
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            _bodyHost = new Border
            {
                Padding = new Thickness(16),
                Child = content
            };
            _bodyHost.SetResourceReference(BackgroundProperty, "QiTuBgBase");
            Grid.SetRow(_bodyHost, 1);
            root.Children.Add(_bodyHost);

            _statusBar = CreateStatusBar(statusText);
            Grid.SetRow(_statusBar, 2);
            root.Children.Add(_statusBar);

            outer.Child = root;

            var chromeRoot = new Grid
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(8)
            };
            chromeRoot.Children.Add(outer);

            return chromeRoot;
        }

        private UIElement CreateTitleBar(string title)
        {
            var titleBar = new Border();
            titleBar.SetResourceReference(StyleProperty, "QiTuToolWindowTitleBar");
            titleBar.MouseLeftButtonDown += OnTitleBarMouseLeftButtonDown;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var markText = new TextBlock
            {
                Text = "企",
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            markText.SetResourceReference(TextBlock.ForegroundProperty, "QiTuTextOnAccent");

            var mark = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(4),
                Child = markText
            };
            mark.SetResourceReference(BackgroundProperty, "QiTuAccentPrimary");
            Grid.SetColumn(mark, 0);
            grid.Children.Add(mark);

            var titleStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleBlock = new TextBlock
            {
                Text = title
            };
            titleBlock.SetResourceReference(StyleProperty, "QiTuToolWindowTitle");
            titleStack.Children.Add(titleBlock);

            var versionButton = new Button
            {
                Content = DisplayVersion,
                ToolTip = "版本信息",
                Margin = new Thickness(8, 0, 0, 0)
            };
            versionButton.SetResourceReference(StyleProperty, "QiTuWindowVersionButton");
            versionButton.Click += OnVersionClicked;
            titleStack.Children.Add(versionButton);

            Grid.SetColumn(titleStack, 1);
            titleStack.Margin = new Thickness(8, 0, 0, 0);
            grid.Children.Add(titleStack);

            var controls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetColumn(controls, 2);
            grid.Children.Add(controls);

            _pinButton = CreateIconButton("置顶", CreatePinIcon());
            _pinButton.Click += OnPinClicked;
            RefreshPinButton();
            controls.Children.Add(_pinButton);

            var settings = CreateIconButton("设置", CreateSettingsIcon());
            settings.Click += OnSettingsClicked;
            controls.Children.Add(settings);

            _collapseButton = CreateIconButton("收起", CreateCollapseIcon(false));
            _collapseButton.Click += OnCollapseClicked;
            controls.Children.Add(_collapseButton);

            var close = CreateIconButton("关闭", CreateCloseIcon());
            close.SetResourceReference(StyleProperty, "QiTuWindowCloseButton");
            close.Click += (sender, args) => Close();
            controls.Children.Add(close);

            titleBar.Child = grid;
            return titleBar;
        }

        private Button CreateIconButton(string tooltip, UIElement icon)
        {
            var button = new Button
            {
                Content = icon,
                ToolTip = tooltip
            };
            button.SetResourceReference(StyleProperty, "QiTuWindowIconButton");
            return button;
        }

        private UIElement CreatePinIcon()
        {
            var icon = new Path
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Data = Geometry.Parse("M5,1 L9,1 M7,1 L7,6 M4,6 L10,6 M5,6 L4,10 L10,10 L9,6 M7,10 L7,13")
            };
            BindIconStroke(icon);
            return icon;
        }

        private UIElement CreateSettingsIcon()
        {
            return CreateSettingsVectorIcon();
        }

        private UIElement CreateSettingsVectorIcon()
        {
            var icon = new Path
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Data = Geometry.Parse("M7,2 L7,4 M7,10 L7,12 M2,7 L4,7 M10,7 L12,7 M3.5,3.5 L4.9,4.9 M9.1,9.1 L10.5,10.5 M10.5,3.5 L9.1,4.9 M4.9,9.1 L3.5,10.5 M5,7 A2,2 0 1 0 9,7 A2,2 0 1 0 5,7")
            };
            BindIconStroke(icon);
            return icon;
        }

        private UIElement CreateCollapseIcon(bool expanded)
        {
            var icon = new Path
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Data = expanded
                    ? Geometry.Parse("M3,8 L7,4 L11,8")
                    : Geometry.Parse("M3,5 L7,9 L11,5")
            };
            BindIconStroke(icon);
            return icon;
        }

        private UIElement CreateCloseIcon()
        {
            var icon = new Path
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse("M3,3 L9,9 M9,3 L3,9")
            };
            BindIconStroke(icon);
            return icon;
        }

        private static void BindIconStroke(Shape icon)
        {
            icon.SetBinding(Shape.StrokeProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
        }

        private UIElement CreateStatusBar(string statusText)
        {
            var border = new Border
            {
                Height = 28,
                Padding = new Thickness(12, 0, 12, 0),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            border.SetResourceReference(BackgroundProperty, "QiTuBgSurface");
            border.SetResourceReference(BorderBrushProperty, "QiTuBorderDefault");

            var text = new TextBlock { Text = statusText };
            text.SetResourceReference(StyleProperty, "QiTuToolWindowStatus");
            border.Child = text;
            return border;
        }

        private void ApplyConfigBeforeShow()
        {
            Topmost = _config.NativePanel.WindowTopmost;

            if (!_config.NativePanel.SaveWindowPosition)
            {
                return;
            }

            if (!_config.NativePanel.ToolWindowPositions.TryGetValue(_toolKey, out var position))
            {
                return;
            }

            if (!IsUsablePosition(position.Left, position.Top))
            {
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = position.Left;
            Top = position.Top;
        }

        private void OnPinClicked(object sender, RoutedEventArgs e)
        {
            SetTopmost(!Topmost);
        }

        private void SetTopmost(bool value)
        {
            Topmost = value;
            _config.NativePanel.WindowTopmost = value;
            SaveConfig();
            RefreshPinButton();

            if (_activePopup != null)
            {
                _activePopup.Topmost = value;
            }
        }

        private void RefreshPinButton()
        {
            if (_pinButton == null)
            {
                return;
            }

            _pinButton.Opacity = Topmost ? 1 : 0.72;
            _pinButton.ToolTip = Topmost ? "取消置顶" : "置顶";
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            ShowPopup("设置", CreateSettingsOverlayContent());
        }

        private void OnVersionClicked(object sender, RoutedEventArgs e)
        {
            ShowPopup("版本信息", CreateVersionOverlayContent());
        }

        private void ShowPopup(string title, UIElement content)
        {
            CloseActivePopup();

            _activePopup = new NativeToolPopupWindow(
                this,
                title,
                content,
                LoadPopupPosition,
                SavePopupPosition)
            {
                Topmost = Topmost
            };
            _activePopup.Closed += (sender, args) =>
            {
                if (ReferenceEquals(sender, _activePopup))
                {
                    _activePopup = null;
                }
            };
            _activePopup.PositionNearOwner();
            _activePopup.Show();
        }

        private UIElement CreateVersionOverlayContent()
        {
            var list = new StackPanel();

            AddVersionItem(
                list,
                "V1.0.0",
                "2026-05-27",
                "新增独立 WPF 小窗口标题栏控件、版本信息入口和设置入口。",
                "统一 420px 视觉宽度、32px 标题栏、6px 圆角和 4px 网格体系。",
                "继续保持单 WebView2 原则，独立小窗口不创建 WebView2。");
            AddVersionItem(
                list,
                "V0.9.6",
                "2026-05-26",
                "完善工具条一级菜单和二级菜单结构。",
                "支持二级工具打开独立 WPF 功能窗口。",
                "增加 NativePanelPreview，用于不启动 CorelDRAW 的可视化验证。");
            AddVersionItem(
                list,
                "V0.9.2",
                "2026-05-25",
                "完成 Bridge Echo、状态机、日志、配置和基础工具链路。",
                "落地批量转曲、一键居中、冗余清理和尺寸规整的架构骨架。");
            AddVersionItem(
                list,
                "V0.8.0",
                "2026-05-24",
                "建立 QiTuCDR 工程目录、Native First 架构和基础文档。",
                "确认 CorelDRAW 宿主稳定性优先的开发红线。");

            return CreateOverlayScrollViewer(list);
        }

        private void AddVersionItem(StackPanel list, string version, string date, params string[] lines)
        {
            var item = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var versionText = new TextBlock
            {
                Text = version,
                FontSize = 13,
                FontWeight = FontWeights.Normal
            };
            versionText.SetResourceReference(TextBlock.ForegroundProperty, "QiTuTextPrimary");
            Grid.SetColumn(versionText, 0);
            header.Children.Add(versionText);

            var dateText = new TextBlock
            {
                Text = date,
                FontSize = 11,
                FontWeight = FontWeights.Normal
            };
            dateText.SetResourceReference(TextBlock.ForegroundProperty, "QiTuTextTertiary");
            Grid.SetColumn(dateText, 1);
            header.Children.Add(dateText);

            item.Children.Add(header);

            foreach (var line in lines)
            {
                var lineText = new TextBlock
                {
                    Text = "- " + line,
                    Margin = new Thickness(0, 6, 0, 0),
                    FontSize = 12,
                    FontWeight = FontWeights.Normal,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 18
                };
                lineText.SetResourceReference(TextBlock.ForegroundProperty, "QiTuTextSecondary");
                item.Children.Add(lineText);
            }

            var divider = new Border
            {
                Height = 1,
                Margin = new Thickness(0, 12, 0, 0)
            };
            divider.SetResourceReference(BackgroundProperty, "QiTuBorderDefault");
            item.Children.Add(divider);

            list.Children.Add(item);
        }

        private UIElement CreateSettingsOverlayContent()
        {
            var stack = new StackPanel();

            stack.Children.Add(CreateSettingsSwitchRow("窗口置顶", Topmost, SetTopmost));
            stack.Children.Add(CreateSettingsSwitchRow("保存窗口位置", _config.NativePanel.SaveWindowPosition, SetSaveWindowPosition));
            stack.Children.Add(CreateSettingsSwitchRow("保存配置参数", _config.NativePanel.SaveToolSettings, value =>
            {
                _config.NativePanel.SaveToolSettings = value;
                SaveConfig();
            }));
            stack.Children.Add(CreateSettingsSwitchRow("自动备份原文件", _config.NativePanel.AutoBackupOriginalFile, value =>
            {
                _config.NativePanel.AutoBackupOriginalFile = value;
                SaveConfig();
            }));
            stack.Children.Add(CreateSettingsSwitchRow("显示执行完成提示", _config.NativePanel.ShowTaskCompletedToast, value =>
            {
                _config.NativePanel.ShowTaskCompletedToast = value;
                SaveConfig();
            }));

            return CreateOverlayScrollViewer(stack);
        }

        private UIElement CreateSettingsSwitchRow(string title, bool isChecked, Action<bool> onChanged)
        {
            var row = new Grid
            {
                MinHeight = 38,
                Margin = new Thickness(0, 0, 0, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "QiTuTextPrimary");
            Grid.SetColumn(titleBlock, 0);
            row.Children.Add(titleBlock);

            var toggle = new CheckBox
            {
                IsChecked = isChecked,
                Margin = new Thickness(16, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            toggle.SetResourceReference(StyleProperty, "QiTuSwitch");
            toggle.Checked += (sender, args) => onChanged(true);
            toggle.Unchecked += (sender, args) => onChanged(false);
            Grid.SetColumn(toggle, 1);
            row.Children.Add(toggle);

            return row;
        }

        private UIElement CreateOverlayScrollViewer(UIElement content)
        {
            var scroll = new ScrollViewer
            {
                MaxHeight = 236,
                Margin = new Thickness(20, 12, 0, 18),
                Padding = new Thickness(0, 0, 20, 0),
                Content = content
            };
            scroll.SetResourceReference(StyleProperty, "QiTuOverlayScrollViewer");
            return scroll;
        }

        private void CloseActivePopup()
        {
            if (_activePopup == null)
            {
                return;
            }

            var popup = _activePopup;
            _activePopup = null;
            popup.Close();
        }

        private void SetSaveWindowPosition(bool value)
        {
            _config.NativePanel.SaveWindowPosition = value;
            if (value)
            {
                SaveToolWindowPosition();
            }

            SaveConfig();
        }

        private Point? LoadPopupPosition(string key)
        {
            if (!_config.NativePanel.SaveWindowPosition)
            {
                return null;
            }

            if (!_config.NativePanel.PopupWindowPositions.TryGetValue(key, out var position))
            {
                return null;
            }

            if (!IsUsablePosition(position.Left, position.Top))
            {
                return null;
            }

            return new Point(position.Left, position.Top);
        }

        private void SavePopupPosition(string key, Point position)
        {
            if (!_config.NativePanel.SaveWindowPosition || !IsUsablePosition(position.X, position.Y))
            {
                return;
            }

            _config.NativePanel.PopupWindowPositions[key] = new WindowPositionConfig
            {
                Left = position.X,
                Top = position.Y
            };
            SaveConfig();
        }

        private void SaveToolWindowPosition()
        {
            if (!_config.NativePanel.SaveWindowPosition || !IsUsablePosition(Left, Top))
            {
                return;
            }

            _config.NativePanel.ToolWindowPositions[_toolKey] = new WindowPositionConfig
            {
                Left = Left,
                Top = Top
            };
        }

        private void SaveConfig()
        {
            _config.Normalize();
            _configStore?.Save(_config);
        }

        private static bool IsUsablePosition(double left, double top)
        {
            return !double.IsNaN(left)
                && !double.IsNaN(top)
                && !double.IsInfinity(left)
                && !double.IsInfinity(top)
                && left > -32000
                && top > -32000;
        }

        private void OnCollapseClicked(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;

            if (_isCollapsed)
            {
                _expandedHeight = ActualHeight;
                if (_bodyHost != null)
                {
                    _bodyHost.Visibility = Visibility.Collapsed;
                }

                if (_statusBar != null)
                {
                    _statusBar.Visibility = Visibility.Collapsed;
                }

                SizeToContent = SizeToContent.Height;
            }
            else
            {
                if (_bodyHost != null)
                {
                    _bodyHost.Visibility = Visibility.Visible;
                }

                if (_statusBar != null)
                {
                    _statusBar.Visibility = Visibility.Visible;
                }

                SizeToContent = SizeToContent.Height;
                if (_expandedHeight > 0)
                {
                    ClearValue(HeightProperty);
                }
            }

            if (_collapseButton != null)
            {
                _collapseButton.Content = CreateCollapseIcon(_isCollapsed);
                _collapseButton.ToolTip = _isCollapsed ? "展开" : "收起";
            }
        }

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveToolWindowPosition();
            SaveConfig();
            CloseActivePopup();
            base.OnClosed(e);
        }
    }
}
