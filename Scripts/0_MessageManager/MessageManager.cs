using System.Collections.Generic;
using UnityEngine.Events;

public class MessageManager : Singleton<MessageManager>
{
    private Dictionary<string, IMessageData> dictionaryMessage;

    public MessageManager()
    {
        InitData();
    }

    private void InitData()
    {
        dictionaryMessage = new Dictionary<string, IMessageData>();
    }

    public void Register<T>(string key, UnityAction<T> action)
    {
        if (dictionaryMessage.TryGetValue(key, out var previousAction))
        {
            if (previousAction is MessageData<T> messageData)
            {
                messageData.MessageEvents += action;
            }
        }
        else
        {
            dictionaryMessage.Add(key, new MessageData<T>(action));
        }
    }

    public void Remove<T>(string key, UnityAction<T> action)
    {
        if (dictionaryMessage.TryGetValue(key, out var previousAction))
        {
            if (previousAction is MessageData<T> messageData)
            {
                messageData.MessageEvents -= action;
            }
        }
    }

    public void Send<T>(string key, T data)
    {
        int listenerCount = 0;
        
        if (dictionaryMessage.TryGetValue(key, out var previousAction))
        {
            if (previousAction is MessageData<T> messageData)
            {
                // 获取监听者数量
                listenerCount = messageData.MessageEvents?.GetInvocationList().Length ?? 0;
                
                // 调用消息事件
                messageData.MessageEvents?.Invoke(data);
            }
        }
        
        // 记录到监控器（通过代理）
        MessageMonitorProxy.RecordMessage(key, data, listenerCount);
    }
    
    /// <summary>
    /// 获取指定消息的监听者数量
    /// </summary>
    public int GetListenerCount<T>(string key)
    {
        if (dictionaryMessage.TryGetValue(key, out var previousAction))
        {
            if (previousAction is MessageData<T> messageData)
            {
                return messageData.MessageEvents?.GetInvocationList().Length ?? 0;
            }
        }
        return 0;
    }

    public void Clear()
    {
        dictionaryMessage.Clear();
    }
}