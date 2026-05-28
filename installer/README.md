# 安装与发布交付

本目录用于放置 QiTuCDR V1 的安装、卸载、依赖检测和 CorelDRAW 注册辅助脚本。

当前阶段是 M9 发布交付骨架，不直接绑定某一个安装包工具。后续可以在此基础上接入 WiX、Inno Setup、MSIX 或企业内部分发脚本。

## 脚本

### `Test-QiTuInstallPrerequisites.ps1`

用途：

- 调用项目环境诊断脚本。
- 检查构建产物目录是否包含 `QiTuCDR.Host.dll`。
- 检查构建产物目录是否包含 `WebUI/index.html`。
- 输出安装前可读报告。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Test-QiTuInstallPrerequisites.ps1
```

### `Install-QiTuCDR.ps1`

用途：

- 初始化 `%LOCALAPPDATA%\QiTuCDR\App`、`Config`、`Logs`。
- 将 Host 构建产物复制到安装目录。
- 确保默认配置文件存在。
- 生成 `install-manifest.json`。
- 可选从已确认的 CorelDRAW 注册 manifest 批量写入多个目标版本路径。
- 真实写入注册表时，会在 `install-manifest.json` 中记录实际写入的注册路径和值，便于回滚和排查。

默认安装：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1
```

安装到临时目录：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 -InstallRoot artifacts\installer-smoke
```

注册表说明：

- 脚本不会猜测 CorelDRAW AddIn 注册表路径。
- 生产注册必须传入 `-CorelDrawRegistrationManifestPath`。
- manifest 必须为 `CONFIRMED`，且目标项必须显式 `Enabled = true`。
- 启用项必须填写 `ProductLabel`、`RegistryPath`、`ConfirmationSource`、`ConfirmedBy` 和合法 `ConfirmedAt`。
- 直接传 `-CorelDrawAddInRegistryPath` 默认会被拒绝；只有受控测试可以额外传 `-AllowDirectRegistryPathForTesting`。
- 真实注册路径需要在 CorelDRAW SDK / 目标版本环境中确认后再固化。

使用已确认 manifest 注册：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

真实写注册表前，建议先执行预览：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -PreviewCorelDrawRegistration `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

预览只显示即将写入的注册表路径和值，不复制文件，不写注册表。

### `Uninstall-QiTuCDR.ps1`

用途：

- 删除安装目录中的 App 文件。
- 可选删除配置和日志。
- 可选从注册 manifest 读取并删除多个已启用目标路径。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1
```

保留用户配置和日志是默认行为。需要完整清理时显式传入：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 -RemoveConfig -RemoveLogs
```

从 manifest 反注册：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 `
  -UnregisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

反注册会输出已删除的注册路径；如果目标路径本来不存在，会输出到 `MissingRegistryPaths`，方便确认是否已经清理过。

受控测试才允许直接指定单一路径：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 `
  -UnregisterCorelDrawAddIn `
  -CorelDrawAddInRegistryPath "HKCU:\Software\Corel\QiTuCDRTest" `
  -AllowDirectRegistryPathForTesting
```

### `Get-QiTuCorelRegistrationPlan.ps1`

用途：

- 探测本机 CorelDRAW TypeLib。
- 只读扫描 Corel / CorelDRAW 相关注册表候选项。
- 生成 JSON 和 Markdown 注册计划报告。
- 报告会输出 `EvidenceSummary`、目标版本覆盖情况、候选注册表路径评分、版本提示和 manifest 字段检查清单。
- 为后续确认 CorelDRAW 2021-2026 AddIn 注册路径提供证据。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Get-QiTuCorelRegistrationPlan.ps1
```

报告默认输出到：

```text
artifacts\registration\
```

注意：该脚本不会写注册表，也不会证明最终注册路径。候选路径只用于人工复核；真实注册路径必须在目标 CorelDRAW SDK / 版本环境中确认后，写入 `CONFIRMED` manifest，再交给安装脚本处理。

### `Get-QiTuCorelRegistrationPreview.ps1`

用途：

- 只读检查 `CONFIRMED` manifest。
- 输出安装脚本将要写入的注册表路径和值。
- 支持 `-Json`，可供发布包验证和自动化检查使用。
- 不写注册表，不复制文件。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Get-QiTuCorelRegistrationPreview.ps1 `
  -ManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

### `New-QiTuCorelRegistrationManifest.ps1`

用途：

- 生成 CorelDRAW 注册 manifest 模板。
- 默认覆盖目标版本标识 `23`、`24`、`25`、`26`、`27`。
- 输出 `DRAFT` 状态，默认不会被安装脚本用于写注册表。
- 可在真实注册路径已确认后，生成只启用一个目标版本的 `CONFIRMED` manifest。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuCorelRegistrationManifest.ps1
```

确认真实路径后，应人工把对应目标项改为：

- `Status = CONFIRMED`
- `Enabled = true`
- 填写 `ProductLabel`
- 填写 `RegistryPath`
- 填写 `ConfirmationSource`
- 填写 `ConfirmedBy`
- 填写合法 `ConfirmedAt`

更推荐使用脚本直接生成单目标确认 manifest，减少手工改 JSON 的字段遗漏：

```powershell
$confirmedAt = (Get-Date).ToString("o")

powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuCorelRegistrationManifest.ps1 `
  -OutputPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json `
  -Status CONFIRMED `
  -EnableCorelVersionIdentifier 27 `
  -ProductLabel QiTuCDR `
  -RegistrationKind AddIn `
  -RegistryPath "HKCU:\Software\Corel\..." `
  -ConfirmationSource "CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md" `
  -ConfirmedBy $env:USERNAME `
  -ConfirmedAt $confirmedAt
```

生成后仍必须执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Test-QiTuCorelRegistrationManifest.ps1 `
  -ManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json `
  -RequireConfirmed `
  -FailOnError
```

该脚本只生成 JSON，不写注册表。真实路径仍必须来自官方文档、SDK 或目标机器实测证据。

### `New-QiTuConfirmedCorelRegistrationManifest.ps1`

用途：

- 在真实 CorelDRAW 注册路径已经确认后，生成单目标 `CONFIRMED` manifest。
- 自动调用 `New-QiTuCorelRegistrationManifest.ps1` 生成 JSON。
- 自动调用 `Test-QiTuCorelRegistrationManifest.ps1 -RequireConfirmed -FailOnError` 校验。
- 不写注册表，只生成可交给安装脚本使用的确认文件。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -CorelVersionIdentifier 27 `
  -RegistrationKind AddIn `
  -RegistryPath "HKCU:\Software\Corel\..." `
  -ConfirmationSource "docs\CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md"
```

生成成功后，再把输出路径传给安装脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-27-*.json
```

### `Test-QiTuCorelRegistrationManifest.ps1`

用途：

- 校验注册 manifest 结构。
- 发布前可要求 manifest 为 `CONFIRMED`。
- 默认要求启用项的注册表路径位于 `HKCU:\Software\Corel\` 或 `HKLM:\Software\Corel\` 下。
- 启用项必须具备完整确认信息，不能只填一个注册表路径。

示例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Test-QiTuCorelRegistrationManifest.ps1 `
  -ManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json `
  -RequireConfirmed `
  -FailOnError
```

## 安全规则

- 安装脚本只复制构建产物，不修改 CorelDRAW 私有文件。
- 卸载脚本只允许删除 `QiTuCDR` 安装根内的路径。
- 注册表写入必须显式传入路径，不做版本猜测。
- 注册表写入默认必须走 `CONFIRMED` manifest；直接路径只允许受控测试。
- manifest 注册必须为 `CONFIRMED`，并且只处理显式启用且确认信息完整的目标项。
- WebView2 Runtime 缺失不会阻止安装，但会在运行时进入 WPF 降级面板；发布前仍应提示用户安装 Runtime。
