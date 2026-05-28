# CorelDRAW 注册确认记录模板

本文档用于记录 QiTuCDR 真实写入 CorelDRAW AddIn / Docker 注册前的确认依据。每个目标 CorelDRAW 版本至少保留一份记录；没有完成本记录前，不允许把目标项写入 `CONFIRMED` manifest。

## 基本信息

| 字段 | 记录 |
|------|------|
| 确认日期 | |
| 确认人员 | |
| 测试机器 | |
| Windows 版本 | |
| CorelDRAW 版本 | |
| CorelDRAW 版本标识 | |
| QiTuCDR 版本 | |
| 注册类型 | AddIn / Docker |
| 是否允许写入注册表 | 否 |

## 证据来源

| 字段 | 记录 |
|------|------|
| 官方文档链接或文件名 | |
| CorelDRAW SDK / TypeLib 路径 | |
| 注册计划 JSON 报告 | |
| 注册计划 Markdown 报告 | |
| 本机注册表候选路径 | |
| 最终确认注册路径 | |

## Manifest 字段映射

确认后，将下表内容一一填入注册 manifest 的启用目标项。

| manifest 字段 | 记录 |
|----------------|------|
| `Enabled` | `true` |
| `CorelVersionIdentifier` | |
| `ProductLabel` | |
| `RegistrationKind` | |
| `RegistryPath` | |
| `ConfirmationSource` | |
| `ConfirmedBy` | |
| `ConfirmedAt` | |

## 写入前检查

| 检查项 | 结果 | 备注 |
|--------|------|------|
| 路径位于 `HKCU:\Software\Corel\` 或 `HKLM:\Software\Corel\` 下 | | |
| 路径不是猜测路径 | | |
| 路径来自官方文档、SDK 或目标机器实测证据 | | |
| 已保存注册计划报告 | | |
| 已通过 `Test-QiTuCorelRegistrationManifest.ps1 -RequireConfirmed -FailOnError` | | |
| 已确认卸载脚本能清理同一路径 | | |

## 真实注册验收

| 场景 | 结果 | 证据 |
|------|------|------|
| 安装脚本写入注册项 | | |
| CorelDRAW 启动后能发现 QiTuCDR | | |
| QiTuCDR 面板能打开 | | |
| 面板关闭只隐藏 | | |
| 卸载脚本能移除注册项 | | |
| 卸载后 CorelDRAW 不再加载 QiTuCDR | | |

## 结论

- 是否允许写入 `CONFIRMED` manifest：
- 不允许的原因：
- 需要补充的证据：
- 下一步动作：
