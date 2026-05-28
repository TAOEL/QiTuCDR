namespace QiTuCDR.Host.NativePanels.Panels
{
    internal sealed class CenterPanel : NativeToolPanelBase
    {
        public CenterPanel()
        {
            var root = CreateRoot("将选中对象居中到当前页面，支持整体居中和多对象独立居中。");
            root.Children.Add(CreateField("居中模式", CreateCombo("多对象整体居中", "多对象独立居中", "单对象页面居中")));
            root.Children.Add(CreateNotice("执行时只读取选区快照，不长期持有 Shape 或 Document。"));
            root.Children.Add(CreateActions("执行居中"));
            Content = root;
        }
    }
}
