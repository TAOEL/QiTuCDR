using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QiTuCDR.Host.NativePanels;

namespace QiTuCDR.NativePanelPreview
{
    internal sealed class PreviewWindow : Window
    {
        private const double ToolbarButtonWidth = 82;
        private const double MainButtonWidth = 92;

        private NativePanelThemeMode _theme = NativePanelThemeMode.Light;
        private bool _floatingMode = true;
        private bool _mainPanelOpen;
        private string? _openMenuKey;
        private string? _activeToolKey;
        private string? _activeToolTitle;
        private int _inspectionIndex;
        private WebView2? _mainPanelWebView;
        private WebPanelLoadState _webPanelLoadState = WebPanelLoadState.NotStarted;
        private double _mainPanelLeft = 410;
        private double _mainPanelTop = 22;
        private Point? _mainPanelDragStart;
        private double _mainPanelDragStartLeft;
        private double _mainPanelDragStartTop;
        private string _statusText = "当前为工具条视觉模拟器，不启动 CorelDRAW，不连接 COM。";

        public PreviewWindow()
        {
            Title = "QiTuCDR 工具条与原生窗口预览器";
            Width = 1180;
            Height = 680;
            MinWidth = 980;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Render();
        }

        private void Render()
        {
            NativeToolWindowManager.SetTheme(_theme);
            Background = Brush("PreviewBg");
            Content = CreateContent();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_mainPanelWebView != null)
            {
                _mainPanelWebView.NavigationCompleted -= OnMainPanelWebViewNavigationCompleted;
                _mainPanelWebView.Dispose();
                _mainPanelWebView = null;
            }

            NativeToolWindowManager.CloseCurrent();
            base.OnClosed(e);
        }

        private UIElement CreateContent()
        {
            var root = new Grid
            {
                Margin = new Thickness(18)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "QiTuCDR 工具条入口模拟器",
                FontSize = 20,
                FontWeight = FontWeights.Normal,
                Foreground = Brush("TextPrimary")
            };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            var description = new TextBlock
            {
                Text = "按真实 CDR 插件入口模拟：企图插件按钮打开综合面板入口，一级工具分组展开二级菜单，二级菜单打开独立 WPF 工具窗口。",
                Margin = new Thickness(0, 8, 0, 14),
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush("TextSecondary")
            };
            Grid.SetRow(description, 1);
            root.Children.Add(description);

            var controls = CreatePreviewControls();
            Grid.SetRow(controls, 2);
            root.Children.Add(controls);

            var stage = CreateCorelStage();
            Grid.SetRow(stage, 3);
            root.Children.Add(stage);

            var footer = CreateFooter();
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            return root;
        }

        private UIElement CreatePreviewControls()
        {
            var outer = CreateCard(12, 0, false);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outer.Child = grid;

            var status = new TextBlock
            {
                Text = "预览设置：" + (_theme == NativePanelThemeMode.Dark ? "暗黑模式" : "浅色模式") + " / " + (_floatingMode ? "工具条悬浮" : "工具条嵌入") + " / " + (_mainPanelOpen ? "综合面板已打开" : "综合面板未打开"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.Normal,
                Foreground = Brush("TextPrimary")
            };
            Grid.SetColumn(status, 0);
            grid.Children.Add(status);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);

            AddControlButton(actions, "浅色", _theme == NativePanelThemeMode.Light, () => SwitchTheme(NativePanelThemeMode.Light));
            AddControlButton(actions, "暗黑", _theme == NativePanelThemeMode.Dark, () => SwitchTheme(NativePanelThemeMode.Dark));
            AddControlButton(actions, "悬浮", _floatingMode, () => SwitchDockMode(true));
            AddControlButton(actions, "嵌入", !_floatingMode, () => SwitchDockMode(false));
            AddControlButton(actions, "核心巡检", false, RunCoreInspectionStep);
            AddControlButton(actions, "重置状态", false, ResetPreviewState);

            return outer;
        }

        private UIElement CreateCorelStage()
        {
            var outer = CreateCard(0, 14, false);
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            outer.Child = root;

            var chrome = new Border
            {
                Height = 34,
                Background = Brush("CdrChrome"),
                BorderBrush = Brush("Border"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 0, 12, 0)
            };
            Grid.SetRow(chrome, 0);
            root.Children.Add(chrome);

            chrome.Child = new TextBlock
            {
                Text = "CorelDRAW 26 工作区模拟",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("TextSecondary"),
                FontSize = 12
            };

            var workspace = new Grid
            {
                Background = Brush("WorkspaceBg")
            };
            Grid.SetRow(workspace, 1);
            root.Children.Add(workspace);

            workspace.Children.Add(CreateCanvasHint());
            workspace.Children.Add(CreateToolbarHost());

            if (_mainPanelOpen)
            {
                workspace.Children.Add(CreateMainPanelPreview());
            }

            var openMenuKey = _openMenuKey;
            if (!string.IsNullOrEmpty(openMenuKey))
            {
                workspace.Children.Add(CreateDropdownLayer(openMenuKey!));
            }

            workspace.Children.Add(CreateCompactStatusBadge());

            return outer;
        }

        private UIElement CreateCanvasHint()
        {
            var hint = new Border
            {
                Width = 280,
                Height = 170,
                Background = Brush("CanvasBg"),
                BorderBrush = Brush("Border"),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(2)
            };

            hint.Child = new TextBlock
            {
                Text = "设计画布占位\n这里只模拟入口，不执行文档操作",
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("TextSecondary"),
                FontSize = 13,
                LineHeight = 22
            };

            return hint;
        }

        private UIElement CreateCompactStatusBadge()
        {
            var badge = new Border
            {
                Width = 300,
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 14, 14),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brush("DropdownBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = _theme == NativePanelThemeMode.Dark ? 0.32 : 0.14 }
            };

            var stack = new StackPanel();
            badge.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Text = "三端一致状态",
                Foreground = Brush("TextPrimary"),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "浏览器 / 预览器 / CDR 共用同一套 WebUI。Web 状态：" + GetWebPanelStateText() + "。拖动综合面板边框可移动位置。",
                Foreground = Brush("TextSecondary"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            });

            return badge;
        }

        private UIElement CreateToolbarHost()
        {
            var host = new Border
            {
                Height = 32,
                MinWidth = _floatingMode ? 604 : 0,
                HorizontalAlignment = _floatingMode ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = _floatingMode ? new Thickness(24, 26, 0, 0) : new Thickness(0),
                Padding = new Thickness(2),
                Background = Brush("ToolbarBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = _floatingMode ? new CornerRadius(4) : new CornerRadius(0),
                Effect = _floatingMode ? new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.18 } : null
            };

            host.Child = CreateToolbar();
            return host;
        }

        private UIElement CreateMainPanelPreview()
        {
            var panel = new Border
            {
                Width = 720,
                Height = 520,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(_mainPanelLeft, _mainPanelTop, 0, 0),
                Padding = new Thickness(5),
                Background = Brush("MainPanelBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 2, Opacity = _theme == NativePanelThemeMode.Dark ? 0.28 : 0.14 },
                ToolTip = "拖动边框可以移动综合面板预览位置"
            };
            panel.MouseLeftButtonDown += OnMainPanelDragStart;
            panel.MouseMove += OnMainPanelDragMove;
            panel.MouseLeftButtonUp += OnMainPanelDragEnd;
            panel.MouseLeave += OnMainPanelDragEnd;
            panel.Child = CreateEmbeddedWebPanel();

            return panel;
        }

        private void OnMainPanelDragStart(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            _mainPanelDragStart = position;
            _mainPanelDragStartLeft = _mainPanelLeft;
            _mainPanelDragStartTop = _mainPanelTop;
            ((UIElement)sender).CaptureMouse();
        }

        private void OnMainPanelDragMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_mainPanelDragStart == null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
            {
                return;
            }

            var position = e.GetPosition(this);
            var delta = position - _mainPanelDragStart.Value;
            _mainPanelLeft = Math.Max(0, _mainPanelDragStartLeft + delta.X);
            _mainPanelTop = Math.Max(0, _mainPanelDragStartTop + delta.Y);

            if (sender is FrameworkElement element)
            {
                element.Margin = new Thickness(_mainPanelLeft, _mainPanelTop, 0, 0);
            }
        }

        private void OnMainPanelDragEnd(object sender, RoutedEventArgs e)
        {
            _mainPanelDragStart = null;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private UIElement CreateMainPanelHeader()
        {
            var header = new Border
            {
                Height = 44,
                Padding = new Thickness(12, 0, 10, 0),
                Background = Brush("MainPanelHeaderBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Child = grid;

            var title = new TextBlock
            {
                Text = "企图插件",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("TextPrimary"),
                FontSize = 14,
                FontWeight = FontWeights.Normal
            };
            Grid.SetColumn(title, 0);
            grid.Children.Add(title);

            var close = new Button
            {
                Content = "X",
                Width = 30,
                Height = 28,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brush("TextSecondary"),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "关闭综合面板模拟"
            };
            close.Click += (sender, args) =>
            {
                _mainPanelOpen = false;
                _statusText = "已关闭综合面板入口模拟。";
                Render();
            };
            Grid.SetColumn(close, 1);
            grid.Children.Add(close);

            return header;
        }

        private UIElement CreateMainPanelNav()
        {
            var nav = new Border
            {
                Background = Brush("MainPanelRailBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            var stack = new StackPanel
            {
                Margin = new Thickness(6, 10, 6, 0)
            };
            nav.Child = stack;

            AddNavMark(stack, "首", true);
            AddNavMark(stack, "工", false);
            AddNavMark(stack, "文", false);
            AddNavMark(stack, "设", false);

            return nav;
        }

        private void AddNavMark(Panel parent, string text, bool active)
        {
            var mark = new Border
            {
                Width = 32,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 8),
                CornerRadius = new CornerRadius(6),
                Background = active ? Brush("Accent") : Brush("MainPanelButtonBg")
            };

            mark.Child = new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = active ? Brushes.White : Brush("TextSecondary"),
                FontSize = 12,
                FontWeight = FontWeights.Normal
            };
            parent.Children.Add(mark);
        }

        private UIElement CreateMainPanelCategories()
        {
            var categories = new Border
            {
                Background = Brush("MainPanelBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(8, 10, 8, 8)
            };

            var stack = new StackPanel();
            categories.Child = stack;

            AddCategory(stack, "常用工具", true);
            AddCategory(stack, "排版处理", false);
            AddCategory(stack, "文字处理", false);
            AddCategory(stack, "清理检查", false);
            AddCategory(stack, "设置", false);

            return categories;
        }

        private void AddCategory(Panel parent, string text, bool active)
        {
            var item = new Border
            {
                Height = 30,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(10, 0, 8, 0),
                CornerRadius = new CornerRadius(6),
                Background = active ? Brush("ToolbarButtonActive") : Brushes.Transparent
            };
            item.Child = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = active ? Brush("AccentText") : Brush("TextSecondary"),
                FontSize = 12,
                FontWeight = FontWeights.Normal
            };
            parent.Children.Add(item);
        }

        private UIElement CreateMainPanelContent()
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brush("MainPanelContentBg")
            };

            var stack = new StackPanel
            {
                Margin = new Thickness(12)
            };
            scroll.Content = stack;

            stack.Children.Add(new TextBlock
            {
                Text = "常用工具",
                Foreground = Brush("TextPrimary"),
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 10)
            });

            AddPanelToolCard(stack, "批量转曲", "按文档、页面或选区批量处理文字对象。", NativeToolWindowManager.ConvertText);
            AddPanelToolCard(stack, "一键居中", "将选中对象快速居中到当前页面。", NativeToolWindowManager.CenterObjects);
            AddPanelToolCard(stack, "冗余清理", "清理辅助线、空文本和隐藏空图层。", NativeToolWindowManager.CleanupRedundant);
            AddPanelToolCard(stack, "尺寸规整", "统一宽高、比例和描边宽度。", NativeToolWindowManager.NormalizeSize);

            return scroll;
        }

        private void AddPanelToolCard(Panel parent, string title, string description, string key)
        {
            var active = string.Equals(_activeToolKey, key, StringComparison.Ordinal);
            var card = new Button
            {
                MinHeight = 62,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10),
                Background = active ? Brush("AccentSoft") : Brush("MainPanelButtonBg"),
                BorderBrush = active ? Brush("Accent") : Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            var stack = new StackPanel();
            card.Content = stack;
            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            stack.Children.Add(titleRow);

            var titleText = new TextBlock
            {
                Text = title,
                Foreground = Brush("TextPrimary"),
                FontSize = 12,
                FontWeight = FontWeights.Normal
            };
            Grid.SetColumn(titleText, 0);
            titleRow.Children.Add(titleText);

            if (active)
            {
                var current = new Border
                {
                    Padding = new Thickness(6, 2, 6, 2),
                    CornerRadius = new CornerRadius(4),
                    Background = Brush("Accent")
                };
                current.Child = new TextBlock
                {
                    Text = "当前",
                    Foreground = Brushes.White,
                    FontSize = 10
                };
                Grid.SetColumn(current, 1);
                titleRow.Children.Add(current);
            }

            stack.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = Brush("TextSecondary"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });

            card.Click += (sender, args) =>
            {
                _activeToolKey = key;
                _activeToolTitle = title;
                _statusText = "已从综合面板模拟入口打开：" + title;
                NativeToolWindowManager.OpenTool(key, title);
                Render();
            };

            parent.Children.Add(card);
        }

        private UIElement CreateEmbeddedWebPanel()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var host = new Grid
            {
                Background = Brush("MainPanelBg")
            };
            Grid.SetRow(host, 0);
            root.Children.Add(host);

            var webView = GetOrCreateMainPanelWebView();
            DetachFromParent(webView);
            host.Children.Add(webView);

            var overlay = CreateWebPanelOverlay();
            if (overlay != null)
            {
                host.Children.Add(overlay);
            }

            return root;
        }

        private WebView2 GetOrCreateMainPanelWebView()
        {
            if (_mainPanelWebView != null)
            {
                return _mainPanelWebView;
            }

            _mainPanelWebView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "QiTuCDR",
                        "NativePanelPreviewWebView2")
                }
            };
            _mainPanelWebView.NavigationCompleted += OnMainPanelWebViewNavigationCompleted;
            NavigateMainPanelWebView();
            return _mainPanelWebView;
        }

        private UIElement CreateThreeSideChecklist()
        {
            var checklist = new Border
            {
                Padding = new Thickness(10, 8, 10, 8),
                Background = Brush("MainPanelHeaderBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var root = new StackPanel();
            checklist.Child = root;

            root.Children.Add(new TextBlock
            {
                Text = "三端一致验收清单",
                Foreground = Brush("TextPrimary"),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 6)
            });

            AddChecklistItem(root, "浏览器端", "http://127.0.0.1:4173/ 视觉达标");
            AddChecklistItem(root, "预览器端", "右侧 WebView2 嵌入同一地址");
            AddChecklistItem(root, "CDR 端", "最终由唯一 WebView2 加载同一套 WebUI 构建产物");

            return checklist;
        }

        private void AddChecklistItem(Panel parent, string label, string text)
        {
            var line = new TextBlock
            {
                Text = label + "： " + text,
                Foreground = Brush("TextSecondary"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 3)
            };
            parent.Children.Add(line);
        }

        private UIElement? CreateWebPanelOverlay()
        {
            if (_webPanelLoadState == WebPanelLoadState.Loaded)
            {
                return null;
            }

            var overlay = new Border
            {
                Margin = new Thickness(18),
                Padding = new Thickness(18),
                MaxWidth = 360,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brush("DropdownBg"),
                BorderBrush = Brush(_webPanelLoadState == WebPanelLoadState.Failed ? "Danger" : "ToolbarBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 2, Opacity = _theme == NativePanelThemeMode.Dark ? 0.36 : 0.18 }
            };

            var stack = new StackPanel();
            overlay.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Text = _webPanelLoadState == WebPanelLoadState.Failed ? "综合面板加载失败" : "正在加载综合面板",
                Foreground = Brush("TextPrimary"),
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 8)
            });

            stack.Children.Add(new TextBlock
            {
                Text = _webPanelLoadState == WebPanelLoadState.Failed
                    ? "请确认前端预览服务正在运行：cd web 后执行 npm run preview -- --host 127.0.0.1，然后点击下方刷新。"
                    : "正在连接 http://127.0.0.1:4173/。如果长时间空白，通常是本地前端服务未启动。",
                Foreground = Brush("TextSecondary"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            });

            var reload = CreateControlButton("刷新综合面板", true);
            reload.Margin = new Thickness(0, 14, 0, 0);
            reload.Click += (sender, args) => ReloadMainPanelWebView();
            stack.Children.Add(reload);

            return overlay;
        }

        private string GetWebPanelStateText()
        {
            switch (_webPanelLoadState)
            {
                case WebPanelLoadState.Loading:
                    return "加载中";
                case WebPanelLoadState.Loaded:
                    return "已加载";
                case WebPanelLoadState.Failed:
                    return "加载失败";
                default:
                    return "未开始";
            }
        }

        private void NavigateMainPanelWebView()
        {
            if (_mainPanelWebView == null)
            {
                return;
            }

            _webPanelLoadState = WebPanelLoadState.Loading;
            _mainPanelWebView.Source = new Uri("http://127.0.0.1:4173/");
        }

        private void ReloadMainPanelWebView()
        {
            NavigateMainPanelWebView();
            _statusText = "已刷新预览器内嵌 Web 综合面板。";
            Render();
        }

        private void OnMainPanelWebViewNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _webPanelLoadState = e.IsSuccess ? WebPanelLoadState.Loaded : WebPanelLoadState.Failed;
                _statusText = e.IsSuccess
                    ? "综合面板 Web 嵌入加载成功，当前三端共用同一套 WebUI 视觉。"
                    : "综合面板 Web 嵌入加载失败，请确认 web 预览服务 http://127.0.0.1:4173/ 正在运行。";
                Render();
            }));
        }

        private static void DetachFromParent(UIElement element)
        {
            if (element is FrameworkElement frameworkElement)
            {
                switch (frameworkElement.Parent)
                {
                    case Panel panel:
                        panel.Children.Remove(element);
                        break;
                    case Decorator decorator:
                        decorator.Child = null;
                        break;
                    case ContentControl contentControl:
                        contentControl.Content = null;
                        break;
                }
            }
        }

        private UIElement CreateToolbar()
        {
            var toolbar = new DockPanel
            {
                Height = 26,
                LastChildFill = false,
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            var main = CreateToolbarButton("企图插件", true, _mainPanelOpen);
            main.ToolTip = "点击打开综合面板入口";
            main.Click += (sender, args) =>
            {
                _openMenuKey = null;
                _mainPanelOpen = true;
                if (_webPanelLoadState == WebPanelLoadState.NotStarted || _webPanelLoadState == WebPanelLoadState.Failed)
                {
                    _webPanelLoadState = WebPanelLoadState.Loading;
                }

                _statusText = "已模拟点击“企图插件”：右侧显示综合面板入口。真实视觉继续在 127.0.0.1:4173 调试。";
                Render();
            };
            DockPanel.SetDock(main, Dock.Left);
            toolbar.Children.Add(main);

            toolbar.Children.Add(CreateSeparator());
            AddToolbarMenuButton(toolbar, "工具面板");
            AddToolbarMenuButton(toolbar, "排版工具");
            AddToolbarMenuButton(toolbar, "文字工具");
            AddToolbarMenuButton(toolbar, "清理工具");

            return toolbar;
        }

        private void AddToolbarMenuButton(DockPanel toolbar, string title)
        {
            var button = CreateToolbarButton(title, false, string.Equals(_openMenuKey, title, StringComparison.Ordinal));
            button.Click += (sender, args) =>
            {
                _openMenuKey = string.Equals(_openMenuKey, title, StringComparison.Ordinal) ? null : title;
                _statusText = _openMenuKey == null ? "已收起工具分组菜单。" : "已展开一级菜单：" + title;
                Render();
            };
            DockPanel.SetDock(button, Dock.Left);
            toolbar.Children.Add(button);
        }

        private Button CreateToolbarButton(string text, bool primary, bool active)
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            content.Children.Add(new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (!primary)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "▼",
                    FontSize = 8,
                    Margin = new Thickness(4, 1, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            return new Button
            {
                Content = content,
                Height = 22,
                MinWidth = primary ? MainButtonWidth : ToolbarButtonWidth,
                Margin = new Thickness(2, 1, 0, 1),
                Padding = new Thickness(8, 0, 8, 0),
                Background = primary ? active ? Brush("AccentActive") : Brush("Accent") : active ? Brush("ToolbarButtonActive") : Brush("ToolbarButtonBg"),
                Foreground = primary ? Brushes.White : Brush("TextPrimary"),
                BorderBrush = primary || active ? Brush("Accent") : Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
        }

        private UIElement CreateSeparator()
        {
            var border = new Border
            {
                Width = 1,
                Height = 18,
                Margin = new Thickness(8, 4, 6, 4),
                Background = Brush("ToolbarBorder")
            };
            DockPanel.SetDock(border, Dock.Left);
            return border;
        }

        private UIElement CreateDropdownLayer(string menuKey)
        {
            var tools = GetTools(menuKey);
            var menuIndex = GetMenuIndex(menuKey);

            var dropdown = new Border
            {
                Width = 178,
                Padding = new Thickness(6),
                Background = Brush("DropdownBg"),
                BorderBrush = Brush("ToolbarBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = GetDropdownMargin(menuIndex),
                Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = _theme == NativePanelThemeMode.Dark ? 0.34 : 0.18 }
            };

            var stack = new StackPanel();
            dropdown.Child = stack;

            var title = new TextBlock
            {
                Text = menuKey,
                Margin = new Thickness(8, 4, 8, 6),
                Foreground = Brush("TextSecondary"),
                FontSize = 11,
                FontWeight = FontWeights.Normal
            };
            stack.Children.Add(title);

            foreach (var tool in tools)
            {
                stack.Children.Add(CreateDropdownItem(tool));
            }

            return dropdown;
        }

        private Thickness GetDropdownMargin(int menuIndex)
        {
            var toolbarLeft = _floatingMode ? 24 : 0;
            var toolbarTop = _floatingMode ? 26 : 0;
            var left = toolbarLeft + 2 + MainButtonWidth + 16 + menuIndex * (ToolbarButtonWidth + 2);
            var top = toolbarTop + 33;
            return new Thickness(left, top, 0, 0);
        }

        private UIElement CreateDropdownItem(ToolSpec tool)
        {
            var active = string.Equals(_activeToolKey, tool.Key, StringComparison.Ordinal);
            var button = new Button
            {
                Height = 30,
                Padding = new Thickness(10, 0, 8, 0),
                Margin = new Thickness(0, 0, 0, 4),
                Background = active ? Brush("AccentSoft") : Brush("DropdownItemBg"),
                BorderBrush = active ? Brush("Accent") : Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            button.Content = grid;

            var name = new TextBlock
            {
                Text = tool.Title,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("TextPrimary"),
                FontSize = 12
            };
            Grid.SetColumn(name, 0);
            grid.Children.Add(name);

            var tag = new Border
            {
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(4),
                Background = tool.IsPlaceholder ? Brush("TagBg") : Brush("AccentSoft")
            };
            tag.Child = new TextBlock
            {
                Text = active ? "当前" : tool.IsPlaceholder ? "占位" : "可预览",
                Foreground = active ? Brushes.White : tool.IsPlaceholder ? Brush("TextSecondary") : Brush("AccentText"),
                FontSize = 10
            };
            if (active)
            {
                tag.Background = Brush("Accent");
            }
            Grid.SetColumn(tag, 1);
            grid.Children.Add(tag);

            button.Click += (sender, args) => OpenTool(tool);
            return button;
        }

        private ToolSpec[] GetTools(string menuKey)
        {
            switch (menuKey)
            {
                case "工具面板":
                    return new[]
                    {
                        ToolSpec.Real("批量转曲", NativeToolWindowManager.ConvertText),
                        ToolSpec.Real("一键居中", NativeToolWindowManager.CenterObjects),
                        ToolSpec.Real("冗余清理", NativeToolWindowManager.CleanupRedundant),
                        ToolSpec.Real("尺寸规整", NativeToolWindowManager.NormalizeSize)
                    };
                case "排版工具":
                    return new[]
                    {
                        ToolSpec.Real("一键居中", NativeToolWindowManager.CenterObjects),
                        ToolSpec.Real("统一尺寸", NativeToolWindowManager.NormalizeSize),
                        ToolSpec.Placeholder("批量对齐", "batchAlign"),
                        ToolSpec.Placeholder("等距分布", "equalDistribution")
                    };
                case "文字工具":
                    return new[]
                    {
                        ToolSpec.Real("批量转曲", NativeToolWindowManager.ConvertText),
                        ToolSpec.Placeholder("空文本清理", "emptyTextCleanup"),
                        ToolSpec.Placeholder("文字统计", "textStatistics")
                    };
                case "清理工具":
                    return new[]
                    {
                        ToolSpec.Real("冗余清理", NativeToolWindowManager.CleanupRedundant),
                        ToolSpec.Placeholder("隐藏图层检查", "hiddenLayerCheck"),
                        ToolSpec.Placeholder("辅助线清理", "guidelineCleanup")
                    };
                default:
                    return new ToolSpec[0];
            }
        }

        private int GetMenuIndex(string menuKey)
        {
            switch (menuKey)
            {
                case "工具面板":
                    return 0;
                case "排版工具":
                    return 1;
                case "文字工具":
                    return 2;
                case "清理工具":
                    return 3;
                default:
                    return 0;
            }
        }

        private void OpenTool(ToolSpec tool)
        {
            _openMenuKey = null;
            _activeToolKey = tool.Key;
            _activeToolTitle = tool.Title;
            _statusText = "已打开二级菜单窗口：" + tool.Title + (tool.IsPlaceholder ? "（占位窗口）" : "（核心窗口）");
            NativeToolWindowManager.OpenTool(tool.Key, tool.Title);
            Render();
        }

        private void RunCoreInspectionStep()
        {
            var tools = GetCoreInspectionTools();
            var tool = tools[_inspectionIndex % tools.Length];
            _inspectionIndex++;
            _mainPanelOpen = true;
            _openMenuKey = null;
            _activeToolKey = tool.Key;
            _activeToolTitle = tool.Title;
            _statusText = "核心窗口巡检 " + _inspectionIndex + "：已打开 " + tool.Title + "。继续点击会检查下一个核心窗口。";
            NativeToolWindowManager.OpenTool(tool.Key, tool.Title);
            Render();
        }

        private void ResetPreviewState()
        {
            NativeToolWindowManager.CloseCurrent();
            _mainPanelOpen = false;
            _openMenuKey = null;
            _activeToolKey = null;
            _activeToolTitle = null;
            _inspectionIndex = 0;
            _statusText = "已重置预览状态。主题和悬浮/嵌入设置保留。";
            Render();
        }

        private static ToolSpec[] GetCoreInspectionTools()
        {
            return new[]
            {
                ToolSpec.Real("批量转曲", NativeToolWindowManager.ConvertText),
                ToolSpec.Real("一键居中", NativeToolWindowManager.CenterObjects),
                ToolSpec.Real("冗余清理", NativeToolWindowManager.CleanupRedundant),
                ToolSpec.Real("尺寸规整", NativeToolWindowManager.NormalizeSize)
            };
        }

        private UIElement CreateFooter()
        {
            var footer = new Grid
            {
                Margin = new Thickness(0, 14, 0, 0)
            };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var status = new TextBlock
            {
                Text = _statusText + (_activeToolTitle == null ? string.Empty : " 当前工具窗口：" + _activeToolTitle + "。"),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush("TextSecondary"),
                FontSize = 12
            };
            Grid.SetColumn(status, 0);
            footer.Children.Add(status);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            Grid.SetColumn(actions, 1);
            footer.Children.Add(actions);

            var closeTool = CreateControlButton("关闭当前工具窗口", false);
            closeTool.Click += (sender, args) =>
            {
                NativeToolWindowManager.CloseCurrent();
                _openMenuKey = null;
                _activeToolKey = null;
                _activeToolTitle = null;
                _statusText = "已关闭当前独立工具窗口。";
                Render();
            };
            actions.Children.Add(closeTool);

            var closePreview = CreateControlButton("关闭预览器", true);
            closePreview.Margin = new Thickness(8, 0, 0, 0);
            closePreview.Click += (sender, args) => Close();
            actions.Children.Add(closePreview);

            return footer;
        }

        private Border CreateCard(double padding, double topMargin, bool rounded)
        {
            return new Border
            {
                Padding = new Thickness(padding),
                Margin = new Thickness(0, topMargin, 0, 0),
                Background = Brush("PanelBg"),
                BorderBrush = Brush("Border"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(rounded ? 8 : 6)
            };
        }

        private void AddControlButton(Panel parent, string text, bool active, Action action)
        {
            var button = CreateControlButton(text, active);
            if (parent.Children.Count > 0)
            {
                button.Margin = new Thickness(8, 0, 0, 0);
            }

            button.Click += (sender, args) => action();
            parent.Children.Add(button);
        }

        private Button CreateControlButton(string text, bool primary)
        {
            return new Button
            {
                Content = text,
                MinHeight = 32,
                Padding = new Thickness(14, 0, 14, 0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = primary ? Brush("Accent") : Brush("ButtonBg"),
                Foreground = primary ? Brushes.White : Brush("TextPrimary"),
                BorderBrush = primary ? Brush("Accent") : Brush("Border"),
                BorderThickness = new Thickness(1)
            };
        }

        private void SwitchTheme(NativePanelThemeMode theme)
        {
            if (_theme == theme)
            {
                return;
            }

            _theme = theme;
            _openMenuKey = null;
            _statusText = "已切换为" + (_theme == NativePanelThemeMode.Dark ? "暗黑模式。" : "浅色模式。");
            Render();
        }

        private void SwitchDockMode(bool floatingMode)
        {
            if (_floatingMode == floatingMode)
            {
                return;
            }

            _floatingMode = floatingMode;
            _openMenuKey = null;
            _statusText = "已切换为工具条" + (_floatingMode ? "悬浮预览。" : "嵌入预览。");
            Render();
        }

        private SolidColorBrush Brush(string name)
        {
            if (_theme == NativePanelThemeMode.Dark)
            {
                switch (name)
                {
                    case "PreviewBg":
                        return new SolidColorBrush(Color.FromRgb(17, 19, 21));
                    case "PanelBg":
                    case "ButtonBg":
                    case "ToolbarBg":
                    case "DropdownBg":
                    case "MainPanelBg":
                    case "MainPanelHeaderBg":
                    case "MainPanelButtonBg":
                        return new SolidColorBrush(Color.FromRgb(24, 27, 31));
                    case "MainPanelRailBg":
                    case "MainPanelContentBg":
                        return new SolidColorBrush(Color.FromRgb(19, 22, 25));
                    case "ToolbarButtonBg":
                    case "DropdownItemBg":
                        return new SolidColorBrush(Color.FromRgb(31, 35, 40));
                    case "ToolbarButtonActive":
                    case "AccentSoft":
                        return new SolidColorBrush(Color.FromRgb(36, 54, 22));
                    case "TagBg":
                        return new SolidColorBrush(Color.FromRgb(38, 42, 48));
                    case "Danger":
                        return new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    case "TextPrimary":
                        return new SolidColorBrush(Color.FromRgb(245, 247, 250));
                    case "TextSecondary":
                        return new SolidColorBrush(Color.FromRgb(183, 192, 204));
                    case "AccentText":
                        return new SolidColorBrush(Color.FromRgb(170, 226, 94));
                    case "AccentActive":
                        return new SolidColorBrush(Color.FromRgb(107, 168, 32));
                    case "Border":
                    case "ToolbarBorder":
                        return new SolidColorBrush(Color.FromRgb(48, 54, 64));
                    case "CdrChrome":
                        return new SolidColorBrush(Color.FromRgb(29, 32, 36));
                    case "WorkspaceBg":
                        return new SolidColorBrush(Color.FromRgb(38, 42, 48));
                    case "CanvasBg":
                        return new SolidColorBrush(Color.FromRgb(18, 20, 23));
                }
            }

            switch (name)
            {
                case "PreviewBg":
                    return new SolidColorBrush(Color.FromRgb(245, 245, 247));
                    case "PanelBg":
                    case "ButtonBg":
                    case "ToolbarBg":
                    case "DropdownBg":
                    case "MainPanelBg":
                    case "MainPanelHeaderBg":
                    case "MainPanelButtonBg":
                        return Brushes.White;
                    case "MainPanelRailBg":
                    case "MainPanelContentBg":
                        return new SolidColorBrush(Color.FromRgb(248, 249, 250));
                case "ToolbarButtonBg":
                case "DropdownItemBg":
                    return new SolidColorBrush(Color.FromRgb(248, 249, 250));
                case "ToolbarButtonActive":
                case "AccentSoft":
                    return new SolidColorBrush(Color.FromRgb(232, 245, 208));
                case "TagBg":
                    return new SolidColorBrush(Color.FromRgb(239, 242, 245));
                case "Danger":
                    return new SolidColorBrush(Color.FromRgb(239, 68, 68));
                case "TextPrimary":
                    return new SolidColorBrush(Color.FromRgb(28, 28, 30));
                case "TextSecondary":
                    return new SolidColorBrush(Color.FromRgb(107, 114, 128));
                case "AccentText":
                    return new SolidColorBrush(Color.FromRgb(83, 132, 22));
                case "AccentActive":
                    return new SolidColorBrush(Color.FromRgb(107, 168, 32));
                case "Border":
                    return new SolidColorBrush(Color.FromRgb(229, 231, 235));
                case "ToolbarBorder":
                    return new SolidColorBrush(Color.FromRgb(214, 220, 226));
                case "CdrChrome":
                    return new SolidColorBrush(Color.FromRgb(239, 242, 245));
                case "WorkspaceBg":
                    return new SolidColorBrush(Color.FromRgb(224, 228, 232));
                case "CanvasBg":
                    return Brushes.White;
            }

            return new SolidColorBrush(Color.FromRgb(123, 192, 41));
        }

        private sealed class ToolSpec
        {
            private ToolSpec(string title, string key, bool isPlaceholder)
            {
                Title = title;
                Key = key;
                IsPlaceholder = isPlaceholder;
            }

            public string Title { get; }

            public string Key { get; }

            public bool IsPlaceholder { get; }

            public static ToolSpec Real(string title, string key)
            {
                return new ToolSpec(title, key, false);
            }

            public static ToolSpec Placeholder(string title, string key)
            {
                return new ToolSpec(title, key, true);
            }
        }

        private enum WebPanelLoadState
        {
            NotStarted,
            Loading,
            Loaded,
            Failed
        }
    }
}
