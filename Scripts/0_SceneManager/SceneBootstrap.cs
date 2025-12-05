using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 放置于 0_Signature 场景：初始化并跳转到首个业务场景
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("初始场景设置")]
    public string initialScene = "1_StartMenu";

    [Header("加载方式")]
    public bool useLoadingScreen = true;
    [Tooltip("与主场景一起加载的叠加场景（可选）")]
    public List<string> additiveScenes = new List<string>();

    [Header("加载参数")]
    public float minShowTime = 0.6f;
    public float activationDelay = 0f;

    void Start()
    {
        if (string.IsNullOrEmpty(initialScene))
        {
            Debug.LogError("[SceneBootstrap] 初始场景名为空，请在 Inspector 配置 initialScene");
            return;
        }

        var opt = new SceneLoadOptions
        {
            useLoadingScreen = useLoadingScreen,
            loadingSceneName = "SP_LoadingScreen",
            minShowTime = minShowTime,
            activationDelay = activationDelay,
            additiveScenes = additiveScenes != null && additiveScenes.Count > 0 ? new List<string>(additiveScenes) : null
        };

        if (useLoadingScreen)
        {
            ScenesSystemAPI.GoToWithLoading(initialScene, additiveScenes, opt);
        }
        else
        {
            ScenesSystemAPI.GoTo(initialScene, opt);
        }
    }
}
