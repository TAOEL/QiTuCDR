# QiTuCDR 真实 CorelDRAW 测试命令清单

生成时间：__GENERATED_AT__

## 0. 当前测试对象

| 项目 | 值 |
|------|----|
| 发布包 | `__PACKAGE_PATH__` |
| 验收包目录 | `__KIT_DIRECTORY__` |
| CorelDRAW 版本标识 | `__COREL_VERSION_IDENTIFIER__` |
| 注册类型 | `__REGISTRATION_KIND__` |
| 待确认注册路径 | `__CONFIRMED_REGISTRY_PATH__` |
| 确认 manifest 路径 | `__CONFIRMED_MANIFEST_PATH__` |
| 安装目录 | `__INSTALL_ROOT__` |

## 1. 先看 readiness 和执行计划

如果 CorelDRAW 已打开，可以先做一次只读 COM 连接烟测：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Invoke-QiTuCorelDrawComSmoke.ps1 `
  -FailOnError
```

该步骤只连接 CorelDRAW COM、读取版本和进程信息，并立即释放 COM 引用，不修改文档。

先打开验收包索引：

```powershell
notepad "__KIT_INDEX_PATH__"
```

再打开执行计划：

```powershell
notepad "__EXECUTION_PLAN_PATH__"
```

如果 readiness 不是 `READY_FOR_MANUAL_HOST_VALIDATION`，先停止，不要进入真实注册。

## 2. 生成注册计划报告

这一步只读，不写注册表，用来辅助人工确认真实 CorelDRAW 注册路径。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Get-QiTuCorelRegistrationPlan.ps1 `
  -OutputDirectory "__REGISTRATION_PLAN_OUTPUT_DIRECTORY__"
```

打开生成的 Markdown 报告，人工判断真实注册路径。不要把候选路径直接当成已确认路径。

## 3. 生成 CONFIRMED manifest

只有当你已经确认真实注册路径后，才执行这一步。把下面的 `__CONFIRMED_REGISTRY_PATH__` 替换为真实路径后再运行。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -OutputPath "__CONFIRMED_MANIFEST_PATH__" `
  -CorelVersionIdentifier "__COREL_VERSION_IDENTIFIER__" `
  -RegistrationKind "__REGISTRATION_KIND__" `
  -RegistryPath "__CONFIRMED_REGISTRY_PATH__" `
  -ConfirmationSource "real CorelDRAW host validation" `
  -ConfirmedBy "$env:USERNAME"
```

## 4. 预览注册写入

推荐先执行一键干跑。它会生成 `CONFIRMED` manifest，并执行结构化预览和安装脚本预览，但不会写注册表：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Invoke-QiTuRealHostRegistrationDryRun.ps1 `
  -SourcePath "App" `
  -InstallRoot "__INSTALL_ROOT__" `
  -CorelVersionIdentifier "__COREL_VERSION_IDENTIFIER__" `
  -RegistrationKind "__REGISTRATION_KIND__" `
  -RegistryPath "__CONFIRMED_REGISTRY_PATH__" `
  -ManifestPath "__CONFIRMED_MANIFEST_PATH__" `
  -FailOnError
```

这一步仍然不写注册表，只预览即将写入的位置和值。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -SourcePath "App" `
  -InstallRoot "__INSTALL_ROOT__" `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__" `
  -PreviewCorelDrawRegistration
```

预览结果必须只包含你刚确认的 CorelDRAW 注册路径。如果路径不对，停止。

## 5. 安装并注册到 CorelDRAW

确认预览无误后再执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -SourcePath "App" `
  -InstallRoot "__INSTALL_ROOT__" `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__" `
  -RegisterCorelDrawAddIn `
  -Force
```

安装完成后，打开 CorelDRAW，检查插件是否加载、面板是否出现。

## 6. 安装后核查

安装注册完成后，先做一次安装状态核查。它不会启动 CorelDRAW，只检查安装目录、安装 manifest 和注册表项是否一致：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuRealHostInstallState.ps1 `
  -InstallRoot "__INSTALL_ROOT__" `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__" `
  -FailOnError
```

状态为 `OK` 后，再打开 CorelDRAW 做真实加载和功能测试。

## 7. 回填真实验收记录

打开验收记录草稿：

```powershell
notepad "__VALIDATION_RECORD_PATH__"
```

只把真实 CorelDRAW 内看到的结果填进去，不要把未测试内容写成通过。

## 8. 反注册和卸载

真实测试结束后，建议执行反注册和卸载，确认不会残留注册项：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Uninstall-QiTuCDR.ps1 `
  -InstallRoot "__INSTALL_ROOT__" `
  -UnregisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath "__CONFIRMED_MANIFEST_PATH__"
```

## 9. 停止条件

遇到以下情况必须停止，不要继续写注册表或继续测试：

- 注册路径不能确认。
- 预览注册路径和人工确认路径不一致。
- manifest 校验失败。
- CorelDRAW 启动明显异常或崩溃。
- WebView2 反复初始化失败。
- 插件加载后出现多个 WebView2 实例。
- 文档操作出现无法恢复的 COM 异常。
