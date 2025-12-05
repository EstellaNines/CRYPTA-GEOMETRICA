using System.Collections;
using UnityEngine;

namespace CryptaGeometrica.UISystem
{
    /// <summary>
    /// UI 头像眨眼控制器
    /// 配合 Animator 使用，随机触发眨眼动画，让角色看起来更生动
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UI_PortraitBlinker : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Animator 组件引用，为空则自动获取")]
        [SerializeField] private Animator targetAnimator;
        
        [Tooltip("Animator 中的 Trigger 参数名")]
        [SerializeField] private string blinkTriggerName = "Blink";
        
        [Tooltip("眨眼间隔 (秒) - 动画播放完毕后等待的时间")]
        [SerializeField] private float interval = 10f;

        private void Awake()
        {
            if (targetAnimator == null) 
                targetAnimator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (targetAnimator == null)
            {
                Debug.LogError("UI_PortraitBlinker: 未找到 Animator 组件！脚本停止运行。");
                return;
            }

            // 启动眨眼协程
            StartCoroutine(BlinkLoop());
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                // 1. 等待固定间隔 (例如 10秒)
                yield return new WaitForSeconds(interval);

                // 2. 触发眨眼动画
                // 你的动画长 0.4秒，这里触发后 Animator 会播放动画
                // 播放完后 Animator 应自动切回 Idle
                targetAnimator.SetTrigger(blinkTriggerName);
            }
        }
    }
}
