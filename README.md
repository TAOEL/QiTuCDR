# QiTuCDR

## UI 设计文档入口

当前 UI 视觉规范已经独立成文档，后续改工具条、综合面板、预览器或独立 WPF 小窗口时，先看这里：

- [UI 设计系统](docs/UI_DESIGN_SYSTEM.md)：黑 / 白 + 绿色强调、颜色令牌、字体、圆角、4px 网格、三端一致规则。
- [独立 WPF 工具窗口 UI 规范](docs/NATIVE_WINDOW_UI.md)：420px 主体宽度、8px 阴影安全边距、32px 标题栏、四个标题栏控件和内部布局尺寸。
- [UI 实施记录](docs/UI_IMPLEMENTATION_LOG.md)：记录每一轮已经落地的 UI 调整，避免只靠聊天记录追溯。

QiTuCDR 是一个面向 CorelDRAW 的 Native First 生产力插件工程骨架。当前仓库已经完成 V1.0 的可运行垂直切片：.NET Framework 4.8 原生宿主层、WPF Shell、单例 WebView2 管理、React Hash Router UI、Bridge DTO、状态机、日志、降级面板和基础测试。

项目优先级固定如下：

```text
CorelDRAW 宿主稳定性 > COM 安全 > 生命周期可控 > 资源释放 > 性能 > UI 打磨
```

## 当前已有能力

- .NET Framework 4.8 solution，包含 `Shared`、`Bridge`、`Infrastructure`、`Core`、`Host` 和测试项目。
- WPF Host Shell，支持延迟预热 WebView2 和原生降级面板。
- 标准 `RequestDto`、`ResponseDto`、`EventDto` JSON 通信协议。
- `PluginStateMachine`、`BridgeDispatcher`、`EventBus`、`TaskCancellationHub`、文件日志和 COM 调度抽象。
- 批量转曲、一键居中、冗余清理、尺寸规整四个工具链路，包含 Native 服务、取消、终态事件和前端运行态。
- React/Vite UI，使用 Hash Router 提供四个工具页面。
- 本地 HostHarness，可在不启动 CorelDRAW 的情况下验证生命周期、单例 WebView2、Bridge 和降级面板。
- MSTest 覆盖 DTO 序列化、状态机流转和 Bridge 分发。

CorelDRAW SDK/Interop 尚未完成真实绑定。当前宿主边界使用 `ICorelHostContext` 和 `dynamic`，这样可以在不提交 CorelDRAW 私有 SDK 程序集的情况下保持工程可编译。

## 仓库结构

```text
CDRWFP/
  QiTuCDR.sln
  Directory.Build.props
  VERSION
  PRD.md
  README.md
  CHANGELOG.md

  docs/             架构、开发、协议、安全和发布文档
  src/              C# 原生插件源码和 WebUI 构建产物
  web/              React + TypeScript + Vite 前端源码
  tests/            Unit / Integration / Stress / HostHarness 测试分层
  build/            构建脚本和 MSBuild props 预留目录
  installer/        安装包与注册脚本预留目录
  tools/            诊断和辅助工具预留目录
```

## 构建与测试

```powershell
dotnet restore QiTuCDR.sln
dotnet build QiTuCDR.sln
dotnet test QiTuCDR.sln
```

构建 WebView UI：

```powershell
cd web
npm install
npm run build
```

Vite 会把静态资源输出到 `src/WebUI`。

运行本地调试宿主：

```powershell
powershell -ExecutionPolicy Bypass -File tools/harness/Start-QiTuHostHarness.ps1
```

HostHarness 不依赖 CorelDRAW。它会以 `corelApplication: null` 启动生命周期，所以真实文档功能会返回标准错误，但可以验证 WebView2 初始化、关闭隐藏策略、Bridge 消息和 WPF 降级路径。

## 预览 UI

打开：

```text
src/WebUI/index.html
```

在普通浏览器预览时，原生桥接不可用，所以命令会返回 `WEBVIEW_NOT_READY`。在 WebView2 内运行时，前端会通过 `window.chrome.webview` 发送消息。

## 文档入口

- [产品需求文档](PRD.md)：V1.0 产品范围、架构原则、功能需求、测试策略和红线。
- [AI 协作规则](AGENTS.md)：代理修改仓库前必须遵守的项目规则、红线和验证命令。
- [架构说明](docs/ARCHITECTURE.md)：系统边界、请求流、状态机、COM 规则和 WebView 生命周期。
- [开发说明](docs/DEVELOPMENT.md)：目录管理、开发顺序和新增模块约定。
- [V1.0 里程碑排期](docs/MILESTONES.md)：M1-M9 阶段状态、交付内容和剩余验收。
- [M8 稳定性与压测计划](docs/STABILITY_TEST_PLAN.md)：真实宿主压测矩阵、记录要求和退出标准。
- [真实宿主绑定清单](docs/CORELDRAW_HOST_BINDING_CHECKLIST.md)：CorelDRAW AddIn、Docker、文档事件和验收入口对应关系。
- [贡献指南](docs/CONTRIBUTING.md)：本地环境、开发流程、新增工具规范和验证清单。
- [COM 安全规范](docs/COM_SAFETY.md)：COM 调用红线、释放规则和线程约束。
- [CorelDRAW SDK 接入说明](docs/CORELDRAW_SDK_INTEGRATION.md)：TypeLib 探测、Interop 接入路线和待确认事项。
- [通信协议](docs/MESSAGE_PROTOCOL.md)：Request、Response、Event DTO 规范。
- [发布检查清单](docs/RELEASE_CHECKLIST.md)：发布前构建、测试、运行时和安装检查。
- [真实宿主验收快速开始](docs/REAL_HOST_ACCEPTANCE_QUICKSTART.md)：拿到发布包后在真实 CorelDRAW 测试机上的最短执行路径。
- [TODO 列表](docs/TODOS.md)：进入生产级 V1 前必须补齐的缺口。
- [变更记录](CHANGELOG.md)：已交付的工程骨架内容。

常用工具：

- [环境诊断](tools/diagnostics/README.md)：检查 WebView2、CorelDRAW TypeLib、WebUI 产物、配置/日志目录。
- [SDK 工具](tools/sdk/README.md)：从本机 TypeLib 生成 ignored Interop artifacts。
- [配置工具](tools/config/README.md)：查看和修改本地 `settings.json`。
- [本地 HostHarness](tools/harness/README.md)：启动独立 WPF 调试宿主，不需要 CorelDRAW。
- [M8 压测工具](tools/stress/README.md)：生成自动化基线报告和真实宿主验收清单。
- [真实宿主验收工具](tools/validation/README.md)：集中生成 readiness 报告、CorelDRAW COM 烟测、CorelDRAW 26 Addons 部署/加载核查、执行计划、命令清单、注册干跑报告、安装后核查、真实 CorelDRAW 验收记录草稿和验收包索引。

## 架构红线

- 原生 C# 负责 CorelDRAW 自动化、COM 调用、生命周期、日志、校验和恢复。
- React 只负责 UI 渲染、参数收集和 Bridge 消息发送。
- 插件生命周期内只允许一个 WebView2 实例。
- 所有 CorelDRAW COM 调用必须经过 `IComDispatcher`。
- 所有业务命令必须经过 `BridgeDispatcher` 和 `PluginStateMachine`。
- Web UI 严禁持有 Shape、Document、Layer 或其他宿主对象状态。

## 下一里程碑

在保持现有边界不变的前提下，接入真实 CorelDRAW Docker / AddIn 注册，并按 M8 稳定性矩阵完成真实宿主压测。当前最高优先级技术缺口是：真实 CorelDRAW 宿主中的大文档、WebView2 恢复、文档关闭取消和 24 小时内存验收。
