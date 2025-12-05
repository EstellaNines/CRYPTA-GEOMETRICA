using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptaGeometrica.UISystem
{
    /// <summary>
    /// 玩家血量 UI 控制器
    /// 负责实例化 Grid 预制件并根据血量更新显示
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Grid 预制件")]
        [SerializeField] private GameObject gridPrefab;
        
        [Tooltip("总血量 (必须是 10 的倍数)")]
        [SerializeField] private int maxHealth = 100;
        
        [Tooltip("每个 Grid 代表的血量")]
        [SerializeField] private int healthPerGrid = 10;

        [Header("Gradient Settings")]
        [Tooltip("是否每个格子颜色固定 (左红右绿)？如果不勾选，则整体颜色随血量变化")]
        [SerializeField] private bool useFixedGradient = true;
        
        [Tooltip("血量渐变色 (左/低血量)")]
        [SerializeField] private Color lowHealthColor = Color.red;
        
        [Tooltip("血量渐变色 (右/高血量)")]
        [SerializeField] private Color highHealthColor = Color.green;

        private List<Image> _healthGrids = new List<Image>();
        private int _currentHealth;

        private void Awake()
        {
            // 初始化 Grid 池
            InitializeGrids();
        }

        // 删除 Start 测试代码，完全由 Manager 控制
        // private void Start() { UpdateHealth(maxHealth); }

        /// <summary>
        /// 初始化生成血条格子
        /// </summary>
        private void InitializeGrids()
        {
            // 清理现有子物体
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            int gridCount = maxHealth / healthPerGrid;

            for (int i = 0; i < gridCount; i++)
            {
                GameObject go = Instantiate(gridPrefab, transform);
                Image img = go.GetComponent<Image>();
                
                if (img == null)
                {
                    // 尝试在子物体中找 Image (如果 prefab 结构复杂)
                    img = go.GetComponentInChildren<Image>();
                }

                if (img != null)
                {
                    _healthGrids.Add(img);
                    
                    // 如果是固定渐变，初始化时就定好颜色
                    if (useFixedGradient)
                    {
                        float t = (float)i / (gridCount - 1);
                        img.color = Color.Lerp(lowHealthColor, highHealthColor, t);
                    }
                }
                else
                {
                    Debug.LogError("HealthUI: Grid Prefab 中未找到 Image 组件！");
                }
            }
        }

        /// <summary>
        /// 更新血量显示
        /// 支持半格血显示 (要求 Grid Image Type 为 Filled)
        /// </summary>
        /// <param name="currentHealth">当前血量</param>
        public void UpdateHealth(int currentHealth)
        {
            _currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            
            for (int i = 0; i < _healthGrids.Count; i++)
            {
                Image grid = _healthGrids[i];
                
                // 计算当前格子的血量范围
                // 例如：第0个格子代表 0-10，第4个格子代表 40-50
                int gridMinHealth = i * healthPerGrid;
                int gridMaxHealth = (i + 1) * healthPerGrid;

                if (_currentHealth >= gridMaxHealth)
                {
                    // 满格
                    grid.enabled = true;
                    grid.fillAmount = 1f;
                }
                else if (_currentHealth <= gridMinHealth)
                {
                    // 空格 (隐藏或 fill=0)
                    // 为了保持占位，如果 Layout 需要它占位，建议 fillAmount=0 但 enabled=true
                    // 或者 disable，取决于 Layout 设置。通常 Disable 会导致 Layout 塌陷，除非有空的背景槽
                    // 既然是"显示血量"，通常没血的地方是空的。
                    grid.fillAmount = 0f;
                    // 可选：如果不需要背景槽，可以 grid.enabled = false;
                }
                else
                {
                    // 半格 (当前血量落在该格子范围内)
                    // 例如 45 血，落在 40-50 之间
                    // 剩余血量 = 45 - 40 = 5
                    // fill = 5 / 10 = 0.5
                    float remain = _currentHealth - gridMinHealth;
                    grid.enabled = true;
                    grid.fillAmount = remain / healthPerGrid;
                }

                // 如果是动态颜色模式（虽然用户选了方案A，但保留逻辑）
                if (!useFixedGradient && grid.enabled)
                {
                    float percent = (float)_currentHealth / maxHealth;
                    grid.color = Color.Lerp(lowHealthColor, highHealthColor, percent);
                }
            }
        }
    }
}
