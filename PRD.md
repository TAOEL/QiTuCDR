# QiTuCDR 产品需求文档

**版本：** V1.0 工程实施版  
**文档性质：** 产品需求 + 架构执行基准  
**目标平台：** CorelDRAW 2021-2026  
**运行环境：** Windows 10 / Windows 11 x64  
**技术栈：** C# + .NET Framework 4.8 + WPF + WebView2 + React  
**架构模式：** Native First + Single WebView + COM Safe  

## 1. 产品定位

QiTuCDR 是面向广告设计、包装设计、图文快印设计师的 CorelDRAW 原生增强插件。它不是 Web 插件，而是以 C# Native Runtime 为核心、WebView2 作为轻量 UI 容器的桌面生产力工具。

核心目标：

- 提升设计师高频操作效率。
- 降低重复机械操作成本。
- 提供轻量现代 UI。
- 保持低内存和低 CPU 占用。
- 保障 CorelDRAW 宿主长期稳定运行。
- 支持 AI 协同开发和长期可维护。
- 实现资源可控、异常可恢复、崩溃不牵连宿主。

最高优先级：

```text
CorelDRAW 宿主稳定性 > COM 安全 > 生命周期可控 > 资源释放 > 性能稳定 > UI 体验
```

## 2. V1.0 产品范围

### 2.1 核心功能

| 功能 | 说明 | 当前状态 |
|------|------|----------|
| 批量转曲 | 按选区、当前页、全文档批量转曲，支持跳过隐藏/锁定对象和分批进度 | 基本完成，待真实大文档验收 |
| 一键居中 | 支持多对象整体居中、多对象独立居中 | 已加固，待真实 CorelDRAW 验收 |
| 冗余清理 | 清理辅助线、隐藏空图层、空文本框等冗余内容 | 基本完成，待真实 CorelDRAW 验收 |
| 尺寸规整 | 批量统一宽高、等比例规整、统一描边宽度 | 已加固，待真实 CorelDRAW 验收 |

### 2.2 基础架构能力

- CorelDRAW 标准 Dock Panel 容器。
- WebView2 单例全局管理。
- JSON 标准化双向通信。
- COM 安全调度中心。
- 插件状态机。
- 生命周期资源管理。
- 本地日志。
- 本地配置持久化。
- WebView2 崩溃或缺失时的 WPF 降级面板。
- 本地 HostHarness 调试宿主。

### 2.3 V1.0 禁止范围

- 云服务、云同步、外网接口。
- 登录、注册、账号绑定。
- 支付、订阅、会员体系。
- 外网 AI 大模型接口。
- 多 WebView2 实例。
- 重型 UI 组件库。
- 复杂动画系统。
- Three.js、WebGL 等重渲染技术。
- 热插拔、微内核、插件市场等过度架构。

## 3. 目标用户与使用场景

### 3.1 目标用户

- 广告设计师。
- 包装设计师。
- 图文快印设计师。
- 需要长期在 CorelDRAW 中进行批量排版、转曲、清理、尺寸整理的生产型用户。

### 3.2 典型场景

- 文件交付前批量转曲，减少字体缺失风险。
- 批量把选中对象居中到页面。
- 快速清理隐藏空图层、辅助线、空文本框等冗余对象。
- 批量统一对象尺寸和描边宽度。

## 4. 核心架构原则

### 4.1 Native First

所有文件修改、Shape 操作、图层遍历、COM 调用、Undo/Redo、文档事务必须由 C# 原生层执行。

Web 层仅允许承担：

- UI 渲染。
- 参数收集。
- 用户事件监听。
- 标准消息发送。

Web 层禁止：

- 直接操作 CorelDRAW。
- 保存 Shape、Document、Layer 等宿主状态。
- 承担业务规则和文档逻辑。

### 4.2 Single WebView

插件生命周期内只允许一个 WebView2 实例。

禁止：

- 多 WebView2 实例。
- 多浏览器 Runtime 进程。
- 页面切换时重建 Web 容器。
- 多独立工具窗口容器。

页面切换必须使用 React Hash Router。

### 4.3 COM Safety First

所有 CorelDRAW COM 对象必须遵守：

```text
Acquire Immediately -> Execute Quickly -> Release Immediately
```

禁止：

- 静态缓存 COM 对象。
- 长生命周期字段持有 COM 引用。
- 跨线程传递 COM 对象。
- 在 `Task.Run` 中直接调用 COM。
- 省略 `finally` 释放逻辑。

### 4.4 Stability Over Visual Effects

任何视觉优化、交互动效、前端体验，都不得牺牲宿主稳定性。

## 5. 系统架构

```text
CorelDRAW Host
  -> VSTA / COM AddIn
  -> WPF Native Shell
     -> Lifecycle Manager
     -> Plugin State Machine
     -> Resource Manager
     -> COM Dispatcher
     -> Bridge Dispatcher
     -> WebView2 Manager
     -> Recovery Manager
  -> Single WebView2
  -> React SPA
```

分层职责：

| 层级 | 职责 |
|------|------|
| Host Layer | 插件入口、宿主注册、生命周期托管 |
| WPF Layer | Dock 面板、DPI、窗口焦点、原生容器 |
| Core Layer | 业务逻辑、参数校验、任务调度、事务封装 |
| COM Layer | COM 安全调用、STA 调度、立即释放 |
| Bridge Layer | 通信协议、DTO 序列化、消息分发、事件转发 |
| Web Layer | UI 渲染、操作入口、结果展示、状态反馈 |

## 6. 插件状态机

状态定义：

```csharp
public enum PluginState
{
    Starting,
    Preheating,
    Ready,
    Busy,
    Recovering,
    Faulted,
    Disposing,
    Disposed
}
```

标准流转：

```text
Starting -> Preheating -> Ready -> Busy -> Ready -> Disposing -> Disposed
任意状态 -> Faulted -> Recovering -> Ready
```

业务命令只能在 `Ready` 状态进入执行。执行时进入 `Busy`，完成后回到 `Ready`。

## 7. 通信协议

### 7.1 Request

```json
{
  "version": "1.0",
  "requestId": "uuid",
  "action": "convertText",
  "payload": {}
}
```

### 7.2 Response

```json
{
  "version": "1.0",
  "requestId": "uuid",
  "success": true,
  "errorCode": null,
  "message": "",
  "payload": {}
}
```

### 7.3 Event

```json
{
  "event": "task.progress",
  "timestamp": 1716600000,
  "payload": {}
}
```

首批错误码：

| 错误码 | 描述 |
|--------|------|
| `NO_DOCUMENT` | 当前无打开文档 |
| `EMPTY_SELECTION` | 未选中任何对象 |
| `INVALID_PAYLOAD` | 参数非法 |
| `WEBVIEW_NOT_READY` | WebView2 未就绪 |
| `COM_EXCEPTION` | COM 调用异常 |
| `TASK_CANCELLED` | 任务取消或超时 |
| `STATE_FORBIDDEN` | 当前状态不允许执行 |

## 8. 核心功能需求

### 8.1 批量转曲

输入参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `range` | enum | `Selection`、`CurrentPage`、`Document` |
| `includeHidden` | bool | 是否包含隐藏对象 |

执行要求：

- 业务链路必须为 `Page -> DTO -> Command -> Validator -> Service -> Adapter -> ComDispatcher -> Event`。
- 选区范围必须先生成 `SelectionSnapshot.ShapeIds`。
- 执行前必须基于 ShapeId 重新解析对象。
- 批量处理默认 `BatchSize = 50`。
- 锁定对象、隐藏对象和异常对象自动跳过。
- 发布 `task.progress`、`task.completed`、`task.failed`。
- 支持取消任务。
- COM 操作包裹文档命令组事务壳。

### 8.2 一键居中

支持模式：

- 多对象整体居中。
- 多对象独立居中。

执行要求：

- 必须基于选区快照执行。
- 必须验证模式合法性。
- 必须支持取消。
- 必须发布完成/失败事件。
- 必须兼容自定义页面尺寸。

### 8.3 冗余清理

支持清理范围：

- 页面辅助线。
- 隐藏空白图层。
- 空文本框或无效文本对象。

执行要求：

- 执行前必须二次确认。
- 必须返回清理数量。
- 必须发布完成/失败事件。
- 错误不得冒泡到 CorelDRAW 宿主。

### 8.4 尺寸规整

支持能力：

- 自定义宽度。
- 自定义高度。
- 等比例锁定。
- 统一描边宽度。
- 允许只统一描边宽度。

执行要求：

- 宽高必须为正数。
- 描边宽度不能为负数。
- 必须基于选区快照执行。
- 必须释放 Outline 等临时 COM 对象。
- 必须支持取消和终态事件。

## 9. WebView2 预热与恢复

禁止在 CorelDRAW 启动瞬间同步初始化 WebView2。

正确策略：

- 宿主启动完成后延迟 4 秒预热，或监听宿主空闲状态。
- 调用 `EnsureCoreWebView2Async()`。
- 初始化失败或 WebView2 Runtime 缺失时进入恢复流程。
- 恢复流程切换到 WPF 原生降级面板。

## 10. 性能预算

| 指标 | 阈值 |
|------|------|
| 插件冷启动 | <= 0.2s |
| 首次面板加载 | <= 1.5s |
| 二次面板打开 | <= 0.1s |
| 静默常驻内存 | <= 300MB |
| 高批量任务内存 | <= 600MB |
| 常规功能响应 | <= 1s |
| 24 小时内存涨幅 | <= 50MB |

## 11. 测试策略

### 11.1 单元测试

- DTO 序列化。
- Validator 参数规则。
- BridgeDispatcher 路由。
- PluginStateMachine 状态流转。
- 工具服务边界。
- 任务取消与终态事件。

### 11.2 集成测试

- Web 与 Native 双向通信。
- WebView2 初始化失败降级。
- COM Dispatcher 安全调用。
- 状态 Busy 时重复请求拦截。
- 取消任务。

### 11.3 压力测试

- 5000+ Shape 批量处理。
- 100 次面板开关。
- 24 小时挂起运行。
- 重复执行同一功能。

### 11.4 恢复测试

- WebView2 Runtime 缺失。
- WebView2 渲染进程崩溃。
- JS 异常。
- 文档执行中关闭。
- COM 非法调用异常。

## 12. 里程碑

详细排期见 [docs/MILESTONES.md](docs/MILESTONES.md)。

简要状态：

| 阶段 | 状态 |
|------|------|
| M1 | 已完成 |
| M2 | 已完成 |
| M3 | 已完成 |
| M4 | 已完成 |
| M5 | 已完成 |
| M6 | 基本完成 |
| M7 | 推进中 |
| M8 | 待开始 |
| M9 | 待开始 |

## 13. 不可逆红线

- 严禁多个 WebView2 实例。
- 严禁 JS / React 直接操作 COM。
- 严禁长期持有 COM 对象引用。
- 严禁 `Task.Run` 直接操作 COM。
- 严禁 COM 对象不释放。
- 严禁引入重型 UI 库和冗余第三方依赖。
- 严禁绕过 `PluginStateMachine`、`ComDispatcher`、`BridgeDispatcher`。
- 严禁 Web 层持有宿主文档、图层、Shape 业务状态。

## 14. 当前实现状态

截至 2026-05-25：

- M1-M5 已完成。
- M6 批量转曲基本完成，等待真实 CorelDRAW 大文档验收。
- M7 中一键居中和尺寸规整已加固。
- M7 中冗余清理仍需补真实清理能力和二次确认闭环。
- M8/M9 尚未开始。

## 15. 文档关系

- `PRD.md`：产品需求主文档和范围基准。
- `docs/ARCHITECTURE.md`：工程架构细节。
- `docs/MILESTONES.md`：M1-M9 排期和状态。
- `docs/COM_SAFETY.md`：COM 安全规范。
- `docs/MESSAGE_PROTOCOL.md`：通信协议。
- `docs/DEVELOPMENT.md`：开发执行说明。
- `docs/TODOS.md`：具体待办和完成记录。
