# 真实 CorelDRAW 验收快速开始

本文档用于拿到 QiTuCDR 发布包后，在真实 CorelDRAW 测试机上按顺序执行验收。它不替代真实记录；最终结果必须回填到 `qitucdr-real-host-validation-*.md`。

## 适用范围

- 目标机器已安装 CorelDRAW 2021-2026 之一。
- 目标机器为 Windows 10 / Windows 11 x64。
- 当前目标是验证安装、注册、加载、面板和卸载清理链路。
- 当前不代表真实 Docker adapter 已完成；默认仍应保持 `DockHostMode = Debug`。

## 1. 解压发布包

将 `qitucdr-v*.zip` 解压到一个临时目录，例如：

```text
C:\QiTuCDR-Release
```

后续命令都在这个目录下执行。

## 2. 验证发布包完整性

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build\scripts\Test-QiTuPackage.ps1 -FailOnError
```

如果你拿到的是仅包含发布内容的 zip，且没有仓库级 `build\scripts` 目录，则跳过这一步，改为检查发布包内必须存在：

- `App\QiTuCDR.Host.dll`
- `App\WebUI\index.html`
- `installer\Install-QiTuCDR.ps1`
- `installer\Uninstall-QiTuCDR.ps1`
- `installer\Get-QiTuCorelRegistrationPlan.ps1`
- `tools\validation\New-QiTuRealHostExecutionPlan.ps1`
- `tools\validation\New-QiTuRealHostValidationRecord.ps1`
- `docs\REAL_HOST_EXECUTION_PLAN_TEMPLATE.md`
- `docs\REAL_HOST_VALIDATION_TEMPLATE.md`

## 3. 生成执行计划

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostExecutionPlan.ps1 `
  -CorelDrawVersion "CorelDRAW 2026" `
  -CorelVersionIdentifier 27
```

通过标准：`artifacts\validation` 下生成 `qitucdr-real-host-execution-plan-*.md`。

## 4. 生成验收记录草稿

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostValidationRecord.ps1 `
  -CorelDrawVersion "CorelDRAW 2026" `
  -CorelVersionIdentifier 27
```

通过标准：生成以下文件：

- `qitucdr-real-host-validation-*.md`
- `qitucdr-registration-confirmation-*.md`
- 注册计划 JSON / Markdown 报告

## 5. 人工确认注册路径

打开最新的 `qitucdr-registration-confirmation-*.md`，只在证据充分时填写最终注册路径。

不要把下面这种示例路径当作真实路径：

```text
HKCU:\Software\Corel\...
```

确认依据必须来自官方文档、SDK、目标机器实测证据或已保存的注册确认记录。

## 6. 生成 CONFIRMED manifest

真实路径确认后再执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -CorelVersionIdentifier 27 `
  -RegistrationKind AddIn `
  -RegistryPath "HKCU:\Software\Corel\..." `
  -ConfirmationSource "artifacts\validation\qitucdr-registration-confirmation-....md"
```

通过标准：脚本输出 `Status: OK`。

## 7. 写入前预览

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -PreviewCorelDrawRegistration `
  -CorelDrawRegistrationManifestPath "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-27-*.json"
```

通过标准：

- 输出 `WouldWriteCount`
- 输出 `RegistryPath`
- 输出 `AssemblyPath`
- 输出 `Preview only. No files or registry entries were changed.`

如果预览路径和注册确认记录不一致，立即停止。

## 8. 安装并注册

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -SourcePath App `
  -Force `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-27-*.json"
```

通过标准：

- 输出 `RegistryWritten: True`
- 输出 `RegisteredPaths`
- `%LOCALAPPDATA%\QiTuCDR\install-manifest.json` 包含 `RegisteredCorelDrawAddInEntries`

## 9. 启动 CorelDRAW 验证

手动启动目标 CorelDRAW 版本，检查：

- CorelDRAW 是否正常启动。
- QiTuCDR 是否被加载。
- QiTuCDR 面板是否能打开。
- 面板关闭是否只隐藏。
- `%LOCALAPPDATA%\QiTuCDR\Logs` 是否持续出现异常。

通过标准：CorelDRAW 不崩溃，QiTuCDR 可见，面板可打开。

## 10. 卸载并反注册

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 `
  -UnregisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-27-*.json"
```

通过标准：

- 输出 `UnregisteredPaths`
- 目标注册路径不再存在
- 重启 CorelDRAW 后不再加载 QiTuCDR

## 11. 回填验收记录

把每一步的结果写回 `qitucdr-real-host-validation-*.md`，至少包括：

- 注册 manifest 校验结果
- 安装注册结果
- CorelDRAW 加载结果
- 面板打开结果
- 注册清理结果
- 卸载后验证结果

## 停止条件 / Stop conditions

遇到以下任一情况立即停止：

- 注册路径无法确认。
- manifest 校验失败。
- 预览输出与确认记录不一致。
- 安装后 manifest 没有记录注册写入明细。
- CorelDRAW 启动异常。
- 日志出现未处理异常。
