using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// L型走廊生成器
    /// 在相邻房间之间生成L型走廊连接
    /// </summary>
    public class LCorridorBuilder
    {
        #region 字段
        
        private readonly LevelGeneratorParams parameters;
        private readonly System.Random random;
        
        // 走廊参数
        private readonly int corridorWidth;
        private readonly int corridorHeight;
        private readonly int maxJumpHeight;
        private readonly bool hasDoubleJump;
        private readonly int platformSpacing;
        
        // 统计信息
        private int corridorsBuilt;
        private int platformsPlaced;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 已生成的走廊数量
        /// </summary>
        public int CorridorsBuilt => corridorsBuilt;
        
        /// <summary>
        /// 已放置的平台数量
        /// </summary>
        public int PlatformsPlaced => platformsPlaced;
        
        #endregion

        #region 构造函数
        
        public LCorridorBuilder(LevelGeneratorParams parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
            
            // 从参数获取走廊配置
            this.corridorWidth = 3;  // 固定3格宽
            this.corridorHeight = 3; // 固定3格高（内部空气层）
            this.platformSpacing = parameters.corridorPlatformSpacing;
            
            // 从战斗房间参数获取跳跃配置
            if (parameters.combatRoomParams != null)
            {
                this.maxJumpHeight = parameters.combatRoomParams.maxJumpHeight;
                this.hasDoubleJump = parameters.combatRoomParams.hasDoubleJump;
            }
            else
            {
                this.maxJumpHeight = 5;
                this.hasDoubleJump = true;
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 为关卡中所有相邻房间生成走廊
        /// </summary>
        /// <param name="levelData">关卡数据</param>
        /// <returns>生成的走廊列表</returns>
        public List<CorridorData> BuildCorridors(LevelData levelData)
        {
            if (levelData == null || levelData.rooms == null || levelData.rooms.Count < 2)
            {
                Debug.LogWarning("[LCorridorBuilder] 房间数量不足，无法生成走廊");
                return new List<CorridorData>();
            }
            
            corridorsBuilt = 0;
            platformsPlaced = 0;
            
            var corridors = new List<CorridorData>();
            
            // 按顺序连接相邻房间
            for (int i = 0; i < levelData.rooms.Count - 1; i++)
            {
                PlacedRoom fromRoom = levelData.rooms[i];
                PlacedRoom toRoom = levelData.rooms[i + 1];
                
                CorridorData corridor = BuildCorridor(fromRoom, toRoom, i);
                
                if (corridor != null)
                {
                    // 检测走廊与其他房间的重叠
                    bool hasOverlap = false;
                    foreach (var room in levelData.rooms)
                    {
                        if (corridor.OverlapsWithRoom(room))
                        {
                            Debug.LogWarning($"[LCorridorBuilder] 走廊 {corridor.id} 与房间 {room.id} 重叠");
                            hasOverlap = true;
                            break;
                        }
                    }
                    
                    if (!hasOverlap)
                    {
                        corridors.Add(corridor);
                        corridorsBuilt++;
                    }
                }
            }
            
            Debug.Log($"[LCorridorBuilder] 生成了 {corridorsBuilt} 条走廊，放置了 {platformsPlaced} 个平台");
            
            return corridors;
        }
        
        /// <summary>
        /// 在两个房间之间生成走廊
        /// </summary>
        /// <param name="fromRoom">起始房间</param>
        /// <param name="toRoom">目标房间</param>
        /// <param name="corridorId">走廊ID</param>
        /// <returns>走廊数据</returns>
        public CorridorData BuildCorridor(PlacedRoom fromRoom, PlacedRoom toRoom, int corridorId)
        {
            if (fromRoom == null || toRoom == null)
            {
                Debug.LogWarning("[LCorridorBuilder] 房间为空，无法生成走廊");
                return null;
            }
            
            // 获取连接点
            Vector2Int startPoint = fromRoom.WorldExit;
            Vector2Int endPoint = toRoom.WorldEntrance;
            
            // 创建走廊数据
            CorridorData corridor = new CorridorData(corridorId, fromRoom.id, toRoom.id)
            {
                startPoint = startPoint,
                endPoint = endPoint,
                width = corridorWidth,
                height = corridorHeight
            };
            
            // 计算高度差
            int deltaY = endPoint.y - startPoint.y;
            
            // 判断是否为直线走廊
            if (deltaY == 0)
            {
                corridor.isStraight = true;
                corridor.cornerPoint = new Vector2Int((startPoint.x + endPoint.x) / 2, startPoint.y);
            }
            else
            {
                corridor.isStraight = false;
                // 拐角点设置在中点
                int cornerX = (startPoint.x + endPoint.x) / 2;
                corridor.cornerPoint = new Vector2Int(cornerX, startPoint.y);
            }
            
            // 计算是否需要平台
            int effectiveJumpHeight = hasDoubleJump ? maxJumpHeight * 2 - 2 : maxJumpHeight;
            int absDeltaY = Mathf.Abs(deltaY);
            
            if (absDeltaY > effectiveJumpHeight)
            {
                // 需要添加平台
                InjectPlatforms(corridor, effectiveJumpHeight);
            }
            
            Debug.Log($"[LCorridorBuilder] 生成走廊: {corridor}");
            
            return corridor;
        }
        
        #endregion

        #region 平台注入
        
        /// <summary>
        /// 在走廊垂直段注入平台
        /// </summary>
        /// <param name="corridor">走廊数据</param>
        /// <param name="effectiveJumpHeight">有效跳跃高度</param>
        private void InjectPlatforms(CorridorData corridor, int effectiveJumpHeight)
        {
            int deltaY = corridor.HeightDifference;
            int absDeltaY = Mathf.Abs(deltaY);
            
            // 计算需要的平台数量
            int platformCount = Mathf.CeilToInt((float)absDeltaY / effectiveJumpHeight);
            
            if (platformCount <= 0) return;
            
            // 计算平台间距
            int spacing = absDeltaY / (platformCount + 1);
            spacing = Mathf.Max(spacing, platformSpacing);
            
            // 确定垂直段的起始和结束Y坐标
            int lowerY = Mathf.Min(corridor.startPoint.y, corridor.endPoint.y);
            int higherY = Mathf.Max(corridor.startPoint.y, corridor.endPoint.y);
            
            // 在垂直段均匀放置平台
            for (int i = 1; i <= platformCount; i++)
            {
                int platformY = lowerY + spacing * i;
                
                // 确保平台不超出垂直段范围
                if (platformY >= higherY) break;
                
                Vector2Int platformPos = new Vector2Int(corridor.cornerPoint.x, platformY);
                corridor.platforms.Add(platformPos);
                platformsPlaced++;
                
                Debug.Log($"[LCorridorBuilder] 在走廊 {corridor.id} 放置平台: {platformPos}");
            }
        }
        
        #endregion

        #region 碰撞检测
        
        /// <summary>
        /// 检测走廊是否与房间列表中的任何房间重叠
        /// </summary>
        /// <param name="corridor">走廊数据</param>
        /// <param name="rooms">房间列表</param>
        /// <returns>重叠的房间列表</returns>
        public List<PlacedRoom> GetOverlappingRooms(CorridorData corridor, List<PlacedRoom> rooms)
        {
            var overlapping = new List<PlacedRoom>();
            
            if (corridor == null || rooms == null) return overlapping;
            
            foreach (var room in rooms)
            {
                if (corridor.OverlapsWithRoom(room))
                {
                    overlapping.Add(room);
                }
            }
            
            return overlapping;
        }
        
        #endregion
    }
}
