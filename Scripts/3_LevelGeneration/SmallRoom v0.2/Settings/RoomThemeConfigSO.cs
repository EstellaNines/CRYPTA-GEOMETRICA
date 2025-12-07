using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间主题配置 ScriptableObject
    /// 用于持久化保存主题列表
    /// </summary>
    [CreateAssetMenu(fileName = "RoomThemeConfig", menuName = "CRYPTA GEOMETRICA/Room Generator V2/Theme Config")]
    public class RoomThemeConfigSO : ScriptableObject
    {
        [Title("主题列表", "Theme List")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "themeName", DraggableItems = true)]
        [InfoBox("配置房间生成使用的视觉主题，每个主题包含墙壁、平台等瓦片引用")]
        public List<RoomThemeV2> themes = new List<RoomThemeV2>();
        
        /// <summary>
        /// 获取指定索引的主题
        /// </summary>
        public RoomThemeV2 GetTheme(int index)
        {
            if (themes == null || themes.Count == 0)
            {
                Debug.LogWarning("[RoomThemeConfigSO] 主题列表为空");
                return default;
            }
            
            index = Mathf.Clamp(index, 0, themes.Count - 1);
            return themes[index];
        }
        
        /// <summary>
        /// 获取随机主题
        /// </summary>
        public RoomThemeV2 GetRandomTheme()
        {
            if (themes == null || themes.Count == 0)
            {
                Debug.LogWarning("[RoomThemeConfigSO] 主题列表为空");
                return default;
            }
            
            int index = UnityEngine.Random.Range(0, themes.Count);
            return themes[index];
        }
        
        /// <summary>
        /// 主题数量
        /// </summary>
        public int ThemeCount => themes?.Count ?? 0;
        
        /// <summary>
        /// 验证主题配置
        /// </summary>
        [Button("验证配置", ButtonSizes.Medium)]
        public void ValidateConfig()
        {
            if (themes == null || themes.Count == 0)
            {
                Debug.LogWarning("[RoomThemeConfigSO] 主题列表为空，请添加至少一个主题");
                return;
            }
            
            int validCount = 0;
            for (int i = 0; i < themes.Count; i++)
            {
                var theme = themes[i];
                bool isValid = true;
                
                if (theme.wallTile == null)
                {
                    Debug.LogWarning($"[RoomThemeConfigSO] 主题 {i} ({theme.themeName}): 缺少墙壁瓦片");
                    isValid = false;
                }
                
                if (theme.platformTile == null)
                {
                    Debug.LogWarning($"[RoomThemeConfigSO] 主题 {i} ({theme.themeName}): 缺少平台瓦片");
                    isValid = false;
                }
                
                if (isValid) validCount++;
            }
            
            Debug.Log($"[RoomThemeConfigSO] 验证完成: {validCount}/{themes.Count} 个主题有效");
        }
    }
}
