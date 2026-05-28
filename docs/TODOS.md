# TODO 列表

## UI 文档与真实宿主视觉验收

**内容：** 在 CorelDRAW 26 中回归验证工具条、综合面板入口、独立 WPF 工具窗口标题栏、圆角、图标、暗黑主题和 4px 网格布局，并把结果回填到 `docs/UI_IMPLEMENTATION_LOG.md`。

**原因：** 当前 UI 规范已经在预览器和 WPF 代码中落地，但最终产品运行环境仍然是 CorelDRAW 真实宿主，必须确认真实 DPI、停靠状态、浮动状态和宿主主题下没有显示偏差。

**起点：** `docs/UI_DESIGN_SYSTEM.md`、`docs/NATIVE_WINDOW_UI.md`、`tests/NativePanelPreview/QiTuCDR.NativePanelPreview`。

**依赖：** CorelDRAW 26 已关闭后再部署；不要影响用户正在使用的 CorelDRAW 27。

## 待处理

### 接入类型化 CorelDRAW SDK

**内容：** 在 Host/Core 边界把 `dynamic` CorelDRAW 调用替换为类型化 Interop wrapper。

**原因：** 当前骨架可以在没有私有 SDK 程序集的情况下编译，但生产可靠性需要类型化 API、明确对象所有权和可验证的 COM 释放行为。

**起点：** `src/Host/Environment/CorelHostContext.cs`、`src/Core/Tools`、`src/Core/Selection`。

**依赖：** 开发机可用 CorelDRAW 2021-2026 SDK/Interop。

### 用真实 Dock Panel 注册替换调试窗口

**内容：** 新增真实 CorelDRAW Docker 版 `IDockPanelHost`，替换当前 `DebugDockPanelHost`。

**原因：** 当前 `DebugDockPanelHost` 和 HostHarness 已可验证单例窗口、关闭隐藏、WebView2 和降级路径，但真实产品必须表现为 CorelDRAW Dock Panel。

**起点：** `src/Host/Docking/IDockPanelHost.cs` 和 `src/Host/Startup/QiTuCdrAddIn.cs`。

**依赖：** CorelDRAW add-in 注册和 docker API 细节。

### 补齐正式安装包

**内容：** 在现有安装脚本骨架上接入正式安装包技术方案、真实 CorelDRAW AddIn 注册路径和升级策略。

**原因：** V1 发布需要在设计师工作站上具备可预测的安装、升级、卸载行为。

**起点：** `installer/Install-QiTuCDR.ps1`、`installer/Uninstall-QiTuCDR.ps1`、`docs/RELEASE_CHECKLIST.md`。

**依赖：** 确定安装包技术方案和 CorelDRAW 2021-2026 注册机制。

### 执行真实 CorelDRAW 压测

**内容：** 按 `docs/STABILITY_TEST_PLAN.md` 执行 5000+ Shape、高频开关面板、WebView2 崩溃恢复、文档关闭取消、长时间空闲挂起等真实宿主验收。

**原因：** 自动化基线入口已建立，但这些场景必须在真实 CorelDRAW 宿主中确认，才能证明 V1 达到生产稳定性要求。

**起点：** `tools/stress/Invoke-QiTuStressBaseline.ps1` 和 `docs/STABILITY_TEST_PLAN.md`。

**依赖：** 可自动化的 CorelDRAW 测试环境。

## 已完成

- **2026-05-25：** 创建 V1 初始垂直骨架、React 工具面板、WebView2 Host Shell、Bridge 协议、状态机和基础测试。
- **2026-05-25：** 将 CorelDRAW 文档访问收敛到 `ICorelDocumentAdapter`，并通过 `StaticID` 实现选区快照回填解析路径。
- **2026-05-25：** 新增 `IDockPanelHost` 和 `DebugDockPanelHost`，本地调试面板已改为单例窗口，关闭仅隐藏。
- **2026-05-25：** 新增 `RuntimeEnvironmentChecker`，启动时检测 WebView2 Runtime、CorelDRAW TypeLib、WebUI 产物和宿主对象，并在 WebView2 缺失时走 WPF 降级。
- **2026-05-25：** 新增本地配置持久化，支持默认配置自动创建、损坏 JSON 备份、启动时加载配置，并统一 `%LOCALAPPDATA%\QiTuCDR` 下的配置和日志路径。
- **2026-05-25：** 加固 Bridge 输入容错，非法 Web JSON 不进入业务分发、不创建任务 token，并返回标准 `INVALID_PAYLOAD`。
- **2026-05-25：** 加固批量转曲进度策略，按已处理对象数进行 50 个对象分批，并保证全跳过任务仍发布最终进度。
- **2026-05-25：** 加固 Host adapter 的 COM 枚举释放路径，集中释放 `Shapes` 集合和逐个临时 `Shape`；构建脚本改为失败即中止。
- **2026-05-25：** 扩展环境诊断脚本，覆盖 WebUI 产物、WebView2 Runtime、CorelDRAW TypeLib、配置/日志目录可写性和 `settings.json` 合法性，并支持失败退出码。
- **2026-05-25：** 新增本地 CorelDRAW Interop 生成脚本，已验证可从 CorelDRAW 27 TypeLib 生成 `CorelDRAW27.Interop.dll` 和 `VGCore.dll` 到 ignored artifacts。
- **2026-05-25：** 新增可选 typed Interop adapter 骨架和显式构建开关，保持默认构建不依赖 CorelDRAW 私有程序集。
- **2026-05-25：** 新增 `CorelDocumentAdapterFactory`，集中管理 dynamic/typed adapter 选择，并修复 WPF 降级面板 XAML 文本。
- **2026-05-25：** 新增 `PreferTypedCorelInterop` 运行时配置开关，typed adapter 必须编译开启且配置明确启用才会优先尝试。
- **2026-05-25：** 新增本地配置工具，可创建默认配置并安全开启/关闭 typed Interop 运行时开关。
- **2026-05-25：** 新增本地 HostHarness WPF 启动项目和启动脚本，可在不启动 CorelDRAW 的情况下验证生命周期、WebView2、Bridge 和降级面板；Host 构建会复制 `src/WebUI` 到运行目录。
- **2026-05-25：** 加固批量转曲 M6 链路：Bridge 只在成功进入 Busy 后创建业务 token，避免取消命令或重复请求干扰当前任务；选区快照执行前会重新解析校验；dynamic/typed adapter 转曲时增加 CorelDRAW 文档命令组事务壳。
- **2026-05-25：** 补齐批量转曲前端验收壳：React 页面新增运行态、取消按钮和 `task.progress` 进度条；普通浏览器预览仍会返回标准 `WEBVIEW_NOT_READY`。
- **2026-05-25：** 补齐批量转曲任务终态事件：Native 成功发布 `task.completed`，取消/异常发布 `task.failed`；React 底部事件区显示可读任务结果；单元测试覆盖进度、完成和取消失败事件。
- **2026-05-25：** 推进尺寸规整 M7 链路：Native 支持仅规整描边、宽高/描边数值校验、选区快照重解析、取消/完成事件；dynamic/typed adapter 增加命令组事务壳并释放 Outline COM 临时对象；React 页面新增运行态和取消按钮。
- **2026-05-25：** 推进一键居中 M7 链路：Native 校验居中模式、选区快照重解析、取消/完成事件；dynamic/typed adapter 增加命令组事务壳；React 页面新增运行态和取消按钮；单元测试覆盖成功、非法模式、快照失效和取消。
- **2026-05-25：** 推进冗余清理 M7 链路：Native 强制二次确认、完成/失败事件和取消处理；dynamic/typed adapter 增加命令组事务壳，并尽力清理页面辅助线、隐藏空图层和空文本对象；React 页面新增运行态和取消按钮；单元测试覆盖确认、成功、取消和异常。
- **2026-05-25：** 启动 M8 稳定性加固：新增稳定性压测计划、基线报告脚本和生命周期护栏单测；构建脚本调整为先构建 WebUI、再编译 Host，避免前端 hash 资源变化导致 Host 输出陈旧。
- **2026-05-25：** 增强 M8 面板压测入口：`PluginLifecycleManager` 提供稳定性快照，`WebView2Manager` 记录创建/附加计数，HostHarness 新增 `-PanelStress` 高频开关模式，压测脚本可验证本地 Shell 不创建第二个 WebView2 控件。
- **2026-05-25：** 完成 HostHarness 100 次面板开关基线压测：修复 WebView2 延迟预热跨线程访问问题，消息投递统一回到 WebView Dispatcher；生命周期 Dispose 改为逐项记录异常且不向宿主冒泡；压测结果 `WebViewCreateCount = 1`、最终状态 `Ready`。
- **2026-05-25：** 新增 M8 24 小时内存监控脚本和真实 CorelDRAW 宿主验收记录模板，稳定性验收开始具备统一采样与记录口径。
- **2026-05-25：** 新增 HostHarness WebView2 恢复压测入口和脚本，模拟 browser failure 后验证 `Faulted -> Recovering -> Ready`，并在恢复时统一取消当前任务。
- **2026-05-25：** 新增文档关闭取消入口和 HostHarness 压测脚本，模拟文档关闭时会取消当前任务、发布 `host.documentChanged` 并保持状态 `Ready`。
- **2026-05-25：** 启动 M9 发布交付骨架：新增安装前检测、安装、卸载脚本，支持安装目录初始化、Host 输出复制、默认配置创建和安装 manifest 生成。
- **2026-05-25：** 新增发布包生成脚本，支持 staging 目录、zip、`package-manifest.json` 和 `SHA256SUMS.txt`，为正式安装包工具接入提供稳定输入。
- **2026-05-25：** 新增发布包验证脚本，支持对发布目录或 zip 执行必需文件、manifest 和 SHA256 校验。
- **2026-05-25：** 新增发布安装冒烟脚本，并将安装脚本改为自包含配置初始化，确保用户拿到发布包后无需完整仓库也能安装。
- **2026-05-25：** 新增 CorelDRAW 注册计划只读探测脚本，输出 TypeLib、Programs 目录和注册表候选项，为真实 AddIn 注册路径确认做准备。
- **2026-05-25：** 发布包验证与发布安装冒烟纳入注册计划脚本，确保用户拿到的 zip 包也包含并可运行注册探测能力。
- **2026-05-25：** 构建、安装前检测和打包脚本补齐 `Configuration` 参数，发布包默认使用 `Release` 输出。
- **2026-05-25：** 新增 `VERSION` 文件，打包脚本默认读取版本号，发布包验证会校验 manifest 与 `VERSION` 一致。
- **2026-05-25：** .NET 程序集版本统一从 `VERSION` 读取，发布包验证会校验 Host DLL 产品版本与 manifest 一致。
- **2026-05-25：** 注册计划脚本新增目标版本覆盖报告和下一步确认动作，用于标记已发现与缺失的 CorelDRAW 版本标识。
- **2026-05-25：** 重新生成 `0.1.0` Release 发布包，发布包验证和从包安装/卸载冒烟均通过；Host DLL 产品版本为 `0.1.0`，文件版本为 `0.1.0.0`；具体包 ID 以 `package-manifest.json` 为准。
- **2026-05-25：** 新增 CorelDRAW 注册 manifest 模板生成与校验脚本，安装/卸载脚本支持从 `CONFIRMED` manifest 批量注册或反注册；已完成受控 HKCU 注册/反注册冒烟。
- **2026-05-25：** 新增 `IDockPanelHostFactory`，解除生命周期层对 `DebugDockPanelHost` 的直接创建依赖；单元测试覆盖多次打开面板只创建一个宿主。
- **2026-05-25：** 新增 `CorelDockPanelHostFactory` 真实 Docker 接入占位；未确认官方 CorelDRAW Docker API 前，真实 Docker 路径不会假装可用。
- **2026-05-25：** 新增 `DockHostMode` 配置、Dock 工厂选择器和配置工具支持，默认保持 `Debug`，`CorelDocker` 未实现时回退调试宿主。
- **2026-05-25：** 新增文档激活、选区变化、宿主退出事件入口和稳定性计数；真实 CorelDRAW 事件后续只需绑定到统一生命周期入口。
- **2026-05-26：** 新增 HostHarness 宿主事件入口压测和 `Invoke-QiTuHostEventStress.ps1`；串行冒烟覆盖面板、恢复、文档关闭和宿主事件压测。
- **2026-05-26：** 新增真实 CorelDRAW 宿主绑定清单，明确 AddIn、Docker、文档事件、注册 manifest 和真实宿主验收入口。
- **2026-05-26：** 新增 `CorelDockPanelHost` 占位类，真实 Docker 工厂改为创建占位 Host；误切 `CorelDocker` 时生命周期会回退调试宿主。
- **2026-05-26：** 将 `CorelDockPanelHost` 占位细化为 `CreateDockerContainer`、`AttachWpfPanel`、`ShowDocker`、`HideDocker`、`ReleaseDocker` 5 个固定槽位，后续接官方 Docker API 时不会污染生命周期和业务层。
- **2026-05-26：** 稳定性快照新增 `ConfiguredDockHostMode`、`ActiveDockPanelHostType` 和 `DockHostFallbackCount`，HostHarness 压测报告可直接看出当前实际 Dock 宿主与回退次数。
- **2026-05-26：** HostHarness 支持进程级临时 `DockHostMode` 覆盖，已能验证误切 `CorelDocker` 时安全回退 `DebugDockPanelHost`，且不会污染用户本地配置。
- **2026-05-26：** 发布包 manifest 新增 `RuntimeSafety` 安全元数据，验包脚本会校验默认 Dock 模式、CorelDocker 占位状态、单 WebView 要求和真实宿主验收要求。
- **2026-05-26：** 注册流程加固：生产注册默认必须走 `CONFIRMED` manifest，启用项必须具备完整确认信息；直接注册表路径只保留给受控测试开关。
- **2026-05-26：** 修正 HostHarness 进程检测逻辑，只把 `HasExited = false` 的进程视为真实运行，避免已退出残留 PID 干扰构建和压测脚本判断。
- **2026-05-26：** 新增 CorelDRAW 注册确认记录模板，并纳入发布包验证；真实宿主验收模板补齐注册 manifest 与注册清理记录项。
- **2026-05-26：** 新增真实宿主验收记录生成脚本，可自动生成验收记录、注册确认记录和只读注册计划报告，并纳入发布包验证。
- **2026-05-26：** 修复真实宿主验收记录生成脚本的表格预填逻辑，避免 Windows PowerShell 5.1 中文编码问题导致字段为空；已验证新记录会写入环境、版本和注册计划路径。
- **2026-05-26：** 发布包验证脚本新增验收记录生成器冒烟检查，后续验包会实际生成草稿并确认关键字段已预填。
- **2026-05-26：** 增强 CorelDRAW 注册 manifest 生成器，支持生成单目标 `CONFIRMED` manifest，并把生成器冒烟检查纳入发布包验证。
- **2026-05-26：** 增强 CorelDRAW 注册计划报告，新增证据摘要、候选路径评分、版本提示和 manifest 字段检查清单，并把报告格式冒烟检查纳入发布包验证。
- **2026-05-26：** 新增 `ICorelDockerAdapter` 真实 Docker API 适配边界，生命周期已能把 CorelDRAW 宿主对象传入 Dock 工厂；修复 CorelDocker 占位失败后 WebView2 父容器污染问题。
- **2026-05-26：** 稳定性快照和 HostHarness 报告新增 Dock host kind、Docker adapter 类型和 adapter 挂载状态，便于后续真实 Docker 接入验收。
- **2026-05-26：** 新增 Docker adapter factory 和 `CorelDockerAdapter` 官方 API 空实现外壳；默认仍使用占位 adapter，避免误判真实 Docker 已接入。
- **2026-05-26：** 新增 `AllowOfficialCorelDockerAdapter` 配置门禁和发布包 RuntimeSafety 校验，默认禁止启用官方 Docker adapter 外壳。
- **2026-05-26：** 新增 Docker adapter 启用门槛文档，并纳入发布包生成和验包检查，明确真实 Docker 启用必须有宿主验收记录。
- **2026-05-26：** 真实宿主验收模板和生成脚本新增 Docker adapter 启用门槛字段，验包会检查生成记录是否包含这些安全门槛。
- **2026-05-26：** 真实宿主验收记录生成脚本已支持只读本机配置，并允许传入 Docker 快照字段；后续真实 CorelDRAW 验收时可直接写入 `ActiveDockPanelHostKind`、`ActiveDockerAdapterType`、`IsDockerAdapterAttached` 和 `WebViewCreateCount`。
- **2026-05-26：** 新增 `New-QiTuConfirmedCorelRegistrationManifest.ps1`，真实注册路径确认后可一键生成并校验单目标 `CONFIRMED` manifest，减少手工编辑 JSON 风险。
- **2026-05-26：** 新增 `Get-QiTuCorelRegistrationPreview.ps1` 和安装脚本 `-PreviewCorelDrawRegistration` 模式，真实写注册表前可只读预览将要写入的 CorelDRAW 注册路径和值。
- **2026-05-26：** 安装 manifest 新增注册写入明细，卸载脚本输出已清理/缺失注册路径；发布安装冒烟已覆盖受控 HKCU 注册写入和反注册清理。
- **2026-05-26：** 新增 `New-QiTuRealHostExecutionPlan.ps1`，真实 CorelDRAW 验收前可生成小白可照做的执行计划，串联记录草稿、注册确认、manifest、预览、安装、加载、卸载和回填。
- **2026-05-26：** 新增 `REAL_HOST_ACCEPTANCE_QUICKSTART.md`，发布包内提供从 zip 解压到真实 CorelDRAW 加载/卸载回填的最短验收路径。
- **2026-05-26：** 新增真实宿主 readiness 检查入口，发布包验证会冒烟运行 `Test-QiTuRealHostReadiness.ps1`，用于确认进入人工 CorelDRAW 验收前的脚本、模板和生成器状态。
- **2026-05-26：** 新增 `New-QiTuRealHostAcceptanceKit.ps1`，可一键生成真实宿主验收包，集中包含 readiness、执行计划、验收记录草稿、注册确认草稿和索引文件。
- **2026-05-26：** 新增真实宿主命令清单模板和生成器，验收包会自动包含 `CONFIRMED` manifest、注册预览、安装注册和反注册卸载命令。
- **2026-05-27：** 新增真实宿主注册干跑脚本，确认真实注册路径后可先生成 `CONFIRMED` manifest 并执行结构化预览，正式写注册表前多一道自动化防呆。
- **2026-05-27：** 新增真实宿主安装后状态核查脚本，安装注册完成后可检查安装目录、`install-manifest.json` 和注册表项是否一致。
- **2026-05-27：** 新增 CorelDRAW COM 只读烟测脚本，可连接当前运行的 CorelDRAW、读取版本和进程信息并立即释放 COM 引用。
- **2026-05-27：** 针对 CorelDRAW 26 增加 Addons 部署和模块加载核查脚本，已能把 QiTuCDR 加载进 26 进程并确认 Host/Core/Bridge/WebView2 模块已加载。
