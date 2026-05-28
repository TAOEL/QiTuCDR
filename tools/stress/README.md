# M8 压测工具

本目录放置 QiTuCDR V1.0 的稳定性与压力验收辅助脚本。

## 当前脚本

### `Invoke-QiTuStressBaseline.ps1`

用途：

- 顺序执行项目构建验证，避免 WebUI hash 变化和 Host 复制资源互相打架。
- 运行环境诊断，记录 WebView2、CorelDRAW TypeLib、WebUI、配置目录和日志目录状态。
- 采集当前 CorelDRAW / QiTuCDR 相关进程的内存快照。
- 生成一份 M8 基线报告，包含真实 CorelDRAW 内需要人工勾选的验收项。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuStressBaseline.ps1
```

仅生成环境与进程报告，不执行构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuStressBaseline.ps1 -SkipBuild
```

### `Invoke-QiTuPanelStress.ps1`

用途：

- 调用 HostHarness 的面板高频开关模式。
- 验证 `ShowPanel -> HidePanel` 重复执行时不创建第二个 WebView2 控件。
- 输出 `WebViewCreateCount`、`WebViewAttachCallCount`、`BrowserRecoveryCount` 等稳定性快照。
- 生成 `artifacts\stress\qitucdr-panel-stress-*.md` 报告。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuPanelStress.ps1 -Iterations 100 -DelayMs 10
```

模拟误切到尚未完成的真实 Docker 宿主：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuPanelStress.ps1 -Iterations 3 -DelayMs 10 -DockHostMode CorelDocker
```

该模式不会修改 `%LOCALAPPDATA%\QiTuCDR\Config\settings.json`，只在本次 HostHarness 进程内临时覆盖配置。通过报告应显示 `ConfiguredDockHostMode = CorelDocker`、`ActiveDockPanelHostType = DebugDockPanelHost`、`ActiveDockPanelHostKind = Debug`、`ActiveDockerAdapterType` 为空、`IsDockerAdapterAttached = False`、`DockHostFallbackCount > 0`。

如果 HostHarness 已经打开，脚本会提示先关闭调试窗口，避免构建输出 DLL 被锁定。
如果存在无法结束的悬挂 HostHarness 进程，脚本会自动构建到临时输出目录并运行压测。
如果悬挂项在 PowerShell 中显示 `HasExited = true`，脚本会忽略它并继续使用默认输出目录。

### `Invoke-QiTuMemoryWatch.ps1`

用途：

- 采样 CorelDRAW / QiTuCDR 相关进程的工作集和私有内存。
- 生成 CSV 明细和 Markdown 汇总报告。
- 用于 M8-05 的 24 小时挂起内存涨幅验收。
- 默认按 `CorelDRW` 和 `QiTuCDR.HostHarness` 进程名采样，不会主动启动或结束任何进程。

24 小时验收示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -DurationHours 24 -IntervalSeconds 60
```

快速冒烟示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -SampleCount 2 -IntervalSeconds 1
```

指定进程名：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -ProcessName CorelDRW,QiTuCDR.HostHarness
```

### `Invoke-QiTuRecoveryStress.ps1`

用途：

- 调用 HostHarness 的恢复压测模式。
- 模拟 WebView2 浏览器失败事件。
- 验证生命周期能完成 `Faulted -> Recovering -> Ready`。
- 验证恢复计数 `BrowserRecoveryCount` 按次数增长。
- 生成 `artifacts\stress\qitucdr-recovery-stress-*.md` 报告。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuRecoveryStress.ps1 -Iterations 3 -DelayMs 50
```

该脚本不会强杀真实 WebView2 进程，只验证 Native 恢复链路。真实渲染进程崩溃仍需要在 CorelDRAW 宿主中补充验收。

### `Invoke-QiTuDocumentCloseStress.ps1`

用途：

- 调用 HostHarness 的文档关闭压测模式。
- 模拟 CorelDRAW 文档关闭事件。
- 验证生命周期统一取消当前任务。
- 验证状态最终保持 `Ready`，不残留 `Busy`。
- 生成 `artifacts\stress\qitucdr-document-close-stress-*.md` 报告。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuDocumentCloseStress.ps1 -Iterations 3 -DelayMs 50
```

该脚本只验证 Native 文档关闭取消入口。真实“批量任务执行中关闭文档”仍需要在 CorelDRAW 宿主中补充验收。

### `Invoke-QiTuHostEventStress.ps1`

用途：

- 调用 HostHarness 的宿主事件入口压测模式。
- 模拟文档激活、选区变化、文档关闭和宿主退出取消。
- 验证文档切换/关闭会取消当前任务，选区变化只发布事件。
- 生成 `artifacts\stress\qitucdr-host-event-stress-*.md` 报告。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuHostEventStress.ps1 -Iterations 3 -DelayMs 50
```

该脚本只验证 Native 宿主事件入口。真实 CorelDRAW 事件绑定仍需要在宿主内验收。

报告默认输出到：

```text
artifacts\stress\
```

## 注意

- 脚本不会伪造真实 CorelDRAW 压测结果。
- 多个 HostHarness / WebView2 压测不要并行运行；请串行执行，避免本地 WebView2 调试资源互相竞争。
- 5000+ Shape、100 次面板开关、WebView2 crash、文档关闭中断和 24 小时挂起仍必须在真实 CorelDRAW 宿主中验收。
- 生成的 `artifacts/` 内容属于本地验证产物，不应提交到仓库。
