# SDK 工具

此目录用于放置 CorelDRAW SDK / TypeLib / Interop 相关的本地辅助脚本。

## 生成本地 Interop

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1
```

脚本默认行为：

- 自动查找本机最高版本 `CorelDRAW.tlb`。
- 自动查找 .NET Framework SDK 中的 `TlbImp.exe`，优先使用 NETFX 4.8 x64 版本。
- 输出到 `artifacts/coreldraw-interop/v<version>/`。
- 同目录可能生成 `VGCore.dll` 等 TypeLib 依赖 Interop 程序集。
- `artifacts/` 已被 `.gitignore` 忽略，生成物不得提交进仓库。

预演命令：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -WhatIf
```

指定 TypeLib：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -TypeLibPath "C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Programs64\TypeLibs\CorelDRAW.tlb"
```

覆盖已生成产物：

```powershell
powershell -ExecutionPolicy Bypass -File tools/sdk/New-CorelDrawInterop.ps1 -Force
```

## 规则

- Interop 程序集只允许作为本地开发产物。
- 不要把 CorelDRAW 私有 SDK、TypeLib 或生成 DLL 提交到仓库。
- 后续 typed adapter 可以引用本地产物进行验证，但生产打包方案需要单独确认授权和分发策略。
- typed adapter 即使已编译进 Host，也需要 `settings.json` 中的 `preferTypedCorelInterop` 为 `true` 才会在运行时优先尝试。

## 当前本机验证

已验证可从 CorelDRAW 27 TypeLib 生成：

```text
artifacts/coreldraw-interop/v27/CorelDRAW27.Interop.dll
artifacts/coreldraw-interop/v27/VGCore.dll
```
