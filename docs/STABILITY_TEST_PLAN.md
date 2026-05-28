# M8 稳定性与压测计划

本文档用于约束 QiTuCDR V1.0 在真实 CorelDRAW 宿主中的 M8 验收。M8 不以“功能能点”为完成标准，而以“长时间运行、异常可恢复、资源不失控”为完成标准。

## 验收优先级

```text
CorelDRAW 宿主稳定性 > COM 安全 > 生命周期可控 > 资源释放 > 性能稳定 > UI 体验
```

## 自动化基线

运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuStressBaseline.ps1
```

输出：

```text
artifacts/stress/qitucdr-stress-baseline-*.md
```

基线报告至少应包含：

- 构建结果。
- 单元测试结果。
- WebUI 静态资源状态。
- WebView2 / CorelDRAW TypeLib / 配置目录 / 日志目录诊断。
- CorelDRAW 与 QiTuCDR 相关进程内存快照。
- 真实宿主人工验收清单。

## HostHarness 面板压测

在不启动 CorelDRAW 的情况下，可以先用 HostHarness 验证 WPF Shell 与 WebView2 单例策略：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuPanelStress.ps1 -Iterations 100 -DelayMs 10
```

验证误切 `CorelDocker` 时的安全回退：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuPanelStress.ps1 -Iterations 3 -DelayMs 10 -DockHostMode CorelDocker
```

该命令只对本次 HostHarness 进程临时生效，不写入用户本地配置文件。报告应显示 `ConfiguredDockHostMode = CorelDocker`、`ActiveDockPanelHostType = DebugDockPanelHost`、`ActiveDockPanelHostKind = Debug`、`ActiveDockerAdapterType` 为空、`IsDockerAdapterAttached = False`、`DockHostFallbackCount > 0`。

通过标准：

- `ConfiguredDockHostMode` 与当前配置一致。
- `ActiveDockPanelHostType` 能看出实际使用的宿主类型。
- `DockHostFallbackCount` 为 0；如果配置为未完成的 `CorelDocker`，允许增长，但必须回退到 `DebugDockPanelHost`。
- `ActiveDockPanelHostKind`、`ActiveDockerAdapterType`、`IsDockerAdapterAttached` 必须能说明当前实际运行的是 Debug 面板、占位 Docker adapter，或未来真实 Docker adapter。
- `WebViewCreateCount` 不大于 1。
- `WebViewAttachCallCount` 可以增长，但不允许导致第二个 WebView2 控件创建。
- 最终状态必须回到 `Ready`，不允许停在 `Busy` 或 `Preheating`。
- 过程不抛出未捕获异常。

该压测只能证明本地 WPF Shell 的关闭隐藏和 WebView2 复用策略；真实 CorelDRAW Docker 面板仍需在宿主内重复验证。

当前基线：

- 2026-05-25 已完成 HostHarness 100 次面板开关压测。
- 报告：`artifacts/stress/qitucdr-panel-stress-20260525-171147.md`
- 结果：`PASSED`
- 快照：`State = Ready`，`WebViewCreateCount = 1`，`WebViewAttachCallCount = 102`，`BrowserRecoveryCount = 0`

## HostHarness 恢复压测

在不强杀真实浏览器进程的情况下，可以先用 HostHarness 验证 Native 恢复链路：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuRecoveryStress.ps1 -Iterations 3 -DelayMs 50
```

通过标准：

- 每次模拟 WebView2 browser failure 后，状态最终回到 `Ready`。
- `BrowserRecoveryCount` 至少等于模拟次数。
- 恢复流程会触发 WPF fallback 路径。
- 恢复流程会取消当前任务中心，避免 Web 层失联后后台任务继续运行。

该压测只能证明 Native 生命周期恢复链路；真实 WebView2 渲染进程崩溃仍需在 CorelDRAW 宿主中补充验证。

## HostHarness 文档关闭取消压测

在不连接真实 CorelDRAW 文档的情况下，可以先验证 Native 文档关闭取消入口：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuDocumentCloseStress.ps1 -Iterations 3 -DelayMs 50
```

通过标准：

- 每次模拟文档关闭后，生命周期会调用当前任务取消。
- `DocumentCloseCancelCount` 至少等于模拟次数。
- 状态最终保持 `Ready`，不残留 `Busy`。
- EventBus 推送 `host.documentChanged`，payload 中包含 `reason = closing`。

该压测只能证明 Native 取消入口；真实“批量任务执行中关闭文档”仍需在 CorelDRAW 宿主中补充验证。

## 宿主事件入口基线

当前 Native 层已提供以下宿主事件入口，真实 CorelDRAW 绑定前可通过单元测试和 HostHarness 稳定性快照观察：

- 文档关闭：`DocumentCloseCancelCount` 增长，并取消当前任务。
- 文档激活：`DocumentActivatedCancelCount` 增长，并取消当前任务，防止跨文档执行。
- 选区变化：`SelectionChangedEventCount` 增长，只发布事件，不读取实时选区。
- 宿主退出：`HostShutdownCancelCount` 增长，并取消全部任务。

这些计数只证明入口可达和取消策略生效。真实宿主验收仍需要在 CorelDRAW 事件绑定后执行。

本地冒烟：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuHostEventStress.ps1 -Iterations 3 -DelayMs 50
```

注意：HostHarness / WebView2 压测请串行执行，不要同时跑多个压测进程。

## 真实宿主验收矩阵

| 编号 | 场景 | 操作 | 通过标准 |
|------|------|------|----------|
| M8-01 | 5000+ Shape 批量转曲 | 打开含 5000+ 对象的文档，执行全文档转曲 | CorelDRAW 不崩溃；进度持续更新；锁定对象跳过；结束后状态回到 Ready |
| M8-02 | 高频开关面板 | 连续打开/关闭面板 100 次 | 关闭只隐藏；不新增第二个 WebView2；二次打开耗时稳定 |
| M8-03 | WebView2 崩溃恢复 | 模拟 WebView 渲染崩溃或初始化失败 | 进入 WPF 降级面板；状态流转 Faulted -> Recovering -> Ready；核心功能仍可用 |
| M8-04 | 执行中文档关闭 | 批量任务进行中关闭当前文档 | 当前任务取消；返回标准 `TASK_CANCELLED` 或明确错误；宿主不崩溃 |
| M8-05 | 24 小时挂起 | 打开 CorelDRAW 和插件面板，空闲挂起 24 小时 | 内存涨幅不超过 50 MB；日志无持续异常刷屏 |
| M8-06 | Undo/Redo 命令组 | 执行转曲、居中、规整、清理后撤销/重做 | CorelDRAW 撤销栈可读；命令组闭合；无残留 Busy 状态 |
| M8-07 | 异常对象容错 | 文档含锁定、隐藏、异常文本和空图层 | 单项失败不拖垮整轮任务；日志记录跳过原因 |

## 24 小时内存监控

运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -DurationHours 24 -IntervalSeconds 60
```

输出：

```text
artifacts/stress/qitucdr-memory-watch-*.csv
artifacts/stress/qitucdr-memory-watch-*.md
```

快速冒烟：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -SampleCount 2 -IntervalSeconds 1
```

通过标准：

- 目标进程存在并被采样。
- 24 小时内 CorelDRAW / QiTuCDR 相关进程未崩溃、未卡死。
- 私有内存涨幅不超过 50 MB。
- QiTuCDR 日志没有持续刷屏式异常。

## 记录要求

每轮真实宿主压测需要记录：

- CorelDRAW 版本。
- Windows 版本。
- 是否启用 typed Interop。
- WebView2 Runtime 版本。
- 测试文档对象数量。
- 开始与结束内存。
- 是否发生崩溃、卡死、异常弹窗。
- 对应日志文件路径。

建议使用 [真实 CorelDRAW 宿主验收记录模板](REAL_HOST_VALIDATION_TEMPLATE.md) 统一记录。

## 退出标准

M8 可以标记为“基本完成”的最低条件：

- 自动化基线脚本通过。
- 四个核心工具在真实 CorelDRAW 中完成一次回归。
- M8-01 至 M8-04 通过。

M8 可以标记为“已完成”的条件：

- M8-01 至 M8-07 全部通过。
- 至少一轮 24 小时挂起完成且内存涨幅不超过 50 MB。
- 所有发现的问题都有明确修复或延期记录。
