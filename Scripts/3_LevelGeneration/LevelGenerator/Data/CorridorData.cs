using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// L型走廊数据
    /// 存储走廊的几何信息和瓦片数据
    /// </summary>
    [Serializable]
    public class CorridorData
    {
        #region 字段
        
        /// <summary>
        /// 走廊唯一ID
        /// </summary>
        public int id;
        
        /// <summary>
        /// 起始房间ID
        /// </summary>
        public int fromRoomId;
        
        /// <summary>
        /// 目标房间ID
        /// </summary>
        public int toRoomId;
        
        /// <summary>
        /// 起点（世界坐标，房间出口位置）
        /// </summary>
        public Vector2Int startPoint;
        
        /// <summary>
        /// 终点（世界坐标，房间入口位置）
        /// </summary>
        public Vector2Int endPoint;
        
        /// <summary>
        /// L型拐角点（世界坐标）
        /// </summary>
        public Vector2Int cornerPoint;
        
        /// <summary>
        /// 走廊宽度（格子数）
        /// </summary>
        public int width = 3;
        
        /// <summary>
        /// 走廊层高（内部空气层高度）
        /// </summary>
        public int height = 3;
        
        /// <summary>
        /// 平台位置列表（用于高度差较大时辅助玩家）
        /// </summary>
        public List<Vector2Int> platforms = new List<Vector2Int>();
        
        /// <summary>
        /// 走廊是否为直线（无需拐角）
        /// </summary>
        public bool isStraight;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 走廊边界（包围整个走廊的矩形）
        /// </summary>
        public RectInt Bounds
        {
            get
            {
                int minX = Mathf.Min(startPoint.x, Mathf.Min(cornerPoint.x, endPoint.x)) - width / 2;
                int maxX = Mathf.Max(startPoint.x, Mathf.Max(cornerPoint.x, endPoint.x)) + width / 2;
                int minY = Mathf.Min(startPoint.y, Mathf.Min(cornerPoint.y, endPoint.y)) - 1;
                int maxY = Mathf.Max(startPoint.y, Mathf.Max(cornerPoint.y, endPoint.y)) + height + 1;
                
                return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
        }
        
        /// <summary>
        /// 高度差（终点Y - 起点Y）
        /// </summary>
        public int HeightDifference => endPoint.y - startPoint.y;
        
        /// <summary>
        /// 是否需要平台（高度差超过跳跃高度）
        /// </summary>
        public bool NeedsPlatforms => platforms != null && platforms.Count > 0;
        
        #endregion

        #region 构造函数
        
        public CorridorData()
        {
            platforms = new List<Vector2Int>();
        }
        
        public CorridorData(int id, int fromRoomId, int toRoomId)
        {
            this.id = id;
            this.fromRoomId = fromRoomId;
            this.toRoomId = toRoomId;
            this.platforms = new List<Vector2Int>();
        }
        
        #endregion

        #region 瓦片生成
        
        /// <summary>
        /// 获取走廊所有瓦片（用于烘焙到Tilemap）
        /// 策略：先生成墙壁，再用空气覆盖内部和连接处
        /// </summary>
        /// <returns>瓦片位置和类型的枚举</returns>
        public IEnumerable<(Vector2Int pos, TileType type)> GetTiles()
        {
            var tiles = new Dictionary<Vector2Int, TileType>();
            
            if (isStraight)
            {
                // 直线走廊：向两端延伸1列连接房间
                int minX = startPoint.x - 1;
                int maxX = endPoint.x + 1;
                int baseY = startPoint.y;
                
                // 先生成墙壁框架（两端都连接房间，都不封闭）
                GenerateHorizontalWalls(tiles, minX, maxX, baseY, closeLeft: false, closeRight: false);
                // 再用空气填充内部
                GenerateHorizontalFloor(tiles, minX, maxX, baseY);
            }
            else
            {
                // L型走廊
                int cornerX = cornerPoint.x;
                int startY = this.startPoint.y;
                int endY = this.endPoint.y;
                bool goingUp = endY > startY;
                
                // 水平段1范围：起点-1 → 拐角X+width
                int h1MinX = startPoint.x - 1;
                int h1MaxX = cornerX + width;
                
                // 水平段2范围：拐角X → 终点+1
                int h2MinX = cornerX;
                int h2MaxX = endPoint.x + 1;
                
                // 先生成两个水平段的墙壁
                // 水平段1：左端连接房间（不封闭），右端连接垂直段（封闭）
                GenerateHorizontalWalls(tiles, h1MinX, h1MaxX, startY, closeLeft: false, closeRight: true);
                // 水平段2：左端连接垂直段（封闭），右端连接房间（不封闭）
                GenerateHorizontalWalls(tiles, h2MinX, h2MaxX, endY, closeLeft: true, closeRight: false);
                
                // 再用空气填充水平段内部
                GenerateHorizontalFloor(tiles, h1MinX, h1MaxX, startY);
                GenerateHorizontalFloor(tiles, h2MinX, h2MaxX, endY);
                
                // 垂直段处理
                int verticalStartY, verticalEndY;
                if (goingUp)
                {
                    verticalStartY = startY + height;
                    verticalEndY = endY;
                }
                else
                {
                    verticalStartY = endY + height;
                    verticalEndY = startY;
                }
                
                // 只有当垂直段有实际高度时才生成
                if (verticalStartY != verticalEndY)
                {
                    // 先生成垂直段墙壁
                    GenerateVerticalWalls(tiles, cornerX, verticalStartY, verticalEndY);
                    // 再用空气填充内部
                    GenerateVerticalFloor(tiles, cornerX, verticalStartY, verticalEndY);
                }
                
                // 处理两个水平段在拐角处重叠区域的墙壁
                // 当两个水平段高度接近时，它们的上下墙壁可能会阻挡对方的内部空间
                // 需要用空气覆盖重叠区域内属于任一水平段内部的位置
                int overlapMinY = Mathf.Min(startY, endY);
                int overlapMaxY = Mathf.Max(startY, endY) + height;
                for (int x = cornerX; x < cornerX + width; x++)
                {
                    for (int y = overlapMinY - 1; y <= overlapMaxY; y++)
                    {
                        // 判断是否在水平段1或水平段2的内部区域
                        bool inSegment1Interior = y >= startY && y < startY + height;
                        bool inSegment2Interior = y >= endY && y < endY + height;
                        if (inSegment1Interior || inSegment2Interior)
                        {
                            tiles[new Vector2Int(x, y)] = TileType.Floor;
                        }
                    }
                }
            }
            
            // 平台瓦片（使用Platform类型，会烘焙到platformTilemap）
            foreach (var platformPos in platforms)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tiles[new Vector2Int(platformPos.x + dx, platformPos.y)] = TileType.Platform;
                }
            }
            
            // 输出所有瓦片
            foreach (var kvp in tiles)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// 生成水平段墙壁（上下两条边界墙壁，可选择是否在两端生成封闭墙壁）
        /// </summary>
        /// <param name="closeLeft">左端是否生成封闭墙壁（与垂直段连接时需要）</param>
        /// <param name="closeRight">右端是否生成封闭墙壁（与垂直段连接时需要）</param>
        private void GenerateHorizontalWalls(Dictionary<Vector2Int, TileType> tiles, int minX, int maxX, int baseY, bool closeLeft = false, bool closeRight = false)
        {
            // 计算上下墙壁的实际范围
            int wallMinX = closeLeft ? minX - 1 : minX;
            int wallMaxX = closeRight ? maxX : maxX - 1;
            
            for (int x = wallMinX; x <= wallMaxX; x++)
            {
                // 底部墙壁
                tiles[new Vector2Int(x, baseY - 1)] = TileType.Wall;
                // 顶部墙壁
                tiles[new Vector2Int(x, baseY + height)] = TileType.Wall;
            }
            
            // 左端封闭墙壁（垂直方向）
            if (closeLeft)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    tiles[new Vector2Int(minX - 1, baseY + dy)] = TileType.Wall;
                }
            }
            
            // 右端封闭墙壁（垂直方向）
            if (closeRight)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    tiles[new Vector2Int(maxX, baseY + dy)] = TileType.Wall;
                }
            }
        }
        
        /// <summary>
        /// 生成水平段空气（内部填充）
        /// </summary>
        private void GenerateHorizontalFloor(Dictionary<Vector2Int, TileType> tiles, int minX, int maxX, int baseY)
        {
            for (int x = minX; x < maxX; x++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    tiles[new Vector2Int(x, baseY + dy)] = TileType.Floor;
                }
            }
        }
        
        /// <summary>
        /// 生成垂直段墙壁（只生成左右两条边界墙壁，不封闭两端）
        /// </summary>
        private void GenerateVerticalWalls(Dictionary<Vector2Int, TileType> tiles, int cornerX, int fromY, int toY)
        {
            int minY = Mathf.Min(fromY, toY);
            int maxY = Mathf.Max(fromY, toY);
            
            // 只生成左右边界墙壁，范围与空气相同
            for (int y = minY; y < maxY; y++)
            {
                // 左侧墙壁
                tiles[new Vector2Int(cornerX - 1, y)] = TileType.Wall;
                // 右侧墙壁
                tiles[new Vector2Int(cornerX + width, y)] = TileType.Wall;
            }
            // 两端不封闭，保持与水平段连通
        }
        
        /// <summary>
        /// 生成垂直段空气（内部填充）
        /// </summary>
        private void GenerateVerticalFloor(Dictionary<Vector2Int, TileType> tiles, int cornerX, int fromY, int toY)
        {
            int minY = Mathf.Min(fromY, toY);
            int maxY = Mathf.Max(fromY, toY);
            
            for (int y = minY; y < maxY; y++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tiles[new Vector2Int(cornerX + dx, y)] = TileType.Floor;
                }
            }
        }
        
        #endregion

        #region 碰撞检测
        
        /// <summary>
        /// 检测走廊是否与指定矩形重叠
        /// </summary>
        /// <param name="rect">要检测的矩形</param>
        /// <param name="padding">额外边距</param>
        /// <returns>是否重叠</returns>
        public bool OverlapsWith(RectInt rect, int padding = 0)
        {
            RectInt expandedBounds = new RectInt(
                Bounds.x - padding,
                Bounds.y - padding,
                Bounds.width + padding * 2,
                Bounds.height + padding * 2
            );
            
            return expandedBounds.Overlaps(rect);
        }
        
        /// <summary>
        /// 检测走廊是否与房间重叠（排除连接的房间）
        /// </summary>
        /// <param name="room">要检测的房间</param>
        /// <returns>是否重叠</returns>
        public bool OverlapsWithRoom(PlacedRoom room)
        {
            // 排除连接的房间
            if (room.id == fromRoomId || room.id == toRoomId)
            {
                return false;
            }
            
            return OverlapsWith(room.WorldBounds, 0);
        }
        
        #endregion

        #region 调试
        
        public override string ToString()
        {
            return $"Corridor#{id}[Room{fromRoomId}→Room{toRoomId}](Start:{startPoint}, End:{endPoint}, Corner:{cornerPoint}, ΔY:{HeightDifference}, Platforms:{platforms?.Count ?? 0})";
        }
        
        #endregion
    }
}
