# 贡献指南

当前项目处于 V1 工程骨架阶段。任何贡献都应优先保护 Native First、COM Safe 的架构，再考虑功能深度。

## 环境要求

- Windows 10 或 Windows 11 x64。
- .NET SDK 8 或更高版本，并安装 .NET Framework 4.8 targeting pack。
- Node.js 和 npm。
- 如需真实宿主集成，需要 CorelDRAW 2021-2026。

## 首次启动

```powershell
dotnet restore QiTuCDR.sln
dotnet build QiTuCDR.sln
dotnet test QiTuCDR.sln
```

构建 Web UI：

```powershell
cd web
npm install
npm run build
```

Web 构建产物会写入 `src/WebUI`，这是 WPF WebView2 Host 加载的静态目录。

## 开发流程

1. 先维护原生契约：在接 UI 前，先补 DTO、校验器、命令和服务。
2. 业务执行必须位于 `BridgeDispatcher` 和 `PluginStateMachine` 之后。
3. 所有 CorelDRAW 调用必须位于 `IComDispatcher` 之后。
4. 在 CorelDRAW 内测试前，先构建 C# 和 Web 静态资源。
5. 为 DTO、状态流转、命令路由、校验器和取消行为补测试。

## 新增工具功能

统一使用这个链路：

```text
React page -> RequestDto -> IBridgeCommand -> Validator -> Service -> IComDispatcher -> EventBus
```

原生层最少需要：

- 在 `src/Shared/Actions.cs` 添加 action 常量。
- 在 `src/Core/Validators` 添加或复用 payload 校验。
- 新增一个 `IBridgeCommand` 实现。
- 新增一个实现 `IToolService` 的服务。
- 在 `CoreServiceFactory` 注册命令。
- 为路由和校验添加单元测试。

Web 层最少需要：

- 新增一个 Hash Router 页面。
- 只发送 JSON 安全的参数。
- 渲染 `ResponseDto` 和相关 `EventDto` 反馈。
- 严禁在 React 中保存 CorelDRAW 对象状态。

## CorelDRAW SDK 接入

CorelDRAW Interop 程序集不存入本仓库。接入真实 SDK 时：

- SDK 引用只允许出现在 Host/Core 边界内。
- 优先使用类型化 wrapper，不要把 `dynamic` 调用散落到各处。
- 不要让 COM 对象类型泄漏到 `web`、`Bridge` 或通用 DTO。
- COM 对象必须在 `finally` 中释放。

## 验证清单

交付前运行：

```powershell
dotnet build QiTuCDR.sln --no-restore
dotnet test QiTuCDR.sln --no-build
cd web
npm run build
```

如果修改了 UI，可打开 `src/WebUI/index.html`，或在 `web` 目录运行 `npm run dev`。

## 代码风格

- 注释要短，只解释不明显的生命周期或 COM 行为。
- 除非架构确实需要，不新增依赖。
- 优先使用小服务和显式命令路由，避免反射或自动发现。
- V1 保持纯本地离线能力。
