# 开发说明

本文档用于管理工程目录和开发顺序，避免后续功能越写越散。

## 目录职责

```text
src/Shared          只放稳定共享常量、枚举、错误码
src/Bridge          只放通信 DTO、序列化、事件、命令契约
src/Infrastructure  只放日志、配置、状态机、取消任务、基础接口
src/Core            放业务命令、校验、服务、选区快照、adapter 契约和调度
src/Host            放 WPF、DockPanelHost、WebView2、生命周期、宿主接入、dynamic adapter 和具体 COM 调度
src/WebUI           只放前端构建产物，不手写业务源码
web                 React 源码
tests/Unit          快速单元测试
tests/Integration   未来 Web 与 Native 集成测试
tests/Stress        未来压力测试
tests/HostHarness   不依赖 CorelDRAW 的本地宿主调试 harness
build               构建脚本和公共构建属性
installer           安装包、注册脚本、运行时检测
tools               诊断脚本和开发辅助工具
```

## 常用脚本

环境诊断：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1
```

输出 JSON 或作为失败门禁：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -Json
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -FailOnError
```

一键构建验证：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1
```

该脚本会检查 `dotnet` 和 `npm` 的退出码；任一步失败都会立即停止，不能把失败构建误报为完成。

生成本地 CorelDRAW Interop：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -WhatIf
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -Force
```

生成物输出到 `artifacts/coreldraw-interop/`，只用于本机开发验证，不进入版本管理。

开启 typed Interop 编译验证：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 -SkipWeb -EnableCorelDrawInterop -CorelDrawInteropDirectory "C:\Users\Administrator\Desktop\DEo\codex\CDRWFP\artifacts\coreldraw-interop\v27"
```

注意：该模式只验证 Host typed adapter 能和本地 Interop DLL 编译通过。默认构建不能依赖它，否则没有 CorelDRAW SDK 的机器会失去可编译性。
默认构建和 typed Interop 构建不要并行执行；WPF XAML 标记编译会共享 `obj` 中间目录，可能互相干扰。
即使编译开启 typed Interop，运行时仍需要在 `settings.json` 中设置 `preferTypedCorelInterop: true` 才会优先尝试 typed adapter。

Host 文档 adapter 必须通过 `CorelDocumentAdapterFactory` 创建。不要在生命周期或业务服务里直接选择 `DynamicCorelDocumentAdapter` / `TypedCorelDocumentAdapter`，避免真实 SDK 接入后选择逻辑扩散。

配置 typed Interop 运行时开关：

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -EnableTypedInterop
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -DisableTypedInterop
```

该工具会创建默认配置，并在 JSON 损坏时先备份再回退默认配置。

只验证 C#：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 -SkipWeb
```

运行本地 HostHarness：

```powershell
powershell -ExecutionPolicy Bypass -File tools/harness/Start-QiTuHostHarness.ps1
```

跳过构建直接运行：

```powershell
powershell -ExecutionPolicy Bypass -File tools/harness/Start-QiTuHostHarness.ps1 -NoBuild
```

HostHarness 会创建一个独立 WPF 控制窗，并通过 `PluginLifecycleManager.Start(null)` 启动插件生命周期。它只用于验证 WebView2、Bridge、状态机和降级壳；真实 CorelDRAW 文档操作仍必须在宿主内验证。

## 本地运行数据

本地日志和配置统一放在 `%LOCALAPPDATA%\QiTuCDR` 下：

```text
%LOCALAPPDATA%\QiTuCDR\Config\settings.json
%LOCALAPPDATA%\QiTuCDR\Logs\yyyyMMdd.log
```

`PluginPaths` 是路径唯一来源。新增本地文件目录时优先扩展 `PluginPaths`，不要在业务代码中散落 `Environment.SpecialFolder.LocalApplicationData` 拼接逻辑。

`JsonPluginConfigStore` 负责配置加载、保存、默认值初始化和损坏文件备份。任何配置读写失败都应降级，不得把异常抛到 CorelDRAW 宿主。

## 开发顺序

阶段排期以 [V1.0 里程碑排期](MILESTONES.md) 为准。日常推进时，先更新对应阶段状态，再补代码、测试和变更记录。

新增能力时按这个顺序推进：

```text
DTO / Action
  -> Validator
  -> IBridgeCommand
  -> Service
  -> ICorelDocumentAdapter
  -> IComDispatcher 调用
  -> EventBus 事件
  -> React 页面
  -> 测试
```

不要先写 UI 再反推 Native 行为。QiTuCDR 的业务事实必须以原生层为准。

Core 工具服务不能直接使用 `dynamic` 或 CorelDRAW COM API。需要宿主能力时，先扩展 `ICorelDocumentAdapter`，再由 Host 层实现。

面板宿主能力通过 `IDockPanelHostFactory` 和 `IDockPanelHost` 隔离。当前 `DebugDockPanelHostFactory` 只创建本地调试窗口；真实 CorelDRAW Docker 接入时新增真实工厂实现，保持 `PluginLifecycleManager.ShowPanel()` 不承载具体宿主细节。

`CorelDockPanelHostFactory` 目前只创建 `CorelDockPanelHost` 占位对象。占位对象不可显示真实 Docker，生命周期层会在失败后回退调试宿主。不要在没有确认官方 CorelDRAW Docker API 和注册机制前把它当作完成态。

业务任务的取消 token 由 `BridgeDispatcher` 在成功进入 Busy 后创建。不要在 Web 消息入口或非业务命令中重置任务 token，否则 `cancelCurrentTask` 和 Busy 重复请求会误伤正在执行的批量任务。

涉及选区的业务必须在服务层捕获 `SelectionSnapshot`，并在执行前通过 adapter 重新解析校验。不能在执行阶段继续依赖实时 `ActiveSelection`。

批量任务的进度判断应放在可测试的 Core 小类中，例如 `ConvertTextProgressTracker`。不要在 `dynamic` COM 循环里散落批处理判断；Host adapter 只负责获取对象、执行 COM、释放对象和调用进度追踪器。

枚举 COM 集合时不要直接写 `foreach (dynamic shape in range.Shapes)`。应通过 Host adapter 内部统一枚举辅助方法，确保 `Shapes` 集合和每一个临时 `Shape` 对象都在 `finally` 中释放。

## 命名约定

- C# 项目命名使用 `QiTuCDR.<Layer>`。
- 工具功能目录使用业务名，例如 `ConvertText`、`Center`、`Cleanup`、`Normalize`。
- 前端页面使用 `XxxPage.tsx`。
- 命令类使用 `XxxCommand`。
- 服务类使用 `XxxService`。

## 构建产物

- `bin/`、`obj/`、`node_modules/` 不进入版本管理。
- `src/WebUI/` 当前保留为可加载静态资源，来源是 `npm run build`。
- `QiTuCDR.Host` 构建时会把 `src/WebUI` 复制到输出目录，保证 CorelDRAW 插件和 HostHarness 都从运行目录加载同一份静态资源。
- 后续安装包如果调整 WebUI 复制位置，应同步修改 Host 项目文件和本说明。
