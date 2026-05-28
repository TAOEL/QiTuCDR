# HostHarness 本地调试宿主

HostHarness 用于在不启动 CorelDRAW 的情况下验证 QiTuCDR 的 WPF Shell、WebView2 单例、Bridge、状态机和降级面板。

## 启动调试窗口

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1
```

如果调试窗口已经打开，脚本会跳过构建，避免正在运行的 `QiTuCDR.HostHarness.exe` 锁住输出 DLL。

如果任务管理器仍显示 `QiTuCDR.HostHarness.exe`，但 PowerShell 中该进程 `HasExited = true`，它不会被视为真实运行，也不会触发构建跳过。

## 面板高频开关压测

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -PanelStress 100 -DelayMs 10
```

写出压测报告：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -PanelStress 100 -DelayMs 10 -ReportPath artifacts\stress\panel-stress.md
```

模拟 `CorelDocker` 配置但不修改本机配置文件：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -PanelStress 3 -DelayMs 10 -DockHostMode CorelDocker -ReportPath artifacts\stress\panel-coreldocker-fallback.md
```

压测会重复执行：

```text
ShowPanel -> HidePanel
```

结束时会输出稳定性快照：

- 当前插件状态。
- 是否创建 DockPanel。
- 面板是否仍可隐藏。
- WebView2 控件创建次数。
- WebView2 attach 调用次数。
- WebView2 恢复次数。
- 配置的 Dock 宿主模式。
- 实际使用的 Dock 宿主类型。
- 实际 Dock 宿主种类。
- 当前 Docker adapter 类型。
- Docker adapter 是否已挂载。
- Dock 宿主失败回退次数。

通过标准：

- `WebViewCreateCount` 不大于 1。
- 最终状态回到 `Ready`。
- `CorelDocker` 未实现时必须回退到 `DebugDockPanelHost`，且 `DockHostFallbackCount` 大于 0。
- `CorelDocker` 未实现并回退后，`ActiveDockPanelHostKind` 应为 `Debug`，`ActiveDockerAdapterType` 应为空，`IsDockerAdapterAttached` 应为 `False`。
- 过程不抛出未捕获异常。

## WebView2 恢复压测

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -RecoveryStress 3 -DelayMs 50
```

写出恢复报告：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -RecoveryStress 3 -DelayMs 50 -ReportPath artifacts\stress\recovery-stress.md
```

压测会模拟 WebView2 浏览器失败事件，并验证：

- 状态流转能够回到 `Ready`。
- `BrowserRecoveryCount` 至少等于模拟次数。
- 当前任务会被取消中心统一取消。
- 降级面板路径可被触发。

该模式不强杀真实 WebView2 进程，只用于验证 Native 生命周期恢复链路。真实渲染进程崩溃仍需要在 CorelDRAW 宿主中验收。

## 文档关闭取消压测

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -DocumentCloseStress 3 -DelayMs 50
```

写出压测报告：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -DocumentCloseStress 3 -DelayMs 50 -ReportPath artifacts\stress\document-close-stress.md
```

压测会模拟 CorelDRAW 文档关闭事件，并验证：

- 生命周期入口会取消当前任务。
- `DocumentCloseCancelCount` 至少等于模拟次数。
- 状态最终保持 `Ready`。
- 文档关闭事件会通过 EventBus 推送 `host.documentChanged`。

该模式不连接真实 CorelDRAW 文档，只验证 Native 取消入口。真实“任务执行中关闭文档”仍必须在 CorelDRAW 宿主内验收。

## 宿主事件入口压测

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -HostEventStress 3 -DelayMs 50
```

写出压测报告：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\harness\Start-QiTuHostHarness.ps1 -HostEventStress 3 -DelayMs 50 -ReportPath artifacts\stress\host-event-stress.md
```

压测会循环模拟：

```text
DocumentActivated -> SelectionChanged -> DocumentClosing
```

最后再模拟一次宿主退出取消，并验证：

- `DocumentActivatedCancelCount` 至少等于模拟次数。
- `SelectionChangedEventCount` 至少等于模拟次数。
- `DocumentCloseCancelCount` 至少等于模拟次数。
- `HostShutdownCancelCount` 至少为 1。
- 状态最终保持 `Ready`。

该模式只验证 Native 宿主事件入口，不连接真实 CorelDRAW 事件源。

## 限制

- HostHarness 不连接真实 CorelDRAW 文档。
- 文档、Shape、Undo/Redo 和真实 Docker 注册仍必须在 CorelDRAW 宿主内验收。
