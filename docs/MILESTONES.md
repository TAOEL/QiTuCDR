# V1.0 里程碑排期

本文档用于跟踪 QiTuCDR V1.0 从工程骨架到发布交付的阶段排期。状态以当前仓库实现为准，后续推进时同步更新。

## 状态说明

| 状态 | 含义 |
|------|------|
| 已完成 | 已有代码、文档和基础验证闭环 |
| 基本完成 | 工程链路已闭合，但仍需要真实 CorelDRAW 宿主或大文档验收 |
| 推进中 | 已开始实现，但仍有明确缺口 |
| 待开始 | 尚未进入工程实施 |

## 总览

| 阶段 | 状态 | 核心交付 | 当前说明 |
|------|------|----------|----------|
| M1 | 已完成 | Solution 和 React 工程骨架 | `src/`、`web/`、`tests/`、构建脚本、DTO、错误码已建立 |
| M2 | 已完成 | 生命周期、Dock Shell、状态机、日志 | `PluginLifecycleManager`、`PluginStateMachine`、`DebugDockPanelHost`、文件日志已实现 |
| M3 | 已完成 | WebView2 单例、延迟预热、降级面板 | `WebView2Manager` 单例、4 秒延迟预热、WPF fallback 已具备 |
| M4 | 已完成 | Bridge 通信层、序列化、EventBus | 标准 Request/Response/Event DTO、`BridgeDispatcher`、前端 bridge 已实现 |
| M5 | 已完成 | Echo 双向互通与状态拦截 | Echo、getState、Busy 拦截、非法 JSON 容错已有测试覆盖 |
| M6 | 基本完成 | 批量转曲完整链路 | 转曲服务、选区快照重解析、分批进度、取消、终态事件、事务壳已完成；待真实 CorelDRAW 大文档验收 |
| M7 | 基本完成 | 一键居中、冗余清理、尺寸规整 | 三项工具均已具备 Native 服务、前端运行态、取消、终态事件和 dynamic/typed adapter 事务壳；待真实 CorelDRAW 文档验收 |
| M8 | 推进中 | 压测与稳定性加固 | HostHarness 100 次面板开关压测已通过；已修复 WebView2 预热跨线程和 Dispose 冒泡风险；真实 CorelDRAW 压测待执行 |
| M9 | 推进中 | 安装包与发布交付 | Release 发布包、验包、从包安装/卸载冒烟、DLL 版本一致性、注册计划覆盖报告和注册 manifest 闭环已通过；真实 AddIn 注册路径和 Docker 接入待确认 |

## M1：工程骨架

**目标：** 从空仓库建立可构建、可扩展、可测试的工程基础。

**交付内容：**
- `.NET Framework 4.8` solution。
- `Shared`、`Bridge`、`Infrastructure`、`Core`、`Host` 分层项目。
- React + TypeScript + Vite 前端工程。
- `src/WebUI` 静态产物输出。
- 基础 DTO、错误码、构建脚本和测试项目。

**状态：已完成。**

## M2：生命周期与状态机

**目标：** 建立 Native 插件生命周期闸门，避免业务绕过状态管理。

**交付内容：**
- `PluginLifecycleManager`。
- `PluginStateMachine`。
- WPF Dock Shell 调试宿主。
- 本地文件日志。
- 启动、预热、Ready、Busy、Recovering、Disposing、Disposed 状态流。

**状态：已完成。**

## M3：WebView2 单例与降级

**目标：** 确保插件全生命周期只有一个 WebView2 实例，并在 WebView 不可用时降级。

**交付内容：**
- `WebView2Manager` 单例管理。
- 延迟预热。
- 面板关闭隐藏策略。
- WebView2 初始化失败检测。
- WPF 原生 fallback 面板。

**状态：已完成。**

## M4：Bridge 与 EventBus

**目标：** 建立 Web 与 Native 的标准通信协议和事件分发机制。

**交付内容：**
- `RequestDto`、`ResponseDto`、`EventDto`。
- JSON 序列化和非法输入容错。
- `BridgeDispatcher`。
- `EventBus`。
- 前端 `nativeBridge` 封装。

**状态：已完成。**

## M5：Echo 联调闭环

**目标：** 验证 Web -> Native -> Web 的最小闭环和状态机拦截。

**交付内容：**
- `echo`。
- `getState`。
- `cancelCurrentTask`。
- Busy 状态重复请求拦截。
- 非法 action / 非法 JSON 标准响应。

**状态：已完成。**

## M6：批量转曲

**目标：** 用第一个真实工具验证完整业务链路。

**交付内容：**
- `convertText` 标准命令。
- range / includeHidden 参数。
- 选区快照 `SelectionSnapshot.ShapeIds`。
- ShapeId 执行前重解析。
- 50 个对象分批进度。
- 跳过隐藏/锁定/异常对象。
- `task.progress`、`task.completed`、`task.failed`。
- 取消任务。
- dynamic / typed adapter 转曲路径。
- CorelDRAW 文档命令组事务壳。
- React 运行态、取消按钮和进度条。

**状态：基本完成。**

**剩余验收：**
- 在真实 CorelDRAW 文档中验证 5000+ Shape。
- 验证锁定对象、隐藏对象、空选区、文档关闭时行为。
- 验证 Undo/Redo 命令组表现。

## M7：其余三项核心工具

**目标：** 补齐 V1.0 核心工具集。

**交付内容：**
- 一键居中。
- 冗余清理。
- 尺寸规整。

**当前状态：基本完成。**

**已完成：**
- 一键居中：模式校验、选区快照重解析、取消/完成事件、事务壳、React 运行态。
- 尺寸规整：宽高/描边校验、仅描边规整、选区快照重解析、取消/完成事件、事务壳、Outline COM 释放、React 运行态。
- 冗余清理：二次确认、取消/完成/失败事件、React 运行态、dynamic/typed adapter 命令组事务壳；当前实现会尽力清理页面辅助线、隐藏空图层和空文本对象，单项失败只记录日志并继续。

**剩余验收：**
- 在真实 CorelDRAW 文档中验证辅助线、隐藏空图层、空文本对象的清理覆盖率。
- 验证清理过程中的取消、文档关闭和 Undo/Redo 命令组表现。

## M8：压测与稳定性

**目标：** 验证插件在真实生产场景下长期稳定。

**计划场景：**
- 5000+ Shape 批处理。
- 面板 100 次开关不新增 WebView2 实例。
- WebView2 crash 降级恢复。
- 文档执行中关闭。
- 24 小时挂起运行。
- 内存涨幅监控。

**状态：推进中。**

**已完成：**
- 新增 [M8 稳定性与压测计划](STABILITY_TEST_PLAN.md)。
- 新增 `tools/stress/Invoke-QiTuStressBaseline.ps1`，可生成构建、诊断、WebUI 产物和进程快照基线报告。
- 新增 HostHarness `-PanelStress` 高频开关模式和 `tools/stress/Invoke-QiTuPanelStress.ps1`，用于验证本地 WPF Shell 不创建第二个 WebView2 控件。
- 新增生命周期护栏单测，覆盖重复业务调度、异常后回到 Ready、取消中心复用。
- HostHarness 100 次面板开关压测已通过：最终状态 `Ready`，`WebViewCreateCount = 1`，`BrowserRecoveryCount = 0`。
- 修复 WebView2 延迟预热跨线程访问问题，WebView 初始化和消息投递统一回到 WebView Dispatcher。
- 加固生命周期 Dispose，卸载阶段异常只记录日志，不再冒泡到宿主。
- 新增 `tools/stress/Invoke-QiTuMemoryWatch.ps1`，用于 24 小时挂起内存涨幅采样并生成 CSV / Markdown 报告。
- 新增 [真实 CorelDRAW 宿主验收记录模板](REAL_HOST_VALIDATION_TEMPLATE.md)，统一记录 M6-M8 真实宿主回归结果。
- 新增 HostHarness WebView2 恢复压测模式和 `tools/stress/Invoke-QiTuRecoveryStress.ps1`，可模拟 browser failure 并验证状态回到 `Ready`。
- 加固 WebView2 browser failure 恢复流程，触发恢复时会同步 `CancelAll` 当前任务。
- 新增文档关闭取消入口 `PluginLifecycleManager.NotifyDocumentClosing()`，AddIn 与 HostHarness 共用该路径。
- 新增 HostHarness 文档关闭压测模式和 `tools/stress/Invoke-QiTuDocumentCloseStress.ps1`，可验证文档关闭时取消当前任务并保持 `Ready`。

**待完成：**
- 真实 CorelDRAW 5000+ Shape 压测。
- 面板 100 次开关不新增 WebView2 实例的宿主内验证。
- WebView2 crash 恢复真实宿主验证。
- 真实 CorelDRAW 任务执行中关闭文档验证。
- 24 小时挂起内存涨幅真实宿主验证。

## M9：安装包与发布

**目标：** 完成可交付安装包和发布前检查。

**计划内容：**
- CorelDRAW AddIn 注册。
- 原生 Docker 接入。
- WebView2 Runtime 检测。
- `%LOCALAPPDATA%\QiTuCDR` 初始化。
- 安装、升级、卸载脚本。
- 发布检查清单执行。

**状态：推进中。**

**已完成：**
- 新增 `installer/Test-QiTuInstallPrerequisites.ps1`，安装前检查 Host DLL、WebUI 产物和环境诊断状态。
- 新增 `installer/Install-QiTuCDR.ps1`，可初始化安装目录、配置目录、日志目录，复制 Host 输出并生成 `install-manifest.json`。
- 新增 `installer/Uninstall-QiTuCDR.ps1`，默认删除 App 文件并保留用户配置/日志，可显式完整清理。
- 注册表写入采用显式参数，不猜测 CorelDRAW 私有注册路径。
- 新增 `build/scripts/Invoke-QiTuPackage.ps1`，可生成发布目录、zip、`package-manifest.json` 和 `SHA256SUMS.txt`。
- 新增 `build/scripts/Test-QiTuPackage.ps1`，可验证发布目录或 zip 的必需文件、manifest 和 SHA256 校验清单。
- 新增 `build/scripts/Test-QiTuReleaseInstall.ps1`，可从发布包执行安装、检查、默认卸载冒烟。
- 安装脚本改为自包含默认配置初始化，不再依赖仓库内 `tools/config`。
- 新增 `installer/Get-QiTuCorelRegistrationPlan.ps1`，只读探测 CorelDRAW TypeLib 和注册表候选项，输出注册计划报告。
- 发布包验证脚本已将注册计划脚本列为必需文件；发布安装冒烟会从包内执行该脚本并确认报告生成。
- 构建与打包脚本支持 `Configuration` 参数；发布包默认读取 `Release` 输出。
- 新增根目录 `VERSION`，打包脚本默认读取该版本号并将其复制进发布包，验包脚本会校验 manifest 与 `VERSION` 一致。
- .NET 程序集版本由 `Directory.Build.props` 集中读取 `VERSION`，发布包验证会校验 Host DLL 产品版本与 manifest 一致。
- 注册计划脚本新增目标版本覆盖报告，可显示当前机器已发现和缺失的 CorelDRAW 版本标识。
- 新增注册 manifest 模板生成与校验脚本，安装/卸载脚本可基于 `CONFIRMED` manifest 批量注册或反注册多个目标版本路径。
- 已完成受控 HKCU 注册/反注册冒烟，测试注册键最终清理完成。
- 新增 `IDockPanelHostFactory`，生命周期层不再直接创建 `DebugDockPanelHost`；真实 CorelDRAW Docker 后续可通过新增工厂接入。
- 新增 `CorelDockPanelHost` 和 `CorelDockPanelHostFactory` 占位结构，未接入官方 Docker API 时会明确失败并回退调试宿主。
- 新增 `DockHostMode` 配置和 Dock 工厂选择器，默认 `Debug`，误切 `CorelDocker` 时会记录错误并回退调试宿主。
- 新增文档激活、选区变化、宿主退出事件入口，统一转入生命周期层；文档切换/关闭会取消当前任务，选区变化只发事件不读 COM。
- 新增 HostHarness 宿主事件入口压测和 `Invoke-QiTuHostEventStress.ps1`，已完成本地串行冒烟。
- 新增 `docs/CORELDRAW_HOST_BINDING_CHECKLIST.md`，明确真实 CorelDRAW AddIn、Docker、事件绑定和验收入口。
- 已生成并验证 `0.1.0` Release 发布包：发布包验证状态 `OK`，发布安装冒烟状态 `OK`；具体包 ID 以 `package-manifest.json` 为准。
- 已抽查 `src/Host/bin/Release/net48/QiTuCDR.Host.dll`：`ProductVersion = 0.1.0`，`FileVersion = 0.1.0.0`。

**待完成：**
- 基于注册计划报告确认 CorelDRAW 2021-2026 AddIn 注册表路径或官方注册机制。
- 接入真实 CorelDRAW Docker 面板。
- 选择正式安装包技术方案。
- 真实安装、升级、卸载回归。

## 当前推进焦点

截至 2026-05-25，当前焦点是：

1. 用真实 CorelDRAW 文档回归 M6/M7 四个工具。
2. 执行 M8 压测基线脚本并保存报告。
3. 接入真实 CorelDRAW Docker / AddIn 注册。
