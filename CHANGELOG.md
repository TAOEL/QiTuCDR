# 变更记录

## 未发布 - 2026-05-28

### 新增

- 新增 `docs/UI_DESIGN_SYSTEM.md`，记录 QiTuCDR 当前采用的黑 / 白 + 绿色强调设计系统、颜色令牌、字体规范、4px 网格、圆角、控件尺寸和三端一致规则。
- 新增 `docs/NATIVE_WINDOW_UI.md`，记录独立 WPF 工具窗口的 420px 视觉主体宽度、8px 阴影安全边距、32px 标题栏、标题栏四控件和内部布局网格。
- 新增 `docs/UI_IMPLEMENTATION_LOG.md`，记录综合面板、预览器、工具条、独立窗口标题栏、圆角、图标、字体和内部布局的阶段性调整。
- 独立 WPF 工具窗口标题栏新增版本标签和版本信息弹层，版本记录支持窄滚动条浏览。
- 独立 WPF 工具窗口新增设置弹层，承载窗口置顶、位置保存、参数保存等配置项。
- 新增 `NativeToolPopupWindow`，标题栏弹层可拖动、可移出独立工具窗口外，且不影响主窗口收起 / 展开尺寸。
- `NativePanel` 配置新增独立窗口设置项和窗口 / 弹层位置字典，用于本地持久化原生工具窗口行为。

### 调整

- 独立 WPF 工具窗口统一为 436px 总宽，其中 420px 为视觉主体，左右各 8px 用于透明圆角和阴影安全区。
- 独立窗口标题栏控件统一为轻量细线图标，图标尺寸 12px，线宽 1.0px；关闭按钮贴右边，标题栏按钮悬停热区取消圆角。
- 独立窗口内部布局收敛到 4px 基准网格：字段标签列 96px，字段行距 12px，输入框和下拉框高度 32px，描述文本行高 20px。
- 设置入口从系统 `MessageBox` 调整为窗口内部 WPF 弹层，避免视觉跳出当前插件体系。
- 弹层滚动条调整为微信风格窄滚动条，浅色 / 暗黑主题分别使用独立颜色令牌。
- 标题栏弹层从主窗口内部叠加层调整为独立 WPF 浮动弹层窗口。
- 标题栏弹层滚动条调整为贴右边缘，默认隐藏，鼠标进入滚动区域后显示。
- 标题栏弹层顶部拖动热区扩展到整行 36px，并对齐标题文字与关闭按钮。
- 标题栏弹层关闭按钮改为贴右边、危险红 hover，与独立 WPF 主窗口关闭按钮保持一致。
- 设置弹层改为左侧文本、右侧开关的设置行样式。
- 标题栏弹层关闭按钮收敛为小型横向矩形 hover 热区，避免红色背景过大。
- 设置弹层移除描述文字和分隔线，仅保留文本与右侧开关。
- 版本弹层和设置弹层统一内容区左右内边距。
- 标题栏弹层关闭按钮调整为上贴边、右贴边的横向长方形红色 hover 热区，并只保留右上角圆角。
- 修正弹层滚动条覆盖正文的问题，滚动条改为右侧独立槽位显示。
- 弹层关闭图标从 12px 调整为 10px。
- 弹层滚动条调整为更贴近外壳右边缘，并通过正文右侧避让避免覆盖内容。
- 弹层滚动条进一步调整为贴弹层外壳右边缘，正文右侧避让增加到 20px。
- 弹层关闭按钮红色 hover 背景高度增加 2px。
- 标题栏弹层新增 `ESC` 关闭、点击外部关闭和位置记忆。
- 设置弹层开关接入本地配置：`窗口置顶` 即时控制当前窗口 `Topmost`，`保存窗口位置` 控制主窗口和标题栏弹层位置持久化，`保存配置参数` 预留给后续工具参数默认值。
- 安装脚本默认配置补齐 `AllowOfficialCorelDockerAdapter` 和 `NativePanel` 节点，新安装环境会直接生成完整默认配置。

### 验证

- `dotnet build QiTuCDR.sln --no-restore` 通过，0 警告，0 错误。
- `dotnet test QiTuCDR.sln --no-build` 通过，50 个测试全部通过。
- 独立窗口配置持久化单元测试已覆盖 `NativePanel` 默认值、旧配置兼容和窗口 / 弹层位置保存读取。

## 0.1.0 - 2026-05-25

### 新增

- 创建 QiTuCDR .NET Framework 4.8 solution，包含 shared contracts、bridge、infrastructure、core services、host shell 和测试项目。
- 新增 WPF Host Shell，包含单例 WebView2 管理器、延迟预热、状态机生命周期和 WPF 降级面板。
- 新增标准 request、response、event DTO 及 JSON 序列化。
- 新增 bridge dispatcher、event bus、cancellation hub、file logger、plugin state machine 和 COM dispatcher 抽象。
- 新增批量转曲、一键居中、冗余清理、尺寸规整四个工具命令骨架。
- 新增 React/Vite Hash Router UI，包含四个工具页面和 WebView2 bridge client。
- 新增 MSTest 覆盖 DTO 序列化、状态流转和 bridge 分发行为。
- 新增 `ICorelDocumentAdapter`，将 Core 工具服务与 CorelDRAW COM 细节隔离。
- 新增基于 `SelectionSnapshot.ShapeIds` 和 CorelDRAW `StaticID` 的选区回填解析路径。
- 新增 `IDockPanelHost` 和 `DebugDockPanelHost`，将调试面板改为单例生命周期，关闭窗口时只隐藏。
- 新增 `RuntimeEnvironmentChecker`，启动时检测 WebView2 Runtime、CorelDRAW TypeLib、WebUI 产物和宿主对象，并支持 WebView2 缺失降级。
- 新增 `PluginPaths`、`JsonPluginConfigStore` 和配置持久化测试，支持 `%LOCALAPPDATA%\QiTuCDR\Config\settings.json` 默认创建、加载和损坏备份。
- 加固 WebView2 Bridge 输入容错，非法 JSON 会返回 `INVALID_PAYLOAD`，缺失 `payload` 会归一化为空对象，Web 消息入口异常不会冒泡到宿主。
- 新增 `ConvertTextProgressTracker`，批量转曲进度按已处理对象数分批发布，并覆盖全跳过对象和非法批大小回退场景。
- 加固 `DynamicCorelDocumentAdapter` 的 COM 枚举释放路径，`Shapes` 集合和每个临时 `Shape` 都通过集中辅助方法释放。
- 加固 `Invoke-QiTuBuild.ps1`，`dotnet` 或 `npm` 任一步失败会立即中止脚本，避免误报验证完成。
- 扩展 `Test-QiTuEnvironment.ps1`，新增 WebUI 构建产物、本地配置目录、本地日志目录、`settings.json` 合法性检查，并支持 `-FailOnError`。
- 新增 `tools/sdk/New-CorelDrawInterop.ps1`，可从本机 CorelDRAW TypeLib 生成 ignored Interop artifacts，并在诊断中报告 `TlbImp.exe`。
- 新增可选 `TypedCorelDocumentAdapter` 骨架和 `EnableCorelDrawInterop` 构建开关，默认构建仍不依赖 CorelDRAW 私有 Interop。
- 新增 `CorelDocumentAdapterFactory`，生命周期层不再直接创建具体 adapter；typed adapter 创建失败会回退 dynamic adapter。
- 修复 `FallbackPanel.xaml` 中文文本和损坏引号，避免 XAML 重新生成时失败。
- 新增 `PreferTypedCorelInterop` 配置开关，typed adapter 需要编译开关和运行时配置双重确认后才会优先启用。
- 新增 `tools/config/Set-QiTuConfig.ps1`，支持查看、创建和安全切换 typed Interop 运行时配置。
- 新增 `tests/HostHarness/QiTuCDR.HostHarness` 本地 WPF 调试宿主，可在不启动 CorelDRAW 的情况下验证生命周期、WebView2 单例、Bridge 和降级面板。
- 新增 `tools/harness/Start-QiTuHostHarness.ps1`，用于构建并启动本地调试宿主。
- `QiTuCDR.Host` 构建时会复制 `src/WebUI` 到输出目录，保证 HostHarness 和未来插件运行目录都能加载同一份前端静态资源。
- 加固 `BridgeDispatcher` 任务 token 创建时机：只有业务请求成功进入 Busy 后才创建新 token，Busy 重复请求不会刷新或影响当前批量任务。
- 批量转曲在捕获选区快照后会进行 ShapeId 可解析性校验，无法解析时返回 `EMPTY_SELECTION`，不进入 COM 转曲循环。
- dynamic/typed CorelDRAW adapter 的批量转曲增加文档命令组事务壳，执行后始终尝试关闭命令组并释放 Document COM 引用。
- 批量转曲 React 页面新增任务运行态、取消按钮和 `task.progress` 进度条，收到任意工具响应后会清理旧进度。
- 批量转曲 Native 服务新增 `task.completed` / `task.failed` 终态事件，前端会把终态事件转为可读任务结果。
- 尺寸规整 Native 服务新增参数边界校验、选区快照重解析、完成/失败事件和取消处理，允许只统一描边宽度。
- 尺寸规整 dynamic/typed adapter 加入 CorelDRAW 文档命令组事务壳，并释放 Outline COM 临时对象。
- 尺寸规整 React 页面新增运行态、取消按钮和安全数字转换，空输入不会再被误发为 `0`。
- 一键居中 Native 服务新增模式校验、选区快照重解析、完成/失败事件和取消处理。
- 一键居中 dynamic/typed adapter 加入 CorelDRAW 文档命令组事务壳。
- 一键居中 React 页面新增运行态和取消按钮，底部事件区可显示居中完成结果。
- 冗余清理 Native 服务新增强制二次确认、完成/失败事件和取消处理。
- 冗余清理 dynamic/typed adapter 加入 CorelDRAW 文档命令组事务壳，并尽力清理页面辅助线、隐藏空图层和空文本对象；单项清理失败会记日志后继续。
- 冗余清理 React 页面新增运行态和取消按钮，底部事件区可显示清理完成结果。
- 新增 `docs/STABILITY_TEST_PLAN.md`，明确 M8 真实 CorelDRAW 宿主压测矩阵、记录要求和退出标准。
- 新增 `tools/stress/Invoke-QiTuStressBaseline.ps1` 和 `tools/stress/README.md`，可生成构建、诊断、WebUI 产物、进程快照和人工验收清单的基线报告。
- 新增稳定性护栏单测，覆盖重复业务调度后回到 Ready、业务异常后不残留 Busy、取消中心取消后可复用。
- 新增 `PluginStabilitySnapshot`，用于观测当前状态、DockPanel 可见性、WebView2 创建次数、attach 次数和恢复次数。
- HostHarness 新增 `--panel-stress` 模式，支持自动执行面板高频打开/隐藏，并在结束时输出稳定性快照。
- 新增 `tools/stress/Invoke-QiTuPanelStress.ps1`，用于执行本地 WPF Shell 面板高频开关压测。
- 新增 `tools/harness/README.md`，说明 HostHarness 调试窗口和面板压测模式。
- 新增 `tools/stress/Invoke-QiTuMemoryWatch.ps1`，用于 24 小时挂起内存采样，生成 CSV 明细和 Markdown 报告。
- 新增 `docs/REAL_HOST_VALIDATION_TEMPLATE.md`，统一记录真实 CorelDRAW 宿主内的 M6-M8 验收结果。
- 新增 HostHarness `--recovery-stress` 模式和 `tools/stress/Invoke-QiTuRecoveryStress.ps1`，用于模拟 WebView2 browser failure 并验证恢复链路。
- 新增 `PluginLifecycleManager.NotifyDocumentClosing()`、HostHarness `--document-close-stress` 模式和 `tools/stress/Invoke-QiTuDocumentCloseStress.ps1`，用于验证文档关闭取消入口。
- 新增 `installer/Test-QiTuInstallPrerequisites.ps1`，用于安装前检查 Host 构建产物、WebUI 产物和环境诊断状态。
- 新增 `installer/Install-QiTuCDR.ps1`，用于初始化安装目录、复制 Host 输出、创建默认配置并生成 `install-manifest.json`。
- 新增 `installer/Uninstall-QiTuCDR.ps1`，用于卸载 App 文件，并可选择清理配置和日志。
- 新增 `build/scripts/Invoke-QiTuPackage.ps1`，用于生成发布 staging 目录、zip、`package-manifest.json` 和 `SHA256SUMS.txt`。
- 新增 `build/scripts/Test-QiTuPackage.ps1`，用于验证发布目录或 zip 的必需文件、manifest 和 SHA256 校验清单。
- 新增 `build/scripts/Test-QiTuReleaseInstall.ps1`，用于从发布包执行安装、安装结果检查和默认卸载冒烟。
- 新增 `installer/Get-QiTuCorelRegistrationPlan.ps1`，用于只读探测 CorelDRAW TypeLib 和注册表候选项，生成注册计划报告。

### 调整

- 调整 `build/scripts/Invoke-QiTuBuild.ps1` 顺序，先构建 WebUI 再编译 .NET Host，避免 Vite hash 资源变化时 Host 输出目录复制到旧文件。
- 加固 `Invoke-QiTuBuild.ps1`，检测到本地 HostHarness 正在运行时自动跳过 HostHarness 输出复制，只构建 Host 与单测项目，避免调试窗口锁定 DLL 导致整体验证失败。
- WebView2 延迟预热与 Native 事件投递统一通过 WebView Dispatcher 执行，避免后台延迟任务跨线程访问 WPF WebView2 控件。
- `PluginLifecycleManager.Dispose` 改为逐项安全释放，卸载阶段异常只记录日志，不再向宿主冒泡。
- `DebugDockPanelHost.Dispose` 捕获关闭窗口异常，避免调试宿主关闭过程影响生命周期收尾。
- `Invoke-QiTuPanelStress.ps1` 检测到已有 HostHarness 进程时，会使用隔离临时输出目录构建并运行压测，避免默认调试输出目录被锁定。
- WebView2 browser failure 恢复流程会统一取消当前任务，避免 Web 层失联后后台业务继续执行。
- AddIn 增加 `OnDocumentBeforeClose()` 入口，后续真实 CorelDRAW 文档关闭事件可直接转入统一取消路径。
- 安装脚本不猜测 CorelDRAW 私有注册表路径；只有显式传入注册路径时才写入注册项。
- 安装脚本改为自包含默认配置初始化，不再依赖仓库内 `tools/config`，保证发布包可独立安装。
- 发布包验证与发布安装冒烟现在会覆盖注册计划脚本，确保 zip 包包含并能运行该诊断入口。
- 构建、安装前检测和打包脚本新增 `Configuration` 参数，发布包默认读取 `Release` 输出。
- 新增根目录 `VERSION` 文件，打包脚本默认读取该版本号，发布包验证会校验 manifest 与 `VERSION` 一致。
- .NET 程序集版本统一由 `Directory.Build.props` 从 `VERSION` 读取，发布包验证会校验 Host DLL 产品版本。
- 注册计划脚本新增目标版本覆盖报告和下一步确认动作，继续保持只读探测，不猜测或写入 CorelDRAW 注册路径。
- 新增 CorelDRAW 注册 manifest 模板生成与校验脚本。
- 安装/卸载脚本支持从 `CONFIRMED` 注册 manifest 批量注册或反注册多个目标版本路径。
- 卸载脚本删除安装目录时增加重试，降低 Windows 文件系统短暂占用导致目录非空的误失败概率。
- 新增 `IDockPanelHostFactory`，生命周期层不再直接创建 `DebugDockPanelHost`，为真实 CorelDRAW Docker 宿主接入预留稳定替换点。
- 新增 `CorelDockPanelHost` 占位类，`CorelDockPanelHostFactory` 改为创建占位 Host；未接入官方 Docker API 时会明确失败并回退调试宿主。
- 新增 `DockHostMode` 配置和 Dock 工厂选择器；默认使用 `Debug`，配置为 `CorelDocker` 且未实现时会回退调试宿主。
- 新增文档激活、选区变化、宿主退出事件入口，并在稳定性快照中记录对应计数。
- HostHarness 新增宿主事件入口压测模式，`tools/stress` 新增 `Invoke-QiTuHostEventStress.ps1`。
- 修复 stress 脚本传递 `-NoBuild` 开关的 PowerShell 5.1 参数转换问题。
- 新增 `docs/CORELDRAW_HOST_BINDING_CHECKLIST.md`，用于真实 CorelDRAW AddIn、Docker 和宿主事件绑定验收。
- `CorelDockPanelHost` 占位拆出 `CreateDockerContainer`、`AttachWpfPanel`、`ShowDocker`、`HideDocker`、`ReleaseDocker` 5 个固定接入槽位，真实 Docker API 后续只在这些位置补齐。
- 稳定性快照新增 `ConfiguredDockHostMode`、`ActiveDockPanelHostType` 和 `DockHostFallbackCount`，HostHarness 压测报告可直接识别实际 Dock 宿主和失败回退次数。
- 面板压测收尾会等待生命周期回到 `Ready`，避免短次数快速冒烟报告停留在 `Preheating`。
- HostHarness 和面板压测脚本支持 `DockHostMode` 进程级临时覆盖，可验证 `CorelDocker` 未实现时会回退到 `DebugDockPanelHost`，且不会修改用户本地配置。
- 发布包 manifest 新增 `RuntimeSafety` 安全元数据，记录默认 Dock 模式、CorelDocker 占位回退状态、单 WebView 要求和真实 CorelDRAW 验收要求。
- 注册流程加固：安装/卸载脚本默认要求使用 `CONFIRMED` manifest 执行 CorelDRAW 注册写入或清理；直接注册表路径仅允许通过受控测试开关启用。
- 注册 manifest 校验增强：启用项必须包含产品标签、注册类型、安全 Corel 注册表路径、确认来源、确认人和合法确认时间。
- 构建与压测脚本修正 HostHarness 运行检测：只把 `HasExited = false` 的进程视为真实运行，避免 Windows 残留挂起项导致误跳过构建或误走隔离输出。
- 新增 `CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md`，用于记录真实 CorelDRAW 注册路径确认证据，并纳入发布包。
- 真实宿主验收模板补齐注册 manifest 校验、安装注册、CorelDRAW 加载、反注册清理和卸载后验证记录项。
- 新增 `tools/validation/New-QiTuRealHostValidationRecord.ps1`，可生成真实宿主验收记录、注册确认记录和只读注册计划报告草稿。
- 加固真实宿主验收记录生成脚本，避免 Windows PowerShell 5.1 中文脚本编码导致表格字段无法自动填充；生成记录会预填环境、版本和注册计划报告路径。
- 发布包验证脚本新增真实宿主验收记录生成器冒烟检查，会实际生成草稿并确认 CorelDRAW 版本与版本标识已写入。
- 增强 CorelDRAW 注册 manifest 生成器，支持在真实路径已确认后生成单目标 `CONFIRMED` manifest，并强制校验产品标签、注册类型、安全 Corel 注册表路径、确认来源、确认人和确认时间。
- 发布包验证脚本新增注册 manifest 生成器冒烟检查，会实际生成单目标 `CONFIRMED` manifest 并通过 `RequireConfirmed` 校验。
- 增强 CorelDRAW 注册计划报告，新增 `EvidenceSummary`、候选注册表路径评分、版本提示和 manifest 字段检查清单，明确候选路径只可人工复核、不可直接视为确认路径。
- 发布包验证脚本新增注册计划报告冒烟检查，会实际生成 JSON/Markdown 并校验新报告字段。
- 新增 `ICorelDockerAdapter` 和 `PlaceholderCorelDockerAdapter`，将真实 CorelDRAW Docker API 接入点收敛到单一 adapter 边界。
- `IDockPanelHostFactory` 新增 `corelApplication` 参数，生命周期层会把 CorelDRAW 宿主对象传入 Dock 宿主工厂，供后续真实 Docker adapter 使用。
- 调整 `CorelDockPanelHost` 挂载顺序：Docker 容器确认创建/挂载前不附加 WebView2，避免占位失败后污染 WebView2 父容器并影响 Debug 回退。
- 稳定性快照和 HostHarness 报告新增 `ActiveDockPanelHostKind`、`ActiveDockerAdapterType`、`IsDockerAdapterAttached`，用于区分 Debug 面板、占位 Docker adapter 和未来真实 Docker adapter。
- 新增 `ICorelDockerAdapterFactory`、`PlaceholderCorelDockerAdapterFactory` 和 `CorelDockerAdapter` 官方 API 空实现外壳，真实 Docker 代码后续只允许落入 adapter 边界；默认 factory 仍返回占位 adapter。
- 新增 `AllowOfficialCorelDockerAdapter` 配置门禁，默认关闭；只有显式允许时才会尝试官方 Docker adapter 外壳。
- 发布包 `RuntimeSafety` 新增 `OfficialCorelDockerAdapterDefaultEnabled = false`，验包脚本会拒绝默认启用真实 Docker adapter 的交付包。
- 新增 `docs/CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md`，集中定义 `AllowOfficialCorelDockerAdapter` 启用门槛、真实宿主验收条件和发布包门禁。
- 发布包生成和验包脚本纳入 Docker adapter 启用门槛文档，并校验关键启用条件文本。
- 真实宿主验收模板新增 Docker adapter 启用门槛验收区，记录 `AllowOfficialCorelDockerAdapter`、`ActiveDockPanelHostKind`、`ActiveDockerAdapterType`、`IsDockerAdapterAttached` 和 `WebViewCreateCount`。
- 真实宿主验收记录生成脚本会预填默认 Docker adapter 门禁状态，验包脚本会检查生成记录包含 Docker 启用门槛字段。
- 真实宿主验收记录生成脚本支持只读本机 `settings.json`，并可通过命令行参数写入真实宿主 Docker 快照字段；发布包验证新增覆盖参数冒烟检查。
- 新增 `installer/New-QiTuConfirmedCorelRegistrationManifest.ps1`，用于在真实注册路径确认后生成并校验单目标 `CONFIRMED` manifest；发布包验证会执行该 helper 冒烟检查。
- 新增 `installer/Get-QiTuCorelRegistrationPreview.ps1` 和安装脚本 `-PreviewCorelDrawRegistration` 模式，用于真实写注册表前只读预览将要写入的 CorelDRAW 注册路径和值；发布包验证会执行预览冒烟检查。
- 安装脚本会把实际写入的 CorelDRAW 注册路径和值记录到 `install-manifest.json`，卸载脚本会输出已清理/缺失注册路径；发布安装冒烟新增受控 HKCU 注册写入和反注册清理验证。
- 新增 `tools/validation/New-QiTuRealHostExecutionPlan.ps1`，用于生成真实 CorelDRAW 验收执行清单，串联记录草稿、注册确认、manifest、预览、安装、加载、卸载和回填步骤；发布包验证会执行该计划生成器冒烟检查。
- 新增 `docs/REAL_HOST_ACCEPTANCE_QUICKSTART.md`，用于发布包解压后的真实 CorelDRAW 验收快速开始；发布包验证会检查关键执行步骤存在。
- 新增 `tools/validation/Test-QiTuRealHostReadiness.ps1`，用于在不启动 CorelDRAW、不写注册表的前提下检查发布包是否具备真实宿主人工验收条件，并生成 JSON/Markdown readiness 报告。
- 发布包验证新增真实宿主 readiness 冒烟检查，确认执行计划、验收记录草稿和必备文件检查可生成。
- 新增 `tools/validation/New-QiTuRealHostAcceptanceKit.ps1`，可一键生成真实宿主验收包，集中输出 readiness、执行计划、验收记录草稿、注册确认草稿和索引文件。
- 新增 `docs/REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md` 和 `tools/validation/New-QiTuRealHostCommandChecklist.ps1`，用于生成真实 CorelDRAW 测试命令清单，覆盖注册计划、`CONFIRMED` manifest、注册预览、安装注册、验收回填和反注册卸载。
- 新增 `tools/validation/Invoke-QiTuRealHostRegistrationDryRun.ps1`，用于在真实写注册表前生成 `CONFIRMED` manifest，并执行结构化注册预览和安装脚本预览。
- 新增 `tools/validation/Test-QiTuRealHostInstallState.ps1`，用于真实安装注册后核查安装目录、`install-manifest.json` 和 CorelDRAW 注册表项是否一致。
- 新增 `tools/validation/Invoke-QiTuCorelDrawComSmoke.ps1`，用于只读连接当前 CorelDRAW COM 应用、读取版本和进程信息，并立即释放 COM 引用。
- 新增 `tools/validation/Install-QiTuCorelDrawAddon.ps1` 和 `tools/validation/Test-QiTuCorelDrawAddonLoad.ps1`，用于 CorelDRAW 26 Addons 目录部署和进程模块加载核查。

### 验证

- HostHarness 100 次面板开关压测通过：`WebViewCreateCount = 1`，最终状态 `Ready`，退出码 `0`。
- Release 发布包验证通过：`0.1.0` 发布包状态 `OK`，具体包 ID 以 `package-manifest.json` 为准。
- Release 发布安装冒烟通过：从 zip 解包、验包、生成 CorelDRAW 注册计划、安装、检查、默认卸载均为 `OK`。
- Host DLL 版本抽查通过：`ProductVersion = 0.1.0`，`FileVersion = 0.1.0.0`。
- 注册 manifest 校验通过；受控 HKCU 注册/反注册冒烟通过，测试注册键最终已清理。
- DockPanelHost 工厂复用测试通过：多次 `ShowPanel()` 只创建一个宿主实例。
- CorelDRAW Docker 占位工厂测试通过：未实现前会明确抛出不可用错误。
- 配置持久化和 Dock 工厂选择器测试通过：`DockHostMode` 可保存、读取和选择目标工厂。
- Dock 稳定性快照测试通过：可观测配置模式、实际宿主类型和回退计数。
- 生命周期配置注入测试通过：HostHarness 可用临时配置模拟 Dock 宿主模式，不污染 `%LOCALAPPDATA%` 设置。
- 发布包验证会校验 `RuntimeSafety` 安全元数据和 CorelDocker 回退文档入口。
- 注册 manifest 校验可拒绝缺少确认信息、重复目标版本、重复注册路径或非 Corel 注册表路径的启用项。
- HostHarness 残留 PID 验证通过：即使任务管理器显示已挂起残留项，默认输出目录仍可正常构建，脚本不会再误判为锁文件。
- 发布包验证会校验注册确认记录模板存在，并覆盖 manifest 必需字段。
- 发布包验证会校验真实宿主验收记录生成脚本存在，并引用真实验收模板、注册确认模板和注册计划脚本。
- 真实宿主验收记录生成脚本验证通过：已生成带自动预填字段的验收记录、注册确认记录和只读注册计划报告。
- 发布包验证新增的验收记录生成器冒烟检查通过，当前 `0.1.0` zip 包状态仍为 `OK`。
- 注册 manifest 生成器验证通过：`DRAFT` 模板和单目标 `CONFIRMED` manifest 均可生成，`RequireConfirmed` 校验通过。
- 注册计划报告验证通过：可生成带 `EvidenceSummary`、候选路径评分和 manifest 字段检查清单的 JSON/Markdown 报告。
- CorelDocker 占位回退冒烟通过：误切 `DockHostMode = CorelDocker` 时回退 `DebugDockPanelHost`，`WebViewCreateCount = 1`，结果 `PASSED`。
- CorelDocker 回退诊断报告验证通过：回退后 `ActiveDockPanelHostKind = Debug`、`ActiveDockerAdapterType` 为空、`IsDockerAdapterAttached = False`。
- Docker adapter 工厂注入单测通过；`CorelDockerAdapter` 当前会明确失败，避免误判真实 Docker 已完成。
- 官方 Docker adapter 门禁测试通过：默认仍选择 `PlaceholderCorelDockerAdapter`，显式允许后才选择 `CorelDockerAdapter` 外壳。
- Docker adapter 启用门槛文档已纳入发布包验证，缺少关键启用条件时验包会失败。
- 真实宿主验收记录生成验证通过：新记录包含 Docker adapter 启用门槛验收区，并预填 `AllowOfficialCorelDockerAdapter = False`。
- 宿主事件入口测试通过：文档激活、选区变化、宿主退出会更新稳定性快照计数。
- 本地串行压测通过：面板开关、WebView2 恢复、文档关闭、宿主事件入口均生成 `PASSED` 报告。
- CorelDRAW Docker 占位槽位测试通过：显示 WebView、显示降级面板和隐藏路径都会明确暴露尚未实现的 Docker 步骤。
- 真实宿主 readiness 检查已接入发布包验证；缺少文件或生成器失败会阻断验包，WebView2 Runtime/CorelDRAW TypeLib 缺失会在 readiness 报告中标记 `BLOCKED`。
- 真实宿主验收包生成器已接入发布包验证，会冒烟确认索引文件和核心验收草稿可生成。
- 真实宿主命令清单已接入验收包和发布包验证，会检查关键注册/卸载命令文本存在。
- 真实宿主注册干跑已接入发布包验证，会冒烟确认 manifest、JSON 预览和 Markdown 干跑报告可生成。
- 发布安装冒烟已接入安装后状态核查，确认受控安装目录和受控注册表项一致后再执行卸载清理。

### 说明

- CorelDRAW SDK/Interop 绑定和 add-in 注册留到下一阶段集成。
- 当前工具服务可以编译并落实架构边界，但真实生产行为仍需要目标 CorelDRAW 宿主回归验收。
