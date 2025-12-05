using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 按钮淡入控制器
/// 监听点阵效果结束后，淡入三个按钮
/// </summary>
public class ButtonFadeInController : MonoBehaviour
{
    [Header("淡入设置")]
    [Tooltip("淡入持续时间（秒）")]
    public float fadeInDuration = 1.0f;
    
    [Tooltip("按钮之间的延迟（秒）")]
    public float buttonDelay = 0.2f;
    
    [Tooltip("淡入曲线")]
    public Ease fadeInEase = Ease.OutCubic;
    
    [Header("调试选项")]
    [Tooltip("是否在控制台显示消息")]
    public bool showDebugLogs = true;
    
    // 三个按钮的 CanvasGroup
    private CanvasGroup[] buttonCanvasGroups;
    
    void Start()
    {
        // 获取三个子对象的 CanvasGroup（如果没有则自动添加）
        SetupButtonCanvasGroups();
        
        // 初始隐藏所有按钮
        HideAllButtons();
        
        // 注册监听点阵效果结束消息
        MessageManager.Instance.Register<string>(MessageDefine.EFFECT_PLAY_END, OnEffectEnd);
        
        if (showDebugLogs)
        {
            Debug.Log("[ButtonFadeInController] 已注册效果结束监听，等待点阵效果完成...");
        }
    }
    
    void OnDestroy()
    {
        // 移除消息监听
        MessageManager.Instance.Remove<string>(MessageDefine.EFFECT_PLAY_END, OnEffectEnd);
        
        // 停止所有 DOTween 动画
        DOTween.Kill(transform);
    }
    
    /// <summary>
    /// 设置按钮的 CanvasGroup
    /// </summary>
    private void SetupButtonCanvasGroups()
    {
        int childCount = transform.childCount;
        
        if (childCount < 3)
        {
            Debug.LogWarning($"[ButtonFadeInController] 子对象数量不足！需要 3 个，当前只有 {childCount} 个");
            buttonCanvasGroups = new CanvasGroup[childCount];
        }
        else
        {
            buttonCanvasGroups = new CanvasGroup[3];
        }
        
        for (int i = 0; i < buttonCanvasGroups.Length; i++)
        {
            Transform child = transform.GetChild(i);
            
            // 获取或添加 CanvasGroup
            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = child.gameObject.AddComponent<CanvasGroup>();
                
                if (showDebugLogs)
                {
                    Debug.Log($"[ButtonFadeInController] 为 {child.name} 添加了 CanvasGroup 组件");
                }
            }
            
            buttonCanvasGroups[i] = canvasGroup;
        }
    }
    
    /// <summary>
    /// 隐藏所有按钮
    /// </summary>
    private void HideAllButtons()
    {
        foreach (var canvasGroup in buttonCanvasGroups)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[ButtonFadeInController] 所有按钮已隐藏");
        }
    }
    
    /// <summary>
    /// 效果结束回调
    /// </summary>
    private void OnEffectEnd(string effectName)
    {
        // 只响应点阵文字效果
        if (effectName.Contains("点阵文字"))
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[ButtonFadeInController]</color> 检测到点阵效果结束: {effectName}");
            }
            
            // 开始淡入按钮
            StartCoroutine(FadeInButtonsCoroutine());
        }
    }
    
    /// <summary>
    /// 淡入按钮协程
    /// </summary>
    private IEnumerator FadeInButtonsCoroutine()
    {
        // 发送按钮淡入开始消息
        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_START, $"按钮淡入效果 [{gameObject.name}]");
        
        if (showDebugLogs)
        {
            Debug.Log("<color=green>[按钮淡入开始]</color> 开始淡入三个按钮");
        }
        
        // 依次淡入每个按钮
        for (int i = 0; i < buttonCanvasGroups.Length; i++)
        {
            if (buttonCanvasGroups[i] != null)
            {
                FadeInButton(buttonCanvasGroups[i], i);
                
                // 等待延迟后再淡入下一个按钮
                if (i < buttonCanvasGroups.Length - 1)
                {
                    yield return new WaitForSeconds(buttonDelay);
                }
            }
        }
        
        // 等待最后一个按钮淡入完成
        yield return new WaitForSeconds(fadeInDuration);
        
        // 发送按钮淡入结束消息
        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_END, $"按钮淡入效果 [{gameObject.name}]");
        
        if (showDebugLogs)
        {
            Debug.Log("<color=yellow>[按钮淡入结束]</color> 所有按钮淡入完成");
        }
    }
    
    /// <summary>
    /// 淡入单个按钮
    /// </summary>
    private void FadeInButton(CanvasGroup canvasGroup, int index)
    {
        // 启用交互
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // DOTween 淡入动画
        canvasGroup.DOFade(1f, fadeInDuration)
            .SetEase(fadeInEase)
            .SetTarget(transform); // 设置目标，方便统一管理
        
        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>[按钮 {index + 1}]</color> 开始淡入");
        }
    }
    
    /// <summary>
    /// 手动触发淡入（用于测试）
    /// </summary>
    [ContextMenu("测试淡入效果")]
    public void TestFadeIn()
    {
        StopAllCoroutines();
        HideAllButtons();
        StartCoroutine(FadeInButtonsCoroutine());
    }
    
    /// <summary>
    /// 重置按钮状态（用于测试）
    /// </summary>
    [ContextMenu("重置按钮状态")]
    public void ResetButtons()
    {
        StopAllCoroutines();
        DOTween.Kill(transform);
        HideAllButtons();
        
        if (showDebugLogs)
        {
            Debug.Log("[ButtonFadeInController] 按钮状态已重置");
        }
    }
}
