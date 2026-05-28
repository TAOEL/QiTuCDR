# CorelDRAW Addons 挂载参考与 QiTuCDR 安全策略

本文记录 QiTuCDR 对 `CdrCloudPlugin`、`qiuku`、`Qi-Tu-CD-R-2` 的挂载方式参考结果，以及当前真实宿主测试必须遵守的安全策略。

## 当前结论

CorelDRAW 26 下三个参考插件都采用同一类 Addons 挂载方式：

```text
Addons/<PluginName>/
  CorelDrw.addon
  AppUI.xslt
  UserUI.xslt
  *.dll
```

`AppUI.xslt` 中通过：

- `itemData type="wpfhost"`
- `hostedType="Addons\<目录>\<DLL>,<入口类型>"`
- `commandBarData type="toolbar"`
- `toolbar guidRef="..."`

把 WPF 控件挂到 CorelDRAW 顶部工具栏区域。

## 参考插件挂载点

| 插件目录 | 入口 DLL / 类型 | UI 类型 | 挂载方式 |
| --- | --- | --- | --- |
| `CdrCloudPlugin` | `D76FEFD27BC84FBD90483B30E9BD7230.dll` | `wpfhost` | `commandBarData type="toolbar"` |
| `qiuku` | `qiuku.dll,qiuku.qiuKu` | `wpfhost` | `commandBarData type="toolbar"` |
| `Qi-Tu-CD-R-2` | `QiTuCDR.dll,QiTuCDR.AddonEntry` | `wpfhost` | `commandBarData type="toolbar"` |
| `QiTuCDR` 当前工程 | `QiTuCDR.Host.dll,QiTuCDR.Host.Addons.AddonEntry` | `wpfhost` | `commandBarData type="toolbar"` |

QiTuCDR 的 XSLT 挂载结构与参考插件一致。当前风险不在 XSLT 挂载位置，而在“加载后立即启动生命周期和 WebView2”。

## 已采取的安全策略

QiTuCDR 真实宿主测试默认改为安全模式：

1. 部署脚本默认生成 `CorelDrw.addon.disabled`，不会让 CorelDRAW 自动加载。
2. 只有显式传入 `-EnableAutoLoad` 时才生成 `CorelDrw.addon`。
3. `AddonEntry` 被加载后默认只显示 Safe Shell。
4. Safe Shell 不自动创建 WebView2，不自动执行业务链路。
5. 只有用户在测试文档中手动点击启动按钮，才进入 `PluginLifecycleManager`。
6. 环境变量 `QITUCDR_ENABLE_ADDON_LIFECYCLE=1` 只允许在独立测试机使用，不允许在正在工作的设计环境使用。

## 禁止事项

- 禁止在 CorelDRAW 27 正在设计生产文件时启用 QiTuCDR 自动加载。
- 禁止直接把 27 的 `QiTuCDR\CorelDrw.addon.disabled` 改回 `CorelDrw.addon`。
- 禁止在未保存设计文件时启动真实宿主测试。
- 禁止一次同时启动 26 和 27 做插件挂载测试。

## 安全启用与禁用命令

默认禁用自动加载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Disable `
  -FailOnError
```

只在独立测试环境启用自动加载：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Enable `
  -FailOnError
```

检查当前状态：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Test-QiTuCorelDrawAddonState.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -FailOnError
```

如果工具条已经出现但内容空白，通常表示 CorelDRAW 当前进程仍加载着旧版 DLL。此时不要直接覆盖 Addons 目录。正确处理方式是：

1. 保存并关闭目标 CorelDRAW 版本。
2. 确认该版本 `CorelDRW.exe` 已退出。
3. 再重新部署 QiTuCDR Addons。
4. 重新打开目标 CorelDRAW。

如需强制阻止旧 DLL 下次被缓存配置拉起，可以在关闭目标 CorelDRAW 后使用硬禁用：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\validation\Set-QiTuCorelDrawAddonAutoLoad.ps1 `
  -CorelProgramsDirectory "C:\Program Files\Corel\CorelDRAW Graphics Suite\26\Programs64" `
  -AddonName QiTuCDR `
  -Disable `
  -HardDisable `
  -FailOnError
```

如果目标 CorelDRAW 仍在加载 `QiTuCDR.Host.dll`，该命令会拒绝执行，不会强行改动正在使用的 DLL。

## 下一步

后续真实 CDR 测试必须先确认：

- CorelDRAW 27 已关闭或没有生产文件正在编辑。
- CorelDRAW 26 使用新建空白测试文档。
- `QiTuCDR` 只在 26 启用。
- 测试结束后立即执行禁用命令。
