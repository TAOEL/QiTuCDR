namespace QiTuCDR.Host.NativePanels.Panels
{
    internal sealed class NormalizePanel : NativeToolPanelBase
    {
        public NormalizePanel()
        {
            var root = CreateRoot("统一选中对象的宽高、比例和描边宽度，批量应用到当前选区快照。");
            root.Children.Add(CreateField("宽度", CreateTextBox("100")));
            root.Children.Add(CreateField("高度", CreateTextBox("100")));
            root.Children.Add(CreateField("描边宽度", CreateTextBox("0.2")));
            root.Children.Add(CreateCheckBox("等比例锁定", true));
            root.Children.Add(CreateActions("应用规整"));
            Content = root;
        }
    }
}
