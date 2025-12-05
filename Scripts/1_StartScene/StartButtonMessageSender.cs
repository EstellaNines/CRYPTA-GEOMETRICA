using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始按钮消息发送器
/// 仅负责在按钮点击时发送场景切换请求消息
/// 实际的场景加载由SceneController处理
/// </summary>
[RequireComponent(typeof(Button))]
public class StartButtonMessageSender : MonoBehaviour
{
    [Header("场景切换设置")]
    [Tooltip("目标场景名称")]
    public string targetSceneName = "2_Save";

    [Tooltip("是否使用Loading屏幕")]
    public bool useLoadingScreen = true;

    [Tooltip("Loading场景名称")]
    public string loadingSceneName = "SP_LoadingScreen";

    [Tooltip("Loading最小显示时间（秒）")]
    [Range(0.5f, 5f)]
    public float minLoadingTime = 1.5f;

    [Header("调试选项")]
    [Tooltip("是否显示调试日志")]
    public bool showDebugLogs = true;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // 注册按钮点击事件
        button.onClick.AddListener(OnButtonClick);

        if (showDebugLogs)
        {
            Debug.Log($"[StartButtonMessageSender] 开始按钮已注册，目标场景: {targetSceneName}");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>
    /// 按钮点击处理：发送场景切换请求消息
    /// </summary>
    private void OnButtonClick()
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[开始按钮点击]</color> 发送场景切换请求: {targetSceneName}");
        }

        // 创建场景切换请求
        var request = new SceneChangeRequest(
            targetScene: targetSceneName,
            useLoading: useLoadingScreen,
            loadingScene: loadingSceneName,
            minLoadingTime: minLoadingTime
        );

        // 通过消息系统发送请求
        MessageManager.Instance.Send(MessageDefine.SCENE_CHANGE_REQUEST, request);

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>[场景切换请求已发送]</color> 目标: {targetSceneName}, Loading: {useLoadingScreen}");
        }
    }

    /// <summary>
    /// 手动触发发送消息（用于测试）
    /// </summary>
    [ContextMenu("测试发送场景切换消息")]
    public void TestSendMessage()
    {
        OnButtonClick();
    }
}
