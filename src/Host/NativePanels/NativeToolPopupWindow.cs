using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QiTuCDR.Host.NativePanels
{
    internal sealed class NativeToolPopupWindow : Window
    {
        private const double PopupVisualWidth = 316;
        private const double PopupOuterMargin = 8;
        private const double PopupWindowWidth = PopupVisualWidth + PopupOuterMargin + PopupOuterMargin;
        private readonly Window _owner;
        private readonly string _positionKey;
        private readonly Func<string, Point?>? _loadPosition;
        private readonly Action<string, Point>? _savePosition;
        private bool _canRememberPosition;

        public NativeToolPopupWindow(
            Window owner,
            string title,
            UIElement content,
            Func<string, Point?>? loadPosition,
            Action<string, Point>? savePosition)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _positionKey = owner.Title + "|" + title;
            _loadPosition = loadPosition;
            _savePosition = savePosition;
            if (owner.IsVisible)
            {
                Owner = owner;
            }

            Title = title + " - QiTuCDR";
            Width = PopupWindowWidth;
            MaxHeight = 320;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            WindowStartupLocation = WindowStartupLocation.Manual;

            NativePanelResourceLoader.MergeInto(this);
            Background = Brushes.Transparent;
            Content = CreateLayout(title, content);
            Deactivated += OnDeactivated;
            LocationChanged += OnLocationChanged;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        public void PositionNearOwner()
        {
            var rememberedPosition = _loadPosition?.Invoke(_positionKey);
            if (rememberedPosition.HasValue)
            {
                Left = rememberedPosition.Value.X;
                Top = rememberedPosition.Value.Y;
                _canRememberPosition = true;
                return;
            }

            var ownerWidth = _owner.ActualWidth > 0 ? _owner.ActualWidth : _owner.Width;
            var ownerLeft = double.IsNaN(_owner.Left) ? 100 : _owner.Left;
            var ownerTop = double.IsNaN(_owner.Top) ? 100 : _owner.Top;
            Left = ownerLeft + ownerWidth - PopupWindowWidth - 8;
            Top = ownerTop + 40;
            _canRememberPosition = true;
        }

        private UIElement CreateLayout(string title, UIElement content)
        {
            var panel = new Border
            {
                Padding = new Thickness(0)
            };
            panel.SetResourceReference(StyleProperty, "QiTuOverlayPanel");

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = CreateHeader(title);
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            Grid.SetRow(content, 1);
            grid.Children.Add(content);
            panel.Child = grid;

            return new Grid
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(8),
                Children = { panel }
            };
        }

        private UIElement CreateHeader(string title)
        {
            var grid = new Grid
            {
                Height = 36,
                Cursor = Cursors.SizeAll
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.MouseLeftButtonDown += OnHeaderMouseLeftButtonDown;

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(14, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleBlock.SetResourceReference(StyleProperty, "QiTuSectionTitle");
            Grid.SetColumn(titleBlock, 0);
            grid.Children.Add(titleBlock);

            var closeButton = new Button
            {
                Content = CreateCloseIcon(),
                Width = 44,
                Height = 30,
                Margin = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top
            };
            closeButton.SetResourceReference(StyleProperty, "QiTuOverlayCloseButton");
            closeButton.Click += (sender, args) => Close();
            Grid.SetColumn(closeButton, 1);
            grid.Children.Add(closeButton);

            return grid;
        }

        private UIElement CreateCloseIcon()
        {
            var icon = new Path
            {
                Width = 10,
                Height = 10,
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse("M3,3 L9,9 M9,3 L3,9")
            };
            icon.SetBinding(Shape.StrokeProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
            return icon;
        }

        private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !IsInsideButton(e.OriginalSource as DependencyObject))
            {
                DragMove();
            }
        }

        private static bool IsInsideButton(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is Button)
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            Close();
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            if (!_canRememberPosition)
            {
                return;
            }

            _savePosition?.Invoke(_positionKey, new Point(Left, Top));
        }
    }
}
