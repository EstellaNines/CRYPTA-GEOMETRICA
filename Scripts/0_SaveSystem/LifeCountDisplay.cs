using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.SaveSystem.UI
{
    /// <summary>
    /// 玩家生命数量显示组件
    /// <para>根据当前的生命数（Lives），实例化对应数量的心形图标。</para>
    /// </summary>
    public class LifeCountDisplay : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("心形图标预制体（必须包含 Image 组件）")]
        public GameObject heartPrefab;

        [Tooltip("图标容器（如果不填则默认为当前物体）")]
        public Transform container;

        [Tooltip("最大显示数量限制（防止数据异常导致生成过多）")]
        public int maxDisplayCount = 10;

        [Header("调试/自动初始化")]
        [Tooltip("是否在 Start 时自动初始化显示")]
        public bool autoInitOnStart = true;
        [Tooltip("自动初始化时的默认命数")]
        public int defaultLives = 3;

        // 内部缓存，避免频繁 Destroy/Instantiate
        private List<GameObject> _hearts = new List<GameObject>();

        void Awake()
        {
            if (container == null) container = transform;
        }

        void Start()
        {
            if (autoInitOnStart)
            {
                UpdateLives(defaultLives);
            }
        }

        /// <summary>
        /// 更新生命数显示
        /// </summary>
        /// <param name="lives">当前生命数量</param>
        public void UpdateLives(int lives)
        {
            // 数据安全检查
            if (lives < 0) lives = 0;
            if (lives > maxDisplayCount) lives = maxDisplayCount;

            // 确保列表与子对象同步（应对非自动生成的初始子对象）
            SyncListWithChildren();

            int currentCount = _hearts.Count;

            if (lives > currentCount)
            {
                // 需要增加图标
                int addCount = lives - currentCount;
                for (int i = 0; i < addCount; i++)
                {
                    CreateHeart();
                }
            }
            else if (lives < currentCount)
            {
                // 需要减少图标 (从末尾开始隐藏或销毁)
                // 为了性能，我们可以选择隐藏而不是销毁，或者直接销毁
                // 这里为了保持 Layout Group 的整洁，选择 SetActive(false) 配合 Layout 忽略，或者直接 Destroy
                // 考虑到存档界面刷新频率不高，Destroy 重建或者 SetActive 都可以。
                // 采用 SetActive 方案以便复用对象池思想，或者简单点直接 SetActive(false)
                
                for (int i = 0; i < currentCount; i++)
                {
                    if (i < lives)
                    {
                        _hearts[i].SetActive(true);
                    }
                    else
                    {
                        _hearts[i].SetActive(false);
                    }
                }
            }
            else
            {
                // 数量相等，确保所有都激活
                for (int i = 0; i < lives; i++)
                {
                    _hearts[i].SetActive(true);
                }
            }
        }

        private void CreateHeart()
        {
            if (heartPrefab == null)
            {
                Debug.LogWarning("[LifeCountDisplay] Heart Prefab is missing!");
                return;
            }

            // 检查是否有隐藏的对象可以复用
            foreach (var h in _hearts)
            {
                if (!h.activeSelf)
                {
                    h.SetActive(true);
                    return;
                }
            }

            // 实例化新的
            GameObject go = Instantiate(heartPrefab, container);
            go.name = $"Heart_{_hearts.Count}";
            _hearts.Add(go);
        }

        private void SyncListWithChildren()
        {
            // 如果列表为空但容器里有东西，重新填充列表
            if (_hearts.Count == 0 && container.childCount > 0)
            {
                foreach (Transform child in container)
                {
                    _hearts.Add(child.gameObject);
                }
            }
        }

        [ContextMenu("测试：3条命")]
        public void TestLives3() => UpdateLives(3);

        [ContextMenu("测试：5条命")]
        public void TestLives5() => UpdateLives(5);
        
        [ContextMenu("测试：1条命")]
        public void TestLives1() => UpdateLives(1);
        
        [ContextMenu("测试：0条命")]
        public void TestLives0() => UpdateLives(0);
    }
}
