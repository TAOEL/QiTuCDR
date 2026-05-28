# QiTuCDR UI 设计系统

本文档是 QiTuCDR V1.0 的 UI 设计基准。后续修改工具条、综合面板、独立 WPF 工具窗口、预览器时，必须先对照本文档。

## 1. 设计来源

用户提供的设计系统来源：

```text
C:\Users\Administrator\Desktop\DEo\qitu-cdr\CDRPlugin-UI-Components
```

已参考的文件：

```text
CDR-Plugin-Design-Tokens.md
CDR-Plugin-Design-System-Complete.md
```

落地原则：

- 不整套搬入外部设计系统，避免引入过重样式和不必要依赖。
- 只提取适合 QiTuCDR V1.0 的核心令牌：颜色、字体、间距、圆角、控件尺寸。
- WPF 独立窗口、Web 综合面板、预览器三端保持视觉一致。
- CorelDRAW 宿主稳定性优先，视觉效果不能影响单 WebView2、COM 安全和插件生命周期。

## 2. 主视觉方向

当前确认主视觉：

```text
黑 / 白 + 绿色强调
```

暗黑主题不是蓝灰风格，而是中性黑灰风格。绿色只用于主操作、选中态、焦点态和少量状态提示。

## 3. 技术边界

| 界面 | 技术 | 说明 |
|------|------|------|
| 工具条 | WPF UserControl | CorelDRAW Addons 工具条入口 |
| 综合面板 | 单例 WebView2 + React | 只允许一个 WebView2 实例 |
| 独立工具窗口 | WPF Window + WPF UserControl | 每个功能独立原生小窗口 |
| 预览器 | WPF + WebView2 预览壳 | 用于减少频繁启动 CorelDRAW |
| 业务执行 | C# Core Service | Web 不直接操作 CorelDRAW |
| CorelDRAW 操作 | ComDispatcher | COM 调用必须走调度器 |

禁止事项：

- 禁止每个独立工具窗口创建 WebView2。
- 禁止 Web 层保存 Shape、Document、Layer 状态。
- 禁止为了视觉效果引入重型 UI 框架、Three.js、WebGL 或复杂动画。

## 4. 颜色令牌

### 4.1 浅色主题

| Token | 当前值 | 用途 |
|------|------|------|
| `QiTuBgBase` | `#F5F5F7` | 页面和窗口底色 |
| `QiTuBgSurface` | `#FFFFFF` | 标题栏、卡片、面板表面 |
| `QiTuBgElevated` | `#F0F0F2` | 抬升层、弱背景 |
| `QiTuBgInset` | `#FAFAFB` | 输入框、内凹控件 |
| `QiTuBgHover` | `#E8E8EB` | 悬停背景 |
| `QiTuBgActive` | `#E8F5D0` | 选中背景 |
| `QiTuAccentPrimary` | `#7BC029` | 主绿色强调 |
| `QiTuAccentPrimaryHover` | `#8FD13A` | 主按钮悬停 |
| `QiTuAccentPrimaryActive` | `#6BA820` | 主按钮按下 |
| `QiTuAccentDanger` | `#EF4444` | 关闭、危险操作 |
| `QiTuTextPrimary` | `#1C1C1E` | 主文字 |
| `QiTuTextSecondary` | `#6B7280` | 辅助文字 |
| `QiTuTextTertiary` | `#9CA3AF` | 弱提示文字 |
| `QiTuBorderDefault` | `#E5E7EB` | 默认边框 |
| `QiTuBorderHover` | `#D1D5DB` | 悬停边框 |
| `QiTuBorderFocus` | `#7BC029` | 焦点边框 |
| `QiTuScrollbarThumb` | `#B8BEC5` | 窄滚动条滑块 |
| `QiTuScrollbarThumbHover` | `#8E96A3` | 窄滚动条悬停滑块 |

### 4.2 暗黑主题

| Token | 当前值 | 用途 |
|------|------|------|
| `QiTuBgBase` | `#080808` | 窗口底色 |
| `QiTuBgSurface` | `#181818` | 标题栏、卡片、表面 |
| `QiTuBgElevated` | `#202020` | 抬升层 |
| `QiTuBgInset` | `#0D0D0D` | 输入框、内凹控件 |
| `QiTuBgHover` | `#262626` | 悬停背景 |
| `QiTuBgActive` | `#17320E` | 绿色选中背景 |
| `QiTuAccentPrimary` | `#7BC029` | 主绿色强调 |
| `QiTuAccentPrimaryHover` | `#8FD13A` | 主按钮悬停 |
| `QiTuAccentPrimaryActive` | `#6BA820` | 主按钮按下 |
| `QiTuAccentDanger` | `#EF4444` | 关闭、危险操作 |
| `QiTuTextPrimary` | `#F5F5F5` | 主文字 |
| `QiTuTextSecondary` | `#C7CDD4` | 辅助文字 |
| `QiTuTextTertiary` | `#8B929A` | 弱提示文字 |
| `QiTuBorderDefault` | `#333333` | 默认边框 |
| `QiTuBorderHover` | `#454545` | 悬停边框 |
| `QiTuBorderFocus` | `#7BC029` | 焦点边框 |
| `QiTuScrollbarThumb` | `#5A5A5A` | 窄滚动条滑块 |
| `QiTuScrollbarThumbHover` | `#737373` | 窄滚动条悬停滑块 |

## 5. 字体规范

全项目统一不使用粗字体。

| 项目 | 规范 |
|------|------|
| 字体 | 系统默认 UI 字体，Windows 优先 `Segoe UI` / `Microsoft YaHei UI` |
| 字重 | 全部 `Normal` / `400` |
| 禁止 | `Bold`、`SemiBold`、`font-weight: 500/600/700` |
| 例外 | 暂无。后续如果要恢复局部强调，必须先更新本文档 |

字号令牌：

| Token | 值 | 用途 |
|------|------|------|
| `QiTuFontXs` | 11px | 状态栏、弱提示 |
| `QiTuFontSm` | 12px | 描述文字 |
| `QiTuFontBase` | 13px | 正文、标签、输入框 |
| `QiTuFontMd` | 14px | 区块标题 |
| `QiTuFontLg` | 16px | 大标题，谨慎使用 |

## 6. 间距与网格

QiTuCDR 当前采用 **4px 基准网格**。

| Token | 值 | 用途 |
|------|------|------|
| `QiTuSpace1` | 4px | 极小间距、弹层偏移 |
| `QiTuSpace2` | 8px | 图标与文字、按钮间距 |
| `QiTuSpace3` | 12px | 字段行距、输入框内边距 |
| `QiTuSpace4` | 16px | 内容区内边距、区块间距 |
| `QiTuSpace5` | 24px | 大区块间距 |

当前独立窗口内部布局：

```text
字段标签列：96px
字段行距：12px
描述区底部间距：16px
提示卡片底部间距：16px
按钮区顶部间距：8px
按钮间距：8px
内容区内边距：16px
```

## 7. 圆角规范

| Token | 值 | 用途 |
|------|------|------|
| `QiTuRadiusSm` | 4px | 小标签、复选框、状态标签 |
| `QiTuRadiusMd` | 6px | 输入框、按钮、独立窗口主外框 |
| `QiTuRadiusLg` | 8px | 卡片、提示面板 |

标题栏窗口控制按钮不使用圆角，悬停热区为矩形，参考桌面软件和微信窗口控件风格。

## 8. 控件尺寸

| 控件 | 当前尺寸 |
|------|------|
| 独立窗口视觉主体宽度 | 420px |
| 外层阴影安全边距 | 左右各 8px |
| WPF Window 总宽度 | 436px |
| 标题栏高度 | 32px |
| 底部状态栏高度 | 28px |
| 输入框高度 | 32px |
| 下拉框高度 | 32px |
| 复选框最小高度 | 32px |
| 主按钮高度 | 36px |
| 次按钮高度 | 36px |
| 标题栏普通按钮热区 | 36px × 32px |
| 标题栏关闭按钮热区 | 44px × 32px |
| 标题栏图标本体 | 12px × 12px |
| 标题栏图标线宽 | 1.0px |
| 标题栏版本标签高度 | 16px |
| 标题栏弹层视觉宽度 | 316px |
| 标题栏弹层窗口总宽 | 332px |
| 标题栏弹层最大高度 | 320px |
| 标题栏弹层拖动区高度 | 36px |
| 标题栏弹层关闭按钮 | 44px × 30px |
| 标题栏弹层关闭图标 | 10px × 10px |
| 设置弹层开关 | 32px × 18px |
| 弹层窄滚动条热区 | 8px |
| 弹层滚动条滑块 | 4px，悬停 6px |

## 9. 标题栏控件规范

独立窗口标题栏固定四个控件：

```text
置顶 -> 设置 -> 展开/收起 -> 关闭
```

标题文本后方允许放置版本标签，例如：

```text
批量转曲  1.0.0
```

规则：

- 关闭按钮必须贴近标题栏最右侧。
- 四个按钮 hover 区域不使用圆角。
- 图标使用 WPF 本地矢量 Path，不依赖外网图标资源。
- 可以参考 IconPark / XIcons 的图形语言，但落地时必须转为本地矢量。
- 图标必须细线、轻量，不允许粗重。
- 版本标签点击打开窗口内部版本信息弹层。
- 设置按钮点击打开窗口内部设置弹层。
- 标题栏弹层使用微信风格窄滚动条，禁止使用默认 Windows 粗滚动条。
- 标题栏弹层使用独立 WPF 浮动窗口实现，可以拖动到独立工具窗口外，不参与主窗口收起 / 展开的尺寸计算。
- 标题栏弹层滚动条必须贴右边缘，默认隐藏，鼠标进入滚动区域后显示。
- 标题栏弹层标题文字和关闭按钮必须同一水平线居中对齐。
- 标题栏弹层关闭按钮使用上贴边、右贴边的横向长方形热区，hover 使用危险红色。
- 标题栏弹层关闭按钮只保留右上角圆角，其余角为直角。
- 版本弹层和设置弹层内容区必须使用同一套左侧 20px、滚动区右侧 0px 的边距，禁止各页面自行漂移。
- 弹层滚动条必须贴弹层外壳右边缘，正文内容必须通过 20px 右侧避让避免被滚动条覆盖。
- 标题栏弹层必须支持 `ESC` 关闭、点击外部关闭；当 `保存窗口位置` 开启时，弹层位置写入本地配置并跨重启恢复。
- 设置弹层采用左侧文本、右侧开关的设置行，不使用复选框列表、描述文字和分隔线。

## 10. 独立窗口配置规则

独立 WPF 工具窗口的设置项统一写入 `%LOCALAPPDATA%\QiTuCDR\Config\settings.json` 的 `NativePanel` 节点。

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `WindowTopmost` | `false` | 控制当前独立窗口是否置顶，标题栏置顶按钮和设置弹层开关必须同步 |
| `SaveWindowPosition` | `true` | 控制独立工具窗口和标题栏弹层是否保存 / 恢复位置 |
| `SaveToolSettings` | `true` | 预留给后续工具参数默认值保存 |
| `AutoBackupOriginalFile` | `false` | 预留给后续文件级危险操作前备份 |
| `ShowTaskCompletedToast` | `true` | 预留给后续任务完成提示 |
| `ToolWindowPositions` | `{}` | 按工具 key 保存独立窗口位置 |
| `PopupWindowPositions` | `{}` | 按弹层 key 保存版本 / 设置弹层位置 |

## 11. 三端一致规则

三端指：

```text
http://127.0.0.1:4173/ Web 综合面板
NativePanelPreview 预览器
CorelDRAW 26 真实插件
```

落地顺序：

1. 先在 `127.0.0.1:4173` 或预览器确认视觉。
2. 再同步到 WPF 独立窗口或 WebUI 构建产物。
3. 最后进入 CorelDRAW 26 做真实验证。

注意：

- 不频繁启动 CorelDRAW 27，用户当前可能用于生产设计。
- 真实 CDR 验证优先使用 CorelDRAW 26。
