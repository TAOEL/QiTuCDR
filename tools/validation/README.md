# 真实宿主验收工具

本目录用于生成真实 CorelDRAW 宿主验收记录草稿。脚本只生成 Markdown 记录和只读注册计划报告，不写注册表，不启动 CorelDRAW。

脚本会自动预填测试日期、当前用户、机器名、Windows 版本、WebView2 Runtime 版本、QiTuCDR 版本、注册确认记录路径、只读注册计划报告路径和默认 Docker adapter 门禁状态。其余需要真实人工验收的字段仍保持空白或标记为等待真实宿主快照，避免把未验证内容写成已完成结论。

脚本会只读 `%LOCALAPPDATA%\QiTuCDR\Config\settings.json`，把当前 `DockHostMode` 和 `AllowOfficialCorelDockerAdapter` 写入验收记录。配置文件不存在或损坏时会使用安全默认值：`DockHostMode = Debug`、`AllowOfficialCorelDockerAdapter = False`。脚本不会修改配置文件。

## 生成验收记录

如果只是想最快进入真实 CorelDRAW 人工测试，可以先生成一个集中验收包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostAcceptanceKit.ps1
```

该脚本会自动查找最新发布 zip，并集中输出：

- readiness 报告
- 真实宿主执行计划
- 真实宿主命令清单
- 真实宿主验收记录草稿
- 注册确认记录草稿
- `README.md` 索引文件

默认输出到：

```text
artifacts\validation\real-host-acceptance-kit-*
```

如果 CorelDRAW 已经打开，可以先做只读 COM 连接烟测：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Invoke-QiTuCorelDrawComSmoke.ps1 -FailOnError
```

该烟测只读取 CorelDRAW 名称、版本、可见状态和进程信息，不修改文档。

如果目标是 CorelDRAW 26 Addons 目录测试，可以部署到 26 的独立 Addons 目录，不影响 CorelDRAW 27：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Install-QiTuCorelDrawAddon.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Force
```

启动 CorelDRAW 26 后，检查 QiTuCDR 模块是否已经被 26 进程加载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuCorelDrawAddonLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -FailOnError
```

如果需要一次性汇总 CorelDRAW 26 Addons 状态，包括进程、Addon 文件、XSLT、HostedType 反射、模块加载、WebView2 子进程和 AddonEntry 日志，可以运行：
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuCorelDrawAddonState.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -FailOnError
```

默认禁用或显式启用 QiTuCDR Addons 自动加载时，使用专用开关脚本，避免手工改错文件名：
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Disable `
  -FailOnError
```

如果目标 CorelDRAW 已关闭，并且需要防止缓存 UI 配置继续拉起旧 DLL，可以增加 `-HardDisable`。目标 CorelDRAW 仍在运行并加载 QiTuCDR 时，该命令会拒绝执行：
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Disable `
  -HardDisable `
  -FailOnError
```

如果已经有验收包，只想重新生成命令清单：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostCommandChecklist.ps1
```

确认真实注册路径后，推荐先执行注册干跑。干跑会生成 `CONFIRMED` manifest，并执行结构化注册预览和安装脚本预览，但不会写注册表：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Invoke-QiTuRealHostRegistrationDryRun.ps1 `
  -CorelVersionIdentifier 27 `
  -RegistryPath "HKCU:\Software\Corel\..."
```

真实安装注册完成后，可以执行安装后核查。它不会启动 CorelDRAW，只检查安装目录、`install-manifest.json` 和注册表项是否一致：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuRealHostInstallState.ps1 `
  -CorelDrawRegistrationManifestPath "artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed-27.json"
```

真实验收前，建议先运行 readiness 检查，确认当前仓库或发布包已经具备人工验收所需的脚本、模板和运行时线索：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuRealHostReadiness.ps1
```

如果要检查某个发布 zip：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuRealHostReadiness.ps1 `
  -PackagePath artifacts\release\qitucdr-v0.1.0-xxxx.zip
```

readiness 检查只读执行，不启动 CorelDRAW，不写注册表。状态为 `READY_FOR_MANUAL_HOST_VALIDATION` 表示可以进入人工宿主验收；状态为 `BLOCKED` 表示还缺少文件、WebView2 Runtime、CorelDRAW TypeLib 或生成器冒烟输出，需要先处理报告中的阻断项。

建议真实验收前先生成执行计划：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostExecutionPlan.ps1 `
  -CorelDrawVersion "CorelDRAW 2026" `
  -CorelVersionIdentifier 27
```

该计划只生成 Markdown 执行清单，不写注册表，不启动 CorelDRAW。

然后生成验收记录草稿：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostValidationRecord.ps1 `
  -CorelDrawVersion "CorelDRAW 2026" `
  -CorelVersionIdentifier 27
```

默认输出到：

```text
artifacts\validation\
```

输出内容：

- `qitucdr-real-host-execution-plan-*.md`
- `qitucdr-real-host-validation-*.md`
- `qitucdr-registration-confirmation-*.md`
- `registration-plan-*\qitucdr-coreldraw-registration-plan-*.json`
- `registration-plan-*\qitucdr-coreldraw-registration-plan-*.md`

## 带真实宿主快照生成

真实 CorelDRAW 宿主内跑出稳定性快照后，可以把快照字段传给脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\New-QiTuRealHostValidationRecord.ps1 `
  -CorelDrawVersion "CorelDRAW 2026" `
  -CorelVersionIdentifier 27 `
  -DockHostMode CorelDocker `
  -AllowOfficialCorelDockerAdapter `
  -ActiveDockPanelHostKind CorelDocker `
  -ActiveDockerAdapterType CorelDockerAdapter `
  -IsDockerAdapterAttached True `
  -WebViewCreateCount 1
```

如果只是本地调试或尚未完成真实 Docker 验收，不要传 `-AllowOfficialCorelDockerAdapter`，也不要把快照字段手动写成通过。

## 使用规则

- 真实写入注册表前，必须先完成注册确认记录。
- `qitucdr-registration-confirmation-*.md` 中的 manifest 字段需要人工确认后再填写到 `CONFIRMED` manifest。
- 该脚本不会证明注册路径正确，只负责生成记录草稿和只读证据入口。
- 生成后的验收记录只是草稿，不代表真实 CorelDRAW 宿主验收已经通过。
- `AllowOfficialCorelDockerAdapter` 默认预填为本机配置值，安全默认值为 `False`。只有真实 Docker API 和真实宿主验收全部完成后，才允许传入 `-AllowOfficialCorelDockerAdapter`。
