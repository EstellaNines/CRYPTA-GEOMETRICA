public static class MessageDefine
{
    public static readonly string TEST_MESSAGE = "TEST_MESSAGE";//这是测试消息Key
    
    // ========== 效果播放消息 ==========
    
    /// <summary>
    /// 效果开始播放（通用）
    /// 数据类型：string (效果名称) 或 EffectMessageData
    /// 用于所有类型的效果（点阵文字、淡入淡出等）
    /// </summary>
    public static readonly string EFFECT_PLAY_START = "EFFECT_PLAY_START";
    
    /// <summary>
    /// 效果播放结束（通用）
    /// 数据类型：string (效果名称) 或 EffectMessageData
    /// 用于所有类型的效果（点阵文字、淡入淡出等）
    /// </summary>
    public static readonly string EFFECT_PLAY_END = "EFFECT_PLAY_END";

    // ========== 场景系统消息 ==========
    
    /// <summary>
    /// 请求切换场景（由UI或其他模块发起）
    /// 数据：SceneChangeRequest { targetScene, useLoading }
    /// </summary>
    public static readonly string SCENE_CHANGE_REQUEST = "SCENE_CHANGE_REQUEST";
    
    /// <summary>
    /// 显示 Loading 场景（进入 SP_LoadingScreen）
    /// 数据：string loadingSceneName
    /// </summary>
    public static readonly string SCENE_LOADING_SHOW = "SCENE_LOADING_SHOW";

    /// <summary>
    /// 开始加载某个场景（Single 或 Additive）
    /// 数据：string sceneName
    /// </summary>
    public static readonly string SCENE_LOAD_START = "SCENE_LOAD_START";

    /// <summary>
    /// 单个场景加载进度（0~1，进度归一化）
    /// 数据：SceneSystemManager.SceneProgressData { scene, progress }
    /// </summary>
    public static readonly string SCENE_LOAD_PROGRESS = "SCENE_LOAD_PROGRESS";

    /// <summary>
    /// 多场景并行加载的聚合进度（0~1）
    /// 数据：float progress
    /// </summary>
    public static readonly string SCENE_AGGREGATE_PROGRESS = "SCENE_AGGREGATE_PROGRESS";

    /// <summary>
    /// 某个场景被设为激活场景（ActiveScene）
    /// 数据：string sceneName
    /// </summary>
    public static readonly string SCENE_ACTIVATED = "SCENE_ACTIVATED";

    /// <summary>
    /// 隐藏 Loading 场景（卸载 SP_LoadingScreen）
    /// 数据：string loadingSceneName
    /// </summary>
    public static readonly string SCENE_LOADING_HIDE = "SCENE_LOADING_HIDE";

    /// <summary>
    /// 开始卸载某个叠加场景
    /// 数据：string sceneName
    /// </summary>
    public static readonly string SCENE_UNLOAD_START = "SCENE_UNLOAD_START";

    /// <summary>
    /// 叠加场景卸载完成
    /// 数据：string sceneName
    /// </summary>
    public static readonly string SCENE_UNLOAD_DONE = "SCENE_UNLOAD_DONE";

    /// <summary>
    /// 完成一次场景切换（目标主场景已激活）
    /// 数据：string sceneName
    /// </summary>
    public static readonly string SCENE_SWITCH_COMPLETE = "SCENE_SWITCH_COMPLETE";

    /// <summary>
    /// 场景加载/切换错误
    /// 数据：string message
    /// </summary>
    public static readonly string SCENE_LOAD_ERROR = "SCENE_LOAD_ERROR";

    // ========== 房间生成消息 ==========

    /// <summary>
    /// 房间生成完成，广播出入口位置
    /// 数据：RoomAnchorsData { startPos, endPos, worldStartPos, worldEndPos }
    /// </summary>
    public static readonly string ROOM_ANCHORS_UPDATE = "ROOM_ANCHORS_UPDATE";

    // ========== 主题背景消息 ==========

    /// <summary>
    /// 关卡主题选择完成，通知背景管理器切换主题背景
    /// 数据：ThemeBackgroundMessageData { themeName, backgroundTexture, themeColor }
    /// </summary>
    public static readonly string THEME_BACKGROUND_CHANGED = "THEME_BACKGROUND_CHANGED";

    /// <summary>
    /// 房间颜色主题变更消息
    /// 数据：RoomColorThemeData { colorTheme, backgroundTexture, roomInfo, transitionDuration }
    /// 用于通知无限背景系统和其他监听者房间颜色主题的变化
    /// </summary>
    public static readonly string ROOM_COLOR_THEME_CHANGED = "ROOM_COLOR_THEME_CHANGED";
}