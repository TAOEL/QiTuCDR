# 真实 CorelDRAW 宿主验收记录模板

本文档用于记录 QiTuCDR V1.0 在真实 CorelDRAW 2021-2026 宿主内的 M6-M8 验收结果。每轮测试建议复制一份到 `artifacts/stress/` 或测试记录系统中保存，不要用口头结论替代记录。

## 基本信息

| 字段 | 记录 |
|------|------|
| 测试日期 | |
| 测试人员 | |
| CorelDRAW 版本 | |
| Windows 版本 | |
| WebView2 Runtime 版本 | |
| QiTuCDR 构建版本/提交标识 | |
| 是否启用 typed Interop | |
| DockHostMode | |
| AllowOfficialCorelDockerAdapter | |
| ActiveDockPanelHostKind | |
| ActiveDockerAdapterType | |
| IsDockerAdapterAttached | |
| WebViewCreateCount | |
| 测试文档路径 | |
| 测试文档对象数量 | |
| 注册 manifest 路径 | |
| 注册确认记录路径 | |
| 日志目录 | `%LOCALAPPDATA%\QiTuCDR\Logs` |

## 注册与加载验收

真实宿主验收前，必须先完成 [CorelDRAW 注册确认记录模板](CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md)。本节只记录本轮真机执行结果。

| 场景 | 操作 | 结果 | 证据 |
|------|------|------|------|
| 注册 manifest 校验 | 执行 `Test-QiTuCorelRegistrationManifest.ps1 -RequireConfirmed -FailOnError` | | |
| 安装注册 | 使用 `Install-QiTuCDR.ps1 -RegisterCorelDrawAddIn -CorelDrawRegistrationManifestPath ...` | | |
| CorelDRAW 加载 | 启动目标 CorelDRAW 版本 | | |
| 面板打开 | 在 CorelDRAW 内打开 QiTuCDR 面板 | | |
| 注册清理 | 使用 `Uninstall-QiTuCDR.ps1 -UnregisterCorelDrawAddIn -CorelDrawRegistrationManifestPath ...` | | |
| 卸载后验证 | 重启 CorelDRAW，确认不再加载 QiTuCDR | | |

## Docker Adapter 启用门槛验收

真实 Docker 启用前必须先阅读并满足 [CorelDRAW Docker Adapter 启用门槛](CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md)。如果本轮未启用真实 Docker adapter，以下结果应明确记录为未启用或不适用。

| 编号 | 检查项 | 结果 | 证据 |
|------|--------|------|------|
| D-01 | 官方 CorelDRAW Docker API 已确认 | | |
| D-02 | `CorelDockerAdapter` 已实现 5 个 adapter 步骤 | | |
| D-03 | `DockHostMode = CorelDocker` 在真实 CorelDRAW 内可打开面板 | | |
| D-04 | `AllowOfficialCorelDockerAdapter = true` 仅在真实验收时启用 | | |
| D-05 | `ActiveDockPanelHostKind = CorelDocker` | | |
| D-06 | `ActiveDockerAdapterType = CorelDockerAdapter` | | |
| D-07 | `IsDockerAdapterAttached = True` | | |
| D-08 | `WebViewCreateCount <= 1` | | |
| D-09 | 面板关闭只隐藏，不销毁 WebView2 | | |
| D-10 | 未完成真实 Docker 验收前发布包保持 `OfficialCorelDockerAdapterDefaultEnabled = false` | | |

## M6 批量转曲验收

| 场景 | 操作 | 结果 | 备注 |
|------|------|------|------|
| 无文档 | 不打开文档执行批量转曲 | | 期望 `NO_DOCUMENT` |
| 空选区 | 选区模式下不选择对象 | | 期望 `EMPTY_SELECTION` |
| 选中对象 | 选择文本对象执行转曲 | | 期望状态回到 Ready |
| 当前页 | 当前页包含文本、锁定对象、隐藏对象 | | 锁定对象跳过 |
| 全文档 5000+ Shape | 执行全文档转曲 | | 进度持续更新，宿主不崩溃 |
| 取消任务 | 执行中点击取消 | | 期望 `TASK_CANCELLED` 或明确取消事件 |
| Undo/Redo | 转曲后撤销/重做 | | 命令组可读且闭合 |

## M7 工具验收

| 工具 | 场景 | 结果 | 备注 |
|------|------|------|------|
| 一键居中 | 单对象页面居中 | | |
| 一键居中 | 多对象整体居中 | | |
| 一键居中 | 多对象独立居中 | | |
| 尺寸规整 | 统一宽高 | | |
| 尺寸规整 | 等比例锁定缩放 | | |
| 尺寸规整 | 仅统一描边宽度 | | |
| 冗余清理 | 未二次确认时执行 | | 期望拒绝 |
| 冗余清理 | 清理页面辅助线 | | |
| 冗余清理 | 清理隐藏空图层 | | |
| 冗余清理 | 清理空文本对象 | | |

## M8 稳定性验收

| 编号 | 场景 | 结果 | 证据 |
|------|------|------|------|
| M8-01 | 5000+ Shape 批处理 | | |
| M8-02 | 面板打开/关闭 100 次 | | |
| M8-03 | WebView2 崩溃或初始化失败降级 | | |
| M8-04 | 执行中文档关闭 | | |
| M8-05 | 24 小时挂起内存涨幅 | | 附 `qitucdr-memory-watch-*.md` |
| M8-06 | Undo/Redo 命令组 | | |
| M8-07 | 锁定、隐藏、异常对象容错 | | |

## 内存监控命令

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\stress\Invoke-QiTuMemoryWatch.ps1 -DurationHours 24 -IntervalSeconds 60
```

通过标准：

- CorelDRAW 宿主不崩溃、不无响应。
- QiTuCDR 不持续刷异常日志。
- 目标进程 24 小时私有内存涨幅不超过 50 MB。

## 问题记录

| 编号 | 问题 | 严重级别 | 复现步骤 | 日志/截图 | 处理结论 |
|------|------|----------|----------|-----------|----------|
| | | | | | |

## 总结

- 本轮是否通过：
- 需要修复的问题：
- 可延期的问题：
- 下一轮回归重点：
