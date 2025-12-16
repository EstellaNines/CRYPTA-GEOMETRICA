using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.MultiRoom;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间放置器
    /// 在 BSP 叶节点内生成房间区域
    /// </summary>
    public class RoomPlacer
    {
        #region 字段
        
        private readonly RoomGenParamsV2 parameters;
        private readonly System.Random random;
        
        // 房间 ID 计数器
        private int nextRoomId;
        
        #endregion

        #region 构造函数
        
        public RoomPlacer(RoomGenParamsV2 parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
            this.nextRoomId = 0;
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 在所有 BSP 叶节点中放置房间
        /// </summary>
        /// <param name="bspRoot">BSP 树根节点</param>
        /// <param name="roomData">房间数据（用于挖掘）</param>
        /// <returns>生成的房间列表</returns>
        public List<RoomRegion> PlaceRooms(BSPNode bspRoot, RoomDataV2 roomData)
        {
            if (bspRoot == null)
            {
                Debug.LogWarning("[RoomPlacer] BSP 根节点为空");
                return new List<RoomRegion>();
            }
            
            nextRoomId = 0;
            List<BSPNode> leaves = bspRoot.GetLeaves();
            List<RoomRegion> rooms = new List<RoomRegion>();
            
            foreach (var leaf in leaves)
            {
                RoomRegion room = CreateRoomInLeaf(leaf);
                
                if (room != null)
                {
                    leaf.room = room;
                    rooms.Add(room);
                    
                    // 挖掘房间区域
                    DigRoom(room, roomData);
                }
            }
            
            Debug.Log($"[RoomPlacer] 放置了 {rooms.Count} 个房间");
            
            return rooms;
        }
        
        /// <summary>
        /// 标记入口和出口所在的房间
        /// </summary>
        public void MarkEntranceExitRooms(List<RoomRegion> rooms, Vector2Int startPos, Vector2Int endPos)
        {
            if (rooms == null || rooms.Count == 0) return;
            
            RoomRegion entranceRoom = null;
            RoomRegion exitRoom = null;
            float minDistToEntrance = float.MaxValue;
            float minDistToExit = float.MaxValue;
            
            foreach (var room in rooms)
            {
                float distToEntrance = Vector2Int.Distance(room.center, startPos);
                float distToExit = Vector2Int.Distance(room.center, endPos);
                
                if (distToEntrance < minDistToEntrance)
                {
                    minDistToEntrance = distToEntrance;
                    entranceRoom = room;
                }
                
                if (distToExit < minDistToExit)
                {
                    minDistToExit = distToExit;
                    exitRoom = room;
                }
            }
            
            if (entranceRoom != null)
            {
                entranceRoom.isEntrance = true;
                entranceRoom.roomType = RoomType.Entrance;
                Debug.Log($"[RoomPlacer] 入口房间: {entranceRoom}");
            }
            
            if (exitRoom != null)
            {
                exitRoom.isExit = true;
                exitRoom.roomType = RoomType.Exit;
                Debug.Log($"[RoomPlacer] 出口房间: {exitRoom}");
            }
            
            // 分配其他房间类型
            AssignRoomTypes(rooms);
        }
        
        /// <summary>
        /// 分配房间类型（休息房/战斗房/连接房）
        /// </summary>
        private void AssignRoomTypes(List<RoomRegion> rooms)
        {
            // 统计已分配的房间
            int combatCount = 0;
            int restCount = 0;
            
            foreach (var room in rooms)
            {
                // 跳过已分配的入口/出口房
                if (room.isEntrance || room.isExit) continue;
                
                // 根据房间大小和位置分配类型
                // 大房间更可能是战斗房，小房间更可能是连接房或休息房
                float areaRatio = (float)room.Area / (parameters.roomWidth * parameters.roomHeight);
                
                if (areaRatio > 0.15f && combatCount < 2)
                {
                    // 较大的房间作为战斗房
                    room.roomType = RoomType.Combat;
                    combatCount++;
                }
                else if (areaRatio < 0.08f || room.Width < 8 || room.Height < 6)
                {
                    // 小房间作为连接房
                    room.roomType = RoomType.Connector;
                }
                else if (restCount < 1)
                {
                    // 中等房间作为休息房
                    room.roomType = RoomType.Rest;
                    restCount++;
                }
                else
                {
                    // 其他作为战斗房
                    room.roomType = RoomType.Combat;
                    combatCount++;
                }
                
                Debug.Log($"[RoomPlacer] 房间类型分配: {room}");
            }
        }
        
        #endregion

        #region 房间生成
        
        /// <summary>
        /// 在叶节点内创建房间
        /// </summary>
        private RoomRegion CreateRoomInLeaf(BSPNode leaf)
        {
            RectInt leafBounds = leaf.bounds;
            
            // 计算房间尺寸
            int roomWidth = CalculateRoomDimension(leafBounds.width);
            int roomHeight = CalculateRoomDimension(leafBounds.height);
            
            // 确保房间至少能容纳玩家 (2×2) + 走廊 (3) + 余量
            int minSize = parameters.corridorWidth + 3;
            roomWidth = Mathf.Max(roomWidth, minSize);
            roomHeight = Mathf.Max(roomHeight, minSize);
            
            // 确保房间不超过叶节点边界
            roomWidth = Mathf.Min(roomWidth, leafBounds.width - parameters.roomPadding * 2);
            roomHeight = Mathf.Min(roomHeight, leafBounds.height - parameters.roomPadding * 2);
            
            // 如果空间不足，返回 null
            if (roomWidth < minSize || roomHeight < minSize)
            {
                Debug.LogWarning($"[RoomPlacer] 叶节点 {leafBounds} 空间不足，跳过");
                return null;
            }
            
            // 计算房间位置（随机偏移）
            int maxOffsetX = leafBounds.width - roomWidth - parameters.roomPadding * 2;
            int maxOffsetY = leafBounds.height - roomHeight - parameters.roomPadding * 2;
            
            int offsetX = maxOffsetX > 0 ? random.Next(0, maxOffsetX + 1) : 0;
            int offsetY = maxOffsetY > 0 ? random.Next(0, maxOffsetY + 1) : 0;
            
            int roomX = leafBounds.x + parameters.roomPadding + offsetX;
            int roomY = leafBounds.y + parameters.roomPadding + offsetY;
            
            // 创建房间区域
            RectInt roomBounds = new RectInt(roomX, roomY, roomWidth, roomHeight);
            RoomRegion room = new RoomRegion(nextRoomId++, roomBounds);
            room.bspBounds = leafBounds;
            
            return room;
        }
        
        /// <summary>
        /// 计算房间尺寸（基于填充率）
        /// </summary>
        private int CalculateRoomDimension(int leafDimension)
        {
            // 基础尺寸 = 叶节点尺寸 × 填充率
            float baseSize = leafDimension * parameters.roomFillRatio;
            
            // 添加随机变化 (±10%)
            float variation = (float)(random.NextDouble() * 0.2 - 0.1);
            float finalSize = baseSize * (1 + variation);
            
            return Mathf.RoundToInt(finalSize);
        }
        
        /// <summary>
        /// 挖掘房间区域
        /// </summary>
        private void DigRoom(RoomRegion room, RoomDataV2 roomData)
        {
            if (room == null || roomData == null) return;
            
            // 挖掘主体区域
            roomData.DigRect(room.bounds);
            
            // 记录地面格子
            room.floorTiles.Clear();
            for (int x = room.bounds.x; x < room.bounds.xMax; x++)
            {
                for (int y = room.bounds.y; y < room.bounds.yMax; y++)
                {
                    room.floorTiles.Add(new Vector2Int(x, y));
                }
            }
        }
        
        #endregion

        #region 房间变体（可扩展）
        
        /// <summary>
        /// 创建 L 形房间（未来扩展）
        /// </summary>
        private RoomRegion CreateLShapeRoom(BSPNode leaf)
        {
            // TODO: 实现 L 形房间
            // 1. 创建两个相交的矩形
            // 2. 合并为一个房间区域
            return CreateRoomInLeaf(leaf);
        }
        
        /// <summary>
        /// 创建 T 形房间（未来扩展）
        /// </summary>
        private RoomRegion CreateTShapeRoom(BSPNode leaf)
        {
            // TODO: 实现 T 形房间
            return CreateRoomInLeaf(leaf);
        }
        
        /// <summary>
        /// 创建不规则房间（未来扩展）
        /// </summary>
        private RoomRegion CreateIrregularRoom(BSPNode leaf)
        {
            // TODO: 使用细胞自动机或其他算法生成不规则形状
            return CreateRoomInLeaf(leaf);
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 获取两个房间之间的最佳连接点
        /// </summary>
        public static (Vector2Int, Vector2Int) GetConnectionPoints(RoomRegion roomA, RoomRegion roomB)
        {
            // 找到两个房间边界上最近的点
            Vector2Int pointA = roomA.GetClosestPointTo(roomB.center);
            Vector2Int pointB = roomB.GetClosestPointTo(roomA.center);
            
            return (pointA, pointB);
        }
        
        /// <summary>
        /// 检查两个房间是否相邻（边界接触）
        /// </summary>
        public static bool AreAdjacent(RoomRegion roomA, RoomRegion roomB, int tolerance = 2)
        {
            // 检查水平相邻
            bool horizontalAdjacent = 
                (Mathf.Abs(roomA.Right - roomB.Left) <= tolerance || 
                 Mathf.Abs(roomB.Right - roomA.Left) <= tolerance) &&
                !(roomA.Top < roomB.Bottom || roomB.Top < roomA.Bottom);
            
            // 检查垂直相邻
            bool verticalAdjacent = 
                (Mathf.Abs(roomA.Top - roomB.Bottom) <= tolerance || 
                 Mathf.Abs(roomB.Top - roomA.Bottom) <= tolerance) &&
                !(roomA.Right < roomB.Left || roomB.Right < roomA.Left);
            
            return horizontalAdjacent || verticalAdjacent;
        }
        
        /// <summary>
        /// 获取房间的连接方向（相对于另一个房间）
        /// </summary>
        public static Vector2Int GetConnectionDirection(RoomRegion from, RoomRegion to)
        {
            Vector2Int diff = to.center - from.center;
            
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            {
                return new Vector2Int(System.Math.Sign(diff.x), 0);
            }
            else
            {
                return new Vector2Int(0, System.Math.Sign(diff.y));
            }
        }
        
        #endregion
    }
}
