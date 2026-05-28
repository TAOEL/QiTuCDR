# 配置工具

此目录用于管理 QiTuCDR 本地配置。配置文件默认位于：

```text
%LOCALAPPDATA%\QiTuCDR\Config\settings.json
```

## 查看或创建默认配置

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1
```

## 开启 typed Interop 优先尝试

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -EnableTypedInterop
```

## 关闭 typed Interop 优先尝试

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -DisableTypedInterop
```

## 设置 Dock 宿主模式

默认使用本地调试窗口：

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -DockHostMode Debug
```

预留真实 CorelDRAW Docker 模式：

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -DockHostMode CorelDocker
```

注意：`CorelDocker` 当前只是接入占位。真实 CorelDRAW Docker API 未确认前，插件会记录错误并回退调试宿主，不代表真实 Docker 面板已完成。

## 输出 JSON

```powershell
powershell -ExecutionPolicy Bypass -File tools/config/Set-QiTuConfig.ps1 -Json
```

## 规则

- 工具会在配置不存在时创建默认配置。
- 如果现有 `settings.json` 损坏，会先备份为 `.bad.<timestamp>`，再写入默认配置。
- typed adapter 仍需要 Host 使用 `EnableCorelDrawInterop=true` 编译；配置开关只决定运行时是否优先尝试 typed adapter。
- `DockHostMode` 默认保持 `Debug`，小白调试和本地预览不要切到 `CorelDocker`。
