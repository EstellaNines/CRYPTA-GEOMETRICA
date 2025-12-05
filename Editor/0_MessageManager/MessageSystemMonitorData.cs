using System;
using System.Collections.Generic;

/// <summary>
/// 消息记录数据
/// </summary>
[Serializable]
public class MessageRecord
{
    public string MessageKey;
    public string DataType;
    public string DataValue;
    public string Timestamp;
    public int ListenerCount;
    
    public MessageRecord(string key, string type, string value, int listenerCount)
    {
        MessageKey = key;
        DataType = type;
        DataValue = value;
        Timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        ListenerCount = listenerCount;
    }
}

/// <summary>
/// 消息统计数据
/// </summary>
[Serializable]
public class MessageStatistics
{
    public Dictionary<string, int> MessageCounts = new Dictionary<string, int>();
    public int TotalMessagesSent = 0;
    
    public void RecordMessage(string key)
    {
        TotalMessagesSent++;
        
        if (MessageCounts.ContainsKey(key))
        {
            MessageCounts[key]++;
        }
        else
        {
            MessageCounts[key] = 1;
        }
    }
    
    public void Clear()
    {
        MessageCounts.Clear();
        TotalMessagesSent = 0;
    }
}
