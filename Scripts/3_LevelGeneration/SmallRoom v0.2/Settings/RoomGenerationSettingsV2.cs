using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间视觉主题
    /// </summary>
    [System.Serializable]
    public struct RoomThemeV2
    {
        [LabelText("主题名称")]
        public string themeName;
        
        [LabelText("墙壁瓦片"), Tooltip("实体墙壁/地面 (Rule Tile)")]
        public TileBase wallTile;
        
        [LabelText("平台瓦片"), Tooltip("浮空平台 (Rule Tile 或普通 Tile)")]
        public TileBase platformTile;
        
        [LabelText("单格平台瓦片"), Tooltip("独立的 1x1 浮空平台 (可选)")]
        public TileBase singlePlatformTile;
        
        [LabelText("门瓦片"), Tooltip("Boss房间专用门砖块")]
        public TileBase doorTile;
        
        [LabelText("背景瓦片"), Tooltip("背景墙 (可选)")]
        public TileBase backgroundTile;
        
        [LabelText("主题颜色"), Tooltip("用于编辑器预览")]
        public Color themeColor;
    }

    /// <summary>
    /// 房间生成配置文件 v0.2
    /// ScriptableObject，用于持久化保存生成参数和主题
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomGenSettingsV2", menuName = "自制工具/程序化关卡/房间生成配置文件 v0.2")]
    public class RoomGenerationSettingsV2 : ScriptableObject
    {
        #region 参数
        
        [TitleGroup("生成参数", "Generation Parameters", TitleAlignments.Centered)]
        [HideLabel, InlineProperty]
        public RoomGenParamsV2 parameters = new RoomGenParamsV2();
        
        #endregion

        #region Tilemap 引用
        
        [TitleGroup("Tilemap 配置", "Tilemap Configuration", TitleAlignments.Centered)]
        [LabelText("墙壁层 Tilemap"), Tooltip("用于放置墙壁和地面")]
        public Tilemap wallTilemap;
        
        [TitleGroup("Tilemap 配置")]
        [LabelText("平台层 Tilemap"), Tooltip("用于放置单向平台")]
        public Tilemap platformTilemap;
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 验证配置
        /// </summary>
        public void Validate()
        {
            parameters?.Validate();
        }
        
        /// <summary>
        /// 重置为默认参数
        /// </summary>
        [Button("重置为默认参数", ButtonSizes.Medium)]
        [TitleGroup("操作")]
        public void ResetToDefaults()
        {
            parameters = new RoomGenParamsV2();
        }
        
        /// <summary>
        /// 复制参数到另一个配置
        /// </summary>
        public void CopyParametersTo(RoomGenerationSettingsV2 other)
        {
            if (other == null) return;
            
            // 使用 JSON 序列化进行深拷贝
            string json = JsonUtility.ToJson(parameters);
            other.parameters = JsonUtility.FromJson<RoomGenParamsV2>(json);
        }
        
        #endregion

        #region 编辑器预览
        
#if UNITY_EDITOR
        [TitleGroup("预览信息", "Preview Info", TitleAlignments.Centered)]
        [ShowInInspector, ReadOnly, LabelText("预计房间数量")]
        public int EstimatedRoomCount
        {
            get
            {
                if (parameters == null) return 0;
                return parameters.targetRoomCount;
            }
        }
        
        [ShowInInspector, ReadOnly, LabelText("预计开阔度")]
        public string EstimatedOpenness
        {
            get
            {
                if (parameters == null) return "N/A";
                return $"{parameters.roomFillRatio * 0.6f:P0} ~ {parameters.roomFillRatio * 0.8f:P0}";
            }
        }
#endif
        
        #endregion
    }
}
