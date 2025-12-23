using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 房间种子池
    /// 预生成战斗房间种子，支持随机抽取
    /// </summary>
    [Serializable]
    public class RoomSeedPool
    {
        #region 字段
        
        /// <summary>
        /// 战斗房间种子列表
        /// </summary>
        [SerializeField]
        private List<RoomSeed> combatRoomSeeds = new List<RoomSeed>();
        
        /// <summary>
        /// 房间生成参数（用于生成种子）
        /// </summary>
        private RoomGenParamsV2 roomGenParams;
        
        /// <summary>
        /// 是否缓存生成的房间数据
        /// </summary>
        private bool cacheRoomData;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 种子池中剩余的种子数量
        /// </summary>
        public int RemainingCount => combatRoomSeeds.Count;
        
        /// <summary>
        /// 种子池是否为空
        /// </summary>
        public bool IsEmpty => combatRoomSeeds.Count == 0;
        
        #endregion
        
        #region 构造函数
        
        public RoomSeedPool(RoomGenParamsV2 roomGenParams, bool cacheRoomData = false)
        {
            this.roomGenParams = roomGenParams;
            this.cacheRoomData = cacheRoomData;
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 生成种子池
        /// </summary>
        /// <param name="count">生成的种子数量</param>
        public void GenerateSeedPool(int count)
        {
            combatRoomSeeds.Clear();
            
            // 开始生成战斗房间种子
            
            for (int i = 0; i < count; i++)
            {
                // 生成唯一种子
                string seed = GenerateUniqueSeed(i);
                
                // 创建种子对象
                RoomSeed roomSeed = new RoomSeed(seed, RoomType.Combat);
                
                // 如果需要缓存，立即生成房间数据
                if (cacheRoomData)
                {
                    roomSeed.cachedData = GenerateRoomData(seed);
                }
                
                combatRoomSeeds.Add(roomSeed);
                
                if ((i + 1) % 5 == 0)
                {
                    // 已生成种子
                }
            }
            
            // 种子池生成完成
        }
        
        /// <summary>
        /// 随机抽取一个种子（不放回）
        /// </summary>
        /// <returns>抽取的种子，如果池为空则返回 null</returns>
        public RoomSeed DrawSeed()
        {
            if (IsEmpty)
            {
                // 种子池已空，无法抽取
                return null;
            }
            
            // 随机选择一个索引
            int index = UnityEngine.Random.Range(0, combatRoomSeeds.Count);
            RoomSeed seed = combatRoomSeeds[index];
            
            // 从池中移除（不放回）
            combatRoomSeeds.RemoveAt(index);
            
            // 抽取种子
            
            return seed;
        }
        
        /// <summary>
        /// 重置种子池（将所有种子放回）
        /// </summary>
        public void Reset()
        {
            // 注意：这里不会重新生成种子，只是清空
            // 如果需要重新生成，请调用 GenerateSeedPool
            // 种子池已重置
        }
        
        /// <summary>
        /// 获取所有种子（只读）
        /// </summary>
        public List<RoomSeed> GetAllSeeds()
        {
            return new List<RoomSeed>(combatRoomSeeds);
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 生成唯一种子字符串
        /// </summary>
        private string GenerateUniqueSeed(int index)
        {
            // 使用时间戳 + 索引 + 随机数确保唯一性
            long timestamp = DateTime.Now.Ticks;
            int randomValue = UnityEngine.Random.Range(1000, 9999);
            
            return $"Combat_{timestamp}_{index}_{randomValue}";
        }
        
        /// <summary>
        /// 使用种子生成房间数据
        /// </summary>
        private RoomDataV2 GenerateRoomData(string seed)
        {
            // 创建临时生成器
            GameObject tempObj = new GameObject("TempRoomGenerator");
            RoomGeneratorV2 generator = tempObj.AddComponent<RoomGeneratorV2>();
            
            // 配置参数
            generator.parameters = roomGenParams;
            generator.parameters.seed = seed;
            generator.parameters.useRandomSeed = false;
            
            // 生成房间
            generator.GenerateRoom();
            
            // 获取数据
            RoomDataV2 roomData = generator.CurrentRoom;
            
            // 清理临时对象
            UnityEngine.Object.DestroyImmediate(tempObj);
            
            return roomData;
        }
        
        #endregion
    }
}
