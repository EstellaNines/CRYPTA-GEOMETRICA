using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 线性房间放置器
    /// 将房间从左到右依次放置，形成线性关卡结构
    /// </summary>
    public class LinearRoomPlacer
    {
        #region 字段
        
        private System.Random random;
        private LevelGeneratorParams parameters;
        
        // 临时房间生成器
        private GameObject tempGeneratorObj;
        private RoomGeneratorV2 tempGenerator;
        
        #endregion

        #region 构造函数
        
        public LinearRoomPlacer(LevelGeneratorParams parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 放置所有房间（线性链式布局）
        /// </summary>
        /// <param name="seedPool">战斗房间种子池</param>
        /// <returns>生成的关卡数据</returns>
        public LevelData PlaceRooms(RoomSeedPool seedPool)
        {
            LevelData level = new LevelData(parameters.levelSeed);
            
            int currentX = 0;
            int roomId = 0;
            
            try
            {
                // 创建临时生成器
                CreateTempGenerator();
                
                // 1. 放置入口房间（固定 Y=0）
                Debug.Log("[LinearRoomPlacer] 放置入口房间...");
                PlacedRoom entrance = CreateRoom(
                    roomId++, 
                    RoomType.Entrance, 
                    parameters.entranceRoomParams,
                    new Vector2Int(currentX, 0),
                    null
                );
                level.AddRoom(entrance);
                currentX += entrance.width + parameters.roomSpacing;
                
                // 2. 放置战斗房间（从种子池抽取，随机Y偏移，确保高度差至少3格）
                Debug.Log($"[LinearRoomPlacer] 放置 {parameters.combatRoomCount} 个战斗房间...");
                PlacedRoom previousRoom = entrance;
                
                for (int i = 0; i < parameters.combatRoomCount; i++)
                {
                    // 从种子池抽取种子
                    RoomSeed seed = seedPool?.DrawSeed();
                    string seedStr = seed?.seed ?? GenerateRandomSeed();
                    
                    // 随机Y偏移，确保与前一个房间的出入口高度差至少3格
                    int yOffset = GenerateValidYOffset(previousRoom, parameters.combatRoomParams);
                    
                    PlacedRoom combat = CreateRoom(
                        roomId++,
                        RoomType.Combat,
                        parameters.combatRoomParams,
                        new Vector2Int(currentX, yOffset),
                        seedStr
                    );
                    level.AddRoom(combat);
                    currentX += combat.width + parameters.roomSpacing;
                    previousRoom = combat;
                    
                    Debug.Log($"[LinearRoomPlacer] 战斗房间 {i + 1}/{parameters.combatRoomCount} 放置完成, Y偏移: {yOffset}");
                }
                
                // 3. 放置Boss房间（确保与最后一个战斗房间高度差至少3格）
                Debug.Log("[LinearRoomPlacer] 放置Boss房间...");
                int bossYOffset = GenerateValidYOffset(previousRoom, parameters.bossRoomParams);
                PlacedRoom boss = CreateRoom(
                    roomId++,
                    RoomType.Boss,
                    parameters.bossRoomParams,
                    new Vector2Int(currentX, bossYOffset),
                    null
                );
                level.AddRoom(boss);
                
                Debug.Log($"[LinearRoomPlacer] 房间放置完成, 总计 {level.RoomCount} 个房间");
            }
            finally
            {
                // 清理临时生成器
                DestroyTempGenerator();
            }
            
            return level;
        }
        
        /// <summary>
        /// 重新生成指定房间的数据（保持位置不变）
        /// </summary>
        public void RegenerateRoom(PlacedRoom room, string newSeed = null)
        {
            if (room == null) return;
            
            try
            {
                CreateTempGenerator();
                
                var roomParams = parameters.GetParamsForRoomType(room.roomType);
                string seed = newSeed ?? room.seed ?? GenerateRandomSeed();
                
                RoomDataV2 roomData = GenerateRoomData(roomParams, seed);
                room.SetRoomData(roomData);
                room.seed = seed;
                
                Debug.Log($"[LinearRoomPlacer] 重新生成房间 #{room.id}, 种子: {seed}");
            }
            finally
            {
                DestroyTempGenerator();
            }
        }
        
        /// <summary>
        /// 批量重新生成所有房间数据（用于加载布局后）
        /// </summary>
        public void RegenerateAllRooms(LevelData level)
        {
            if (level == null || level.rooms == null) return;
            
            try
            {
                CreateTempGenerator();
                
                foreach (var room in level.rooms)
                {
                    var roomParams = parameters.GetParamsForRoomType(room.roomType);
                    string seed = room.seed ?? GenerateRandomSeed();
                    
                    RoomDataV2 roomData = GenerateRoomData(roomParams, seed);
                    room.SetRoomData(roomData);
                    
                    Debug.Log($"[LinearRoomPlacer] 重新生成房间 #{room.id} [{room.roomType}]");
                }
                
                Debug.Log($"[LinearRoomPlacer] 批量重新生成完成, 共 {level.RoomCount} 个房间");
            }
            finally
            {
                DestroyTempGenerator();
            }
        }
        
        #endregion

        #region 私有方法
        
        /// <summary>
        /// 创建房间
        /// </summary>
        private PlacedRoom CreateRoom(int id, RoomType roomType, RoomGenParamsV2 roomParams, Vector2Int position, string seed)
        {
            // 生成房间数据
            string actualSeed = seed ?? GenerateRandomSeed();
            RoomDataV2 roomData = GenerateRoomData(roomParams, actualSeed);
            
            // 创建已放置房间
            PlacedRoom room = new PlacedRoom(id, roomType, position, roomData, actualSeed);
            
            return room;
        }
        
        /// <summary>
        /// 生成房间数据
        /// </summary>
        private RoomDataV2 GenerateRoomData(RoomGenParamsV2 roomParams, string seed)
        {
            if (tempGenerator == null)
            {
                throw new InvalidOperationException("临时生成器未初始化");
            }
            
            // 配置参数
            tempGenerator.parameters = roomParams;
            tempGenerator.parameters.seed = seed;
            tempGenerator.parameters.useRandomSeed = false;
            
            // 生成房间
            tempGenerator.GenerateRoom();
            
            return tempGenerator.CurrentRoom;
        }
        
        /// <summary>
        /// 创建临时生成器
        /// </summary>
        private void CreateTempGenerator()
        {
            if (tempGeneratorObj != null) return;
            
            tempGeneratorObj = new GameObject("TempRoomGenerator");
            tempGeneratorObj.hideFlags = HideFlags.HideAndDontSave;
            tempGenerator = tempGeneratorObj.AddComponent<RoomGeneratorV2>();
        }
        
        /// <summary>
        /// 销毁临时生成器
        /// </summary>
        private void DestroyTempGenerator()
        {
            if (tempGeneratorObj != null)
            {
                UnityEngine.Object.DestroyImmediate(tempGeneratorObj);
                tempGeneratorObj = null;
                tempGenerator = null;
            }
        }
        
        /// <summary>
        /// 生成随机种子
        /// </summary>
        private string GenerateRandomSeed()
        {
            return $"Room_{DateTime.Now.Ticks}_{random.Next(1000, 9999)}";
        }
        
        /// <summary>
        /// 生成有效的Y偏移，确保与前一个房间的出入口高度差至少为走廊宽度（3格）
        /// </summary>
        /// <param name="previousRoom">前一个房间</param>
        /// <param name="nextRoomParams">下一个房间的参数</param>
        /// <returns>有效的Y偏移</returns>
        private int GenerateValidYOffset(PlacedRoom previousRoom, RoomGenParamsV2 nextRoomParams)
        {
            const int minHeightDiff = 3; // 走廊宽度，最小高度差
            
            // 获取前一个房间出口的Y坐标（相对于房间底部）
            int prevExitY = previousRoom.WorldExit.y;
            
            // 下一个房间入口的本地Y坐标（通常在房间底部偏上几格）
            // 默认入口位置约为 y=3（参考PlacedRoom.WorldEntrance的默认值）
            int nextEntranceLocalY = 3;
            
            // 生成候选Y偏移列表（排除高度差小于3的值）
            List<int> validOffsets = new List<int>();
            
            for (int yOffset = parameters.yOffsetRange.x; yOffset <= parameters.yOffsetRange.y; yOffset++)
            {
                // 计算下一个房间入口的世界Y坐标
                int nextEntranceY = yOffset + nextEntranceLocalY;
                
                // 计算高度差
                int heightDiff = Mathf.Abs(nextEntranceY - prevExitY);
                
                // 只有高度差至少为minHeightDiff时才有效
                if (heightDiff >= minHeightDiff)
                {
                    validOffsets.Add(yOffset);
                }
            }
            
            // 如果有有效偏移，随机选择一个
            if (validOffsets.Count > 0)
            {
                return validOffsets[random.Next(validOffsets.Count)];
            }
            
            // 如果没有有效偏移（范围太小），强制选择一个能满足条件的偏移
            // 选择使高度差最大的偏移
            int bestOffset = parameters.yOffsetRange.x;
            int maxDiff = 0;
            
            for (int yOffset = parameters.yOffsetRange.x; yOffset <= parameters.yOffsetRange.y; yOffset++)
            {
                int nextEntranceY = yOffset + nextEntranceLocalY;
                int heightDiff = Mathf.Abs(nextEntranceY - prevExitY);
                
                if (heightDiff > maxDiff)
                {
                    maxDiff = heightDiff;
                    bestOffset = yOffset;
                }
            }
            
            Debug.LogWarning($"[LinearRoomPlacer] 无法找到高度差>=3的Y偏移，使用最大高度差: {maxDiff}");
            return bestOffset;
        }
        
        #endregion
    }
}
