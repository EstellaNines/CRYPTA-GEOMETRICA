using System;
using UnityEngine;
using Sirenix.OdinInspector;
using CryptaGeometrica.LevelGeneration.MultiRoom;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间生成参数 v0.2
    /// 包含 BSP 分割、图连接、双向游走、跳跃分析等高级参数
    /// </summary>
    [Serializable]
    public class RoomGenParamsV2
    {
        #region 基础设置
        
        [TitleGroup("基础设置", "Basic Settings", TitleAlignments.Centered)]
        [LabelText("房间类型"), Tooltip("选择房间生成类型")]
        public MultiRoom.RoomType roomType = MultiRoom.RoomType.Combat;
        
        [TitleGroup("基础设置")]
        [LabelText("房间宽度"), Range(20, 100)]
        public int roomWidth = 40;
        
        [TitleGroup("基础设置")]
        [LabelText("房间高度"), Range(15, 60)]
        public int roomHeight = 25;
        
        [TitleGroup("基础设置")]
        [LabelText("随机种子")]
        public string seed = "";
        
        [TitleGroup("基础设置")]
        [LabelText("使用随机种子")]
        public bool useRandomSeed = true;
        
        #endregion

        #region 出入口设置
        
        [TitleGroup("出入口设置", "Anchors Settings", TitleAlignments.Centered)]
        [LabelText("强制生成锚点"), Tooltip("确保出入口区域被清理")]
        public bool enforceAnchors = true;
        
        [TitleGroup("出入口设置")]
        [LabelText("左侧入口Y坐标"), Tooltip("-1 表示随机")]
        public int entranceY = -1;
        
        [TitleGroup("出入口设置")]
        [LabelText("右侧出口Y坐标"), Tooltip("-1 表示随机")]
        public int exitY = -1;
        
        [TitleGroup("出入口设置")]
        [LabelText("出入口清理深度"), Range(3, 8)]
        public int entranceClearDepth = 5;
        
        #endregion

        #region BSP 设置
        
        [TitleGroup("BSP 空间分割", "BSP Settings", TitleAlignments.Centered)]
        [LabelText("目标房间数量"), Range(3, 8), Tooltip("生成的小房间数量")]
        public int targetRoomCount = 4;
        
        [TitleGroup("BSP 空间分割")]
        [LabelText("最小分割尺寸"), Range(6, 16), Tooltip("BSP 叶节点的最小尺寸")]
        public int minBSPSize = 8;
        
        [TitleGroup("BSP 空间分割")]
        [LabelText("最大分割深度"), Range(3, 8), Tooltip("越深房间越多")]
        public int maxBSPDepth = 6;
        
        [TitleGroup("BSP 空间分割")]
        [LabelText("分割比例范围"), MinMaxSlider(0.3f, 0.7f, true)]
        public Vector2 splitRatioRange = new Vector2(0.35f, 0.65f);
        
        #endregion

        #region 房间设置
        
        [TitleGroup("房间生成", "Room Settings", TitleAlignments.Centered)]
        [LabelText("房间填充率"), Range(0.5f, 0.95f), Tooltip("房间占 BSP 叶节点的比例")]
        public float roomFillRatio = 0.65f;
        
        [TitleGroup("房间生成")]
        [LabelText("房间边距"), Range(1, 4), Tooltip("房间与 BSP 边界的最小距离")]
        public int roomPadding = 2;
        
        #endregion

        #region 图连接设置
        
        [TitleGroup("图连接", "Graph Settings", TitleAlignments.Centered)]
        [LabelText("额外边比例"), Range(0f, 0.5f), Tooltip("在 MST 基础上额外添加的边，形成环路")]
        public float extraEdgeRatio = 0.2f;
        
        #endregion

        #region 走廊设置
        
        [TitleGroup("走廊生成", "Corridor Settings", TitleAlignments.Centered)]
        [LabelText("走廊宽度"), Range(2, 5), Tooltip("玩家 2×2，建议至少 3")]
        public int corridorWidth = 3;
        
        [TitleGroup("走廊生成")]
        [LabelText("L形走廊概率"), Range(0f, 1f)]
        public float lShapeCorridorChance = 0.7f;
        
        #endregion

        #region 连通性保障
        
        [TitleGroup("连通性保障", "Connectivity Settings", TitleAlignments.Centered)]
        [LabelText("启用双向游走"), Tooltip("入口→出口 + 出口→入口 双向游走确保连通")]
        public bool enableBidirectionalWalk = true;
        
        [TitleGroup("连通性保障")]
        [LabelText("游走刷子尺寸"), Range(2, 5), Tooltip("玩家 2×2，建议 3")]
        public int walkBrushSize = 3;
        
        [TitleGroup("连通性保障")]
        [LabelText("水平移动偏好"), Range(0.5f, 0.9f), Tooltip("游走时水平移动的概率")]
        public float horizontalBias = 0.7f;
        
        #endregion

        #region 平台注入
        
        [TitleGroup("平台注入", "Platform Injection", TitleAlignments.Centered)]
        [LabelText("启用跳跃可达性分析")]
        public bool enableJumpAnalysis = true;
        
        [TitleGroup("平台注入")]
        [LabelText("玩家最大跳跃高度"), Range(3, 8), Tooltip("二段跳总高度")]
        public int maxJumpHeight = 5;
        
        [TitleGroup("平台注入")]
        [LabelText("玩家最大跳跃距离"), Range(4, 10)]
        public int maxJumpDistance = 7;
        
        [TitleGroup("平台注入")]
        [LabelText("最大平台数量"), Range(0, 12)]
        public int maxPlatforms = 6;
        
        [TitleGroup("平台注入")]
        [LabelText("平台宽度范围"), MinMaxSlider(2, 10, true)]
        public Vector2Int platformWidthRange = new Vector2Int(3, 6);
        
        [TitleGroup("平台注入")]
        [LabelText("最小平台宽度"), Range(2, 5)]
        public int minPlatformWidth = 3;
        
        [TitleGroup("平台注入")]
        [LabelText("最大平台宽度"), Range(4, 10)]
        public int maxPlatformWidth = 6;
        
        [TitleGroup("平台注入")]
        [LabelText("平台排斥半径"), Range(3, 8), Tooltip("平台之间的最小间距")]
        public int platformExclusionRadius = 4;
        
        [TitleGroup("平台注入")]
        [LabelText("最大水平跳跃距离"), Range(3, 8)]
        public int maxHorizontalJump = 5;
        
        [TitleGroup("平台注入")]
        [LabelText("玩家跳跃力"), Range(5, 12), Tooltip("用于计算跳跃高度")]
        public float playerJumpForce = 8f;
        
        [TitleGroup("平台注入")]
        [LabelText("允许二段跳")]
        public bool hasDoubleJump = true;
        
        #endregion

        #region 敌人生成
        
        [TitleGroup("敌人生成", "Spawn Settings", TitleAlignments.Centered)]
        [LabelText("房间难度"), Tooltip("决定敌人配置")]
        public RoomDifficulty roomDifficulty = RoomDifficulty.Normal;
        
        [TitleGroup("敌人生成")]
        [LabelText("最小地面连续长度"), Range(3, 8), Tooltip("地面敌人生成点需要的最小连续地面")]
        public int minGroundSpan = 4;
        
        [TitleGroup("敌人生成")]
        [LabelText("最小空中高度"), Range(3, 8), Tooltip("空中敌人生成点距地面的最小高度")]
        public int minAirHeight = 4;
        
        [TitleGroup("敌人生成")]
        [LabelText("最大敌人数量"), Range(0, 10)]
        public int maxEnemies = 5;
        
        [TitleGroup("敌人生成")]
        [LabelText("敌人最小间距"), Range(3, 10)]
        public int minSpawnDistance = 6;
        
        #endregion

        #region 安全区设置
        
        [TitleGroup("安全区", "Safety Settings", TitleAlignments.Centered)]
        [LabelText("边缘留空"), Range(1, 4)]
        public int edgePadding = 2;
        
        #endregion

        #region 特殊房间配置
        
        [TitleGroup("特殊房间配置", "Special Room Settings", TitleAlignments.Centered)]
        [LabelText("入口房间：平台数量"), Range(2, 5)]
        [ShowIf("roomType", MultiRoom.RoomType.Entrance)]
        public int entrancePlatformCount = 3;
        
        [TitleGroup("特殊房间配置")]
        [LabelText("入口房间：平台高度范围")]
        [ShowIf("roomType", MultiRoom.RoomType.Entrance)]
        public Vector2Int entrancePlatformHeightRange = new Vector2Int(10, 25);
        
        [TitleGroup("特殊房间配置")]
        [LabelText("Boss房间：竞技场宽度"), Range(60, 120)]
        [ShowIf("roomType", MultiRoom.RoomType.Boss)]
        public int bossArenaWidth = 80;
        
        [TitleGroup("特殊房间配置")]
        [LabelText("Boss房间：竞技场高度"), Range(40, 80)]
        [ShowIf("roomType", MultiRoom.RoomType.Boss)]
        public int bossArenaHeight = 50;
        
        [TitleGroup("特殊房间配置")]
        [LabelText("Boss房间：平台层数"), Range(2, 4)]
        [ShowIf("roomType", MultiRoom.RoomType.Boss)]
        public int bossPlatformLayers = 3;
        
        #endregion

        #region 验证方法
        
        /// <summary>
        /// 验证并修正参数
        /// </summary>
        public void Validate()
        {
            // 确保房间尺寸合理
            roomWidth = Mathf.Max(roomWidth, minBSPSize * 2);
            roomHeight = Mathf.Max(roomHeight, minBSPSize * 2);
            
            // 确保走廊宽度至少能容纳玩家 (2×2)
            corridorWidth = Mathf.Max(corridorWidth, 3);
            
            // 确保游走刷子尺寸至少能容纳玩家
            walkBrushSize = Mathf.Max(walkBrushSize, 3);
            
            // 确保平台宽度范围合理
            if (platformWidthRange.x > platformWidthRange.y)
            {
                platformWidthRange = new Vector2Int(platformWidthRange.y, platformWidthRange.x);
            }
            platformWidthRange.x = Mathf.Max(platformWidthRange.x, 3);
        }
        
        /// <summary>
        /// 克隆参数对象
        /// </summary>
        public RoomGenParamsV2 Clone()
        {
            return (RoomGenParamsV2)this.MemberwiseClone();
        }
        
        #endregion
    }
}
