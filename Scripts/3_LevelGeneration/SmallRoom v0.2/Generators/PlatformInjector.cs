using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 平台注入器
    /// 基于跳跃可达性分析在垂直落差处注入单向平台
    /// </summary>
    public class PlatformInjector
    {
        #region 字段
        
        private readonly RoomGenParamsV2 parameters;
        private readonly System.Random random;
        
        // 跳跃参数（基于玩家物理）
        private readonly float jumpForce;
        private readonly int maxJumpHeight;
        private readonly bool hasDoubleJump;
        
        // 统计信息
        private int platformsPlaced;
        private int gapsAnalyzed;
        private int unreachableFixed;
        
        // 排斥区域（避免平台过于密集）
        private List<RectInt> exclusionZones;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 放置的平台数量
        /// </summary>
        public int PlatformsPlaced => platformsPlaced;
        
        /// <summary>
        /// 分析的落差数量
        /// </summary>
        public int GapsAnalyzed => gapsAnalyzed;
        
        /// <summary>
        /// 修复的不可达区域数量
        /// </summary>
        public int UnreachableFixed => unreachableFixed;
        
        #endregion

        #region 构造函数
        
        public PlatformInjector(RoomGenParamsV2 parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
            
            // 从参数获取跳跃配置
            this.jumpForce = parameters.playerJumpForce;
            this.maxJumpHeight = parameters.maxJumpHeight;
            this.hasDoubleJump = parameters.hasDoubleJump;
            
            this.exclusionZones = new List<RectInt>();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 执行平台注入
        /// </summary>
        public void InjectPlatforms(RoomDataV2 roomData)
        {
            if (roomData == null)
            {
                Debug.LogWarning("[PlatformInjector] 房间数据为空");
                return;
            }
            
            if (!parameters.enableJumpAnalysis)
            {
                Debug.Log("[PlatformInjector] 跳跃分析已禁用");
                return;
            }
            
            platformsPlaced = 0;
            gapsAnalyzed = 0;
            unreachableFixed = 0;
            exclusionZones.Clear();
            
            // 添加入口/出口保护区
            AddEntranceExitExclusion(roomData);
            
            // Step 1: 在每个房间内部注入阶梯式平台
            InjectPlatformsInRooms(roomData);
            
            // Step 2: 分析垂直落差并注入平台
            AnalyzeAndInjectByColumn(roomData);
            
            // Step 3: 分析水平跳跃距离
            AnalyzeHorizontalGaps(roomData);
            
            // Step 4: 验证可达性并修复
            FixUnreachableAreas(roomData);
            
            Debug.Log($"[PlatformInjector] 平台注入完成: 放置={platformsPlaced}, 分析落差={gapsAnalyzed}, 修复不可达={unreachableFixed}");
        }
        
        #endregion

        #region 房间内部平台注入
        
        /// <summary>
        /// 在每个房间内部注入阶梯式平台
        /// </summary>
        private void InjectPlatformsInRooms(RoomDataV2 roomData)
        {
            if (roomData.roomGraph == null || roomData.roomGraph.rooms == null) return;
            
            foreach (var room in roomData.roomGraph.rooms)
            {
                // 跳过太小的房间
                if (room.Height <= maxJumpHeight * 2) continue;
                
                // 计算房间内需要的平台数量
                int effectiveJumpHeight = hasDoubleJump ? maxJumpHeight * 2 - 2 : maxJumpHeight;
                int platformsNeeded = Mathf.CeilToInt((float)(room.Height - 4) / effectiveJumpHeight) - 1;
                
                if (platformsNeeded <= 0) continue;
                
                // 计算平台间距
                int spacing = (room.Height - 4) / (platformsNeeded + 1);
                spacing = Mathf.Max(spacing, effectiveJumpHeight - 2);
                
                // 在房间内放置阶梯式平台（左右交替）
                for (int i = 1; i <= platformsNeeded && platformsPlaced < parameters.maxPlatforms; i++)
                {
                    int platformY = room.Bottom + 2 + spacing * i;
                    
                    // 左右交替放置，形成之字形路径
                    int platformX;
                    if (i % 2 == 1)
                    {
                        // 奇数层放在左侧
                        platformX = room.Left + room.Width / 4;
                    }
                    else
                    {
                        // 偶数层放在右侧
                        platformX = room.Right - room.Width / 4;
                    }
                    
                    // 验证位置有效性
                    if (IsValidPlatformPosition(platformX, platformY, roomData))
                    {
                        PlacePlatform(platformX, platformY, roomData);
                    }
                }
            }
        }
        
        #endregion

        #region 垂直落差分析
        
        /// <summary>
        /// 按列分析垂直落差
        /// </summary>
        private void AnalyzeAndInjectByColumn(RoomDataV2 roomData)
        {
            int edgePadding = parameters.edgePadding;
            int playerWidth = 2; // 玩家宽度
            
            for (int x = edgePadding + playerWidth; x < roomData.width - edgePadding - playerWidth; x++)
            {
                if (platformsPlaced >= parameters.maxPlatforms) break;
                
                // 跳过排斥区
                if (IsInExclusionZone(x, roomData.height / 2)) continue;
                
                AnalyzeColumn(x, roomData);
            }
        }
        
        /// <summary>
        /// 分析单列的垂直落差
        /// </summary>
        private void AnalyzeColumn(int x, RoomDataV2 roomData)
        {
            int lastSolidY = -1;
            bool inGap = false;
            int gapStartY = 0;
            
            for (int y = 0; y < roomData.height; y++)
            {
                TileType tile = roomData.GetTile(x, y);
                bool isSolid = (tile == TileType.Wall || tile == TileType.Platform);
                
                if (isSolid)
                {
                    if (inGap && lastSolidY >= 0)
                    {
                        // 结束一个落差
                        int gapHeight = y - lastSolidY - 1;
                        gapsAnalyzed++;
                        
                        // 检查是否需要平台
                        if (gapHeight > maxJumpHeight)
                        {
                            InjectPlatformsInGap(x, lastSolidY + 1, y - 1, roomData);
                        }
                    }
                    
                    lastSolidY = y;
                    inGap = false;
                }
                else if (tile == TileType.Floor)
                {
                    if (!inGap && lastSolidY >= 0)
                    {
                        inGap = true;
                        gapStartY = y;
                    }
                }
            }
        }
        
        /// <summary>
        /// 在落差中注入平台
        /// </summary>
        private void InjectPlatformsInGap(int x, int gapStartY, int gapEndY, RoomDataV2 roomData)
        {
            int gapHeight = gapEndY - gapStartY + 1;
            
            // 计算需要的平台数量
            int effectiveJumpHeight = hasDoubleJump ? maxJumpHeight * 2 - 2 : maxJumpHeight;
            int platformsNeeded = Mathf.CeilToInt((float)gapHeight / effectiveJumpHeight);
            
            if (platformsNeeded <= 0) return;
            
            // 计算平台间距
            int spacing = gapHeight / (platformsNeeded + 1);
            spacing = Mathf.Max(spacing, 3); // 最小间距
            
            for (int i = 1; i <= platformsNeeded && platformsPlaced < parameters.maxPlatforms; i++)
            {
                int platformY = gapStartY + spacing * i;
                
                // 验证位置有效性
                if (!IsValidPlatformPosition(x, platformY, roomData)) continue;
                
                // 放置平台
                PlacePlatform(x, platformY, roomData);
            }
        }
        
        #endregion

        #region 水平跳跃分析
        
        /// <summary>
        /// 分析水平跳跃距离
        /// </summary>
        private void AnalyzeHorizontalGaps(RoomDataV2 roomData)
        {
            int edgePadding = parameters.edgePadding;
            
            // 扫描每一行，查找水平落差
            for (int y = edgePadding + 2; y < roomData.height - edgePadding - 2; y++)
            {
                if (platformsPlaced >= parameters.maxPlatforms) break;
                
                int lastSolidX = -1;
                
                for (int x = 0; x < roomData.width; x++)
                {
                    TileType tile = roomData.GetTile(x, y);
                    bool isSolid = (tile == TileType.Wall || tile == TileType.Platform);
                    
                    if (isSolid)
                    {
                        if (lastSolidX >= 0)
                        {
                            int horizontalGap = x - lastSolidX - 1;
                            
                            // 如果水平距离超过跳跃距离
                            if (horizontalGap > parameters.maxHorizontalJump)
                            {
                                // 在中间放置平台
                                int midX = (lastSolidX + x) / 2;
                                if (IsValidPlatformPosition(midX, y, roomData))
                                {
                                    PlacePlatform(midX, y, roomData);
                                }
                            }
                        }
                        lastSolidX = x;
                    }
                    else if (tile == TileType.Floor)
                    {
                        // 继续扫描
                    }
                    else
                    {
                        // 遇到墙壁，重置
                        lastSolidX = -1;
                    }
                }
            }
        }
        
        #endregion

        #region 可达性修复
        
        /// <summary>
        /// 修复不可达区域
        /// </summary>
        private void FixUnreachableAreas(RoomDataV2 roomData)
        {
            // 获取从入口可达的所有格子
            HashSet<Vector2Int> reachable = GetReachableTiles(roomData, roomData.startPos);
            
            // 检查出口是否可达
            if (!reachable.Contains(roomData.endPos))
            {
                Debug.LogWarning("[PlatformInjector] 出口不可达，尝试修复...");
                
                // 在入口和出口之间的路径上添加平台
                FixPathBetween(roomData.startPos, roomData.endPos, roomData);
            }
            
            // 检查所有房间是否可达
            if (roomData.roomGraph != null)
            {
                foreach (var room in roomData.roomGraph.rooms)
                {
                    if (!reachable.Contains(room.center))
                    {
                        // 找到最近的可达点
                        Vector2Int nearestReachable = FindNearestReachable(room.center, reachable);
                        if (nearestReachable != Vector2Int.zero)
                        {
                            FixPathBetween(nearestReachable, room.center, roomData);
                            unreachableFixed++;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取从起点可达的所有格子（考虑跳跃）
        /// </summary>
        private HashSet<Vector2Int> GetReachableTiles(RoomDataV2 roomData, Vector2Int start)
        {
            HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            
            queue.Enqueue(start);
            reachable.Add(start);
            
            // 移动方向：上下左右 + 跳跃方向
            Vector2Int[] moves = GetJumpMoves();
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                foreach (var move in moves)
                {
                    Vector2Int next = current + move;
                    
                    if (roomData.IsValid(next.x, next.y) &&
                        !reachable.Contains(next) &&
                        roomData.GetTile(next.x, next.y) != TileType.Wall)
                    {
                        // 检查是否可以到达（简化的跳跃检查）
                        if (CanReach(current, next, roomData))
                        {
                            reachable.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }
            
            return reachable;
        }
        
        /// <summary>
        /// 获取跳跃移动向量
        /// </summary>
        private Vector2Int[] GetJumpMoves()
        {
            List<Vector2Int> moves = new List<Vector2Int>
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };
            
            // 添加跳跃范围内的移动
            for (int dx = -parameters.maxHorizontalJump; dx <= parameters.maxHorizontalJump; dx++)
            {
                for (int dy = -maxJumpHeight; dy <= maxJumpHeight; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    moves.Add(new Vector2Int(dx, dy));
                }
            }
            
            return moves.ToArray();
        }
        
        /// <summary>
        /// 检查是否可以从 from 到达 to
        /// </summary>
        private bool CanReach(Vector2Int from, Vector2Int to, RoomDataV2 roomData)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int dy = to.y - from.y;
            
            // 水平移动
            if (dy == 0 && dx <= 1) return true;
            
            // 向下移动（掉落）
            if (dy < 0) return true;
            
            // 向上移动（跳跃）
            if (dy > 0)
            {
                int effectiveJumpHeight = hasDoubleJump ? maxJumpHeight * 2 - 2 : maxJumpHeight;
                return dy <= effectiveJumpHeight && dx <= parameters.maxHorizontalJump;
            }
            
            return dx <= parameters.maxHorizontalJump;
        }
        
        /// <summary>
        /// 在两点之间修复路径（递归放置多个平台）
        /// </summary>
        private void FixPathBetween(Vector2Int from, Vector2Int to, RoomDataV2 roomData, int depth = 0)
        {
            // 防止无限递归
            if (depth > 10 || platformsPlaced >= parameters.maxPlatforms) return;
            
            int dy = Mathf.Abs(to.y - from.y);
            int dx = Mathf.Abs(to.x - from.x);
            int effectiveJumpHeight = hasDoubleJump ? maxJumpHeight * 2 - 2 : maxJumpHeight;
            
            // 如果高度差超过跳跃高度，需要添加平台
            if (dy > effectiveJumpHeight || dx > parameters.maxHorizontalJump)
            {
                // 计算中间点
                int midY = (from.y + to.y) / 2;
                int midX = (from.x + to.x) / 2;
                
                // 尝试在中间点放置平台
                bool placed = false;
                
                // 在中间点附近搜索有效位置
                for (int offsetX = 0; offsetX <= 3 && !placed; offsetX++)
                {
                    for (int sign = -1; sign <= 1 && !placed; sign += 2)
                    {
                        int testX = midX + offsetX * sign;
                        if (IsValidPlatformPosition(testX, midY, roomData))
                        {
                            PlacePlatform(testX, midY, roomData);
                            placed = true;
                            
                            // 递归修复上半部分和下半部分
                            Vector2Int mid = new Vector2Int(testX, midY);
                            FixPathBetween(from, mid, roomData, depth + 1);
                            FixPathBetween(mid, to, roomData, depth + 1);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 在两点之间修复路径（重载，保持向后兼容）
        /// </summary>
        private void FixPathBetween(Vector2Int from, Vector2Int to, RoomDataV2 roomData)
        {
            FixPathBetween(from, to, roomData, 0);
        }
        
        /// <summary>
        /// 找到最近的可达点
        /// </summary>
        private Vector2Int FindNearestReachable(Vector2Int target, HashSet<Vector2Int> reachable)
        {
            Vector2Int nearest = Vector2Int.zero;
            float minDist = float.MaxValue;
            
            foreach (var point in reachable)
            {
                float dist = Vector2Int.Distance(point, target);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = point;
                }
            }
            
            return nearest;
        }
        
        #endregion

        #region 平台放置
        
        /// <summary>
        /// 放置平台
        /// </summary>
        private void PlacePlatform(int centerX, int y, RoomDataV2 roomData)
        {
            int platformWidth = parameters.minPlatformWidth;
            int halfWidth = platformWidth / 2;
            
            // 随机变化宽度
            if (random.NextDouble() < 0.3f)
            {
                platformWidth = random.Next(parameters.minPlatformWidth, parameters.maxPlatformWidth + 1);
                halfWidth = platformWidth / 2;
            }
            
            // 放置平台瓦片
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                int x = centerX + dx;
                if (roomData.IsValid(x, y) && roomData.GetTile(x, y) == TileType.Floor)
                {
                    roomData.SetTile(x, y, TileType.Platform);
                }
            }
            
            // 添加排斥区
            int exclusionRadius = parameters.platformExclusionRadius;
            exclusionZones.Add(new RectInt(
                centerX - exclusionRadius,
                y - exclusionRadius,
                exclusionRadius * 2,
                exclusionRadius * 2
            ));
            
            platformsPlaced++;
        }
        
        /// <summary>
        /// 检查平台位置是否有效
        /// </summary>
        private bool IsValidPlatformPosition(int x, int y, RoomDataV2 roomData)
        {
            // 边界检查
            if (!roomData.IsValid(x, y)) return false;
            
            // 必须是空气
            if (roomData.GetTile(x, y) != TileType.Floor) return false;
            
            // 检查排斥区
            if (IsInExclusionZone(x, y)) return false;
            
            // 检查头顶空间（玩家高度 2 格）
            int playerHeight = 2;
            for (int dy = 1; dy <= playerHeight; dy++)
            {
                if (roomData.IsValid(x, y + dy) && roomData.GetTile(x, y + dy) == TileType.Wall)
                {
                    return false;
                }
            }
            
            // 检查平台宽度是否足够
            int minWidth = parameters.minPlatformWidth;
            int halfWidth = minWidth / 2;
            int validCount = 0;
            
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                if (roomData.IsValid(x + dx, y) && roomData.GetTile(x + dx, y) == TileType.Floor)
                {
                    validCount++;
                }
            }
            
            return validCount >= minWidth;
        }
        
        /// <summary>
        /// 检查是否在排斥区内
        /// </summary>
        private bool IsInExclusionZone(int x, int y)
        {
            Vector2Int pos = new Vector2Int(x, y);
            foreach (var zone in exclusionZones)
            {
                if (zone.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 添加入口/出口保护区
        /// </summary>
        private void AddEntranceExitExclusion(RoomDataV2 roomData)
        {
            int protectionRadius = 6;
            
            // 入口保护区
            exclusionZones.Add(new RectInt(
                roomData.startPos.x - protectionRadius,
                roomData.startPos.y - protectionRadius,
                protectionRadius * 2,
                protectionRadius * 2
            ));
            
            // 出口保护区
            exclusionZones.Add(new RectInt(
                roomData.endPos.x - protectionRadius,
                roomData.endPos.y - protectionRadius,
                protectionRadius * 2,
                protectionRadius * 2
            ));
        }
        
        #endregion
    }
}
