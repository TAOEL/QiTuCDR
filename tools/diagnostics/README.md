# diagnostics

此目录用于放置本地诊断工具，例如：

- WebView2 Runtime 检测。
- 日志收集。
- 插件注册状态检查。
- CorelDRAW 版本探测。

当前仅预留目录。

## 当前脚本

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1
```

该脚本会检测：

- `dotnet`
- `node`
- `npm`
- .NET Framework 4.8 targeting pack
- CorelDRAW 安装目录
- CorelDRAW TypeLib
- WebView2 Runtime
- `web/package.json`
- `src/WebUI/index.html`
- `%LOCALAPPDATA%\QiTuCDR\Config` 是否可写
- `%LOCALAPPDATA%\QiTuCDR\Logs` 是否可写
- `settings.json` 是否存在且为合法 JSON

输出 JSON：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -Json
```

用于 CI、安装包预检或发布门禁时，可以开启失败退出码：

```powershell
powershell -ExecutionPolicy Bypass -File tools/diagnostics/Test-QiTuEnvironment.ps1 -FailOnError
```

`settings.json` 尚未创建不视为失败；插件首次启动或配置服务加载时会自动创建默认配置。
如果 `settings.json` 已存在且合法，诊断报告会显示 `PreferTypedCorelInterop` 和 `DockHostMode` 当前值。
