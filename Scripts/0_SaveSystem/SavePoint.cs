using UnityEngine;

namespace Systems.SaveSystem
{
    /// <summary>
    /// 存档点 (Save Point)
    /// <para>挂载在场景中的交互物体上（如安全屋的打字机、存档终端）。</para>
    /// <para>当玩家进入触发区域并按下交互键时，触发保存逻辑。</para>
    /// </summary>
    public class SavePoint : MonoBehaviour
    {
        [Header("交互设置 (Interaction Settings)")]
        [Tooltip("触发保存所需的按键。")]
        public KeyCode InteractionKey = KeyCode.E;

        [Tooltip("可选：保存成功时播放的粒子特效。")]
        public ParticleSystem SaveEffect;

        // 内部状态：玩家是否在触发范围内
        private bool _isPlayerInRange;

        private void Update()
        {
            // 如果玩家在范围内且按下了交互键
            if (_isPlayerInRange && Input.GetKeyDown(InteractionKey))
            {
                PerformSave();
            }
        }

        /// <summary>
        /// 执行保存操作
        /// </summary>
        private void PerformSave()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[SavePoint] SaveManager 实例缺失！无法执行保存。");
                return;
            }

            // 调用管理器保存当前游戏到选定的槽位
            SaveManager.Instance.SaveGame();

            // 播放反馈特效
            if (SaveEffect != null)
            {
                SaveEffect.Play();
            }
            Debug.Log("[SavePoint] 通过交互触发了保存。");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerInRange = true;
                // TODO: 这里可以添加 UI 提示，例如显示 "按 E 保存"
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerInRange = false;
                // TODO: 这里隐藏 UI 提示
            }
        }
    }
}
