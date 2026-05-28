# 通信协议

Web 与 Native 之间只通过 JSON DTO 通信。Web 层不得传递或保存 COM 对象。

## RequestDto

```json
{
  "version": "1.0",
  "requestId": "uuid",
  "action": "convertText",
  "payload": {}
}
```

字段说明：

- `version`：协议版本，当前固定为 `1.0`。
- `requestId`：请求唯一 ID，用于匹配响应。
- `action`：命令名，对应 `src/Shared/Actions.cs`。
- `payload`：JSON 参数对象。

Native 端会把缺失的 `payload` 归一化为空对象。非法 JSON、空请求体或无法反序列化的请求必须返回 `INVALID_PAYLOAD`，不得让异常冒泡到 CorelDRAW 宿主。

## ResponseDto

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

失败响应必须提供 `errorCode` 和可读 `message`。

## EventDto

```json
{
  "event": "task.progress",
  "timestamp": 1716600000,
  "payload": {}
}
```

事件用于任务进度、状态变更、恢复流程和宿主事件同步。

## 标准错误码

- `NO_DOCUMENT`：当前没有打开文档。
- `EMPTY_SELECTION`：当前没有有效选区。
- `INVALID_PAYLOAD`：参数非法。
- `WEBVIEW_NOT_READY`：WebView2 或原生桥接不可用。
- `COM_EXCEPTION`：COM 调用异常。
- `TASK_CANCELLED`：任务被取消或超时。
- `STATE_FORBIDDEN`：当前插件状态不允许执行业务命令。

## 容错规则

- WebView2 收到的消息先经过 `BridgeJsonSerializer.TryDeserializeRequest`。
- JSON 解析失败时，Native 返回标准失败响应，不进入 `BridgeDispatcher`，也不创建业务任务 token。
- `PluginLifecycleManager` 的 Web 消息入口必须捕获所有异常；兜底响应发送失败也只写日志。
- 任何坏消息都不能触发第二个 WebView2、不能改变当前文档、不能绕过状态机。

## 当前 Action

- `echo`
- `getState`
- `convertText`
- `centerObjects`
- `cleanupRedundant`
- `normalizeSize`
- `cancelCurrentTask`
