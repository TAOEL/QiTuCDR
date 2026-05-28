using System.Windows;
using System.Windows.Controls;

namespace QiTuCDR.Host.NativePanels.Panels
{
    internal abstract class NativeToolPanelBase : UserControl
    {
        private const double FieldLabelWidth = 96;
        private const double FieldBottomGap = 12;
        private const double SectionBottomGap = 16;
        private const double ActionTopGap = 8;
        private const double ButtonGap = 8;

        protected NativeToolPanelBase()
        {
            NativePanelResourceLoader.MergeInto(this);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
        }

        protected StackPanel CreateRoot(string description)
        {
            var root = new StackPanel();
            root.Children.Add(CreateDescription(description));
            return root;
        }

        protected TextBlock CreateDescription(string text)
        {
            var block = new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 0, 0, SectionBottomGap)
            };
            block.SetResourceReference(StyleProperty, "QiTuDescriptionText");
            return block;
        }

        protected Grid CreateField(string label, UIElement editor)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, FieldBottomGap) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(FieldLabelWidth) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock { Text = label };
            labelBlock.SetResourceReference(StyleProperty, "QiTuFieldLabel");
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            Grid.SetColumn(editor, 1);
            grid.Children.Add(editor);
            return grid;
        }

        protected ComboBox CreateCombo(params string[] values)
        {
            var combo = new ComboBox { SelectedIndex = 0 };
            combo.SetResourceReference(StyleProperty, "QiTuComboBox");
            foreach (var value in values)
            {
                combo.Items.Add(value);
            }

            return combo;
        }

        protected TextBox CreateTextBox(string value)
        {
            var textBox = new TextBox { Text = value };
            textBox.SetResourceReference(StyleProperty, "QiTuTextBox");
            return textBox;
        }

        protected CheckBox CreateCheckBox(string text, bool isChecked = false)
        {
            var checkBox = new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Margin = new Thickness(0, 0, 0, FieldBottomGap)
            };
            checkBox.SetResourceReference(StyleProperty, "QiTuCheckBox");
            return checkBox;
        }

        protected Border CreateNotice(string text)
        {
            var border = new Border { Margin = new Thickness(0, 4, 0, SectionBottomGap) };
            border.SetResourceReference(StyleProperty, "QiTuPanelCard");
            var block = CreateDescription(text);
            block.Margin = new Thickness(0);
            border.Child = block;
            return border;
        }

        protected StackPanel CreateActions(string primaryText, bool danger = false)
        {
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, ActionTopGap, 0, 0)
            };

            var secondary = new Button
            {
                Content = "取消任务",
                IsEnabled = false,
                Margin = new Thickness(0, 0, ButtonGap, 0)
            };
            secondary.SetResourceReference(StyleProperty, "QiTuSecondaryButton");
            actions.Children.Add(secondary);

            var primary = new Button { Content = primaryText };
            primary.SetResourceReference(StyleProperty, danger ? "QiTuDangerButton" : "QiTuPrimaryButton");
            actions.Children.Add(primary);
            return actions;
        }
    }
}
