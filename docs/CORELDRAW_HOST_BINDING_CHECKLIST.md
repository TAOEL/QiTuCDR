# CorelDRAW 真实宿主绑定清单

本文档用于真实接入 CorelDRAW 2021-2026 时逐项勾选，不用于替代官方 SDK 文档。  
原则：先绑定宿主事件入口，再运行真实宿主验收；不要把业务逻辑直接写进 AddIn 事件。

## 当前状态

| 项目 | 状态 |
|------|------|
| 本地 WPF HostHarness | 已可运行 |
| 单 WebView2 生命周期 | 已实现 |
| React 工具页 | 已实现 |
| Bridge 请求/响应/事件 | 已实现 |
| 文档关闭取消入口 | 已实现 |
| 文档激活取消入口 | 已实现 |
| 选区变化事件入口 | 已实现 |
| 宿主退出取消入口 | 已实现 |
| 真实 CorelDRAW Docker 面板 | 待接入 |
| 真实 AddIn 注册机制 | 待确认 |

## 绑定总原则

- AddIn 入口只做事件转发，不直接操作 Shape、Document、Layer。
- 所有业务功能仍从 `BridgeDispatcher` 进入。
- 所有 COM 操作仍通过 `IComDispatcher`。
- 文档切换、文档关闭、宿主退出必须取消任务。
- 选区变化只发布事件，不读取实时 `ActiveSelection`。
- 面板创建必须保持单例，不允许创建第二个 WebView2。

## AddIn 生命周期入口

| CorelDRAW 宿主时机 | 当前 Native 入口 | 当前动作 | 验收方式 |
|--------------------|------------------|----------|----------|
| 插件加载 / 连接宿主 | `QiTuCdrAddIn.OnConnection(object corelApplication)` | 调用 `PluginLifecycleManager.Start(corelApplication)` | 插件状态进入 `Preheating -> Ready`，日志有运行时检测 |
| 打开工具面板 | `QiTuCdrAddIn.ShowPanel()` | 调用 `PluginLifecycleManager.ShowPanel()` | 面板显示；重复打开不新增第二个 WebView2 |
| 面板关闭 | `IDockPanelHost.Hide()` | 只隐藏，不销毁 WebView2 | 再次打开速度稳定，`WebViewCreateCount <= 1` |
| 插件卸载 / 断开宿主 | `QiTuCdrAddIn.OnDisconnection()` | 先取消全部任务，再释放资源 | 状态进入 `Disposing -> Disposed`，异常只写日志 |

## CorelDRAW 文档事件入口

| CorelDRAW 宿主事件 | 当前 Native 入口 | 当前动作 | 不能做的事 |
|--------------------|------------------|----------|------------|
| 文档关闭前 | `QiTuCdrAddIn.OnDocumentBeforeClose()` | `NotifyDocumentClosing()`，取消当前任务，发布 `host.documentChanged` | 不遍历文档，不读取 Shape |
| 文档激活 / 文档切换 | `QiTuCdrAddIn.OnDocumentActivated()` | `NotifyDocumentActivated()`，取消当前任务，发布 `host.documentChanged` | 不复用旧文档 COM 引用 |
| 选区变化 | `QiTuCdrAddIn.OnSelectionChanged()` | `NotifySelectionChanged()`，发布 `host.selectionChanged` | 不读取实时 `ActiveSelection` |
| CorelDRAW 退出 | `QiTuCdrAddIn.OnApplicationQuit()` | `NotifyHostShuttingDown()`，取消全部任务 | 不启动新任务，不打开 UI |

## Docker 面板接入点

当前默认配置：

```json
{
  "DockHostMode": "Debug",
  "AllowOfficialCorelDockerAdapter": false
}
```

真实 Docker 接入后才允许切换：

```json
{
  "DockHostMode": "CorelDocker",
  "AllowOfficialCorelDockerAdapter": true
}
```

当前占位：

- `DebugDockPanelHostFactory`：本地调试窗口，默认使用。
- `CorelDockPanelHostFactory`：创建真实 Docker 占位 Host。
- `CorelDockPanelHost`：真实 Docker 占位对象，未实现前会明确失败并由生命周期回退调试宿主。
- `ICorelDockerAdapter`：真实 CorelDRAW Docker API 的唯一适配边界，后续官方 API 只允许收敛到此接口实现中。
- `ICorelDockerAdapterFactory`：Docker adapter 创建边界，默认返回占位 adapter。
- `PlaceholderCorelDockerAdapter`：当前占位实现，所有 Docker 步骤都会明确失败，不会伪装成真实 Docker 已可用。
- `CorelDockerAdapter`：未来官方 Docker API 的代码落点，目前仍明确抛出未实现，默认不会被工厂启用。

真实接入时要补齐：

- `CorelDockPanelHost`
- `CorelDockPanelHostFactory`
- `ICorelDockerAdapter` 的真实实现
- 将 `CorelDockPanelHostFactory` 的默认 adapter factory 从 `PlaceholderCorelDockerAdapterFactory` 切换为已验收的真实 adapter factory。
- CorelDRAW Docker 容器创建逻辑
- WPF `QiTuDockPanel` 挂载逻辑
- 面板关闭隐藏逻辑
- Docker 生命周期释放逻辑

`CorelDockPanelHost` 内部已通过 `ICorelDockerAdapter` 预留 5 个固定接入槽位，后续填真实官方 API 时只补 adapter 实现，不改业务链路：

| 槽位 | 未来职责 | 当前状态 |
|------|----------|----------|
| `CreateContainer(object? corelApplication)` | 创建 CorelDRAW Docker 容器，并拿到可挂载 WPF 内容的宿主句柄 | 明确抛出未实现 |
| `AttachPanel(QiTuDockPanel panel)` | 把 `QiTuDockPanel` 挂进真实 Docker 容器 | 明确抛出未实现 |
| `Show()` | 显示并激活真实 Docker 面板 | 明确抛出未实现 |
| `Hide()` | 拦截关闭并只隐藏 Docker 面板，不销毁 WebView2 | 明确抛出未实现 |
| `Release()` | 插件卸载时释放 Docker 容器和 WPF 挂载关系 | 当前只记录占位未实现，不向宿主冒泡 |

这 5 个槽位没有填完前，`DockHostMode` 必须保持 `Debug`。如果误切到 `CorelDocker`，生命周期会捕获失败并回退调试宿主，避免影响本地可运行状态。

注意：`CorelDockPanelHost` 在 Docker 容器确认创建/挂载前不会把 WebView2 挂到临时面板，避免占位失败后污染 WebView2 父容器，导致回退 Debug 面板时无法二次挂载。

注意：`CorelDockerAdapter` 文件存在不代表真实 Docker 已接入。只有当官方 API 代码填入、真实 CorelDRAW 宿主验收通过、默认 factory 明确切换后，才允许把它视为真实 Docker 实现。

`AllowOfficialCorelDockerAdapter` 默认必须为 `false`。真实 Docker API 未完成前，即使 `DockHostMode = CorelDocker`，也只能走占位 adapter 并安全回退。

完整启用门槛见 `docs/CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md`。任何发布包在真实 Docker 验收完成前，都必须保持 `OfficialCorelDockerAdapterDefaultEnabled = false`。

## 注册与安装绑定

真实注册前必须完成：

- 运行 `installer\Get-QiTuCorelRegistrationPlan.ps1`。
- 保存目标机器的 TypeLib 和注册候选报告。
- 确认官方 AddIn / Docker 注册路径。
- 用 `installer\New-QiTuConfirmedCorelRegistrationManifest.ps1` 生成并校验单目标 `CONFIRMED` manifest。
- 每个启用项必须来自真实确认记录，不能手工猜测 `RegistryPath`。

真实路径已确认后，优先用脚本生成单目标确认 manifest，避免手工编辑 JSON 漏字段：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -OutputPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json `
  -CorelVersionIdentifier 27 `
  -RegistrationKind AddIn `
  -RegistryPath "HKCU:\Software\Corel\..." `
  -ConfirmationSource "artifacts\validation\qitucdr-registration-confirmation-....md"
```

真实写入前必须先预览：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -PreviewCorelDrawRegistration `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

禁止：

- 猜测注册表路径。
- 在没有官方确认前写入设计师工作站注册表。
- 生产安装时绕过 manifest 直接传注册表路径。
- 将本机 CorelDRAW 私有 Interop DLL 提交进仓库。

## 本地绑定前验证

先运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build\scripts\Invoke-QiTuBuild.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuPanelStress.ps1 -Iterations 100 -DelayMs 10
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuHostEventStress.ps1 -Iterations 3 -DelayMs 50
```

通过标准：

- Release 构建无错误。
- 单元测试全部通过。
- 面板压测 `PASSED`。
- 宿主事件入口压测 `PASSED`。
- `WebViewCreateCount <= 1`。
- `ActiveDockPanelHostType` 与当前阶段一致：本地调试应为 `DebugDockPanelHost`，真实接入后才应为真实 Docker 宿主。
- `ActiveDockPanelHostKind` 与当前阶段一致：本地调试应为 `Debug`，真实接入后才应为 `CorelDocker`。
- `ActiveDockerAdapterType` 可识别当前 Docker adapter；回退 Debug 面板时应为空。
- `IsDockerAdapterAttached` 可识别真实 Docker adapter 是否已挂载；回退 Debug 面板时应为 `False`。
- `DockHostFallbackCount` 可用于识别是否发生过 CorelDocker 失败回退。

## 真实 CorelDRAW 内验收

真实绑定后至少验收：

| 编号 | 场景 | 通过标准 |
|------|------|----------|
| H-01 | CorelDRAW 启动后加载插件 | 不明显拖慢宿主启动；日志有启动记录 |
| H-02 | 打开 QiTuCDR 面板 | 显示 WPF/WebView2 面板；只创建一个 WebView2 |
| H-03 | 关闭再打开面板 | 关闭只隐藏；二次打开不重建 WebView2 |
| H-04 | 文档切换 | 当前任务取消；状态不残留 `Busy` |
| H-05 | 文档关闭 | 当前任务取消；CorelDRAW 不崩溃 |
| H-06 | 选区变化 | 只发布事件；不触发 COM 遍历 |
| H-07 | CorelDRAW 退出 | 取消全部任务；资源释放；不弹未处理异常 |
| H-08 | WebView2 崩溃 | 进入 WPF 降级面板；核心功能仍可用 |

## 小白检查口径

判断“真实宿主接入完成”必须同时满足：

- CorelDRAW 内能打开 QiTuCDR 面板。
- 面板关闭再打开不新增 WebView2。
- 四个工具能在真实 CDR 文档里运行。
- 文档关闭、文档切换、宿主退出不会崩。
- 发布包能安装、卸载，并能生成注册计划报告。

只在浏览器里能看 UI，不能算真实 CorelDRAW 插件完成。
