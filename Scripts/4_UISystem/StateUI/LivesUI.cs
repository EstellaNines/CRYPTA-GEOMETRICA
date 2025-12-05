using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CryptaGeometrica.UISystem
{
    /// <summary>
    /// 玩家生命数 (Lives) 显示控制器
    /// 管理 Heart 图标的显示
    /// </summary>
    public class LivesUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Heart 预制件")]
        [SerializeField] private GameObject heartPrefab;
        
        [Tooltip("最大生命数")]
        [SerializeField] private int maxLives = 3;

        [Header("Display Mode")]
        [Tooltip("是否保留空位？(True=失去生命变暗/隐藏但占位, False=直接销毁或隐藏不占位)")]
        [SerializeField] private bool keepEmptySlots = true;
        
        [Tooltip("失去生命时的颜色 (如果保留空位)")]
        [SerializeField] private Color lostLifeColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private List<GameObject> _hearts = new List<GameObject>();

        private void Awake()
        {
            // 初始化生成
            InitializeHearts();
        }

        // 删除 Start 测试代码，完全由 Manager 控制
        // private void Start() { UpdateLives(maxLives); }

        private void InitializeHearts()
        {
            // 清理旧对象
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _hearts.Clear();

            // 生成最大数量的心
            for (int i = 0; i < maxLives; i++)
            {
                GameObject go = Instantiate(heartPrefab, transform);
                _hearts.Add(go);
            }
        }

        /// <summary>
        /// 更新生命数显示
        /// </summary>
        /// <param name="currentLives">当前剩余生命</param>
        public void UpdateLives(int currentLives)
        {
            currentLives = Mathf.Clamp(currentLives, 0, maxLives);

            for (int i = 0; i < _hearts.Count; i++)
            {
                GameObject heartGO = _hearts[i];
                Image heartImage = heartGO.GetComponent<Image>();
                
                // 第 i 个心是否应该是"拥有"状态
                // 例如 2 命：i=0 (True), i=1 (True), i=2 (False)
                bool hasLife = i < currentLives;

                if (hasLife)
                {
                    // 拥有生命：显示且正常颜色
                    heartGO.SetActive(true);
                    if (heartImage != null) heartImage.color = Color.white;
                }
                else
                {
                    // 失去生命
                    if (keepEmptySlots)
                    {
                        // 保留占位：变暗
                        heartGO.SetActive(true);
                        if (heartImage != null) heartImage.color = lostLifeColor;
                    }
                    else
                    {
                        // 不保留：隐藏
                        heartGO.SetActive(false);
                    }
                }
            }
        }
    }
}
