using UnityEngine;
using UnityEngine.UI;

namespace Systems.SaveSystem.UI
{
    /// <summary>
    /// 存档槽位视觉控制器 (View)
    /// <para>仅负责 UI 元素的显示与隐藏，不包含业务逻辑。</para>
    /// </summary>
    public class SaveSlotView : MonoBehaviour
    {
        [Header("状态节点")]
        [Tooltip("当槽位有存档时显示的根物体")]
        public GameObject activeStateRoot;

        [Tooltip("当槽位为空时显示的根物体")]
        public GameObject emptyStateRoot;

        [Header("详细信息组件")]
        [Tooltip("生命值条组件")]
        public BatteryHealthBar batteryHealthBar;

        [Tooltip("生命计数显示组件")]
        public LifeCountDisplay lifeCountDisplay;
        
        [Tooltip("头像显示组件")]
        public Image avatarImage;

        /// <summary>
        /// 显示存档内容
        /// </summary>
        public void ShowSaveContent(GameSaveData data)
        {
            if (activeStateRoot != null) activeStateRoot.SetActive(true);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);

            if (data == null) return;

            // 刷新生命值
            if (batteryHealthBar != null)
            {
                batteryHealthBar.UpdateHealth(data.CurrentHealth);
            }

            // 刷新命数
            if (lifeCountDisplay != null)
            {
                lifeCountDisplay.UpdateLives(data.CurrentLives);
            }
            
            // 刷新头像
            if (avatarImage != null)
            {
                avatarImage.gameObject.SetActive(true);
                // 预留：avatarImage.sprite = ...
            }
        }

        /// <summary>
        /// 显示空槽位状态（New Game）
        /// </summary>
        public void ShowEmpty()
        {
            if (activeStateRoot != null) activeStateRoot.SetActive(false);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(true);
            
            // 确保子组件状态重置（可选）
            if (batteryHealthBar != null) batteryHealthBar.UpdateHealth(0);
            if (lifeCountDisplay != null) lifeCountDisplay.UpdateLives(0);
        }
    }
}
