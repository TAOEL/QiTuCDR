# build

此目录用于放置构建脚本、公共 MSBuild props、打包前处理和 WebUI 复制流程。

当前保留目录结构，后续接入安装包或 CI 时再补具体脚本。

## 当前脚本

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1
```

默认执行：

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- `npm install`
- `npm run build`

Release 构建：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 -Configuration Release
```

## 可选 typed Interop 构建

在本机已经生成 `artifacts/coreldraw-interop/v27` 后，可以验证 Host typed adapter：

```powershell
powershell -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuBuild.ps1 -SkipWeb -EnableCorelDrawInterop -CorelDrawInteropDirectory "C:\Users\Administrator\Desktop\DEo\codex\CDRWFP\artifacts\coreldraw-interop\v27"
```

该模式不应成为默认 CI 路径，除非 CI 机器明确准备了合法的 CorelDRAW Interop 产物。

不要和默认构建并行运行该命令；两条路径会写同一套 WPF `obj` 中间目录，可能干扰 XAML 标记编译。需要同时验证时按顺序执行。

## 发布包生成

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuPackage.ps1
```

默认从 `src/Host/bin/Release/net48` 读取 Host 输出，生成：

- `artifacts/release/qitucdr-v<version>-<timestamp>/`
- `artifacts/release/qitucdr-v<version>-<timestamp>.zip`
- `package-manifest.json`
- `SHA256SUMS.txt`

仅生成目录，不压缩：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuPackage.ps1 -NoZip
```

指定版本号：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuPackage.ps1 -Version 0.1.0
```

未显式传入 `-Version` 时，脚本会读取仓库根目录的 `VERSION` 文件。

从 Debug 输出打包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Invoke-QiTuPackage.ps1 -Configuration Debug
```

## 发布包验证

验证最新 zip 或发布目录：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuPackage.ps1 -FailOnError
```

验证指定 zip：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuPackage.ps1 -PackagePath artifacts\release\qitucdr-v0.1.0-20260525-174642.zip -FailOnError
```

验证内容包括：

- 必需文件是否存在。
- `package-manifest.json` 是否可解析。
- `VERSION` 是否存在，且与 manifest 版本一致。
- `SHA256SUMS.txt` 是否可解析。
- 校验清单中的文件是否存在且 hash 一致。
- zip 是否可解压并通过同一套校验。

## 发布安装冒烟

从最新 zip 解压、安装到临时目录、检查安装结果、执行默认卸载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuReleaseInstall.ps1 -FailOnError
```

指定发布包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build/scripts/Test-QiTuReleaseInstall.ps1 -PackagePath artifacts\release\qitucdr-v0.1.0-20260525-174642.zip -FailOnError
```

验证内容包括：

- zip 发布包能通过 `Test-QiTuPackage.ps1`。
- 包内注册计划脚本能生成 JSON 和 Markdown 报告。
- 包内安装脚本可以从 `App/` 安装。
- 安装后存在 Host DLL、WebUI、配置文件、日志目录和安装 manifest。
- 默认卸载会移除 App 和 manifest，并保留用户配置与日志。
