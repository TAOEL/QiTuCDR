namespace QiTuCDR.Host.NativePanels.Panels
{
    internal sealed class ConvertTextPanel : NativeToolPanelBase
    {
        public ConvertTextPanel()
        {
            var root = CreateRoot("按范围批量转曲。执行前会生成选区快照，后续通过 C# 原生层和 COM Dispatcher 调用 CorelDRAW。");
            root.Children.Add(CreateField("处理范围", CreateCombo("选中对象", "当前页面", "全文档")));
            root.Children.Add(CreateCheckBox("包含隐藏对象"));
            root.Children.Add(CreateNotice("批处理阈值：50 个对象一组；锁定对象会跳过并写入日志。"));
            root.Children.Add(CreateActions("执行转曲"));
            Content = root;
        }
    }
}
