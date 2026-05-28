namespace QiTuCDR.Host.NativePanels.Panels
{
    internal sealed class PlaceholderToolPanel : NativeToolPanelBase
    {
        public PlaceholderToolPanel(string title)
        {
            var root = CreateRoot(title + " 已接入独立窗口入口。当前是占位面板，后续会按 Page -> DTO -> Command -> Service -> ComDispatcher 链路补齐。");
            root.Children.Add(CreateNotice("该窗口只用于验证二级菜单和独立面板交互，不执行 CorelDRAW 操作。"));
            root.Children.Add(CreateActions("即将接入"));
            Content = root;
        }
    }
}
