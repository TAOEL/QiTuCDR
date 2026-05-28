# 独立 WPF 工具窗口 UI 规范

本文档记录 QiTuCDR 独立 WPF 工具窗口的当前尺寸、布局和控件规则。该窗口用于二级菜单打开的独立功能面板，例如批量转曲、一键居中、冗余清理、尺寸规整。

## 1. 当前窗口尺寸

```text
WPF Window 总宽度：436px
外层阴影安全边距：8px × 2
面板主体视觉宽度：420px
最小总宽度：416px
最大高度：720px
高度策略：根据内容自动撑开
```

计算关系：

```text
436 - 8 - 8 = 420
```

也就是说，用户肉眼看到的窗口主体是 420px，外层额外 16px 用于透明圆角和阴影安全区。

## 2. 窗口结构

```text
NativeToolWindow
├─ 外层透明安全区：8px
├─ 主窗口 Border：420px 视觉宽度，6px 圆角
│  ├─ 标题栏：32px
│  ├─ 内容区：16px padding
│  └─ 状态栏：28px
```

当前窗口不是 WebView2。它是纯 WPF Window + WPF UserControl。

## 3. 标题栏

标题栏高度：

```text
32px
```

标题栏布局：

```text
左侧企图标识 20px × 20px
标题文本 13px / Normal
版本标签 1.0.0
右侧控件：置顶、设置、展开/收起、关闭
```

版本标签规则：

```text
位置：标题文本后方
高度：16px
字号：11px / Normal
点击行为：打开窗口内部“版本信息”弹层
```

右侧控件尺寸：

| 控件 | 热区 | 图标 | 线宽 |
|------|------|------|------|
| 置顶 | 36px × 32px | 12px × 12px | 1.0px |
| 设置 | 36px × 32px | 12px × 12px | 1.0px |
| 展开/收起 | 36px × 32px | 12px × 12px | 1.0px |
| 关闭 | 44px × 32px | 12px × 12px | 1.0px |

交互规则：

- 普通按钮 hover 使用矩形背景，不加圆角。
- 关闭按钮 hover 使用危险红色背景。
- 关闭按钮贴标题栏最右侧。
- 置顶激活状态后续需要做明确视觉高亮。
- 设置按钮点击打开窗口内部“设置”弹层，不能再使用系统 MessageBox。

## 4. 内容区网格

内容区采用 4px 基准网格。

```text
内容区 padding：16px
字段标签列：96px
字段行距：12px
说明文本底部间距：16px
提示卡片底部间距：16px
按钮区顶部间距：8px
按钮之间间距：8px
```

控件尺寸：

```text
输入框高度：32px
下拉框高度：32px
复选框最小高度：32px
主按钮高度：36px
次按钮高度：36px
提示卡片内边距：12px
```

## 5. 当前已落地窗口

```text
批量转曲
一键居中
冗余清理
尺寸规整
占位工具窗口
```

这些窗口都继承 `NativeToolPanelBase`，必须统一使用同一套字段、说明、提示和按钮布局。

## 6. 标题栏弹层

独立窗口的标题栏弹层统一使用轻量 WPF 浮动窗口 `NativeToolPopupWindow`。它不是 WebView2，也不会参与独立工具窗口的内容高度计算。

当前弹层：

```text
版本信息：点击标题后方版本标签打开
设置：点击标题栏设置按钮打开
```

弹层视觉规则：

```text
WPF Popup Window 总宽度：332px
外层阴影安全边距：8px × 2
弹层主体视觉宽度：316px
最大高度：320px
默认位置：标题栏下方右侧
圆角：6px
标题区高度：36px，顶部整行可拖动
内容左边距：20px
滚动区域右边距：0px
内容右侧避让：20px
标题和内容间距：12px
滚动内容最大高度：236px
```

交互规则：

- 弹层标题区域可以拖动，拖动热区从弹层顶部开始。
- 标题文字和关闭按钮必须保持同一水平线居中对齐。
- 关闭按钮为上贴边、右贴边的横向长方形热区，hover 使用危险红色。
- 关闭按钮只保留右上角圆角，其余角为直角，右上角圆角与外壳裁剪保持一致。
- 弹层允许拖出当前独立工具窗口外。
- 弹层不影响独立工具窗口的收起 / 展开 / 自动高度计算。
- 同一个独立工具窗口内同时只显示一个标题栏弹层。
- 关闭独立工具窗口时，自动关闭它打开的弹层。
- 按 `ESC` 关闭当前弹层。
- 点击弹层外部后，弹层自动关闭。
- 弹层位置在当前运行会话内记忆：拖动后关闭再打开，会回到上次位置；退出程序后重置。

滚动条规则：

```text
风格：微信式窄滚动条
轨道：透明
默认滑块宽度：4px
悬停滑块宽度：6px
滚动条热区：8px
位置：贴弹层外壳右边缘
布局：滚动条浮在滚动区域最右侧，内容通过 20px 右侧避让防止遮挡
显示时机：鼠标进入滚动区域时显示，离开后隐藏
不显示默认 Windows 箭头按钮
```

设置弹层规则：

```text
布局：左侧文本，右侧开关
开关尺寸：32px × 18px
开启颜色：QiTuAccentPrimary
关闭颜色：QiTuBorderHover
行高：最小 38px
内容左边距：20px，与版本弹层保持一致
滚动区域右边距：0px，与版本弹层保持一致
不显示描述文字
不显示行分隔线
```

## 7. 文件位置

窗口壳：

```text
src/Host/NativePanels/NativeToolWindow.cs
src/Host/NativePanels/Styles/NativeWindowStyles.xaml
src/Host/NativePanels/Styles/NativeButtonStyles.xaml
src/Host/NativePanels/Styles/NativeFormStyles.xaml
```

工具面板：

```text
src/Host/NativePanels/Panels/NativeToolPanelBase.cs
src/Host/NativePanels/Panels/ConvertTextPanel.cs
src/Host/NativePanels/Panels/CenterPanel.cs
src/Host/NativePanels/Panels/CleanupPanel.cs
src/Host/NativePanels/Panels/NormalizePanel.cs
src/Host/NativePanels/Panels/PlaceholderToolPanel.cs
```

主题：

```text
src/Host/NativePanels/Themes/NativeLightTheme.xaml
src/Host/NativePanels/Themes/NativeDarkTheme.xaml
```

## 8. 后续待优化

- 置顶激活态需要增加可见状态。
- 设置弹层目前是 UI 骨架，后续接入工具参数记忆、默认值配置和窗口位置保存。
- 冗余清理确认勾选后，按钮禁用/启用逻辑需要接入真实命令。
- 独立窗口视觉确认后，需要进入 CorelDRAW 26 做真实宿主验证。
