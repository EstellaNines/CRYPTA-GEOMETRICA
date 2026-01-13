using System;
using UnityEngine;
using Sirenix.OdinInspector;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 关卡生成参数
    /// 配置多房间关卡的生成规则
    /// </summary>
    [Serializable]
    public class LevelGeneratorParams
    {
        #region 房间数量配置
        
        [TitleGroup("房间数量", "Room Count Settings", TitleAlignments.Centered)]
        [LabelText("战斗房间数量"), Range(1, 10)]
        [Tooltip("关卡中战斗房间的数量（不包括入口和Boss房间）")]
        public int combatRoomCount = 5;
        
        #endregion

        #region 间距配置
        
        [TitleGroup("间距配置", "Spacing Settings", TitleAlignments.Centered)]
        [LabelText("房间间距"), Range(20, 40)]
        [Tooltip("相邻房间之间的水平间距（格子数）")]
        public int roomSpacing = 20;
        
        #endregion

        #region 走廊配置
        
        [TitleGroup("走廊配置", "Corridor Settings", TitleAlignments.Centered)]
        [LabelText("走廊宽度"), Range(4, 10)]
        [Tooltip("走廊的宽度（格子数），建议至少5以确保玩家通行")]
        public int corridorWidth = 5;
        
        [TitleGroup("走廊配置")]
        [LabelText("高低差走廊概率"), Range(0f, 1f)]
        [Tooltip("生成带平台的高低差走廊的概率")]
        public float elevatedCorridorChance = 0.5f;
        
        [TitleGroup("走廊配置")]
        [LabelText("走廊平台间距"), Range(3, 8)]
        [Tooltip("高低差走廊中平台之间的垂直间距")]
        public int corridorPlatformSpacing = 4;
        
        #endregion

        #region 随机种子
        
        [TitleGroup("随机种子", "Seed Settings", TitleAlignments.Centered)]
        [LabelText("关卡种子")]
        [Tooltip("用于生成关卡的随机种子，留空则自动生成")]
        public string levelSeed = "";
        
        [TitleGroup("随机种子")]
        [LabelText("使用随机种子")]
        [Tooltip("是否每次使用新的随机种子")]
        public bool useRandomSeed = true;
        
        #endregion

        #region 房间Y轴随机范围
        
        [TitleGroup("房间位置", "Room Position Settings", TitleAlignments.Centered)]
        [LabelText("Y偏移范围"), MinMaxSlider(-30, 30, true)]
        [Tooltip("战斗房间相对于基准线的Y轴随机偏移范围，增加范围可以让房间落差更大，确保走廊不重叠")]
        public Vector2Int yOffsetRange = new Vector2Int(-15, 15);
        
        #endregion

        #region 房间生成参数引用
        
        [TitleGroup("房间生成参数", "Room Generation Parameters", TitleAlignments.Centered)]
        [LabelText("入口房间参数")]
        [Tooltip("入口房间的生成参数")]
        [InlineProperty, HideLabel]
        [FoldoutGroup("房间生成参数/入口房间")]
        public RoomGenParamsV2 entranceRoomParams;
        
        [TitleGroup("房间生成参数")]
        [LabelText("战斗房间参数")]
        [Tooltip("战斗房间的生成参数")]
        [InlineProperty, HideLabel]
        [FoldoutGroup("房间生成参数/战斗房间")]
        public RoomGenParamsV2 combatRoomParams;
        
        [TitleGroup("房间生成参数")]
        [LabelText("Boss房间参数")]
        [Tooltip("Boss房间的生成参数")]
        [InlineProperty, HideLabel]
        [FoldoutGroup("房间生成参数/Boss房间")]
        public RoomGenParamsV2 bossRoomParams;
        
        #endregion

        #region 构造函数
        
        public LevelGeneratorParams()
        {
            // 初始化默认参数
            InitializeDefaultParams();
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 初始化默认房间参数
        /// </summary>
        public void InitializeDefaultParams()
        {
            // 入口房间参数（不生成敌人）
            if (entranceRoomParams == null)
            {
                entranceRoomParams = new RoomGenParamsV2
                {
                    roomType = RoomType.Entrance,
                    roomWidth = 20,
                    roomHeight = 15,
                    useRandomSeed = false,
                    maxEnemies = 0  // 入口房间不生成敌人
                };
            }
            
            // 战斗房间参数
            if (combatRoomParams == null)
            {
                combatRoomParams = new RoomGenParamsV2
                {
                    roomType = RoomType.Combat,
                    roomWidth = 40,
                    roomHeight = 25,
                    useRandomSeed = false
                };
            }
            
            // Boss房间参数
            if (bossRoomParams == null)
            {
                bossRoomParams = new RoomGenParamsV2
                {
                    roomType = RoomType.Boss,
                    roomWidth = 80,
                    roomHeight = 50,
                    useRandomSeed = false
                };
            }
        }
        
        /// <summary>
        /// 验证并修正参数
        /// </summary>
        public void Validate()
        {
            // 确保房间数量合理
            combatRoomCount = Mathf.Clamp(combatRoomCount, 1, 10);
            
            // 确保间距合理
            roomSpacing = Mathf.Max(roomSpacing, 20);
            
            // 确保走廊宽度至少能容纳玩家（建议5格以上）
            corridorWidth = Mathf.Max(corridorWidth, 4);
            
            // 确保Y偏移范围合理
            if (yOffsetRange.x > yOffsetRange.y)
            {
                yOffsetRange = new Vector2Int(yOffsetRange.y, yOffsetRange.x);
            }
            
            // 验证子参数
            entranceRoomParams?.Validate();
            combatRoomParams?.Validate();
            bossRoomParams?.Validate();
        }
        
        /// <summary>
        /// 获取指定房间类型的参数
        /// </summary>
        public RoomGenParamsV2 GetParamsForRoomType(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Entrance:
                    return entranceRoomParams;
                case RoomType.Boss:
                    return bossRoomParams;
                case RoomType.Combat:
                default:
                    return combatRoomParams;
            }
        }
        
        /// <summary>
        /// 复制参数
        /// </summary>
        public LevelGeneratorParams Clone()
        {
            return new LevelGeneratorParams
            {
                combatRoomCount = this.combatRoomCount,
                roomSpacing = this.roomSpacing,
                corridorWidth = this.corridorWidth,
                elevatedCorridorChance = this.elevatedCorridorChance,
                corridorPlatformSpacing = this.corridorPlatformSpacing,
                levelSeed = this.levelSeed,
                useRandomSeed = this.useRandomSeed,
                yOffsetRange = this.yOffsetRange,
                entranceRoomParams = this.entranceRoomParams,
                combatRoomParams = this.combatRoomParams,
                bossRoomParams = this.bossRoomParams
            };
        }
        
        #endregion
    }
}
