# QiTuCDR 架构说明

QiTuCDR 按 Native First 的 CorelDRAW 插件方式构建。Web 层是轻量操作面板，原生层负责生命周期、CorelDRAW 自动化、COM 访问、容灾恢复、日志、校验和任务控制。

## 当前实现

```text
CorelDRAW Host
  -> QiTuCdrAddIn
  -> PluginLifecycleManager
     -> PluginStateMachine
     -> IDockPanelHost
     -> RuntimeEnvironmentChecker
     -> JsonPluginConfigStore
     -> WebView2Manager
     -> BridgeDispatcher
     -> EventBus
     -> Core tool services
     -> CorelDocumentAdapterFactory
     -> ICorelDocumentAdapter
     -> ComDispatcher
  -> Single WebView2
  -> React Hash Router UI
```

当前仓库可以编译 .NET Framework 4.8 solution 和 React/Vite UI。CorelDRAW SDK/Interop 注册尚未提交到仓库；在接入目标 CorelDRAW SDK 之前，宿主边界由 `ICorelHostContext` 和 `dynamic` COM 访问表达。

## 项目边界

- `src/Shared`：稳定常量、枚举、错误码和底层共享类型。
- `src/Bridge`：消息 DTO、JSON 序列化、事件和命令契约。
- `src/Infrastructure`：日志、配置、取消任务、状态机和 COM 调度接口。
- `src/Core`：业务命令、校验器、工具服务、选区快照模型、CorelDRAW adapter 契约和 Bridge 分发器。
- `src/Host`：WPF Shell、DockPanelHost、运行时环境检测、WebView2 单例、生命周期管理、CorelDRAW 宿主上下文、dynamic adapter 和具体 COM 调度器。
- `web`：React UI、Bridge 客户端、页面和样式。
- `src/WebUI`：WebView2 加载的前端静态构建产物。

## 请求流

```text
React page
  -> nativeBridge.send(action, payload)
  -> WebView2 web message
  -> PluginLifecycleManager
  -> BridgeJsonSerializer
  -> BridgeDispatcher
  -> PluginStateMachine gate
  -> Tool command
  -> Validator / Service
  -> ICorelDocumentAdapter
  -> ComDispatcher
  -> CorelDRAW COM
  -> ResponseDto + EventDto
  -> React status console
```

业务请求执行前进入 `Busy` 状态，执行完成后回到 `Ready`。`echo`、`getState`、`cancelCurrentTask` 这类非业务命令不要求插件处于 `Ready`。

Web 消息入口执行两级容错：先由 `BridgeJsonSerializer.TryDeserializeRequest` 过滤非法 JSON，并把缺失的 `payload` 归一化为空对象；再由 `PluginLifecycleManager.OnWebMessageReceived` 捕获后续所有异常。坏消息返回标准失败响应，不允许异常冒泡到 CorelDRAW 宿主消息循环。

## 不可破坏的规则

- 不允许创建第二个 WebView2 实例。
- 不允许 React 或 JavaScript 调用 CorelDRAW COM API。
- 不允许用静态字段或长生命周期成员持有 COM 对象。
- 不允许在 `Task.Run` 或任意后台线程中调用 COM。
- 不允许绕过 `PluginStateMachine`、`BridgeDispatcher` 或 `ComDispatcher`。
- 不允许 Web UI 保存 Shape、Document、Layer 或其他宿主对象状态。
- V1 不引入重型 UI 框架、WebGL、Three.js、云 API、账号系统或支付能力。

## WebView 生命周期

`WebView2Manager` 拥有唯一 WebView2 控件。`PluginLifecycleManager` 通过延迟执行 `EnsureCoreWebView2Async` 完成预热。面板关闭应隐藏 UI，而不是销毁 `WebView2Manager` 或 WebView 实例。

如果 WebView2 初始化失败或渲染进程崩溃，生命周期管理器会进入恢复流程，并把 WPF 内容切换到 `FallbackPanel`。

当前 `DebugDockPanelHost` 用单例 WPF `Window` 模拟 CorelDRAW Dock Panel。`ShowPanel` 重复调用不会创建多个窗口；窗口关闭会被拦截并隐藏。Dock 宿主由 `IDockPanelHostFactory` 创建，后续接入真实 CorelDRAW Docker 时，应新增真实工厂和 `IDockPanelHost` 实现，不改动业务生命周期。

`CorelDockPanelHostFactory` 目前会创建 `CorelDockPanelHost` 占位对象。占位对象在显示真实 Docker 时会明确报“未实现”，生命周期层会记录错误并回退 `DebugDockPanelHost`，避免误切 `CorelDocker` 时拖垮当前可运行调试环境。

`CorelDockPanelHost` 已拆出真实 Docker 接入的固定槽位：`CreateDockerContainer()`、`AttachWpfPanel()`、`ShowDocker()`、`HideDocker()`、`ReleaseDocker()`。这些方法现在仍是占位，不会假装真实 CorelDRAW Docker 已接通；后续只允许在这些槽位内补官方 API，不应把 Docker 细节扩散到 `PluginLifecycleManager` 或工具服务层。

稳定性快照会记录 `ConfiguredDockHostMode`、`ActiveDockPanelHostType` 和 `DockHostFallbackCount`。这样压测报告能直接说明“配置想用什么宿主”“实际用了什么宿主”“是否发生过 Docker 失败回退”，避免把调试窗口误判为真实 CorelDRAW Docker。

## 运行时检测

`RuntimeEnvironmentChecker` 在启动时检查：

- CorelDRAW host object 是否已注入。
- WebView2 Runtime 是否存在。
- CorelDRAW TypeLib 是否可发现。
- `src/WebUI/index.html` 是否可加载。

WebView2 Runtime 缺失被视为 fatal Web UI 故障，生命周期会进入 `Recovering` 并切换到 WPF 降级面板。CorelDRAW host object 缺失是 warning，方便本地调试壳运行，但真实宿主功能会返回标准错误。

## 宿主事件入口

`QiTuCdrAddIn` 只负责把 CorelDRAW 宿主事件转交给 `PluginLifecycleManager`，不在入口中直接执行业务或 COM 操作。

当前已预留入口：

- `OnDocumentBeforeClose()`：转入 `NotifyDocumentClosing()`，取消当前任务并发布 `host.documentChanged`，`reason = closing`。
- `OnDocumentActivated()`：转入 `NotifyDocumentActivated()`，取消当前任务并发布 `host.documentChanged`，`reason = activated`，避免任务跨文档继续执行。
- `OnSelectionChanged()`：转入 `NotifySelectionChanged()`，只发布 `host.selectionChanged`，不读取实时选区、不触发 COM 遍历。
- `OnApplicationQuit()` / `OnDisconnection()`：转入 `NotifyHostShuttingDown()`，取消所有任务，再执行生命周期释放。

这些入口是给真实 CorelDRAW 事件绑定使用的稳定边界。后续接 SDK 时，只绑定事件，不把业务逻辑写进 AddIn 入口。

## 本地配置

本地配置由 `JsonPluginConfigStore` 管理，默认文件为 `%LOCALAPPDATA%\QiTuCDR\Config\settings.json`。插件启动时先加载配置，再创建 Core 服务和 WebView2 预热任务。

当前配置项包括：

- `WebViewPreheatDelayMs`：WebView2 延迟预热时间，默认 `4000`。
- `BatchSize`：批处理分组大小，默认 `50`。
- `TaskTimeoutMs`：单次任务超时时间，默认 `120000`。
- `PreferTypedCorelInterop`：是否优先尝试 typed CorelDRAW Interop adapter，默认 `false`。
- `DockHostMode`：Dock 宿主模式，默认 `Debug`；可选值为 `Debug`、`CorelDocker`。`CorelDocker` 当前只用于真实 Docker 接入占位，未确认官方 API 前会回退到调试宿主。

配置文件不存在时会自动写入默认配置。配置 JSON 损坏时会备份为 `settings.json.bad.<timestamp>`，然后回退并重写默认配置；异常只写入日志，不允许抛出到 CorelDRAW 宿主。

日志目录由同一套路径规范管理，默认写入 `%LOCALAPPDATA%\QiTuCDR\Logs`。

## COM 安全

`ICorelDocumentAdapter` 是 Core 工具服务访问 CorelDRAW 文档能力的边界。Core 工具服务不直接使用 `dynamic`、`ActiveSelectionRange` 或其他 COM API。

`IComDispatcher` 是 adapter 内部执行 COM 调用的调度抽象。`Host.COM.ComDispatcher` 将它绑定到 WPF Dispatcher，确保 CorelDRAW COM 调用停留在宿主 STA/UI 线程。

COM 处理必须遵循：

```csharp
try
{
    // 获取后快速执行。
}
finally
{
    Marshal.ReleaseComObject(obj);
    obj = null;
}
```

当前 `DynamicCorelDocumentAdapter` 将临时 `dynamic` 调用集中在 Host 层。下一步接入 SDK 时，应在同样边界内把它替换为类型化的 CorelDRAW Interop adapter。

typed Interop 骨架位于 `TypedCorelDocumentAdapter`，默认不参与主构建。开启 `EnableCorelDrawInterop=true` 且提供本地 `VGCore.dll` 后才会编译，用于逐步替换 dynamic adapter。

`PluginLifecycleManager` 不直接 new 具体 adapter，而是通过 `CorelDocumentAdapterFactory` 创建。默认构建只会创建 `DynamicCorelDocumentAdapter`；开启 typed Interop 编译并设置 `PreferTypedCorelInterop = true` 时，工厂才会优先尝试 `TypedCorelDocumentAdapter`。创建失败会记录日志并回退 dynamic adapter，避免 typed 接入问题阻断插件启动。

## 批量转曲进度

批量转曲通过 `ConvertTextProgressTracker` 按“已处理对象数”发布进度，而不是按成功转曲数量发布。`converted + skipped` 达到批大小时发布一次，最终对象处理完成时必须发布最终进度。

这样可以覆盖大量锁定对象、隐藏对象、异常跳过对象的场景，避免 `converted = 0` 时重复刷进度，也避免全跳过任务没有最终反馈。配置中的 `BatchSize` 非法时回退到默认 `50`。

## 状态机

```text
Starting -> Preheating -> Ready -> Busy -> Ready -> Disposing -> Disposed
                     \-> Faulted -> Recovering -> Ready
```

非法流转会立即抛出异常。业务命令只有在插件处于 `Ready` 时才允许执行，否则返回 `STATE_FORBIDDEN`。

## 前端纪律

React UI 使用 Hash Router，因为 WebView2 加载的是本地静态文件。前端可以收集参数、显示状态、发送 DTO、渲染响应，但不能执行 CorelDRAW 业务逻辑，不能缓存宿主状态，也不能推断文档模型细节。
