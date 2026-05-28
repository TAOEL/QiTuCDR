# AGENTS.md

本文档是 QiTuCDR 项目的 AI 协作入口。任何代理在修改本仓库前，都必须先阅读并遵守本文档。

## 项目定位

QiTuCDR 是 CorelDRAW 原生增强插件。项目不是 Web 插件，而是以 C# Native Runtime 为核心、WebView2 作为轻量 UI 容器的桌面生产力工具。

产品范围和验收基准以 `PRD.md` 为准；阶段排期以 `docs/MILESTONES.md` 为准。

最高优先级：

```text
CorelDRAW 宿主稳定性 > COM 安全 > 生命周期可控 > 资源释放 > 性能 > UI 体验
```

## 回复语言

- 默认使用中文回复用户。
- 文档默认使用中文。
- 文件名、类名、命令、API、DTO、日志字段保持英文原样。

## 常用命令

完整验证：

```powershell
powershell -ExecutionPolicy Bypass -File build\scripts\Invoke-QiTuBuild.ps1
```

环境诊断：

```powershell
powershell -ExecutionPolicy Bypass -File tools\diagnostics\Test-QiTuEnvironment.ps1
```

手动验证：

```powershell
dotnet restore QiTuCDR.sln
dotnet build QiTuCDR.sln
dotnet test QiTuCDR.sln
cd web
npm run build
```

## 目录职责

```text
src/Shared          常量、枚举、错误码
src/Bridge          DTO、JSON 序列化、事件、命令契约
src/Infrastructure  日志、配置、状态机、取消任务、基础接口
src/Core            业务命令、校验器、工具服务、adapter 契约、选区快照、BridgeDispatcher
src/Host            WPF Shell、DockPanelHost、WebView2、生命周期、宿主接入、dynamic adapter、具体 COM 调度器
src/WebUI           前端构建产物
web                 React + TypeScript + Vite 前端源码
tests/Unit          单元测试
tests/Integration   未来集成测试
tests/Stress        未来压力测试
tests/HostHarness   未来 CorelDRAW 宿主 harness
docs                工程文档
build               构建脚本
installer           安装包和注册逻辑
tools               诊断和辅助工具
```

## 架构红线

- 禁止新增第二个 WebView2 实例。
- 禁止 `ShowPanel` 重复创建多个面板窗口或 WebView 容器。
- 禁止 WebView2 Runtime 缺失时让插件整体不可用；必须走 WPF 降级路径。
- 禁止 React 或 JavaScript 直接操作 CorelDRAW COM。
- 禁止 Web 层保存 Shape、Document、Layer 或其他宿主对象状态。
- 禁止长期持有 COM 对象引用。
- 禁止把 COM 对象放入静态字段或长生命周期字段。
- 禁止 `Task.Run` 直接调用 CorelDRAW COM。
- 禁止绕过 `PluginStateMachine`、`BridgeDispatcher`、`IComDispatcher`。
- 禁止省略 COM 释放逻辑。
- 禁止 Core 工具服务直接使用 `dynamic`、`ActiveSelectionRange`、`ActiveDocument`、`ActivePage`。
- 禁止引入云服务、账号系统、支付系统、外网 AI API。
- 禁止引入重型 UI 框架、Three.js、WebGL 或复杂动画系统。

## 新增功能流程

新增工具功能必须按以下链路落地：

```text
React page
  -> RequestDto
  -> IBridgeCommand
  -> Validator
  -> Service
  -> ICorelDocumentAdapter
  -> IComDispatcher
  -> EventBus
  -> ResponseDto / EventDto
```

最低要求：

- 在 `src/Shared/Actions.cs` 添加 action 常量。
- 在 `src/Core/Validators` 添加参数校验。
- 新增 `IBridgeCommand` 实现。
- 新增 `IToolService` 服务。
- 如需访问 CorelDRAW 文档能力，先扩展 `ICorelDocumentAdapter`。
- 在 `CoreServiceFactory` 注册命令。
- 前端只发送 JSON 参数，不保存宿主对象状态。
- 添加或更新单元测试。

## COM 规则

所有 CorelDRAW COM 调用必须遵循：

```text
Acquire Immediately -> Execute Quickly -> Release Immediately
```

标准释放形式：

```csharp
dynamic? obj = null;

try
{
    obj = app.ActiveDocument;
    // 快速执行 COM 操作。
}
finally
{
    if (obj != null && Marshal.IsComObject(obj))
    {
        Marshal.ReleaseComObject(obj);
    }
}
```

选区相关功能必须先生成 `SelectionSnapshot`，后续应基于 `ShapeIds` 重新解析对象，不能长期依赖实时 `ActiveSelectionRange`。

## CorelDRAW SDK 状态

当前本机已检测到：

```text
C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64\TypeLibs\CorelDRAW.tlb
C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Programs64\TypeLibs\CorelDRAW.tlb
```

不要把 CorelDRAW 私有 SDK 或生成的大型 Interop 文件随意提交到业务目录。接入路线见：

```text
docs/CORELDRAW_SDK_INTEGRATION.md
```

## 文档规则

- 产品需求主文档写入 `PRD.md`。
- 工程说明写入 `docs/`。
- 用户入口和快速说明写入 `README.md`。
- 变更写入 `CHANGELOG.md`。
- 待办写入 `docs/TODOS.md`。
- 新增脚本时同步更新对应目录下的 `README.md`。

## 验证规则

修改 C# 后至少运行：

```powershell
dotnet build QiTuCDR.sln --no-restore
dotnet test QiTuCDR.sln --no-build
```

修改前端后至少运行：

```powershell
cd web
npm run build
```

修改构建脚本、目录结构或多层代码后运行完整验证脚本。

## 当前重点

1. 接入类型化 CorelDRAW SDK/Interop adapter。
2. 基于 `SelectionSnapshot.ShapeIds` 重新解析 Shape。
3. 替换当前调试窗口为真实 CorelDRAW Dock Panel 注册。
4. 添加安装包、插件注册和卸载清理。
5. 扩展压力测试和恢复测试。
