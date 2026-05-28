# 发布检查清单

此清单用于 V1 之后的打包和交付阶段。当前工程还处于骨架阶段，部分检查项需要等 CorelDRAW SDK 和安装包接入后执行。

## 构建检查

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 -Configuration Release
```

环境诊断：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -FailOnError
```

要求：

- C# 构建 0 error。
- 单元测试全部通过。
- 前端成功输出到 `src/WebUI`。
- 诊断脚本状态为 `OK`。
- 发布包必须来自 `Release` 输出，除非明确标记为调试包。

## 发布包检查

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuPackage.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuPackage.ps1 -FailOnError
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuReleaseInstall.ps1 -FailOnError
```

要求：

- 发布包目录包含 `App/QiTuCDR.Host.dll`。
- 发布包目录包含 `App/WebUI/index.html`。
- 发布包目录包含 `installer/Install-QiTuCDR.ps1` 和 `installer/Uninstall-QiTuCDR.ps1`。
- 发布包目录包含 `installer/Get-QiTuCorelRegistrationPlan.ps1`。
- 发布包目录包含 `installer/Get-QiTuCorelRegistrationPreview.ps1`、`installer/New-QiTuCorelRegistrationManifest.ps1`、`installer/New-QiTuConfirmedCorelRegistrationManifest.ps1` 和 `installer/Test-QiTuCorelRegistrationManifest.ps1`。
- 发布包目录包含 `tools/validation/New-QiTuRealHostExecutionPlan.ps1`、`tools/validation/New-QiTuRealHostValidationRecord.ps1`、`tools/validation/Test-QiTuRealHostReadiness.ps1`、`tools/validation/New-QiTuRealHostAcceptanceKit.ps1`、`tools/validation/New-QiTuRealHostCommandChecklist.ps1`、`tools/validation/Invoke-QiTuCorelDrawComSmoke.ps1`、`tools/validation/Install-QiTuCorelDrawAddon.ps1`、`tools/validation/Test-QiTuCorelDrawAddonLoad.ps1`、`tools/validation/Invoke-QiTuRealHostRegistrationDryRun.ps1` 和 `tools/validation/Test-QiTuRealHostInstallState.ps1`。
- 发布包目录包含 `VERSION`。
- 发布包生成 `package-manifest.json`。
- 发布包生成 `SHA256SUMS.txt`。
- zip 文件可解压，且校验清单与目录内容一致。
- 发布包验证脚本状态为 `OK`。
- `package-manifest.json` 必须包含 `RuntimeSafety`，并声明默认 `DefaultDockHostMode = Debug`。
- `RuntimeSafety.CorelDockerStatus` 必须为 `PlaceholderFallbackRequired`，直到真实 CorelDRAW Docker API 接通并验收。
- `RuntimeSafety.OfficialCorelDockerAdapterDefaultEnabled` 必须为 `false`，直到真实 CorelDRAW Docker API 完成并通过真实宿主验收。
- 发布包必须包含 `docs/STABILITY_TEST_PLAN.md` 和 `docs/CORELDRAW_HOST_BINDING_CHECKLIST.md`，用于说明 `CorelDocker` 安全回退和真实宿主验收口径。
- 发布包必须包含 `docs/REAL_HOST_ACCEPTANCE_QUICKSTART.md`、`docs/REAL_HOST_EXECUTION_PLAN_TEMPLATE.md` 和 `docs/REAL_HOST_COMMAND_CHECKLIST_TEMPLATE.md`，用于说明真实 CorelDRAW 测试机上的最短验收路径。
- 发布包必须包含 `docs/CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md`，用于说明 `AllowOfficialCorelDockerAdapter` 的启用门槛。
- `CORELDRAW_DOCKER_ADAPTER_ENABLEMENT.md` 必须明确 `ActiveDockPanelHostKind = CorelDocker`、`ActiveDockerAdapterType = CorelDockerAdapter`、`IsDockerAdapterAttached = True` 和 `WebViewCreateCount <= 1` 等真实启用条件。
- 发布包必须包含 `docs/CORELDRAW_REGISTRATION_CONFIRMATION_TEMPLATE.md`，用于记录真实注册路径确认依据。
- 发布包必须包含真实宿主验收记录生成脚本，且脚本引用真实验收模板、注册确认模板和注册计划脚本。
- 发布包验证脚本必须实际冒烟运行真实宿主验收记录生成器，确认关键字段可自动预填。
- 发布包验证脚本必须实际冒烟运行真实宿主 readiness 检查器，确认执行计划、验收记录草稿和必备文件检查可生成；WebView2 Runtime 或 CorelDRAW TypeLib 缺失只能让 readiness 状态变为 `BLOCKED`，不能直接证明发布包损坏。
- 发布包验证脚本必须实际冒烟运行真实宿主验收包生成器，确认 readiness、执行计划、验收记录草稿、注册确认草稿和索引文件都能集中生成。
- 真实宿主验收包必须包含命令清单，明确 `CONFIRMED` manifest、注册预览、安装注册和反注册卸载命令。
- 发布包验证脚本必须实际冒烟运行真实宿主注册干跑，确认可生成 `CONFIRMED` manifest、结构化预览报告和 Markdown 干跑报告，且不写入真实注册表。
- 发布安装冒烟必须运行安装后状态核查，确认安装目录、`install-manifest.json` 和受控注册表项一致。
- 发布包验证脚本必须实际冒烟运行注册 manifest 生成器，确认可生成单目标 `CONFIRMED` manifest 并通过 `RequireConfirmed` 校验。
- 发布包验证脚本必须实际冒烟运行注册计划脚本，确认 JSON 包含 `EvidenceSummary` 和 `ManifestFieldChecklist`，Markdown 包含候选路径人工复核说明。
- `package-manifest.json` 的 `Version` 与 `VERSION` 文件一致。
- `App/QiTuCDR.Host.dll` 的产品版本与 `package-manifest.json` 的 `Version` 一致。
- 发布安装冒烟脚本状态为 `OK`。
- 发布安装冒烟会从包内生成 CorelDRAW 注册计划报告。

## 宿主检查

- CorelDRAW 2021-2026 至少选择一个版本完成真实加载。
- 插件启动不明显拖慢 CorelDRAW 启动。
- WebView2 只创建一个实例。
- 面板关闭只隐藏，不销毁 WebView2。
- WebView2 初始化失败时进入 WPF 降级面板。

## COM 检查

- 所有 COM 调用经过 `IComDispatcher`。
- 没有 `Task.Run` 直接调用 COM。
- 没有静态 COM 对象引用。
- 所有临时 COM 对象有 `finally` 释放路径。
- 选区功能使用快照，不依赖执行期间实时选区。

## 功能检查

- 批量转曲支持选区、当前页、全文档。
- 一键居中支持整体居中和独立居中。
- 冗余清理必须二次确认。
- 尺寸规整支持宽高和描边宽度。
- 取消任务可用。
- Busy 状态会拒绝重复业务请求。

## 安装检查

- 安装前执行 `installer/Test-QiTuInstallPrerequisites.ps1`。
- 注册路径确认前执行 `installer/Get-QiTuCorelRegistrationPlan.ps1` 并保存报告。
- 安装脚本可复制 Host 构建产物到安装目录。
- 安装脚本可初始化配置目录和日志目录。
- 安装脚本可生成 `install-manifest.json`。
- 默认卸载可移除 App 和 manifest，同时保留配置与日志。
- 安装包可注册插件，真实注册表路径必须来自目标 CorelDRAW SDK / 版本验证。
- 如使用注册 manifest，manifest 必须通过 `Test-QiTuCorelRegistrationManifest.ps1 -RequireConfirmed -FailOnError`。
- 真实写注册表前，必须先用 `Install-QiTuCDR.ps1 -PreviewCorelDrawRegistration -CorelDrawRegistrationManifestPath ...` 或 `Get-QiTuCorelRegistrationPreview.ps1` 预览将要写入的路径和值。
- 推荐通过 `New-QiTuConfirmedCorelRegistrationManifest.ps1 -CorelVersionIdentifier ... -RegistryPath ...` 生成并校验单目标确认 manifest，减少人工改 JSON 漏字段。
- 安装脚本写入注册表后，`install-manifest.json` 必须记录 `RegisteredCorelDrawAddInEntries`，用于确认和回滚。
- 发布安装冒烟必须覆盖受控 HKCU 注册写入和反注册清理，且不得残留测试注册表键。
- 生产注册必须走 `CONFIRMED` manifest，不允许直接传单个注册表路径。
- manifest 启用项必须包含 `ProductLabel`、`RegistryPath`、`ConfirmationSource`、`ConfirmedBy` 和合法 `ConfirmedAt`。
- manifest 注册只允许启用已确认目标项，默认路径应位于 `HKCU:\Software\Corel\` 或 `HKLM:\Software\Corel\` 下。
- 可检测 WebView2 Runtime。
- WebView2 Runtime 缺失时必须进入 WPF 降级面板。
- CorelDRAW TypeLib 探测结果需要写入日志。
- 日志目录可写。
- 配置目录可写。
- `settings.json` 不存在时可自动创建默认配置。
- `settings.json` 损坏时可备份 `.bad.<timestamp>` 并回退默认配置。
- 卸载后不残留危险注册项。
- 卸载默认保留用户配置和日志，只有显式传入清理开关才删除。
## CorelDRAW Addons 安全检查补充

- 发布包必须包含 `tools/validation/Set-QiTuCorelDrawAddonAutoLoad.ps1`，用于安全启用或禁用 `CorelDrw.addon`。
- CorelDRAW Addons 部署默认不得自动启用，默认标记应为 `CorelDrw.addon.disabled`。
- 只有独立测试环境允许显式使用 `-EnableAutoLoad`。
- 真实宿主测试前必须确认 CorelDRAW 27 没有生产文件正在编辑。
- QiTuCDR 的挂载参考记录见 `docs/CORELDRAW_ADDONS_MOUNT_REFERENCE.md`。
