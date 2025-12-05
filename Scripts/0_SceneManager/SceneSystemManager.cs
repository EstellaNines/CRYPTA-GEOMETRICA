using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景系统核心管理器。
/// - 支持全局持久化实例（Primary）与场景本地实例（LocalPerScene）并存与选举
/// - 提供异步场景切换（可带 Loading 层）、多场景并行加载与聚合进度
/// - 内置忙时请求队列（FIFO），统一排队处理
/// - 通过事件与消息（MessageManager）对外观测加载过程
/// 请通过 <see cref="ScenesSystemAPI"/> 使用对外 API。
/// </summary>
public class SceneSystemManager : MonoBehaviour
{
    /// <summary>
    /// 实例运行模式：
    /// - GlobalPersistent：尝试成为主实例（Primary），并跨场景不销毁（DontDestroyOnLoad）
    /// - LocalPerScene：仅存在于当前场景，不参与跨场景持久化
    /// </summary>
    public enum InstanceMode
    {
        [InspectorName("全局持久化")] GlobalPersistent,
        [InspectorName("场景本地")] LocalPerScene
    }

    /// <summary>
    /// 当前实例的运行模式。默认 GlobalPersistent。
    /// </summary>
    public InstanceMode mode = InstanceMode.GlobalPersistent;

    /// <summary>
    /// 主实例竞争优先级（同帧多个持久化实例出现时，数值越大者优先）。
    /// </summary>
    public int priority = 0;

    /// <summary>
    /// 当前全局主实例（Primary）。
    /// ScenesSystemAPI 将优先路由到该实例。
    /// </summary>
    public static SceneSystemManager Primary { get; private set; }

    private readonly Queue<TaskRequest> _queue = new Queue<TaskRequest>();
    private bool _processing;
    private readonly Dictionary<string, AsyncOperation> _preloads = new Dictionary<string, AsyncOperation>();

    /// <summary>
    /// 是否正忙（执行中或队列非空）。
    /// </summary>
    public bool IsBusy => _processing || _queue.Count > 0;

    /// <summary>
    /// 等待队列长度。
    /// </summary>
    public int QueueLength => _queue.Count;

    /// <summary>
    /// 当前激活场景名（ActiveScene）。
    /// </summary>
    public string CurrentSceneName => SceneManager.GetActiveScene().name;

    /// <summary>
    /// 进入 Loading 场景时触发（例如 SP_LoadingScreen）。
    /// </summary>
    public event Action<string> OnLoadingShown;
    /// <summary>
    /// 某场景开始加载时触发（Single 或 Additive）。
    /// </summary>
    public event Action<string> OnLoadStarted;
    /// <summary>
    /// 单个场景进度（0~1，内部归一化）。
    /// </summary>
    public event Action<string, float> OnLoadProgress;
    /// <summary>
    /// 并行多场景的聚合进度（0~1）。
    /// </summary>
    public event Action<float> OnAggregateProgress;
    /// <summary>
    /// 某场景被设为激活场景时触发。
    /// </summary>
    public event Action<string> OnSceneActivated;
    /// <summary>
    /// Loading 场景被隐藏（卸载）时触发。
    /// </summary>
    public event Action<string> OnLoadingHidden;
    /// <summary>
    /// 开始卸载某个叠加场景时触发。
    /// </summary>
    public event Action<string> OnUnloadStarted;
    /// <summary>
    /// 叠加场景卸载完成时触发。
    /// </summary>
    public event Action<string> OnUnloadCompleted;
    /// <summary>
    /// 完成一次场景切换（目标主场景已激活）时触发。
    /// </summary>
    public event Action<string> OnSwitchCompleted;
    /// <summary>
    /// 加载/切换错误。
    /// </summary>
    public event Action<string, string> OnError;

    /// <summary>
    /// 单场景进度数据。
    /// </summary>
    public class SceneProgressData
    {
        public string scene;
        public float progress;
        public SceneProgressData(string s, float p) { scene = s; progress = p; }
    }

    /// <summary>
    /// 竞争或注册主实例。若模式为 GlobalPersistent，将进行 DontDestroyOnLoad。
    /// </summary>
    private void Awake()
    {
        TryRegisterPrimary();
    }

    /// <summary>
    /// 若当前实例为 Primary，销毁时清空 Primary 引用，便于后续重新选举。
    /// </summary>
    private void OnDestroy()
    {
        if (Primary == this)
        {
            Primary = null;
        }
    }

    /// <summary>
    /// 注册/竞争成为主实例（Primary）。
    /// - 若当前无 Primary，则将自身设为 Primary，并在 GlobalPersistent 模式下调用 DontDestroyOnLoad。
    /// - 若已存在 Primary，且本实例为 GlobalPersistent 且优先级更高，将替换当前 Primary。
    /// </summary>
    void TryRegisterPrimary()
    {
        if (Primary == null)
        {
            Primary = this;
            if (mode == InstanceMode.GlobalPersistent) DontDestroyOnLoad(gameObject);
            return;
        }
        if (Primary != this && mode == InstanceMode.GlobalPersistent && priority > Primary.priority)
        {
            Primary = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 解决场景中重复的 EventSystem 和 AudioListener 冲突。
    /// </summary>
    void ResolveConflicts()
    {
        // 1. 处理 EventSystem
        var eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystems.Length > 1)
        {
            // 简单的策略：保留最新的（通常是刚加载的场景里的），销毁旧的
            // 或者保留第一个 DontDestroyOnLoad 的
            // 这里采用：优先保留当前激活场景中的 EventSystem
            var activeScene = SceneManager.GetActiveScene();
            UnityEngine.EventSystems.EventSystem targetES = null;

            foreach (var es in eventSystems)
            {
                if (es.gameObject.scene == activeScene)
                {
                    targetES = es;
                    break;
                }
            }

            // 如果激活场景没找到，就保留数组第一个作为主
            if (targetES == null) targetES = eventSystems[0];

            foreach (var es in eventSystems)
            {
                if (es != targetES)
                {
                    // Debug.Log($"[SceneSystemManager] 自动销毁多余 EventSystem: {es.gameObject.name}");
                    Destroy(es.gameObject);
                }
            }
        }

        // 2. 处理 AudioListener
        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            // 策略：只启用一个。优先启用激活场景中的。
            var activeScene = SceneManager.GetActiveScene();
            AudioListener targetListener = null;

            foreach (var l in listeners)
            {
                if (l.enabled && l.gameObject.activeInHierarchy && l.gameObject.scene == activeScene)
                {
                    targetListener = l;
                    break;
                }
            }
            
            if (targetListener == null) targetListener = listeners[0];

            foreach (var l in listeners)
            {
                if (l != targetListener && l.enabled)
                {
                    // Debug.Log($"[SceneSystemManager] 自动禁用多余 AudioListener: {l.gameObject.name}");
                    l.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// 启动处理队列的协程。
    /// </summary>
    void StartProcessing()
    {
        if (_processing) return;
        StartCoroutine(ProcessQueue());
    }

    /// <summary>
    /// 队列主循环：按 FIFO 顺序执行所有任务请求。
    /// </summary>
    IEnumerator ProcessQueue()
    {
        _processing = true;
        while (_queue.Count > 0)
        {
            var req = _queue.Dequeue();
            switch (req.type)
            {
                case TaskType.SwitchDirect:
                    yield return SwitchDirect(req.target, req.options);
                    break;
                case TaskType.SwitchWithLoading:
                    yield return SwitchWithLoading(req.target, req.additives, req.options);
                    break;
                case TaskType.Reload:
                    yield return SwitchDirect(SceneManager.GetActiveScene().name, req.options);
                    break;
                case TaskType.LoadAdditive:
                    yield return LoadAdditiveInternal(req.target, req.options);
                    break;
                case TaskType.UnloadAdditive:
                    yield return UnloadAdditiveInternal(req.target);
                    break;
                case TaskType.SetActive:
                    SetActiveInternal(req.target);
                    break;
                case TaskType.Preload:
                    yield return PreloadInternal(req.target, req.onProgress);
                    break;
                case TaskType.ActivatePreloaded:
                    yield return ActivatePreloadedInternal(req.target);
                    break;
            }
        }
        _processing = false;
    }

    /// <summary>
    /// 入队：切换到指定主场景（不使用 Loading 层）。
    /// </summary>
    /// <param name="targetScene">目标主场景名（需在 Build Settings）。</param>
    /// <param name="opt">加载选项（资源清理、日志等）。</param>
    public void EnqueueGoTo(string targetScene, SceneLoadOptions opt = null)
    {
        if (!ValidateSceneName(targetScene)) return;
        _queue.Enqueue(TaskRequest.SwitchDirect(targetScene, opt));
        StartProcessing();
    }

    /// <summary>
    /// 入队：使用 Loading 层切换到主场景，并行加载多个叠加场景。
    /// </summary>
    /// <param name="targetScene">目标主场景名。</param>
    /// <param name="additiveScenes">要一并加载的叠加场景集合（可为 null）。</param>
    /// <param name="opt">加载选项（如 minShowTime/activationDelay 等）。</param>
    public void EnqueueGoToWithLoading(string targetScene, IEnumerable<string> additiveScenes, SceneLoadOptions opt = null)
    {
        if (!ValidateSceneName(targetScene)) return;
        _queue.Enqueue(TaskRequest.SwitchWithLoading(targetScene, additiveScenes, opt));
        StartProcessing();
    }

    /// <summary>
    /// 入队：重载当前主场景。
    /// </summary>
    public void EnqueueReload(SceneLoadOptions opt = null)
    {
        _queue.Enqueue(TaskRequest.Reload(opt));
        StartProcessing();
    }

    /// <summary>
    /// 入队：以 Additive 方式加载一个场景。
    /// </summary>
    /// <param name="scene">场景名。</param>
    /// <param name="opt">加载选项（可延迟激活）。</param>
    public void EnqueueLoadAdditive(string scene, SceneLoadOptions opt = null)
    {
        if (!ValidateSceneName(scene)) return;
        _queue.Enqueue(TaskRequest.LoadAdditive(scene, opt));
        StartProcessing();
    }

    /// <summary>
    /// 入队：卸载一个已加载的 Additive 场景。
    /// </summary>
    public void EnqueueUnloadAdditive(string scene)
    {
        _queue.Enqueue(TaskRequest.UnloadAdditive(scene));
        StartProcessing();
    }

    /// <summary>
    /// 入队：将指定场景设为激活场景（ActiveScene）。
    /// </summary>
    public void EnqueueSetActive(string scene)
    {
        _queue.Enqueue(TaskRequest.SetActive(scene));
        StartProcessing();
    }

    /// <summary>
    /// 入队：预加载到 0.9（Additive 且不激活）。
    /// </summary>
    /// <param name="scene">场景名。</param>
    /// <param name="onProgress">进度回调（0~1）。</param>
    public void EnqueuePreload(string scene, Action<float> onProgress)
    {
        if (!ValidateSceneName(scene)) return;
        _queue.Enqueue(TaskRequest.Preload(scene, onProgress));
        StartProcessing();
    }

    /// <summary>
    /// 入队：激活已预加载到 0.9 的场景并设为 ActiveScene。
    /// </summary>
    public void EnqueueActivatePreloaded(string scene)
    {
        _queue.Enqueue(TaskRequest.ActivatePreloaded(scene));
        StartProcessing();
    }

    /// <summary>
    /// 校验场景名是否非空且已加入 Build Settings。
    /// </summary>
    /// <param name="scene">场景名。</param>
    /// <returns>true 表示有效；false 表示无效并上报错误。</returns>
    bool ValidateSceneName(string scene)
    {
        if (string.IsNullOrEmpty(scene))
        {
            ReportError(scene, "Scene name is null or empty");
            return false;
        }
        if (!IsSceneInBuild(scene))
        {
            ReportError(scene, $"Scene '{scene}' not in Build Settings");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 查询给定场景名是否存在于 Build Settings。
    /// </summary>
    bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>
    /// 直接切换主场景（Single 模式）。
    /// </summary>
    /// <param name="target">目标主场景名。</param>
    /// <param name="opt">加载选项。</param>
    IEnumerator SwitchDirect(string target, SceneLoadOptions opt)
    {
        opt ??= new SceneLoadOptions();
        OnLoadStarted?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, target);

        var op = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            OnLoadProgress?.Invoke(target, p);
            MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_PROGRESS, new SceneProgressData(target, p));
            yield return null;
        }

        var s = SceneManager.GetSceneByName(target);
        if (s.IsValid()) SceneManager.SetActiveScene(s);
        OnSceneActivated?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_ACTIVATED, target);
        OnSwitchCompleted?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_SWITCH_COMPLETE, target);

        if (opt.unloadUnusedAssets) yield return Resources.UnloadUnusedAssets();
        if (opt.runGC) System.GC.Collect();
        
        // 解决冲突
        ResolveConflicts();
    }

    /// <summary>
    /// 使用 Loading 层切换：
    /// 1) 切到 Loading（Single）并显示；2) 并行加载目标主场景与 Additive 场景（均不激活）；
    /// 3) 聚合进度与最小显示时长控制；4) 统一允许激活；5) 卸载 Loading；6) 资源清理与完成事件。
    /// </summary>
    /// <param name="target">目标主场景名。</param>
    /// <param name="additives">需要并行加载的 Additive 场景集合（可空）。</param>
    /// <param name="opt">加载选项（包含 loadingSceneName/minShowTime/activationDelay 等）。</param>
    IEnumerator SwitchWithLoading(string target, IEnumerable<string> additives, SceneLoadOptions opt)
    {
        opt ??= new SceneLoadOptions { useLoadingScreen = true };
        string loading = string.IsNullOrEmpty(opt.loadingSceneName) ? "SP_LoadingScreen" : opt.loadingSceneName;

        var loadLoading = SceneManager.LoadSceneAsync(loading, LoadSceneMode.Single);
        while (!loadLoading.isDone) yield return null;
        OnLoadingShown?.Invoke(loading);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOADING_SHOW, loading);

        float startTime = Time.realtimeSinceStartup;

        var ops = new List<AsyncOperation>();

        OnLoadStarted?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, target);
        var opTarget = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
        opTarget.allowSceneActivation = false;
        ops.Add(opTarget);

        if (additives != null)
        {
            foreach (var a in additives)
            {
                if (!ValidateSceneName(a)) continue;
                OnLoadStarted?.Invoke(a);
                MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, a);
                var opa = SceneManager.LoadSceneAsync(a, LoadSceneMode.Additive);
                opa.allowSceneActivation = false;
                ops.Add(opa);
            }
        }

        bool allReady;
        do
        {
            float sum = 0f;
            for (int i = 0; i < ops.Count; i++)
            {
                var p = Mathf.Clamp01(ops[i].progress / 0.9f);
                sum += p * 0.9f;
            }
            float aggregate = (ops.Count > 0) ? sum / ops.Count : 1f;
            
            // --- 逻辑修改：强制进度与最小显示时间同步 ---
            // 计算基于时间的虚拟进度 (0~1)
            float timeElapsed = Time.realtimeSinceStartup - startTime;
            float timeProgress = Mathf.Clamp01(timeElapsed / Mathf.Max(0.1f, opt.minShowTime));
            
            // 真实的加载进度 (0~1)
            float realProgress = Mathf.Clamp01(aggregate / 0.9f);
            
            // 最终进度取两者的较小值，确保进度条不会快于时间
            float finalProgress = Mathf.Min(realProgress, timeProgress);
            
            OnAggregateProgress?.Invoke(finalProgress);
            MessageManager.Instance.Send(MessageDefine.SCENE_AGGREGATE_PROGRESS, finalProgress);

            for (int i = 0; i < ops.Count; i++)
            {
                string n = (i == 0) ? target : (additives as List<string>)?[i - 1];
                float p = Mathf.Clamp01(ops[i].progress / 0.9f);
                // 单场景进度也受制于总时间吗？通常不需要，但为了保持一致性，这里只修改总聚合进度
                OnLoadProgress?.Invoke(n, p);
                MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_PROGRESS, new SceneProgressData(n, p));
            }

            allReady = true;
            for (int i = 0; i < ops.Count; i++)
                if (ops[i].progress < 0.9f) { allReady = false; break; }

            // 检查时间是否满足
            bool minTimeOk = timeElapsed >= Mathf.Max(0f, opt.minShowTime);
            
            if (allReady && minTimeOk) break;
            yield return null;
        } while (true);

        yield return new WaitForSeconds(Mathf.Max(0f, opt.activationDelay));
        for (int i = 0; i < ops.Count; i++) ops[i].allowSceneActivation = true;

        bool allDone;
        do
        {
            allDone = true;
            for (int i = 0; i < ops.Count; i++) if (!ops[i].isDone) { allDone = false; break; }
            yield return null;
        } while (!allDone);

        var targetScene = SceneManager.GetSceneByName(target);
        if (targetScene.IsValid()) SceneManager.SetActiveScene(targetScene);
        OnSceneActivated?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_ACTIVATED, target);

        var loadingScene = SceneManager.GetSceneByName(loading);
        if (loadingScene.IsValid())
        {
            var unload = SceneManager.UnloadSceneAsync(loadingScene);
            while (!unload.isDone) yield return null;
        }
        OnLoadingHidden?.Invoke(loading);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOADING_HIDE, loading);

        if (opt.unloadUnusedAssets) yield return Resources.UnloadUnusedAssets();
        if (opt.runGC) System.GC.Collect();

        OnSwitchCompleted?.Invoke(target);
        MessageManager.Instance.Send(MessageDefine.SCENE_SWITCH_COMPLETE, target);
        
        // 解决冲突
        ResolveConflicts();
    }

    /// <summary>
    /// Additive 加载一个场景（可选择延迟激活）。
    /// </summary>
    IEnumerator LoadAdditiveInternal(string scene, SceneLoadOptions opt)
    {
        opt ??= new SceneLoadOptions { additive = true };
        OnLoadStarted?.Invoke(scene);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, scene);
        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        op.allowSceneActivation = opt.allowSceneActivation;
        while (!op.isDone)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            OnLoadProgress?.Invoke(scene, p);
            MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_PROGRESS, new SceneProgressData(scene, p));
            yield return null;
        }
        var s = SceneManager.GetSceneByName(scene);
        if (s.IsValid() && opt.additive == false) SceneManager.SetActiveScene(s);
    }

    /// <summary>
    /// 卸载一个已加载的 Additive 场景。
    /// </summary>
    IEnumerator UnloadAdditiveInternal(string scene)
    {
        OnUnloadStarted?.Invoke(scene);
        MessageManager.Instance.Send(MessageDefine.SCENE_UNLOAD_START, scene);
        var op = SceneManager.UnloadSceneAsync(scene);
        while (op != null && !op.isDone) yield return null;
        OnUnloadCompleted?.Invoke(scene);
        MessageManager.Instance.Send(MessageDefine.SCENE_UNLOAD_DONE, scene);
    }

    /// <summary>
    /// 将指定场景设为激活场景（ActiveScene）。
    /// </summary>
    void SetActiveInternal(string scene)
    {
        var s = SceneManager.GetSceneByName(scene);
        if (s.IsValid()) SceneManager.SetActiveScene(s);
    }

    /// <summary>
    /// 预加载（Additive + 不激活）指定场景到 0.9，以支持后续秒切。
    /// </summary>
    IEnumerator PreloadInternal(string scene, Action<float> onProgress)
    {
        if (_preloads.ContainsKey(scene)) yield break;
        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        _preloads[scene] = op;
        while (op.progress < 0.9f)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            onProgress?.Invoke(p);
            OnLoadProgress?.Invoke(scene, p);
            MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_PROGRESS, new SceneProgressData(scene, p));
            yield return null;
        }
    }

    /// <summary>
    /// 激活之前通过 <see cref="PreloadInternal"/> 预加载的场景，并将其设为 ActiveScene。
    /// </summary>
    IEnumerator ActivatePreloadedInternal(string scene)
    {
        if (_preloads.TryGetValue(scene, out var op))
        {
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            _preloads.Remove(scene);
            var s = SceneManager.GetSceneByName(scene);
            if (s.IsValid()) SceneManager.SetActiveScene(s);
            OnSceneActivated?.Invoke(scene);
            MessageManager.Instance.Send(MessageDefine.SCENE_ACTIVATED, scene);
        }
    }

    /// <summary>
    /// 向事件与消息系统上报错误。
    /// </summary>
    void ReportError(string scene, string msg)
    {
        OnError?.Invoke(scene, msg);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_ERROR, $"{scene}: {msg}");
    }

    /// <summary>
    /// 队列任务类型。
    /// </summary>
    enum TaskType
    {
        SwitchDirect,
        SwitchWithLoading,
        Reload,
        LoadAdditive,
        UnloadAdditive,
        SetActive,
        Preload,
        ActivatePreloaded
    }

    /// <summary>
    /// 队列任务请求。
    /// 通过静态工厂方法创建对应类型的请求。
    /// </summary>
    struct TaskRequest
    {
        public TaskType type;
        public string target;
        public List<string> additives;
        public SceneLoadOptions options;
        public Action<float> onProgress;

        /// <summary>创建“直接切换”请求。</summary>
        public static TaskRequest SwitchDirect(string t, SceneLoadOptions o) => new TaskRequest { type = TaskType.SwitchDirect, target = t, options = o };
        /// <summary>创建“使用 Loading 切换”请求。</summary>
        public static TaskRequest SwitchWithLoading(string t, IEnumerable<string> adds, SceneLoadOptions o) => new TaskRequest { type = TaskType.SwitchWithLoading, target = t, additives = adds != null ? new List<string>(adds) : null, options = o };
        /// <summary>创建“重载当前场景”请求。</summary>
        public static TaskRequest Reload(SceneLoadOptions o) => new TaskRequest { type = TaskType.Reload, options = o };
        /// <summary>创建“加载叠加场景”请求。</summary>
        public static TaskRequest LoadAdditive(string t, SceneLoadOptions o) => new TaskRequest { type = TaskType.LoadAdditive, target = t, options = o };
        /// <summary>创建“卸载叠加场景”请求。</summary>
        public static TaskRequest UnloadAdditive(string t) => new TaskRequest { type = TaskType.UnloadAdditive, target = t };
        /// <summary>创建“设为激活场景”请求。</summary>
        public static TaskRequest SetActive(string t) => new TaskRequest { type = TaskType.SetActive, target = t };
        /// <summary>创建“预加载”请求。</summary>
        public static TaskRequest Preload(string t, Action<float> cb) => new TaskRequest { type = TaskType.Preload, target = t, onProgress = cb };
        /// <summary>创建“激活已预加载场景”请求。</summary>
        public static TaskRequest ActivatePreloaded(string t) => new TaskRequest { type = TaskType.ActivatePreloaded, target = t };
    }
}
