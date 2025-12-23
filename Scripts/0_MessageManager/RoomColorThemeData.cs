using UnityEngine;

/// <summary>
/// 房间颜色主题消息数据
/// 用于在多房间生成器和无限背景系统之间传递颜色主题信息
/// </summary>
[System.Serializable]
public class RoomColorThemeData
{
    /// <summary>
    /// 颜色主题类型
    /// </summary>
    public RoomColorTheme colorTheme;
    
    /// <summary>
    /// 对应的背景纹理
    /// 每个颜色主题都有一张专属的后背景图
    /// </summary>
    public Texture2D backgroundTexture;
    
    /// <summary>
    /// 主题颜色
    /// 用于UI着色或其他颜色相关的视觉效果
    /// </summary>
    public Color themeColor;
    
    /// <summary>
    /// 房间信息描述
    /// 包含房间类型、位置等描述信息
    /// </summary>
    public string roomInfo;
    
    /// <summary>
    /// 过渡动画持续时间（秒）
    /// 用于控制主题切换的过渡效果时长
    /// </summary>
    public float transitionDuration;
    
    /// <summary>
    /// 消息时间戳
    /// 记录消息发送的时间
    /// </summary>
    public float timestamp;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="colorTheme">颜色主题</param>
    /// <param name="backgroundTexture">背景纹理</param>
    /// <param name="themeColor">主题颜色</param>
    /// <param name="roomInfo">房间信息</param>
    /// <param name="transitionDuration">过渡时间</param>
    public RoomColorThemeData(RoomColorTheme colorTheme, Texture2D backgroundTexture, Color themeColor, string roomInfo = "", float transitionDuration = 1.0f)
    {
        this.colorTheme = colorTheme;
        this.backgroundTexture = backgroundTexture;
        this.themeColor = themeColor;
        this.roomInfo = roomInfo;
        this.transitionDuration = transitionDuration;
        this.timestamp = Time.time;
    }
    
    /// <summary>
    /// 空构造函数（用于序列化）
    /// </summary>
    public RoomColorThemeData()
    {
        colorTheme = RoomColorTheme.Red;
        backgroundTexture = null;
        themeColor = Color.white;
        roomInfo = "";
        transitionDuration = 1.0f;
        timestamp = 0f;
    }
    
    /// <summary>
    /// 是否有有效的背景纹理
    /// </summary>
    public bool HasValidTexture => backgroundTexture != null;
    
    /// <summary>
    /// 获取颜色主题的字符串表示
    /// </summary>
    public string GetThemeString()
    {
        return colorTheme switch
        {
            RoomColorTheme.Red => "红色主题",
            RoomColorTheme.Blue => "蓝色主题", 
            RoomColorTheme.Yellow => "黄色主题",
            _ => "未知主题"
        };
    }
    
    /// <summary>
    /// 获取默认主题颜色
    /// </summary>
    public static Color GetDefaultThemeColor(RoomColorTheme theme)
    {
        return theme switch
        {
            RoomColorTheme.Red => new Color(1f, 0.2f, 0.2f, 1f),
            RoomColorTheme.Blue => new Color(0.2f, 0.4f, 1f, 1f),
            RoomColorTheme.Yellow => new Color(1f, 0.9f, 0.2f, 1f),
            _ => Color.white
        };
    }
    
    public override string ToString()
    {
        return $"RoomColorThemeData(Theme: {GetThemeString()}, HasTexture: {HasValidTexture}, Color: {themeColor}, Room: {roomInfo}, Duration: {transitionDuration}s, Time: {timestamp:F2}s)";
    }
}
