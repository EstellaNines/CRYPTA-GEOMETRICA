using System;

/// <summary>
/// 消息监控代理
/// 用于在运行时代码中调用编辑器监控功能
/// </summary>
public static class MessageMonitorProxy
{
    /// <summary>
    /// 记录消息的委托
    /// </summary>
    public static Action<string, string, string, int> OnMessageSent;
    
    /// <summary>
    /// 记录消息发送
    /// </summary>
    public static void RecordMessage<T>(string key, T data, int listenerCount)
    {
#if UNITY_EDITOR
        // 在编辑器模式下，如果有监听者，则记录消息
        OnMessageSent?.Invoke(
            key,
            typeof(T).Name,
            data?.ToString() ?? "null",
            listenerCount
        );
#endif
    }
}
