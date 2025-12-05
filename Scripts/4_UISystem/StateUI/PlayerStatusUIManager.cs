using UnityEngine;

namespace CryptaGeometrica.UISystem
{
    /// <summary>
    /// 玩家状态 UI 管理器 (Facade)
    /// 统一管理血条和生命数的显示，对外提供简单的调用接口
    /// </summary>
    public class PlayerStatusUIManager : MonoBehaviour
    {
        // 简单的单例模式，方便全局访问
        public static PlayerStatusUIManager Instance { get; private set; }

        [Header("UI Components")]
        [Tooltip("血条控制器")]
        [SerializeField] private HealthUI healthUI;
        
        [Tooltip("生命数控制器")]
        [SerializeField] private LivesUI livesUI;

        private void Awake()
        {
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 自动查找组件 (如果未手动赋值)
            if (healthUI == null) healthUI = GetComponentInChildren<HealthUI>();
            if (livesUI == null) livesUI = GetComponentInChildren<LivesUI>();
        }

        /// <summary>
        /// 初始化状态显示 (通常由存档系统调用)
        /// </summary>
        /// <param name="health">当前血量</param>
        /// <param name="lives">当前生命数</param>
        public void InitStatus(int health, int lives)
        {
            UpdateHealth(health);
            UpdateLives(lives);
        }

        /// <summary>
        /// 更新血量显示
        /// </summary>
        public void UpdateHealth(int currentHealth)
        {
            if (healthUI != null)
            {
                healthUI.UpdateHealth(currentHealth);
            }
            else
            {
                Debug.LogWarning("PlayerStatusUIManager: HealthUI 未连接！");
            }
        }

        /// <summary>
        /// 更新生命数显示
        /// </summary>
        public void UpdateLives(int currentLives)
        {
            if (livesUI != null)
            {
                livesUI.UpdateLives(currentLives);
            }
            else
            {
                Debug.LogWarning("PlayerStatusUIManager: LivesUI 未连接！");
            }
        }
    }
}
