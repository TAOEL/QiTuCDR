# COM 安全规范

CorelDRAW COM 是 QiTuCDR 的最高风险边界。任何功能都必须优先保护宿主稳定性。

## 基本原则

```text
Acquire Immediately -> Execute Quickly -> Release Immediately
```

含义：

- 需要时才获取 COM 对象。
- 获取后尽快完成操作。
- 操作结束必须释放。
- 不跨线程传递 COM 对象。
- 不在长期字段或静态字段里保存 COM 对象。

## 禁止项

- 禁止 React 或 JavaScript 直接操作 COM。
- 禁止 `Task.Run` 中直接调用 CorelDRAW COM API。
- 禁止后台线程读写 Shape、Document、Layer。
- 禁止缓存 `ActiveDocument`、`ActivePage`、`Shape` 等对象。
- 禁止绕过 `IComDispatcher`。
- 禁止省略 `finally` 释放逻辑。

## 标准释放形式

```csharp
dynamic? obj = null;

try
{
    obj = app.ActiveDocument;
    // 快速执行 COM 操作。
}
finally
{
    if (obj != null && Marshal.IsComObject(obj))
    {
        Marshal.ReleaseComObject(obj);
    }
}
```

## 集合枚举规则

枚举 CorelDRAW `Shapes`、`ShapeRange.Shapes` 或类似 COM 集合时，集合对象和每一个临时 Shape 对象都必须释放：

```csharp
dynamic? shapes = null;

try
{
    shapes = range.Shapes;
    foreach (object shapeObject in shapes)
    {
        try
        {
            dynamic shape = shapeObject;
            // 快速读取或修改 Shape。
        }
        finally
        {
            ReleaseComObject(shapeObject);
        }
    }
}
finally
{
    ReleaseComObject(shapes);
}
```

Host 层当前通过 `DynamicCorelDocumentAdapter.ForEachShape` 集中处理这一规则。新增 typed Interop adapter 时也必须保留等价释放路径。

## 线程规则

- Worker Thread 只允许执行 DTO 解析、参数校验、纯计算、日志预处理。
- STA/UI Thread 才允许执行 Shape 操作、文档修改、图层遍历、Undo/Redo 和其他 COM 调用。
- `Host.COM.ComDispatcher` 是当前唯一具体 COM 调度器。

## 选区规则

任何依赖选区的功能，任务开始前必须生成 `SelectionSnapshot`。任务执行期间不得把实时 `ActiveSelectionRange` 当作长期事实来源。

当前 `DynamicCorelDocumentAdapter` 已优先通过快照中的 `StaticID` 重新创建 `ShapeRange`。后续 typed Interop adapter 需要保留同样行为。
