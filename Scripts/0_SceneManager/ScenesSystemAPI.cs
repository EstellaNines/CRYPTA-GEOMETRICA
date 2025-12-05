using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景系统统一调用门面（Facade）。
/// 提供稳定易用的静态方法以替代项目中所有对 Unity SceneManager 的直接调用。
/// 内部会确保存在一个 <see cref="SceneSystemManager"/> 实例（优先使用 Primary），并将调用转发给它的排队系统处理。
/// </summary>
public static class ScenesSystemAPI
{
    /// <summary>
    /// 确保存在可用的 SceneSystemManager：
    /// 1) 优先返回 Primary；2) 其次在当前场景查找一个实例；3) 若仍不存在则自动创建一个全局持久化实例。
    /// </summary>
    static SceneSystemManager EnsureManager()
    {
        if (SceneSystemManager.Primary != null) return SceneSystemManager.Primary;
        var found = UnityEngine.Object.FindObjectOfType<SceneSystemManager>();
        if (found != null) return found;
        var go = new GameObject("SceneSystemManager(Auto)");
        var mgr = go.AddComponent<SceneSystemManager>();
        mgr.mode = SceneSystemManager.InstanceMode.GlobalPersistent;
        return mgr;
    }

    /// <summary>
    /// 当前场景系统是否正忙（正在处理队列或有任务在执行）。
    /// </summary>
    public static bool IsBusy => EnsureManager().IsBusy;
    
    /// <summary>
    /// 等待队列长度（排队中的请求数）。
    /// </summary>
    public static int QueueLength => EnsureManager().QueueLength;
    
    /// <summary>
    /// 当前激活场景名称。
    /// </summary>
    public static string CurrentSceneName => EnsureManager().CurrentSceneName;

    /// <summary>
    /// 切换到指定主场景（不使用 Loading 层）。
    /// </summary>
    /// <param name="targetScene">目标主场景名称（需加入 Build Settings）。</param>
    /// <param name="opt">可选加载参数（资源清理、日志等）。</param>
    public static void GoTo(string targetScene, SceneLoadOptions opt = null)
        => EnsureManager().EnqueueGoTo(targetScene, opt);

    /// <summary>
    /// 使用 Loading 层切换到指定主场景，后台可并行加载多个叠加场景。
    /// </summary>
    /// <param name="targetScene">目标主场景名称。</param>
    /// <param name="additiveScenes">需要一起加载的 Additive 场景集合（可为 null）。</param>
    /// <param name="opt">加载参数（例如 minShowTime/activationDelay 等）。</param>
    public static void GoToWithLoading(string targetScene, IEnumerable<string> additiveScenes = null, SceneLoadOptions opt = null)
        => EnsureManager().EnqueueGoToWithLoading(targetScene, additiveScenes, opt);

    /// <summary>
    /// 重载当前主场景。
    /// </summary>
    public static void Reload(SceneLoadOptions opt = null)
        => EnsureManager().EnqueueReload(opt);

    /// <summary>
    /// 以 Additive 方式加载一个场景。
    /// </summary>
    public static void LoadAdditive(string scene, SceneLoadOptions opt = null)
        => EnsureManager().EnqueueLoadAdditive(scene, opt);

    /// <summary>
    /// 卸载一个已加载的 Additive 场景。
    /// </summary>
    public static void UnloadAdditive(string scene)
        => EnsureManager().EnqueueUnloadAdditive(scene);

    /// <summary>
    /// 将某已加载场景设为激活场景（ActiveScene）。
    /// </summary>
    public static void SetActive(string scene)
        => EnsureManager().EnqueueSetActive(scene);

    /// <summary>
    /// 预加载（Additive + 不激活）指定场景到 0.9，便于后续秒切。
    /// </summary>
    public static void Preload(string scene, Action<float> onProgress = null)
        => EnsureManager().EnqueuePreload(scene, onProgress);

    /// <summary>
    /// 激活已预加载到 0.9 的场景，并将其设为 ActiveScene。
    /// </summary>
    public static void ActivatePreloaded(string scene)
        => EnsureManager().EnqueueActivatePreloaded(scene);
}
