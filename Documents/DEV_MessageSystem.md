# 消息系统开发文档（Message System）

版本：1.0.0  
适用范围：运行时与编辑器（监控仅在编辑器）

---

## 功能概述
- **类型安全**：通过 `UnityAction<T>` 与 `MessageData<T>` 实现泛型事件，编译期保证类型一致性。
- **解耦通信**：发送端与接收端无需相互引用，仅依赖消息键 `string key` 与数据类型 `T`。
- **多监听者**：同一消息键允许多个监听者；按注册顺序依次调用。
- **可监控**：运行时 `Send` 会通过 `MessageMonitorProxy` 记录到编辑器监控器窗口（不影响打包版本）。
- **统一管理**：`MessageManager` 提供注册、发送、移除、清空、统计等 API。

---

## 系统描述
- `MessageManager` 采用 `Singleton<MessageManager>`（非 MonoBehaviour）作为核心调度中心，内部以 `Dictionary<string, IMessageData>` 维护所有消息键与其数据容器。
- `MessageData<T>` 实现 `IMessageData`，保存 `UnityAction<T> MessageEvents` 委托链。
- `Register<T>(key, action)`：为 `key` 注册一个类型为 `T` 的监听者；若 `key` 首次出现则创建 `MessageData<T>`。
- `Remove<T>(key, action)`：移除监听者；若移除后无监听者，不强制删除字典项（可按需扩展清理策略）。
- `Send<T>(key, data)`：查找 `key` 对应的 `MessageData<T>` 并触发事件；随后调用 `MessageMonitorProxy.RecordMessage` 写入监控。
- `GetListenerCount<T>(key)`：返回给定 `key` 下的监听者数量。
- `Clear()`：清空内部 `Dictionary`。

> 注意：消息键与数据类型 `T` 必须匹配。若 `Send<T>` 的 `T` 与注册时的 `T` 不一致，则不会触发事件。

---

## 关键接口（API 摘要）
- `MessageManager.Instance.Register<T>(string key, UnityAction<T> action)`
- `MessageManager.Instance.Remove<T>(string key, UnityAction<T> action)`
- `MessageManager.Instance.Send<T>(string key, T data)`
- `MessageManager.Instance.GetListenerCount<T>(string key) : int`
- `MessageManager.Instance.Clear()`

> 常量键统一在 `MessageDefine` 中维护。

---

## 数据结构（ER 图）
```mermaid
erDiagram
    MESSAGE_MANAGER ||--o{ MESSAGE_STORE : contains
    MESSAGE_STORE ||--o{ MESSAGE_DATA : holds
    MESSAGE_DATA ||--o{ LISTENER : subscribes

    MESSAGE_MANAGER {
        string name
    }
    MESSAGE_STORE {
        string key PK
    }
    MESSAGE_DATA {
        generic T
        UnityAction~T~ events
    }
    LISTENER {
        UnityAction~T~ callback
    }
```

---

## 类图（Class Diagram）
```mermaid
classDiagram
    class Singleton~T~ {
        +T Instance
        +void ClearInstance()
        +bool HasInstance
    }

    class IMessageData
    <<interface>> IMessageData

    class MessageData~T~ {
        +UnityAction~T~ MessageEvents
        +MessageData(UnityAction~T~ action)
    }
    MessageData~T~ ..|> IMessageData

    class MessageManager {
        -Dictionary~string, IMessageData~ dictionaryMessage
        +Register~T~(string key, UnityAction~T~ action)
        +Remove~T~(string key, UnityAction~T~ action)
        +Send~T~(string key, T data)
        +GetListenerCount~T~(string key) int
        +Clear() void
    }
    MessageManager --* IMessageData : uses
    MessageManager ..> MessageData~T~ : creates
    MessageManager ..|> Singleton~MessageManager~

    class MessageDefine {
        <<static>> string TEST_MESSAGE
        <<static>> string EFFECT_PLAY_START
        <<static>> string EFFECT_PLAY_END
    }

    class MessageMonitorProxy {
        +RecordMessage(string key, object data, int listenerCount) void
    }
    MessageManager ..> MessageMonitorProxy : record
```

---

## 鲁棒图（Robustness Diagram）
```mermaid
flowchart LR
    subgraph Boundary[Boundary]
        Sender[<<boundary>> Sender]
        MonitorView[<<boundary>> Monitor Window]
    end

    subgraph Control[Control]
        Manager[<<control>> MessageManager]
    end

    subgraph Entity[Entity]
        Store[<<entity>> Dictionary<string, IMessageData>]
        DataT[<<entity>> MessageData<T>]
    end

    Sender -->|Register/Remove/Send| Manager
    Manager -->|lookup/add| Store
    Store --> DataT
    Manager -->|Invoke events| DataT
    Manager -->|RecordMessage| MonitorView
```

---

## 时序图（Sequence Diagram）
```mermaid
sequenceDiagram
    participant S as Sender
    participant M as MessageManager
    participant D as Dictionary
    participant MD as MessageData<T>
    participant V as MonitorProxy

    S->>M: Register<T>(key, action)
    M->>D: TryGetValue(key)
    alt exists
        M->>MD: MessageEvents += action
    else new
        M->>D: Add(key, new MessageData<T>(action))
    end

    S->>M: Send<T>(key, data)
    M->>D: TryGetValue(key)
    alt found & type T matches
        M->>MD: Invoke(data)
        M->>V: RecordMessage(key, data, listenerCount)
    else not found / type mismatch
        M->>V: RecordMessage(key, data, 0)
    end
```

---

## 状态图（State Diagram）
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Idle: Register/Remove
    Idle --> Sending: Send(key, data)
    Sending --> Dispatching: Found listeners
    Dispatching --> Idle: All callbacks invoked
    Sending --> Idle: No listeners / type mismatch
    Idle --> Idle: GetListenerCount
    Idle --> Idle: Clear()
```

---

## 消息系统本身
```csharp
// 定义键（建议统一放入 MessageDefine）
const string KEY_HELLO = "HELLO";

// 注册
MessageManager.Instance.Register<string>(KEY_HELLO, OnHello);

// 发送
MessageManager.Instance.Send(KEY_HELLO, "Hello World");

// 移除
MessageManager.Instance.Remove<string>(KEY_HELLO, OnHello);

void OnHello(string msg)
{
    // 处理消息
}
```

---

## 约束与注意
- `Register<T>` / `Send<T>` 的类型 `T` 必须一致，否则不会触发回调。
- `OnDestroy`/生命周期结束时务必成对 `Remove<T>`，避免悬挂委托。
- `MessageMonitorProxy` 仅在编辑器下工作；构建版不产生编辑器负担。
- `Singleton<MessageManager>` 为非 MonoBehaviour，场景切换不会自动销毁；如需重置可调用 `MessageManager.Instance.Clear()`。
