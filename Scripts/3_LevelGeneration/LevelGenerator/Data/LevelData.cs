using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 关卡数据
    /// 存储整个关卡的所有房间和走廊信息
    /// </summary>
    [Serializable]
    public class LevelData
    {
        #region 字段
        
        /// <summary>
        /// 关卡种子
        /// </summary>
        public string levelSeed;
        
        /// <summary>
        /// 所有已放置房间
        /// </summary>
        public List<PlacedRoom> rooms = new List<PlacedRoom>();
        
        /// <summary>
        /// 所有走廊数据
        /// </summary>
        public List<CorridorData> corridors = new List<CorridorData>();
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 房间数量
        /// </summary>
        public int RoomCount => rooms?.Count ?? 0;
        
        /// <summary>
        /// 走廊数量
        /// </summary>
        public int CorridorCount => corridors?.Count ?? 0;
        
        /// <summary>
        /// 关卡总边界（包围所有房间和走廊）
        /// </summary>
        public RectInt TotalBounds
        {
            get
            {
                if (rooms == null || rooms.Count == 0)
                {
                    return new RectInt(0, 0, 0, 0);
                }
                
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;
                
                // 计算房间边界
                foreach (var room in rooms)
                {
                    var bounds = room.WorldBounds;
                    minX = Mathf.Min(minX, bounds.x);
                    minY = Mathf.Min(minY, bounds.y);
                    maxX = Mathf.Max(maxX, bounds.xMax);
                    maxY = Mathf.Max(maxY, bounds.yMax);
                }
                
                // 计算走廊边界
                if (corridors != null)
                {
                    foreach (var corridor in corridors)
                    {
                        var bounds = corridor.Bounds;
                        minX = Mathf.Min(minX, bounds.x);
                        minY = Mathf.Min(minY, bounds.y);
                        maxX = Mathf.Max(maxX, bounds.xMax);
                        maxY = Mathf.Max(maxY, bounds.yMax);
                    }
                }
                
                return new RectInt(minX, minY, maxX - minX, maxY - minY);
            }
        }
        
        /// <summary>
        /// 获取入口房间
        /// </summary>
        public PlacedRoom EntranceRoom => rooms?.FirstOrDefault(r => r.roomType == RoomType.Entrance);
        
        /// <summary>
        /// 获取 Boss 房间
        /// </summary>
        public PlacedRoom BossRoom => rooms?.FirstOrDefault(r => r.roomType == RoomType.Boss);
        
        /// <summary>
        /// 获取所有战斗房间
        /// </summary>
        public List<PlacedRoom> CombatRooms => rooms?.Where(r => r.roomType == RoomType.Combat).ToList() ?? new List<PlacedRoom>();
        
        #endregion

        #region 构造函数
        
        public LevelData()
        {
            rooms = new List<PlacedRoom>();
            corridors = new List<CorridorData>();
        }
        
        public LevelData(string seed)
        {
            levelSeed = seed;
            rooms = new List<PlacedRoom>();
            corridors = new List<CorridorData>();
        }
        
        #endregion

        #region 房间操作
        
        /// <summary>
        /// 根据 ID 获取房间
        /// </summary>
        public PlacedRoom GetRoom(int id)
        {
            return rooms?.FirstOrDefault(r => r.id == id);
        }
        
        /// <summary>
        /// 添加房间
        /// </summary>
        public void AddRoom(PlacedRoom room)
        {
            if (room == null) return;
            rooms ??= new List<PlacedRoom>();
            rooms.Add(room);
        }
        
        /// <summary>
        /// 移除房间
        /// </summary>
        public bool RemoveRoom(int id)
        {
            var room = GetRoom(id);
            if (room != null)
            {
                rooms.Remove(room);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 更新房间位置
        /// </summary>
        public void UpdateRoomPosition(int id, Vector2Int newPosition)
        {
            var room = GetRoom(id);
            if (room != null)
            {
                room.worldPosition = newPosition;
            }
        }
        
        #endregion

        #region 走廊操作
        
        /// <summary>
        /// 添加走廊
        /// </summary>
        public void AddCorridor(CorridorData corridor)
        {
            if (corridor == null) return;
            corridors ??= new List<CorridorData>();
            corridors.Add(corridor);
        }
        
        /// <summary>
        /// 设置走廊列表
        /// </summary>
        public void SetCorridors(List<CorridorData> newCorridors)
        {
            corridors = newCorridors ?? new List<CorridorData>();
        }
        
        /// <summary>
        /// 根据ID获取走廊
        /// </summary>
        public CorridorData GetCorridor(int id)
        {
            return corridors?.Find(c => c.id == id);
        }
        
        /// <summary>
        /// 获取连接指定房间的走廊
        /// </summary>
        public List<CorridorData> GetCorridorsForRoom(int roomId)
        {
            if (corridors == null) return new List<CorridorData>();
            return corridors.FindAll(c => c.fromRoomId == roomId || c.toRoomId == roomId);
        }
        
        /// <summary>
        /// 清空所有走廊
        /// </summary>
        public void ClearCorridors()
        {
            corridors?.Clear();
        }
        
        #endregion

        #region 碰撞检测
        
        /// <summary>
        /// 检测所有房间重叠情况
        /// </summary>
        /// <param name="padding">额外间距</param>
        /// <returns>重叠的房间 ID 对列表</returns>
        public List<(int roomA, int roomB)> GetOverlappingRooms(int padding = 0)
        {
            var overlaps = new List<(int, int)>();
            
            if (rooms == null || rooms.Count < 2) return overlaps;
            
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (rooms[i].OverlapsWith(rooms[j], padding))
                    {
                        overlaps.Add((rooms[i].id, rooms[j].id));
                    }
                }
            }
            
            return overlaps;
        }
        
        /// <summary>
        /// 检测指定房间是否与其他房间重叠
        /// </summary>
        public bool IsRoomOverlapping(int roomId, int padding = 0)
        {
            var room = GetRoom(roomId);
            if (room == null) return false;
            
            foreach (var other in rooms)
            {
                if (other.id != roomId && room.OverlapsWith(other, padding))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取与指定房间重叠的所有房间
        /// </summary>
        public List<PlacedRoom> GetOverlappingRoomsFor(int roomId, int padding = 0)
        {
            var result = new List<PlacedRoom>();
            var room = GetRoom(roomId);
            if (room == null) return result;
            
            foreach (var other in rooms)
            {
                if (other.id != roomId && room.OverlapsWith(other, padding))
                {
                    result.Add(other);
                }
            }
            
            return result;
        }
        
        #endregion

        #region 工具方法
        
        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            rooms?.Clear();
            corridors?.Clear();
            levelSeed = "";
        }
        
        /// <summary>
        /// 初始化运行时数据（反序列化后调用）
        /// </summary>
        public void InitializeRuntimeData()
        {
            if (rooms == null) rooms = new List<PlacedRoom>();
            if (corridors == null) corridors = new List<CorridorData>();
        }
        
        /// <summary>
        /// 获取下一个可用的房间 ID
        /// </summary>
        public int GetNextRoomId()
        {
            if (rooms == null || rooms.Count == 0) return 0;
            return rooms.Max(r => r.id) + 1;
        }
        
        public override string ToString()
        {
            var bounds = TotalBounds;
            return $"LevelData(Seed:{levelSeed}, Rooms:{RoomCount}, Corridors:{CorridorCount}, Bounds:{bounds.width}x{bounds.height})";
        }
        
        #endregion
    }
}
