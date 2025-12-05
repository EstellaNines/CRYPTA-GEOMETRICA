/// <summary>
/// 场景切换请求数据
/// 用于通过消息系统发送场景切换请求
/// </summary>
[System.Serializable]
public class SceneChangeRequest
{
    /// <summary>
    /// 目标场景名称
    /// </summary>
    public string targetScene;
    
    /// <summary>
    /// 是否使用Loading场景
    /// </summary>
    public bool useLoading;
    
    /// <summary>
    /// Loading场景名称（默认为SP_LoadingScreen）
    /// </summary>
    public string loadingSceneName;
    
    /// <summary>
    /// Loading最小显示时间（秒）
    /// </summary>
    public float minLoadingTime;

    public SceneChangeRequest(string targetScene, bool useLoading = true, string loadingScene = "SP_LoadingScreen", float minLoadingTime = 1.5f)
    {
        this.targetScene = targetScene;
        this.useLoading = useLoading;
        this.loadingSceneName = loadingScene;
        this.minLoadingTime = minLoadingTime;
    }
}
