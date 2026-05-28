namespace QiTuCDR.Host.NativePanels.Panels
{
    internal sealed class CleanupPanel : NativeToolPanelBase
    {
        public CleanupPanel()
        {
            var root = CreateRoot("清理页面辅助线、隐藏空图层、空文本框等冗余内容。此功能必须二次确认。");
            root.Children.Add(CreateCheckBox("我确认要执行冗余清理"));
            root.Children.Add(CreateNotice("为避免误删，未勾选确认前，后续执行按钮会接入禁用逻辑。"));
            root.Children.Add(CreateActions("开始清理", true));
            Content = root;
        }
    }
}
