# NativePanelPreview

该目录用于预览 QiTuCDR 的工具条入口和原生 WPF 独立工具窗口。

## 用途

```text
不启动 CorelDRAW
不连接 COM
不执行真实文档操作
只查看工具条入口和 WPF 独立功能窗口的视觉效果
支持浅色 / 暗黑主题切换预览
支持工具条悬浮 / 嵌入状态预览
支持一键重置预览状态
支持核心窗口逐项巡检
支持嵌入同源 Web 综合面板
支持 Web 综合面板加载状态提示
支持右下角精简三端一致状态提示
支持拖动综合面板外框移动位置
```

## 运行

```powershell
dotnet run --project tests\NativePanelPreview\QiTuCDR.NativePanelPreview\QiTuCDR.NativePanelPreview.csproj
```

## 当前可预览内容

- QiTuCDR 工具条入口
- “企图插件”综合面板入口模拟层
- 嵌入 `http://127.0.0.1:4173/` 的同源 Web 综合面板
- Web 综合面板加载中 / 已加载 / 加载失败提示
- 右下角三端一致状态提示
- 综合面板可拖动移动
- 工具分组一级菜单
- 自定义二级菜单视觉
- 二级菜单打开独立 WPF 窗口
- 当前综合面板和当前工具窗口状态反馈
- 重置状态入口
- 核心窗口巡检入口
- 批量转曲
- 一键居中
- 冗余清理
- 尺寸规整
- 占位工具窗口

## 注意

该预览器只做界面验收，不会调用 CorelDRAW，也不会修改 CDR 文档。

综合面板区域依赖本地前端预览服务：

```powershell
cd web
npm run preview -- --host 127.0.0.1
```
