using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景流程控制器 (Scene Flow Controller)
/// <para>提供高层级的场景导航功能，如“下一关”、“上一关”或“返回主菜单”。</para>
/// <para>可以直接挂载在 UI 按钮上使用。</para>
/// </summary>
public class SceneFlowController : MonoBehaviour
{
    [Header("配置 (Configuration)")]
    [Tooltip("是否按 Build Settings 的顺序自动导航。\n如果为 true，将忽略下方的 Scene Sequence 列表。")]
    public bool useBuildSettingsOrder = true;

    [Tooltip("自定义场景切换顺序列表（仅当 useBuildSettingsOrder 为 false 时生效）。")]
    public List<string> sceneSequence = new List<string>();

    [Header("加载选项 (Loading Options)")]
    [Tooltip("是否使用 Loading 界面过渡。")]
    public bool useLoadingScreen = true;

    [Tooltip("Loading 场景的名称（仅当 useLoadingScreen 为 true 时生效）。")]
    public string loadingSceneName = "SP_LoadingScreen";

    [Tooltip("Loading 界面的最小显示时间（秒）。")]
    public float minLoadingTime = 1.0f;

    /// <summary>
    /// 加载下一个场景
    /// </summary>
    public void LoadNextScene()
    {
        string nextScene = GetNextSceneName();
        if (!string.IsNullOrEmpty(nextScene))
        {
            LoadSceneInternal(nextScene);
        }
        else
        {
            Debug.LogWarning("[SceneFlowController] 已经是最后一个场景，无法切换到下一场景。");
        }
    }

    /// <summary>
    /// 加载上一个场景
    /// </summary>
    public void LoadPreviousScene()
    {
        string prevScene = GetPreviousSceneName();
        if (!string.IsNullOrEmpty(prevScene))
        {
            LoadSceneInternal(prevScene);
        }
        else
        {
            Debug.LogWarning("[SceneFlowController] 已经是第一个场景，无法切换到上一场景。");
        }
    }

    /// <summary>
    /// 加载指定名称的场景
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneFlowController] 目标场景名为空！");
            return;
        }
        LoadSceneInternal(sceneName);
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        var opt = new SceneLoadOptions 
        { 
            useLoadingScreen = useLoadingScreen,
            loadingSceneName = loadingSceneName,
            minShowTime = minLoadingTime
        };
        
        ScenesSystemAPI.Reload(opt);
    }

    // --- 内部逻辑 ---

    private void LoadSceneInternal(string sceneName)
    {
        var opt = new SceneLoadOptions 
        { 
            useLoadingScreen = useLoadingScreen,
            loadingSceneName = loadingSceneName,
            minShowTime = minLoadingTime
        };

        if (useLoadingScreen)
        {
            ScenesSystemAPI.GoToWithLoading(sceneName, null, opt);
        }
        else
        {
            ScenesSystemAPI.GoTo(sceneName, opt);
        }
    }

    private string GetNextSceneName()
    {
        if (useBuildSettingsOrder)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int next = current + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
            {
                return GetSceneNameByBuildIndex(next);
            }
        }
        else
        {
            string current = SceneManager.GetActiveScene().name;
            int index = sceneSequence.IndexOf(current);
            if (index >= 0 && index < sceneSequence.Count - 1)
            {
                return sceneSequence[index + 1];
            }
        }
        return null;
    }

    private string GetPreviousSceneName()
    {
        if (useBuildSettingsOrder)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int prev = current - 1;
            if (prev >= 0)
            {
                return GetSceneNameByBuildIndex(prev);
            }
        }
        else
        {
            string current = SceneManager.GetActiveScene().name;
            int index = sceneSequence.IndexOf(current);
            if (index > 0)
            {
                return sceneSequence[index - 1];
            }
        }
        return null;
    }

    private string GetSceneNameByBuildIndex(int buildIndex)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}
