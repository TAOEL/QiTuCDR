# CorelDRAW Docker Adapter 启用门槛

本文档定义 `AllowOfficialCorelDockerAdapter` 从 `false` 切换为 `true` 的最低准入条件。当前 V1.0 阶段默认禁止启用官方 Docker adapter 外壳。

## 当前结论

| 项目 | 当前状态 |
|------|----------|
| `CorelDockerAdapter` | 已有官方 API 代码落点 |
| 官方 Docker API 绑定 | 未完成 |
| 真实 CorelDRAW Docker 面板 | 未验收 |
| 默认启用状态 | 禁止 |
| 发布包要求 | `OfficialCorelDockerAdapterDefaultEnabled = false` |

## 绝对禁止

- 禁止因为 `CorelDockerAdapter.cs` 文件存在，就认为真实 Docker 已完成。
- 禁止在没有真实 CorelDRAW 宿主验收记录时启用 `AllowOfficialCorelDockerAdapter`。
- 禁止绕过 `ICorelDockerAdapter` 把 Docker API 直接写进生命周期层或业务层。
- 禁止在未确认关闭隐藏策略前销毁 WebView2。
- 禁止真实 Docker 路径创建第二个 WebView2 实例。

## 启用前必须满足

启用 `AllowOfficialCorelDockerAdapter = true` 前，必须同时满足以下条件：

| 编号 | 条件 | 证据 |
|------|------|------|
| D-01 | 官方 CorelDRAW Docker API 已确认 | SDK 文档、TypeLib、官方示例或目标机器实测记录 |
| D-02 | `CorelDockerAdapter` 已实现 5 个 adapter 步骤 | `CreateContainer`、`AttachPanel`、`Show`、`Hide`、`Release` |
| D-03 | `DockHostMode = CorelDocker` 在真实 CorelDRAW 内可打开面板 | 真实宿主验收记录 |
| D-04 | 面板关闭只隐藏，不销毁 WebView2 | `WebViewCreateCount <= 1` |
| D-05 | 重复打开/关闭面板 100 次通过 | HostHarness 和真实宿主记录 |
| D-06 | `ActiveDockPanelHostKind = CorelDocker` | 稳定性快照 |
| D-07 | `ActiveDockerAdapterType = CorelDockerAdapter` | 稳定性快照 |
| D-08 | `IsDockerAdapterAttached = True` | 稳定性快照 |
| D-09 | 四个工具仍通过真实文档验收 | 批量转曲、一键居中、冗余清理、尺寸规整 |
| D-10 | WebView2 崩溃后仍能降级恢复 | 恢复验收记录 |

## 启用步骤

1. 完成 `docs/CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md`。
2. 完成 `docs/REAL_HOST_VALIDATION_TEMPLATE.md`。
3. 确认 `CorelDockerAdapter` 真实实现不绕过 `ICorelDockerAdapter`。
4. 确认 `CorelDockPanelHostFactory` 默认 adapter factory 切换有对应验收记录。
5. 修改配置：

```json
{
  "DockHostMode": "CorelDocker",
  "AllowOfficialCorelDockerAdapter": true
}
```

6. 运行完整发布验证。

## 发布包门禁

在真实 Docker 验收完成前，发布包必须保持：

```json
{
  "RuntimeSafety": {
    "DefaultDockHostMode": "Debug",
    "CorelDockerStatus": "PlaceholderFallbackRequired",
    "OfficialCorelDockerAdapterDefaultEnabled": false
  }
}
```

如果上述字段被改为默认启用，发布包验证必须失败。

## 小白判断口径

只看到 `CorelDockerAdapter.cs` 文件，不代表真实 Docker 完成。

必须在真实 CorelDRAW 里看到 QiTuCDR 作为 Docker 面板打开，并且稳定性快照显示：

```text
ActiveDockPanelHostKind = CorelDocker
ActiveDockerAdapterType = CorelDockerAdapter
IsDockerAdapterAttached = True
WebViewCreateCount <= 1
```

才可以进入真实 Docker 完成态评估。
