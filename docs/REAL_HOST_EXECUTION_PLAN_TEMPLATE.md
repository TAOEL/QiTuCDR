# QiTuCDR 真实 CorelDRAW 宿主验收执行计划

生成时间：__GENERATED_AT__

本文档是执行顺序清单，不代表验收已经通过。请在目标测试机按顺序执行，并把结果回填到真实宿主验收记录。

## 本轮目标

| 字段 | 值 |
|------|----|
| CorelDRAW 版本 | __CORELDRAW_VERSION__ |
| CorelDRAW 版本标识 | __COREL_VERSION_IDENTIFIER__ |
| 注册类型 | __REGISTRATION_KIND__ |
| 已确认注册路径 | __CONFIRMED_REGISTRY_PATH__ |
| 确认 manifest 路径 | __CONFIRMED_MANIFEST_PATH__ |
| 安装目录 | __INSTALL_ROOT__ |

## 0. 前置原则

- 不要把示例路径当作真实路径。
- 注册路径必须来自官方文档、SDK、目标机器实测证据或已保存的注册确认记录。
- 未确认真实 Docker 前，保持 `DockHostMode = Debug`、`AllowOfficialCorelDockerAdapter = False`。
- 每一步都保存命令输出、日志路径或截图，不用口头结论替代证据。

## 1. 生成验收记录草稿和注册计划

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostValidationRecord.ps1 `
  -CorelDrawVersion "__CORELDRAW_VERSION__" `
  -CorelVersionIdentifier __COREL_VERSION_IDENTIFIER__
```

通过标准：生成 `qitucdr-real-host-validation-*.md`、`qitucdr-registration-confirmation-*.md` 和注册计划 JSON/Markdown。

## 2. 人工填写注册确认记录

打开最新的 `qitucdr-registration-confirmation-*.md`，填写并确认：

- 官方文档链接或 SDK 证据。
- 本机注册表候选路径。
- 最终确认注册路径。
- `CorelVersionIdentifier`、`ProductLabel`、`RegistrationKind`、`RegistryPath`、`ConfirmationSource`、`ConfirmedBy`、`ConfirmedAt`。

通过标准：记录里明确写出允许生成 `CONFIRMED` manifest 的证据。

## 3. 生成 CONFIRMED manifest

确认路径后再执行。不要在路径仍为 `HKCU:\Software\Corel\...` 时执行真实安装。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -OutputPath "__CONFIRMED_MANIFEST_PATH__" `
  -CorelVersionIdentifier __COREL_VERSION_IDENTIFIER__ `
  -RegistrationKind __REGISTRATION_KIND__ `
  -RegistryPath "__CONFIRMED_REGISTRY_PATH__" `
  -ConfirmationSource "artifacts\validation\qitucdr-registration-confirmation-....md"
```

通过标准：脚本输出 `Status: OK`，且 manifest 为 `CONFIRMED`。

## 4. 写入前预览

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -InstallRoot "__INSTALL_ROOT__" `
  -PreviewCorelDrawRegistration `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__"
```

通过标准：输出 `WouldWriteCount`、`RegistryPath`、`AssemblyPath`，并显示 `Preview only. No files or registry entries were changed.`

## 5. 安装并注册

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -InstallRoot "__INSTALL_ROOT__" `
  -Force `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__"
```

通过标准：输出 `RegistryWritten: True`，`install-manifest.json` 包含 `RegisteredCorelDrawAddInEntries`。

## 6. 启动 CorelDRAW 并验收加载

手动启动目标 CorelDRAW 版本，记录：

- CorelDRAW 是否正常启动。
- QiTuCDR 是否被发现或加载。
- 面板是否能打开。
- 关闭面板是否只是隐藏。
- `%LOCALAPPDATA%\QiTuCDR\Logs` 是否有异常。

通过标准：CorelDRAW 不崩溃，QiTuCDR 可见，面板打开后不新增第二个 WebView2。

## 7. 卸载并反注册

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 `
  -InstallRoot "__INSTALL_ROOT__" `
  -UnregisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__"
```

通过标准：输出 `UnregisteredPaths`，目标注册路径不再存在。

## 8. 回填真实宿主验收记录

把以上结果回填到 `qitucdr-real-host-validation-*.md`：

- 注册 manifest 校验结果。
- 安装注册结果。
- CorelDRAW 加载结果。
- 面板打开结果。
- 注册清理结果。
- 卸载后验证结果。

## 9. 停止条件 / Stop conditions

遇到以下情况立即停止，不继续写注册表或启动真实设计环境：

- 注册路径无法确认。
- manifest 校验失败。
- 预览输出的路径和值与确认记录不一致。
- 安装后 `install-manifest.json` 没有记录注册写入明细。
- CorelDRAW 启动异常或日志出现未处理异常。
