using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 走廊生成器
    /// 在房间之间生成连接走廊
    /// </summary>
    public class CorridorBuilder
    {
        #region 字段
        
        private readonly RoomGenParamsV2 parameters;
        private readonly System.Random random;
        
        // 统计信息
        private int corridorsBuilt;
        private int totalTilesDug;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 已生成的走廊数量
        /// </summary>
        public int CorridorsBuilt => corridorsBuilt;
        
        /// <summary>
        /// 挖掘的总瓦片数
        /// </summary>
        public int TotalTilesDug => totalTilesDug;
        
        #endregion

        #region 构造函数
        
        public CorridorBuilder(RoomGenParamsV2 parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 根据房间图生成所有走廊
        /// </summary>
        /// <param name="roomGraph">房间连接图</param>
        /// <param name="roomData">房间数据（用于挖掘）</param>
        public void BuildCorridors(RoomGraph roomGraph, RoomDataV2 roomData)
        {
            if (roomGraph == null || roomData == null)
            {
                Debug.LogWarning("[CorridorBuilder] 参数为空，跳过走廊生成");
                return;
            }
            
            corridorsBuilt = 0;
            totalTilesDug = 0;
            
            foreach (var edge in roomGraph.finalEdges)
            {
                RoomRegion roomA = roomGraph.GetRoom(edge.roomA);
                RoomRegion roomB = roomGraph.GetRoom(edge.roomB);
                
                if (roomA == null || roomB == null)
                {
                    Debug.LogWarning($"[CorridorBuilder] 找不到房间 {edge.roomA} 或 {edge.roomB}");
                    continue;
                }
                
                // 生成走廊
                BuildCorridor(roomA, roomB, roomData);
                corridorsBuilt++;
            }
            
            Debug.Log($"[CorridorBuilder] 生成了 {corridorsBuilt} 条走廊，挖掘了 {totalTilesDug} 个瓦片");
        }
        
        /// <summary>
        /// 在两个房间之间生成走廊
        /// </summary>
        public void BuildCorridor(RoomRegion roomA, RoomRegion roomB, RoomDataV2 roomData)
        {
            // 获取连接点（房间边界上最近的点）
            Vector2Int startPoint = roomA.GetClosestPointTo(roomB.center);
            Vector2Int endPoint = roomB.GetClosestPointTo(roomA.center);
            
            // 决定走廊类型
            if (random.NextDouble() < parameters.lShapeCorridorChance)
            {
                // L 形走廊
                BuildLShapeCorridor(startPoint, endPoint, roomData);
            }
            else
            {
                // 直线走廊（如果可能）或 L 形
                BuildLShapeCorridor(startPoint, endPoint, roomData);
            }
        }
        
        #endregion

        #region 走廊生成算法
        
        /// <summary>
        /// 生成 L 形走廊
        /// </summary>
        private void BuildLShapeCorridor(Vector2Int start, Vector2Int end, RoomDataV2 roomData)
        {
            int width = parameters.corridorWidth;
            
            // 决定先水平还是先垂直
            bool horizontalFirst = random.NextDouble() < 0.5;
            
            Vector2Int corner;
            if (horizontalFirst)
            {
                // 先水平后垂直
                corner = new Vector2Int(end.x, start.y);
            }
            else
            {
                // 先垂直后水平
                corner = new Vector2Int(start.x, end.y);
            }
            
            // 绘制第一段
            DrawCorridorSegment(start, corner, width, roomData);
            
            // 绘制第二段
            DrawCorridorSegment(corner, end, width, roomData);
            
            // 在拐角处额外挖掘，确保转弯顺畅
            DigCorner(corner, width, roomData);
        }
        
        /// <summary>
        /// 绘制走廊段（水平或垂直）
        /// </summary>
        private void DrawCorridorSegment(Vector2Int from, Vector2Int to, int width, RoomDataV2 roomData)
        {
            int halfWidth = width / 2;
            
            int minX = Mathf.Min(from.x, to.x);
            int maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y);
            int maxY = Mathf.Max(from.y, to.y);
            
            // 水平走廊
            if (from.y == to.y || Mathf.Abs(from.y - to.y) < Mathf.Abs(from.x - to.x))
            {
                int centerY = (from.y + to.y) / 2;
                
                for (int x = minX; x <= maxX; x++)
                {
                    for (int dy = -halfWidth; dy <= halfWidth; dy++)
                    {
                        int y = centerY + dy;
                        if (roomData.IsValid(x, y))
                        {
                            roomData.SetTile(x, y, TileType.Floor);
                            totalTilesDug++;
                        }
                    }
                }
            }
            // 垂直走廊
            else
            {
                int centerX = (from.x + to.x) / 2;
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int dx = -halfWidth; dx <= halfWidth; dx++)
                    {
                        int x = centerX + dx;
                        if (roomData.IsValid(x, y))
                        {
                            roomData.SetTile(x, y, TileType.Floor);
                            totalTilesDug++;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 在拐角处挖掘额外空间
        /// </summary>
        private void DigCorner(Vector2Int corner, int width, RoomDataV2 roomData)
        {
            int halfWidth = width / 2;
            
            // 挖掘一个正方形区域确保转弯顺畅
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                for (int dy = -halfWidth; dy <= halfWidth; dy++)
                {
                    int x = corner.x + dx;
                    int y = corner.y + dy;
                    
                    if (roomData.IsValid(x, y))
                    {
                        roomData.SetTile(x, y, TileType.Floor);
                        totalTilesDug++;
                    }
                }
            }
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 检查两点之间是否可以直线连接（无障碍）
        /// </summary>
        public static bool CanConnectDirectly(Vector2Int from, Vector2Int to, RoomDataV2 roomData, int width)
        {
            // 使用 Bresenham 算法检查路径
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            
            int x = from.x;
            int y = from.y;
            int halfWidth = width / 2;
            
            while (true)
            {
                // 检查当前位置周围是否有足够空间
                for (int ddx = -halfWidth; ddx <= halfWidth; ddx++)
                {
                    for (int ddy = -halfWidth; ddy <= halfWidth; ddy++)
                    {
                        if (!roomData.IsValid(x + ddx, y + ddy))
                        {
                            return false;
                        }
                    }
                }
                
                if (x == to.x && y == to.y) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取两个房间之间的最佳连接方向
        /// </summary>
        public static Vector2Int GetConnectionDirection(RoomRegion from, RoomRegion to)
        {
            Vector2Int diff = to.center - from.center;
            
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            {
                return new Vector2Int(Math.Sign(diff.x), 0);
            }
            else
            {
                return new Vector2Int(0, Math.Sign(diff.y));
            }
        }
        
        #endregion
    }
}
