using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/// <summary>
/// 场景加载参数对象。
/// 封装了主场景切换、叠加加载、预加载以及 Loading 层的行为控制参数。
/// 建议通过 <see cref="ScenesSystemAPI"/> 的 GoTo/GoToWithLoading/LoadAdditive/Preload 等接口传入。
/// </summary>
public class SceneLoadOptions
{
    /// <summary>
    /// 是否以 Additive 方式加载。
    /// - 对 LoadAdditive/Preload 等 API 有效。
    /// - GoTo/GoToWithLoading 内部会按流程选择 Single 或 Additive，不必手动设置。
    /// 默认：false。
    /// </summary>
    public bool additive = false;
    
    /// <summary>
    /// 是否允许到达 0.9 进度后立即激活场景（<see cref="AsyncOperation.allowSceneActivation"/>）。
    /// - 对 Preload/LoadAdditive 可用于延迟激活。
    /// 默认：true。
    /// </summary>
    public bool allowSceneActivation = true;

    /// <summary>
    /// 是否启用加载层（Loading 场景）。
    /// - 与 GoToWithLoading 配合使用，显示 <see cref="loadingSceneName"/> 并在后台并行加载目标与叠加场景。
    /// 默认：false。
    /// </summary>
    public bool useLoadingScreen = false;
    
    /// <summary>
    /// Loading 场景名称。默认固定为 "SP_LoadingScreen"。
    /// 需要确保该场景已加入 Build Settings。
    /// </summary>
    public string loadingSceneName = "SP_LoadingScreen";
    
    /// <summary>
    /// 需要与目标主场景一并加载的叠加场景清单（可选）。
    /// - 仅 GoToWithLoading 时用于并行加载。
    /// - 可为空或空列表。
    /// </summary>
    public List<string> additiveScenes = null;

    /// <summary>
    /// Loading 场景的最小显示时间（秒）。
    /// - 防止进度过快导致 Loading 一闪而过。
    /// 默认：0.6。
    /// </summary>
    public float minShowTime = 0.6f;
    
    /// <summary>
    /// 全部就绪（达到 0.9）后，统一激活前的额外延迟（秒）。
    /// - 便于与过渡动画/音效节奏对齐。
    /// 默认：0。
    /// </summary>
    public float activationDelay = 0f;

    /// <summary>
    /// 切换完成后是否调用 <see cref="Resources.UnloadUnusedAssets"/> 进行内存清理。
    /// 默认：true。
    /// </summary>
    public bool unloadUnusedAssets = true;
    
    /// <summary>
    /// 切换完成后是否触发 GC。
    /// 默认：true。
    /// </summary>
    public bool runGC = true;
    
    /// <summary>
    /// 是否输出详细日志（用于调试）。
    /// 默认：true。
    /// </summary>
    public bool logVerbose = true;
}
