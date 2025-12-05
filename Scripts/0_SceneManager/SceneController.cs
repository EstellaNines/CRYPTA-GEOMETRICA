using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景管理器（单例）
/// 监听场景切换请求消息，执行异步场景加载
/// 基于消息系统（MessageManager）进行通信
/// </summary>
public class SceneController : MonoBehaviour
{
    private static SceneController _instance;
    public static SceneController Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[SceneController]");
                _instance = go.AddComponent<SceneController>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("调试设置")]
    [Tooltip("是否显示详细日志")]
    public bool showDebugLogs = true;

    private bool _isLoading = false;
    private Coroutine _currentLoadingCoroutine;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册场景切换请求消息
        MessageManager.Instance.Register<SceneChangeRequest>(MessageDefine.SCENE_CHANGE_REQUEST, OnSceneChangeRequest);

        if (showDebugLogs)
        {
            Debug.Log("<color=cyan>[SceneController]</color> 场景管理器已初始化，监听场景切换请求");
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            MessageManager.Instance.Remove<SceneChangeRequest>(MessageDefine.SCENE_CHANGE_REQUEST, OnSceneChangeRequest);
            
            if (showDebugLogs)
            {
                Debug.Log("<color=yellow>[SceneController]</color> 场景管理器已销毁");
            }
        }
    }

    /// <summary>
    /// 接收场景切换请求
    /// </summary>
    private void OnSceneChangeRequest(SceneChangeRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.targetScene))
        {
            Debug.LogError("[SceneController] 无效的场景切换请求");
            MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_ERROR, "无效的场景切换请求");
            return;
        }

        if (_isLoading)
        {
            Debug.LogWarning("[SceneController] 场景正在加载中，请稍后再试");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[场景切换请求]</color> 目标场景: {request.targetScene}, 使用Loading: {request.useLoading}");
        }

        // 执行场景切换
        if (request.useLoading)
        {
            _currentLoadingCoroutine = StartCoroutine(LoadSceneWithLoading(request));
        }
        else
        {
            _currentLoadingCoroutine = StartCoroutine(LoadSceneDirect(request));
        }
    }

    /// <summary>
    /// 直接加载场景（不使用Loading屏幕）
    /// </summary>
    private IEnumerator LoadSceneDirect(SceneChangeRequest request)
    {
        _isLoading = true;

        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, request.targetScene);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(request.targetScene);
        
        while (!asyncOp.isDone)
        {
            float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            MessageManager.Instance.Send(MessageDefine.SCENE_AGGREGATE_PROGRESS, progress);
            yield return null;
        }

        MessageManager.Instance.Send(MessageDefine.SCENE_ACTIVATED, request.targetScene);
        MessageManager.Instance.Send(MessageDefine.SCENE_SWITCH_COMPLETE, request.targetScene);

        _isLoading = false;

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>[场景加载完成]</color> {request.targetScene}");
        }
    }

    /// <summary>
    /// 带Loading屏幕的场景加载
    /// </summary>
    private IEnumerator LoadSceneWithLoading(SceneChangeRequest request)
    {
        _isLoading = true;

        // 1. 加载Loading场景
        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>[加载Loading场景]</color> {request.loadingSceneName}");
        }

        MessageManager.Instance.Send(MessageDefine.SCENE_LOADING_SHOW, request.loadingSceneName);
        
        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(request.loadingSceneName);
        yield return loadingOp;

        // 记录开始时间（用于确保最小显示时间）
        float loadingStartTime = Time.time;

        // 2. 后台异步加载目标场景
        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[开始加载目标场景]</color> {request.targetScene}");
        }

        MessageManager.Instance.Send(MessageDefine.SCENE_LOAD_START, request.targetScene);
        
        AsyncOperation targetOp = SceneManager.LoadSceneAsync(request.targetScene);
        targetOp.allowSceneActivation = false; // 先不激活，等Loading显示足够时间

        // 3. 监控加载进度
        while (targetOp.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(targetOp.progress / 0.9f);
            MessageManager.Instance.Send(MessageDefine.SCENE_AGGREGATE_PROGRESS, progress);
            yield return null;
        }

        // 场景加载到90%，等待最小显示时间
        float elapsedTime = Time.time - loadingStartTime;
        float remainingTime = request.minLoadingTime - elapsedTime;

        if (remainingTime > 0)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>[等待Loading最小显示时间]</color> 剩余 {remainingTime:F2} 秒");
            }
            yield return new WaitForSeconds(remainingTime);
        }

        // 4. 激活目标场景
        MessageManager.Instance.Send(MessageDefine.SCENE_AGGREGATE_PROGRESS, 1f);
        targetOp.allowSceneActivation = true;

        yield return targetOp;

        // 5. 发送完成消息
        MessageManager.Instance.Send(MessageDefine.SCENE_ACTIVATED, request.targetScene);
        MessageManager.Instance.Send(MessageDefine.SCENE_LOADING_HIDE, request.loadingSceneName);
        MessageManager.Instance.Send(MessageDefine.SCENE_SWITCH_COMPLETE, request.targetScene);

        _isLoading = false;

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>[场景切换完成]</color> {request.targetScene}");
        }
    }

    /// <summary>
    /// 手动触发场景切换（用于测试）
    /// </summary>
    [ContextMenu("测试：切换到2_Save场景")]
    public void TestLoadSaveScene()
    {
        var request = new SceneChangeRequest("2_Save", useLoading: true);
        MessageManager.Instance.Send(MessageDefine.SCENE_CHANGE_REQUEST, request);
    }
}
