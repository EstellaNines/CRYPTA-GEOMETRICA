using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 房间种子池测试脚本
    /// 用于验证 Phase 1 功能
    /// </summary>
    public class RoomSeedPoolTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("生成的种子数量")]
        public int seedCount = 20;
        
        [Tooltip("是否缓存房间数据")]
        public bool cacheRoomData = false;
        
        [Header("房间生成参数")]
        public RoomGenParamsV2 roomGenParams;
        
        private RoomSeedPool seedPool;
        
        [ContextMenu("测试：生成种子池")]
        public void TestGenerateSeedPool()
        {
            if (roomGenParams == null)
            {
                Debug.LogError("[RoomSeedPoolTest] 请先配置 roomGenParams");
                return;
            }
            
            Debug.Log("=== 开始测试种子池生成 ===");
            
            // 创建种子池
            seedPool = new RoomSeedPool(roomGenParams, cacheRoomData);
            
            // 生成种子
            seedPool.GenerateSeedPool(seedCount);
            
            Debug.Log($"=== 测试完成，种子池剩余: {seedPool.RemainingCount} ===");
        }
        
        [ContextMenu("测试：抽取种子")]
        public void TestDrawSeed()
        {
            if (seedPool == null || seedPool.IsEmpty)
            {
                Debug.LogWarning("[RoomSeedPoolTest] 种子池为空，请先生成种子池");
                return;
            }
            
            Debug.Log("=== 开始测试抽取种子 ===");
            
            // 抽取 5 个种子
            for (int i = 0; i < 5 && !seedPool.IsEmpty; i++)
            {
                RoomSeed seed = seedPool.DrawSeed();
                Debug.Log($"抽取第 {i + 1} 个种子: {seed}");
            }
            
            Debug.Log($"=== 抽取完成，剩余种子: {seedPool.RemainingCount} ===");
        }
        
        [ContextMenu("测试：显示所有种子")]
        public void TestShowAllSeeds()
        {
            if (seedPool == null)
            {
                Debug.LogWarning("[RoomSeedPoolTest] 种子池未初始化");
                return;
            }
            
            Debug.Log("=== 种子池中的所有种子 ===");
            
            var seeds = seedPool.GetAllSeeds();
            for (int i = 0; i < seeds.Count; i++)
            {
                Debug.Log($"[{i}] {seeds[i]}");
            }
            
            Debug.Log($"=== 总计: {seeds.Count} 个种子 ===");
        }
    }
}
