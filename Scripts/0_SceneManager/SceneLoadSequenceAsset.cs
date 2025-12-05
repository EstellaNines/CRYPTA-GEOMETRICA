using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[CreateAssetMenu(menuName = "自制工具/系统/场景系统/场景加载序列组件", fileName = "SceneLoadSequence")]
#if ODIN_INSPECTOR
[InlineEditor(InlineEditorObjectFieldModes.Foldout)]
[InfoBox("编辑场景加载序列。请确保所有场景已加入 Build Settings。", InfoMessageType.Info)]
#endif
public class SceneLoadSequenceAsset : ScriptableObject
{
#if ODIN_INSPECTOR
    [LabelText("主场景名")]
#endif
    public string mainScene;
#if ODIN_INSPECTOR
    [LabelText("叠加场景名列表")]
    [ListDrawerSettings(DraggableItems = true, ShowFoldout = true, ShowIndexLabels = true)]
#endif
    public List<string> additiveScenes = new List<string>();

#if ODIN_INSPECTOR
    [LabelText("使用加载场景")]
#endif
    public bool useLoadingScreen = true;
#if ODIN_INSPECTOR
    [LabelText("加载场景名")]
#endif
    public string loadingSceneName = "SP_LoadingScreen";
#if ODIN_INSPECTOR
    [LabelText("最小显示时间(秒)")]
#endif
    public float minShowTime = 0.6f;
#if ODIN_INSPECTOR
    [LabelText("激活延迟(秒)")]
#endif
    public float activationDelay = 0f;

#if ODIN_INSPECTOR
    [LabelText("清理未使用资源")]
#endif
    public bool unloadUnusedAssets = true;
#if ODIN_INSPECTOR
    [LabelText("切换后执行GC")]
#endif
    public bool runGC = true;

    public SceneLoadOptions ToOptions()
    {
        return new SceneLoadOptions
        {
            useLoadingScreen = this.useLoadingScreen,
            loadingSceneName = string.IsNullOrEmpty(this.loadingSceneName) ? "SP_LoadingScreen" : this.loadingSceneName,
            minShowTime = this.minShowTime,
            activationDelay = this.activationDelay,
            unloadUnusedAssets = this.unloadUnusedAssets,
            runGC = this.runGC,
            logVerbose = true
        };
    }
}
