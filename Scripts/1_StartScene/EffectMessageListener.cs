using UnityEngine;

/// <summary>
/// 效果消息监听器示例
/// 演示如何监听效果播放开始和结束消息
/// </summary>
public class EffectMessageListener : MonoBehaviour
{
    [Header("调试选项")]
    [Tooltip("是否在控制台显示消息")]
    public bool showDebugLogs = true;
    
    void Start()
    {
        // 注册效果播放消息监听
        MessageManager.Instance.Register<string>(MessageDefine.EFFECT_PLAY_START, OnEffectPlayStart);
        MessageManager.Instance.Register<string>(MessageDefine.EFFECT_PLAY_END, OnEffectPlayEnd);
        
        if (showDebugLogs)
        {
            Debug.Log("[EffectMessageListener] 效果消息监听已注册");
        }
    }
    
    void OnDestroy()
    {
        // 移除消息监听
        MessageManager.Instance.Remove<string>(MessageDefine.EFFECT_PLAY_START, OnEffectPlayStart);
        MessageManager.Instance.Remove<string>(MessageDefine.EFFECT_PLAY_END, OnEffectPlayEnd);
        
        if (showDebugLogs)
        {
            Debug.Log("[EffectMessageListener] 效果消息监听已移除");
        }
    }
    
    /// <summary>
    /// 效果开始播放回调
    /// </summary>
    private void OnEffectPlayStart(string effectName)
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[效果开始]</color> {effectName} 开始播放");
        }
        
        // 在这里可以执行效果开始时的逻辑
        // 例如：禁用用户输入、显示加载动画等
    }
    
    /// <summary>
    /// 效果播放结束回调
    /// </summary>
    private void OnEffectPlayEnd(string effectName)
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>[效果结束]</color> {effectName} 播放完成");
        }
        
        // 在这里可以执行效果结束时的逻辑
        // 例如：启用用户输入、隐藏加载动画、触发下一个效果等
    }
}
