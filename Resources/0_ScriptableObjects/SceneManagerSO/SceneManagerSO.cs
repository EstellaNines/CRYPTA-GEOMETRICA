using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 场景管理工具数据配置
/// 存储项目中的场景信息、加载配置，并用于生成消息代码
/// </summary>
[CreateAssetMenu(fileName = "SceneManagerData", menuName = "自制工具/系统/场景系统/场景配置数据")]
public class SceneManagerSO : ScriptableObject
{
    [BoxGroup("场景列表配置", centerLabel: true)]
    [ListDrawerSettings(ShowIndexLabels = true, AddCopiesLastElement = true, OnTitleBarGUI = "OnListTitleBarGUI")]
    [LabelText("项目场景清单")]
    public List<SceneConfigData> scenes = new List<SceneConfigData>();

    [BoxGroup("全局设置")]
    [LabelText("默认Loading场景名")]
    public string defaultLoadingScene = "SP_LoadingScreen";
    
    [BoxGroup("全局设置")]
    [LabelText("默认最小Loading时间")]
    public float defaultMinLoadingTime = 1.5f;

#if UNITY_EDITOR
    private void OnListTitleBarGUI()
    {
        if (GUILayout.Button("从Build Settings同步", EditorStyles.miniButton))
        {
            SyncFromBuildSettings();
        }
    }

    public void SyncFromBuildSettings()
    {
        var buildScenes = EditorBuildSettings.scenes;
        // 创建临时字典以便快速查找现有配置
        var existingConfigMap = new Dictionary<string, SceneConfigData>();
        foreach (var scene in scenes)
        {
            if (!string.IsNullOrEmpty(scene.scenePath))
            {
                existingConfigMap[scene.scenePath] = scene;
            }
        }

        scenes.Clear();

        foreach (var buildScene in buildScenes)
        {
            // 尝试获取场景名称
            string path = buildScene.path;
            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            // 如果已有配置则保留，否则创建新配置
            if (existingConfigMap.TryGetValue(path, out var existing))
            {
                existing.sceneName = name; // 更新名称以防变更
                scenes.Add(existing);
            }
            else
            {
                scenes.Add(new SceneConfigData
                {
                    sceneName = name,
                    scenePath = path,
                    useLoadingScreen = true,
                    loadingSceneName = defaultLoadingScene,
                    minLoadingTime = defaultMinLoadingTime
                });
            }
        }
        
        Debug.Log($"[SceneManagerSO] 已从 Build Settings 同步 {scenes.Count} 个场景");
    }
#endif
}

[System.Serializable]
public class SceneConfigData
{
    [HorizontalGroup("Main", 0.7f)]
    [VerticalGroup("Main/Info")]
    [LabelText("场景名称")]
    [ReadOnly]
    public string sceneName;

    [VerticalGroup("Main/Info")]
    [LabelText("场景路径")]
    [ReadOnly]
    [FolderPath]
    public string scenePath;

    [HorizontalGroup("Main", 0.3f)]
    [VerticalGroup("Main/Config")]
    [LabelText("使用Loading")]
    public bool useLoadingScreen = true;

    [VerticalGroup("Main/Config")]
    [ShowIf("useLoadingScreen")]
    [LabelText("Loading场景")]
    public string loadingSceneName = "SP_LoadingScreen";

    [VerticalGroup("Main/Config")]
    [ShowIf("useLoadingScreen")]
    [LabelText("最小时间(秒)")]
    public float minLoadingTime = 1.5f;
}
