using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 关卡布局 ScriptableObject
    /// 用于保存和加载关卡布局配置
    /// </summary>
    [CreateAssetMenu(fileName = "LevelLayout", menuName = "自制工具/程序化关卡/关卡布局配置")]
    public class LevelLayoutSO : ScriptableObject
    {
        #region 关卡信息
        
        [TitleGroup("关卡信息", "Level Information", TitleAlignments.Centered)]
        [LabelText("关卡名称")]
        public string levelName = "NewLevel";
        
        [TitleGroup("关卡信息")]
        [LabelText("关卡种子")]
        [ReadOnly]
        public string levelSeed;
        
        [TitleGroup("关卡信息")]
        [LabelText("创建时间")]
        [ReadOnly]
        public string createTime;
        
        #endregion

        #region 房间配置
        
        [TitleGroup("房间配置", "Room Configurations", TitleAlignments.Centered)]
        [LabelText("房间列表")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "GetLabel")]
        public List<PlacedRoomConfig> roomConfigs = new List<PlacedRoomConfig>();
        
        #endregion

        #region 走廊配置
        
        [TitleGroup("走廊配置", "Corridor Configurations", TitleAlignments.Centered)]
        [LabelText("走廊列表")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        public List<CorridorConfig> corridorConfigs = new List<CorridorConfig>();
        
        #endregion

        #region 生成参数
        
        [TitleGroup("生成参数", "Generator Parameters", TitleAlignments.Centered)]
        [LabelText("关卡生成参数")]
        [InlineProperty, HideLabel]
        public LevelGeneratorParams generatorParams = new LevelGeneratorParams();
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 从 LevelData 保存布局
        /// </summary>
        public void SaveFromLevelData(LevelData levelData, LevelGeneratorParams parameters)
        {
            if (levelData == null) return;
            
            levelSeed = levelData.levelSeed;
            createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            generatorParams = parameters?.Clone() ?? new LevelGeneratorParams();
            
            // 保存房间配置
            roomConfigs.Clear();
            foreach (var room in levelData.rooms)
            {
                roomConfigs.Add(new PlacedRoomConfig
                {
                    id = room.id,
                    roomType = room.roomType,
                    seed = room.seed,
                    worldPosition = room.worldPosition,
                    width = room.width,
                    height = room.height
                });
            }
            
            // 走廊功能已删除
            corridorConfigs.Clear();
            
            Debug.Log($"[LevelLayoutSO] 已保存关卡布局: {levelName}, 房间数: {roomConfigs.Count}");
        }
        
        /// <summary>
        /// 加载为 LevelData（仅布局信息，不包含房间详细数据）
        /// </summary>
        public LevelData LoadToLevelData()
        {
            LevelData levelData = new LevelData(levelSeed);
            
            // 加载房间配置
            foreach (var config in roomConfigs)
            {
                PlacedRoom room = new PlacedRoom
                {
                    id = config.id,
                    roomType = config.roomType,
                    seed = config.seed,
                    worldPosition = config.worldPosition,
                    width = config.width,
                    height = config.height
                };
                levelData.rooms.Add(room);
            }
            
            // 走廊功能已删除
            
            Debug.Log($"[LevelLayoutSO] 已加载关卡布局: {levelName}, 房间数: {levelData.RoomCount}");
            
            return levelData;
        }
        
        /// <summary>
        /// 清空布局
        /// </summary>
        [Button("清空布局", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
        public void ClearLayout()
        {
            roomConfigs.Clear();
            corridorConfigs.Clear();
            levelSeed = "";
            Debug.Log($"[LevelLayoutSO] 已清空关卡布局: {levelName}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 已放置房间配置（用于序列化）
    /// </summary>
    [Serializable]
    public class PlacedRoomConfig
    {
        [HorizontalGroup("Row", Width = 40)]
        [LabelText("ID"), LabelWidth(20)]
        public int id;
        
        [HorizontalGroup("Row", Width = 80)]
        [LabelText("类型"), LabelWidth(30)]
        public RoomType roomType;
        
        [HorizontalGroup("Row")]
        [LabelText("位置"), LabelWidth(30)]
        public Vector2Int worldPosition;
        
        [HorizontalGroup("Row", Width = 50)]
        [LabelText("宽"), LabelWidth(20)]
        public int width;
        
        [HorizontalGroup("Row", Width = 50)]
        [LabelText("高"), LabelWidth(20)]
        public int height;
        
        [HideInInspector]
        public string seed;
        
        /// <summary>
        /// 获取显示标签
        /// </summary>
        public string GetLabel()
        {
            return $"#{id} [{roomType}] ({width}x{height})";
        }
    }
    
    /// <summary>
    /// 走廊配置（用于序列化）
    /// </summary>
    [Serializable]
    public class CorridorConfig
    {
        [HorizontalGroup("Row", Width = 80)]
        [LabelText("起始"), LabelWidth(30)]
        public int fromRoomId;
        
        [HorizontalGroup("Row", Width = 80)]
        [LabelText("目标"), LabelWidth(30)]
        public int toRoomId;
        
        [HorizontalGroup("Row")]
        [LabelText("起点"), LabelWidth(30)]
        public Vector2Int startPoint;
        
        [HorizontalGroup("Row")]
        [LabelText("终点"), LabelWidth(30)]
        public Vector2Int endPoint;
        
        [HorizontalGroup("Row", Width = 60)]
        [LabelText("高低差"), LabelWidth(40)]
        public bool isElevated;
        
        [HideInInspector]
        public int width = 3;
        
        [HideInInspector]
        public List<Vector2Int> pathPoints = new List<Vector2Int>();
    }
}
