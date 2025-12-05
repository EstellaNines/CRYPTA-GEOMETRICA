using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.SaveSystem.UI
{
    /// <summary>
    /// 电池样式生命值条
    /// <para>将生命值显示为 10 个格子的电池样式，并带有红绿渐变。</para>
    /// </summary>
    public class BatteryHealthBar : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("最大生命值")]
        public float maxHealth = 100f;

        [Tooltip("电池格子数量")]
        public int totalSegments = 10;

        [Header("外观")]
        [Tooltip("格子预制体（必须包含 Image 组件）")]
        public GameObject segmentPrefab;

        [Tooltip("格子容器（如果不填则默认为当前物体）")]
        public Transform container;

        [Tooltip("生命值颜色渐变（左红 -> 右绿）")]
        public Gradient colorGradient;

        [Tooltip("未点亮格子的颜色（底色）")]
        public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Tooltip("是否在运行时自动生成格子")]
        public bool autoGenerateOnStart = true;

        // 内部缓存
        private List<Image> _segments = new List<Image>();

        void Awake()
        {
            if (container == null) container = transform;
            
            // 如果没有设置渐变，通过代码创建一个默认的红绿渐变
            if (colorGradient == null || colorGradient.colorKeys.Length == 0)
            {
                colorGradient = new Gradient();
                var colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.green, 1.0f);
                
                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
                
                colorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateSegments();
            }
        }

        /// <summary>
        /// 生成格子 UI
        /// </summary>
        [ContextMenu("生成格子 (Generate Segments)")]
        public void GenerateSegments()
        {
            // 清理现有格子
            _segments.Clear();
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in container)
            {
                toDestroy.Add(child.gameObject);
            }
            // 在编辑器模式下使用 DestroyImmediate
            foreach (var go in toDestroy)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            if (segmentPrefab == null)
            {
                Debug.LogError("[BatteryHealthBar] 未设置 Segment Prefab！");
                return;
            }

            // 生成新格子
            for (int i = 0; i < totalSegments; i++)
            {
                GameObject go = Instantiate(segmentPrefab, container);
                go.name = $"Segment_{i}";
                
                Image img = go.GetComponent<Image>();
                if (img != null)
                {
                    _segments.Add(img);
                    // 默认初始化为 100 血（满状态）
                    // 根据索引计算满血时的颜色
                    float t = (float)i / (totalSegments - 1);
                    if (totalSegments <= 1) t = 1f;
                    img.color = colorGradient.Evaluate(t); 
                }
            }
        }

        /// <summary>
        /// 更新生命值显示
        /// </summary>
        /// <param name="currentHealth">当前生命值</param>
        public void UpdateHealth(float currentHealth)
        {
            // 如果列表为空（可能是非自动生成模式，手动在编辑器摆放的），尝试获取子对象
            if (_segments.Count == 0)
            {
                foreach (Transform child in container)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) _segments.Add(img);
                }
            }

            if (_segments.Count == 0) return;

            // 计算每一格代表的血量
            float healthPerSegment = maxHealth / totalSegments;

            // 计算当前应该点亮的格子数 (向下取整机制)
            // 需求：58血量 -> 显示到50 -> 5格
            // 算法：floor(58 / 10) = 5
            int activeSegments = Mathf.FloorToInt(currentHealth / healthPerSegment);
            
            // 限制范围
            activeSegments = Mathf.Clamp(activeSegments, 0, totalSegments);

            for (int i = 0; i < _segments.Count; i++)
            {
                Image seg = _segments[i];
                
                if (i < activeSegments)
                {
                    // 点亮状态
                    // 计算当前格子的颜色采样位置 (0~1)
                    float t = (float)i / (totalSegments - 1);
                    if (totalSegments <= 1) t = 1f; // 避免除以0

                    seg.color = colorGradient.Evaluate(t);
                }
                else
                {
                    // 熄灭状态
                    seg.color = emptyColor;
                }
            }
        }

        /// <summary>
        /// 测试方法
        /// </summary>
        [ContextMenu("测试：设置为 58 血")]
        public void TestHealth58()
        {
            UpdateHealth(58);
        }
        
        [ContextMenu("测试：设置为 100 血")]
        public void TestHealth100()
        {
            UpdateHealth(100);
        }
        
        [ContextMenu("测试：设置为 15 血")]
        public void TestHealth15()
        {
            UpdateHealth(15);
        }
    }
}
