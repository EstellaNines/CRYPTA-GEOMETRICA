using UnityEngine;

/// <summary>
/// 效果消息数据
/// 用于在效果播放消息中传递详细信息
/// </summary>
[System.Serializable]
public class EffectMessageData
{
    /// <summary>
    /// 效果名称
    /// </summary>
    public string EffectName;
    
    /// <summary>
    /// 效果类型（例如：DotMatrix, FadeIn, FadeOut）
    /// </summary>
    public string EffectType;
    
    /// <summary>
    /// 效果持续时间（秒）
    /// </summary>
    public float Duration;
    
    /// <summary>
    /// 效果发送者（GameObject 名称）
    /// </summary>
    public string Sender;
    
    /// <summary>
    /// 额外数据（可选）
    /// </summary>
    public string ExtraData;
    
    public EffectMessageData(string effectName, string effectType = "", float duration = 0f, string sender = "", string extraData = "")
    {
        EffectName = effectName;
        EffectType = effectType;
        Duration = duration;
        Sender = sender;
        ExtraData = extraData;
    }
    
    public override string ToString()
    {
        return $"[效果] {EffectName} (类型: {EffectType}, 时长: {Duration}s, 发送者: {Sender})";
    }
}
