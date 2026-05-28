using System;
using System.Windows;
using System.Windows.Controls;
using QiTuCDR.Host.Lifecycle;

namespace QiTuCDR.HostHarness
{
    internal sealed class HostHarnessWindow : Window
    {
        private readonly PluginLifecycleManager _lifecycleManager;

        public HostHarnessWindow(PluginLifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));

            Title = "QiTuCDR 本地调试宿主";
            Width = 360;
            Height = 220;
            MinWidth = 320;
            MinHeight = 190;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            Content = CreateContent();

            Loaded += OnLoaded;
        }

        private UIElement CreateContent()
        {
            var root = new StackPanel
            {
                Margin = new Thickness(18)
            };

            root.Children.Add(new TextBlock
            {
                Text = "QiTuCDR HostHarness",
                FontSize = 20,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 8)
            });

            root.Children.Add(new TextBlock
            {
                Text = "用于在不启动 CorelDRAW 的情况下验证生命周期、WebView2 单例、Bridge 和降级面板。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            });

            var openButton = new Button
            {
                Content = "打开 / 恢复插件面板",
                Height = 34,
                Margin = new Thickness(0, 0, 0, 8)
            };
            openButton.Click += (_, __) => _lifecycleManager.ShowPanel();
            root.Children.Add(openButton);

            var exitButton = new Button
            {
                Content = "退出本地调试宿主",
                Height = 34
            };
            exitButton.Click += (_, __) => Close();
            root.Children.Add(exitButton);

            return root;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lifecycleManager.ShowPanel();
        }
    }
}
