using UnityEngine;

/// <summary>
/// 消息系统测试脚本
/// 用于测试消息系统和监控器
/// </summary>
public class MessageSystemTest : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("是否自动发送测试消息")]
    public bool autoSendMessages = true;
    
    [Tooltip("发送间隔（秒）")]
    [Range(0.1f, 5f)]
    public float sendInterval = 1f;
    
    private float timer = 0f;
    private int messageCount = 0;
    
    void Start()
    {
        // 注册测试消息监听
        MessageManager.Instance.Register<string>(MessageDefine.TEST_MESSAGE, OnTestMessage);
        MessageManager.Instance.Register<int>("SCORE_CHANGED", OnScoreChanged);
        MessageManager.Instance.Register<PlayerTestData>("PLAYER_DATA", OnPlayerData);
        
        Debug.Log("[MessageSystemTest] 消息监听已注册");
    }
    
    void Update()
    {
        if (!autoSendMessages) return;
        
        timer += Time.deltaTime;
        
        if (timer >= sendInterval)
        {
            timer = 0f;
            SendTestMessages();
        }
    }
    
    void OnDestroy()
    {
        // 移除监听
        MessageManager.Instance.Remove<string>(MessageDefine.TEST_MESSAGE, OnTestMessage);
        MessageManager.Instance.Remove<int>("SCORE_CHANGED", OnScoreChanged);
        MessageManager.Instance.Remove<PlayerTestData>("PLAYER_DATA", OnPlayerData);
        
        Debug.Log("[MessageSystemTest] 消息监听已移除");
    }
    
    /// <summary>
    /// 发送测试消息
    /// </summary>
    private void SendTestMessages()
    {
        messageCount++;
        
        // 发送字符串消息
        MessageManager.Instance.Send(MessageDefine.TEST_MESSAGE, $"测试消息 #{messageCount}");
        
        // 发送整数消息
        MessageManager.Instance.Send("SCORE_CHANGED", messageCount * 100);
        
        // 发送自定义数据消息
        var playerData = new PlayerTestData
        {
            PlayerName = $"玩家{messageCount % 5 + 1}",
            HP = Random.Range(50, 100),
            Level = messageCount % 10 + 1
        };
        MessageManager.Instance.Send("PLAYER_DATA", playerData);
    }
    
    #region 消息回调
    
    private void OnTestMessage(string message)
    {
        Debug.Log($"[TEST_MESSAGE] 收到: {message}");
    }
    
    private void OnScoreChanged(int score)
    {
        Debug.Log($"[SCORE_CHANGED] 分数: {score}");
    }
    
    private void OnPlayerData(PlayerTestData data)
    {
        Debug.Log($"[PLAYER_DATA] {data}");
    }
    
    #endregion
    
    #region 手动测试按钮（Inspector 中调用）
    
    [ContextMenu("发送单条测试消息")]
    public void SendSingleTestMessage()
    {
        MessageManager.Instance.Send(MessageDefine.TEST_MESSAGE, "手动测试消息");
        Debug.Log("已发送单条测试消息");
    }
    
    [ContextMenu("发送批量测试消息")]
    public void SendBatchTestMessages()
    {
        for (int i = 0; i < 10; i++)
        {
            MessageManager.Instance.Send(MessageDefine.TEST_MESSAGE, $"批量消息 #{i}");
        }
        Debug.Log("已发送 10 条批量测试消息");
    }
    
    [ContextMenu("清除所有消息")]
    public void ClearAllMessages()
    {
        MessageManager.Instance.Clear();
        Debug.Log("已清除所有消息");
    }
    
    #endregion
}

/// <summary>
/// 测试用玩家数据
/// </summary>
[System.Serializable]
public class PlayerTestData
{
    public string PlayerName;
    public int HP;
    public int Level;
    
    public override string ToString()
    {
        return $"玩家: {PlayerName}, HP: {HP}, 等级: {Level}";
    }
}
