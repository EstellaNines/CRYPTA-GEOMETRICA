using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 已放置房间数据
    /// 存储房间在关卡中的位置和生成数据
    /// </summary>
    [Serializable]
    public class PlacedRoom
    {
        #region 字段
        
        /// <summary>
        /// 房间唯一 ID
        /// </summary>
        public int id;
        
        /// <summary>
        /// 房间类型
        /// </summary>
        public RoomType roomType;
        
        /// <summary>
        /// 生成种子
        /// </summary>
        public string seed;
        
        /// <summary>
        /// 世界坐标偏移（房间左下角在关卡中的位置）
        /// </summary>
        public Vector2Int worldPosition;
        
        /// <summary>
        /// 房间数据（由 RoomGeneratorV2 生成）
        /// </summary>
        [NonSerialized]
        public RoomDataV2 roomData;
        
        /// <summary>
        /// 房间宽度（用于序列化保存）
        /// </summary>
        public int width;
        
        /// <summary>
        /// 房间高度（用于序列化保存）
        /// </summary>
        public int height;
        
        /// <summary>
        /// 入口本地坐标（用于序列化保存）
        /// </summary>
        public Vector2Int entranceLocalPos;
        
        /// <summary>
        /// 出口本地坐标（用于序列化保存）
        /// </summary>
        public Vector2Int exitLocalPos;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 世界边界（房间在关卡中的实际边界）
        /// </summary>
        public RectInt WorldBounds => new RectInt(
            worldPosition.x, 
            worldPosition.y, 
            width, 
            height
        );
        
        /// <summary>
        /// 左侧入口世界坐标
        /// </summary>
        public Vector2Int WorldEntrance
        {
            get
            {
                if (roomData != null)
                {
                    return worldPosition + roomData.startPos;
                }
                // 使用序列化保存的本地坐标
                if (entranceLocalPos != Vector2Int.zero)
                {
                    return worldPosition + entranceLocalPos;
                }
                // 默认入口位置（左侧中间偏下）
                return new Vector2Int(worldPosition.x, worldPosition.y + 3);
            }
        }
        
        /// <summary>
        /// 右侧出口世界坐标
        /// </summary>
        public Vector2Int WorldExit
        {
            get
            {
                if (roomData != null)
                {
                    return worldPosition + roomData.endPos;
                }
                // 使用序列化保存的本地坐标
                if (exitLocalPos != Vector2Int.zero)
                {
                    return worldPosition + exitLocalPos;
                }
                // 默认出口位置（右侧中间偏下）
                return new Vector2Int(worldPosition.x + width - 1, worldPosition.y + 3);
            }
        }
        
        #endregion

        #region 构造函数
        
        public PlacedRoom()
        {
        }
        
        public PlacedRoom(int id, RoomType roomType, Vector2Int worldPosition)
        {
            this.id = id;
            this.roomType = roomType;
            this.worldPosition = worldPosition;
        }
        
        public PlacedRoom(int id, RoomType roomType, Vector2Int worldPosition, RoomDataV2 roomData, string seed = "")
        {
            this.id = id;
            this.roomType = roomType;
            this.worldPosition = worldPosition;
            this.seed = seed;
            SetRoomData(roomData);
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 设置房间数据并同步尺寸和出入口位置
        /// </summary>
        public void SetRoomData(RoomDataV2 data)
        {
            roomData = data;
            if (data != null)
            {
                width = data.width;
                height = data.height;
                seed = data.seed;
                // 保存出入口本地坐标（用于序列化）
                entranceLocalPos = data.startPos;
                exitLocalPos = data.endPos;
            }
        }
        
        /// <summary>
        /// 检测与另一个房间是否重叠
        /// </summary>
        /// <param name="other">另一个房间</param>
        /// <param name="padding">额外间距（用于检测是否太近）</param>
        /// <returns>是否重叠</returns>
        public bool OverlapsWith(PlacedRoom other, int padding = 0)
        {
            if (other == null) return false;
            
            RectInt expandedBounds = new RectInt(
                WorldBounds.x - padding,
                WorldBounds.y - padding,
                WorldBounds.width + padding * 2,
                WorldBounds.height + padding * 2
            );
            
            return expandedBounds.Overlaps(other.WorldBounds);
        }
        
        /// <summary>
        /// 计算到另一个房间的距离（中心点距离）
        /// </summary>
        public float DistanceTo(PlacedRoom other)
        {
            if (other == null) return float.MaxValue;
            
            Vector2 center = new Vector2(
                worldPosition.x + width / 2f,
                worldPosition.y + height / 2f
            );
            
            Vector2 otherCenter = new Vector2(
                other.worldPosition.x + other.width / 2f,
                other.worldPosition.y + other.height / 2f
            );
            
            return Vector2.Distance(center, otherCenter);
        }
        
        /// <summary>
        /// 获取房间中心点（世界坐标）
        /// </summary>
        public Vector2 GetWorldCenter()
        {
            return new Vector2(
                worldPosition.x + width / 2f,
                worldPosition.y + height / 2f
            );
        }
        
        /// <summary>
        /// 获取世界坐标的刷怪点列表
        /// </summary>
        public List<SpawnPointV2> GetWorldSpawnPoints()
        {
            var worldSpawns = new List<SpawnPointV2>();
            
            if (roomData?.potentialSpawns == null) return worldSpawns;
            
            foreach (var spawn in roomData.potentialSpawns)
            {
                // 创建新的刷怪点，应用世界坐标偏移
                var worldSpawn = new SpawnPointV2
                {
                    position = spawn.position + worldPosition,
                    type = spawn.type,
                    groundSpan = spawn.groundSpan,
                    heightAboveGround = spawn.heightAboveGround
                };
                worldSpawns.Add(worldSpawn);
            }
            
            return worldSpawns;
        }
        
        /// <summary>
        /// 将本地坐标转换为世界坐标
        /// </summary>
        public Vector2Int LocalToWorld(Vector2Int localPos)
        {
            return worldPosition + localPos;
        }
        
        /// <summary>
        /// 将世界坐标转换为本地坐标
        /// </summary>
        public Vector2Int WorldToLocal(Vector2Int worldPos)
        {
            return worldPos - worldPosition;
        }
        
        public override string ToString()
        {
            return $"PlacedRoom#{id}[{roomType}](Pos:{worldPosition}, Size:{width}x{height}, Seed:{seed})";
        }
        
        #endregion
    }
}
