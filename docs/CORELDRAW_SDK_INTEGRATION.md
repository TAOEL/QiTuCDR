# CorelDRAW SDK 接入说明

当前工程已经发现本机存在 CorelDRAW TypeLib：

```text
C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64\TypeLibs\CorelDRAW.tlb
C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Programs64\TypeLibs\CorelDRAW.tlb
```

这些文件属于本机 CorelDRAW 安装，不应直接提交到仓库。

## 接入目标

下一阶段要把当前 `dynamic` COM 调用收敛成类型化边界，但仍然保持：

- Web 层不感知 CorelDRAW。
- Bridge DTO 不暴露 COM 类型。
- Core 层只通过受控 adapter 或 host context 访问宿主。
- 所有 COM 操作仍必须经过 `IComDispatcher`。

## 推荐路线

1. 用 CorelDRAW TypeLib 生成本地 Interop 程序集。
2. 将 Interop 引用限制在 `src/Host` 或专门的 adapter 项目中。
3. 以现有 `ICorelDocumentAdapter` 为第一层稳定契约，必要时再拆分 `ICorelApplicationAdapter`、`ICorelSelectionAdapter`。
4. Core 工具服务依赖 adapter 接口，不直接散落 Interop 类型。
5. 为 adapter 增加 fake 实现，方便单元测试。

## 本地 Interop 生成

仓库提供本地生成脚本：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1
```

预演生成计划：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -WhatIf
```

当前本机验证结果：

```text
artifacts/coreldraw-interop/v27/CorelDRAW27.Interop.dll
artifacts/coreldraw-interop/v27/VGCore.dll
```

这些文件只允许作为本地开发产物。`artifacts/` 已被 `.gitignore` 忽略，不能提交 CorelDRAW TypeLib、私有 SDK 或生成的 Interop DLL。

## 可选 typed adapter 编译

`src/Host/Environment/TypedCorelDocumentAdapter.cs` 是 typed Interop adapter 骨架，默认不会参与主构建。只有显式开启 MSBuild 属性时才会编译：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 `
  -SkipWeb `
  -EnableCorelDrawInterop `
  -CorelDrawInteropDirectory "C:\Users\Administrator\Desktop\DEo\codex\CDRWFP\artifacts\coreldraw-interop\v27"
```

等价的 `dotnet build`：

```powershell
dotnet build src/Host/QiTuCDR.Host.csproj `
  /p:EnableCorelDrawInterop=true `
  /p:CorelDrawInteropDirectory="C:\Users\Administrator\Desktop\DEo\codex\CDRWFP\artifacts\coreldraw-interop\v27"
```

默认主构建仍然不引用 `VGCore.dll`，确保没有 CorelDRAW SDK 的机器也能编译工程骨架。

adapter 选择由 `CorelDocumentAdapterFactory` 负责，并采用“编译开关 + 配置开关”双重确认：

- 默认构建：创建 `DynamicCorelDocumentAdapter`。
- typed 构建 + `PreferTypedCorelInterop = true`：优先尝试 `TypedCorelDocumentAdapter`。
- typed 创建失败：记录日志，回退 `DynamicCorelDocumentAdapter`。

生命周期层只依赖 `ICorelDocumentAdapterFactory` 和 `ICorelDocumentAdapter`，不直接持有 typed/dynamic 选择逻辑。

运行时配置示例：

```json
{
  "webViewPreheatDelayMs": 4000,
  "batchSize": 50,
  "taskTimeoutMs": 120000,
  "preferTypedCorelInterop": true
}
```

该配置只在 typed Interop 已编译进 Host 时生效。默认值是 `false`，用于保护早期真实宿主验证的稳定性。

可以用配置工具切换：

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -EnableTypedInterop
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -DisableTypedInterop
```

## 当前 Adapter 状态

- `src/Core/Host/ICorelDocumentAdapter.cs` 定义 Core 层可见的文档操作契约。
- `src/Host/Environment/DynamicCorelDocumentAdapter.cs` 是当前临时实现，内部仍使用 `dynamic`，但调用已集中在 Host 层。
- `src/Host/Environment/TypedCorelDocumentAdapter.cs` 是 typed Interop 骨架，默认排除，仅用于本地显式编译验证。
- `src/Host/Environment/CorelDocumentAdapterFactory.cs` 负责 adapter 创建和 typed 回退 dynamic。
- Core 工具服务已不再直接访问 `ActiveSelectionRange`、`ActivePage`、`ActiveDocument`。
- 已通过 CorelDRAW 27 TypeLib 探测确认 `Shapes.FindShape(String Name, cdrShapeType Type, Int32 StaticID, Boolean Recursive, String Query)` 可作为 `SelectionSnapshot.ShapeIds` 回填解析路径。
- `DynamicCorelDocumentAdapter` 对选区范围优先通过快照中的 `StaticID` 重新创建 `ShapeRange`，只有缺少快照时才退回实时 `ActiveSelectionRange`。
- 后续 typed Interop 实现应替换 `DynamicCorelDocumentAdapter`，而不是改动每个工具服务。

## 不推荐路线

- 不要把 `dynamic` 调用继续扩散到所有服务。
- 不要把生成的巨大 Interop 文件手工复制进业务目录。
- 不要让 React payload 携带 ShapeId 之外的宿主对象细节。
- 不要为了调试方便绕过 `ComDispatcher`。

## 诊断命令

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1
```

输出 JSON：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -Json
```

安装包或宿主集成前置检查可以使用：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -FailOnError
```

当前诊断会检查 TypeLib、WebView2 Runtime、WebUI 构建产物、本地配置目录、本地日志目录和 `settings.json` 合法性。`settings.json` 不存在是允许状态，因为插件首次启动会创建默认配置。
诊断同时会报告 `TlbImp.exe` 是否可用；它是开发机生成本地 Interop 的工具，不是终端用户运行插件的必需依赖。

## AddIn 注册路径探测

仓库提供只读注册计划脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Get-QiTuCorelRegistrationPlan.ps1
```

该脚本会：

- 探测本机 `CorelDRAW.tlb`。
- 推导 CorelDRAW 版本和 `Programs64` / `Programs` 目录。
- 汇总目标 CorelDRAW 版本标识覆盖情况，默认检查 `23`、`24`、`25`、`26`、`27`。
- 只读扫描 Corel 相关注册表候选项。
- 生成 JSON 与 Markdown 报告到 `artifacts/registration/`。

如需调整目标版本标识，可传入 `-TargetCorelVersions`：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Get-QiTuCorelRegistrationPlan.ps1 `
  -TargetCorelVersions 26,27
```

该脚本不会写注册表，也不会把候选项视为最终路径。确认官方 AddIn / Docker 注册机制后，必须把明确路径写入 `CONFIRMED` manifest，再交给安装脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

## 注册 Manifest

为了避免把确认过的注册路径写死在安装脚本中，仓库提供注册 manifest 模板：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuCorelRegistrationManifest.ps1
```

默认输出到：

```text
artifacts\registration\qitucdr-coreldraw-registration-manifest.template.json
```

确认真实 CorelDRAW AddIn / Docker 注册路径后，优先用确认脚本生成单目标 `CONFIRMED` manifest，避免手工编辑 JSON 漏字段：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\New-QiTuConfirmedCorelRegistrationManifest.ps1 `
  -CorelVersionIdentifier 27 `
  -RegistrationKind AddIn `
  -RegistryPath "HKCU:\Software\Corel\..." `
  -ConfirmationSource "artifacts\validation\qitucdr-registration-confirmation-....md"
```

发布前也可以单独执行校验：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Test-QiTuCorelRegistrationManifest.ps1 `
  -ManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json `
  -RequireConfirmed `
  -FailOnError
```

真实写入注册表前，先执行预览，确认路径和值无误：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -PreviewCorelDrawRegistration `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

安装脚本可从已确认 manifest 批量写入多个目标版本路径：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File installer\Install-QiTuCDR.ps1 `
  -RegisterCorelDrawAddIn `
  -CorelDrawRegistrationManifestPath artifacts\registration\qitucdr-coreldraw-registration-manifest.confirmed.json
```

该机制只解决“已确认路径如何进入安装流程”的问题，不替代官方注册机制确认。

## 待确认事项

- CorelDRAW 26 与 27 的 TypeLib 是否能共用同一套 wrapper。
- CorelDRAW 26 与 27 的 `FindShape(... StaticID ...)` 行为是否完全一致。
- 原生 Docker 注册方式是否随 CorelDRAW 版本变化。
- 安装包是否需要同时支持多版本 CorelDRAW 注册。

真实宿主事件、Docker 面板和验收入口请同步参考：

```text
docs/CORELDRAW_HOST_BINDING_CHECKLIST.md
```
