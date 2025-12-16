using System;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 房间种子数据
    /// 用于预生成和缓存房间
    /// </summary>
    [Serializable]
    public class RoomSeed
    {
        /// <summary>
        /// 种子字符串
        /// </summary>
        public string seed;
        
        /// <summary>
        /// 房间类型
        /// </summary>
        public RoomType roomType;
        
        /// <summary>
        /// 缓存的房间数据（可选，用于避免重复生成）
        /// </summary>
        public SmallRoomV2.RoomDataV2 cachedData;
        
        /// <summary>
        /// 生成时间戳
        /// </summary>
        public long timestamp;
        
        public RoomSeed()
        {
            timestamp = DateTime.Now.Ticks;
        }
        
        public RoomSeed(string seed, RoomType roomType)
        {
            this.seed = seed;
            this.roomType = roomType;
            this.timestamp = DateTime.Now.Ticks;
        }
        
        public override string ToString()
        {
            return $"RoomSeed(Seed:{seed}, Type:{roomType})";
        }
    }
}
