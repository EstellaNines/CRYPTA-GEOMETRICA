using UnityEngine;
using CryptaGeometrica.LevelGeneration.MultiRoom;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 特殊房间生成器
    /// 负责生成入口房间和Boss房间
    /// </summary>
    public static class SpecialRoomGenerator
    {
        #region 入口房间生成
        
        /// <summary>
        /// 生成入口房间
        /// 特点：简单矩形房间、完整平坦地面、无平台、无怪物生成点、左右开放
        /// </summary>
        public static void GenerateEntranceRoom(RoomDataV2 roomData, RoomGenParamsV2 parameters, System.Random random)
        {
            Debug.Log("[SpecialRoomGenerator] 生成入口房间...");
            
            int wallThickness = 2;
            int groundLevel = wallThickness; // 地面层（墙壁）
            
            // 1. 填充整个房间为墙壁
            roomData.Fill(TileType.Wall);
            
            // 2. 挖空内部区域，保持完整地面
            // 从地面上方开始挖空，地面层保持为墙壁
            for (int x = wallThickness; x < roomData.width - wallThickness; x++)
            {
                for (int y = groundLevel + 1; y < roomData.height - wallThickness; y++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 3. 打开左侧入口通道（只清空地面上方的墙壁，地面层保持为墙壁）
            for (int y = groundLevel + 1; y < groundLevel + 4; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 4. 打开右侧出口通道（只清空地面上方的墙壁，地面层保持为墙壁）
            for (int y = groundLevel + 1; y < groundLevel + 4; y++)
            {
                for (int x = roomData.width - wallThickness; x < roomData.width; x++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 5. 设置出入口位置
            // 左侧入口（玩家出生点）- 贴近左墙
            roomData.startPos = new Vector2Int(wallThickness, groundLevel + 1);
            
            // 出口 - 房间右下角位置（贴近右墙边界，向上1格）
            roomData.endPos = new Vector2Int(roomData.width - 1, wallThickness + 1);
            
            // 6. 清空生成点列表（入口房间不生成任何怪物）
            roomData.potentialSpawns.Clear();
            
            // 7. 不需要门
            roomData.needsDoorAtExit = false;
            
            Debug.Log("[SpecialRoomGenerator] 入口房间生成完成");
        }
        
        #endregion
        
        #region Boss房间生成
        
        /// <summary>
        /// 生成Boss房间
        /// 特点：简单矩形房间、完整平坦地面、无平台、只有Boss生成点、左侧开放、右侧用门封闭
        /// </summary>
        public static void GenerateBossRoom(RoomDataV2 roomData, RoomGenParamsV2 parameters, System.Random random)
        {
            Debug.Log("[SpecialRoomGenerator] 生成Boss房间...");
            
            int wallThickness = 2;
            int groundLevel = wallThickness; // 地面层（墙壁）
            
            // 1. 填充整个房间为墙壁
            roomData.Fill(TileType.Wall);
            
            // 2. 挖空内部区域，保持完整地面
            // 从地面上方开始挖空，地面层保持为墙壁
            for (int x = wallThickness; x < roomData.width - wallThickness; x++)
            {
                for (int y = groundLevel + 1; y < roomData.height - wallThickness; y++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 3. 打开左侧入口通道（只清空地面上方的墙壁，地面层保持为墙壁）
            for (int y = groundLevel + 1; y < groundLevel + 4; y++)
            {
                for (int x = 0; x < wallThickness; x++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 4. 打开右侧出口通道（只清空地面上方的墙壁，地面层保持为墙壁）
            for (int y = groundLevel + 1; y < groundLevel + 4; y++)
            {
                for (int x = roomData.width - wallThickness; x < roomData.width; x++)
                {
                    roomData.SetTile(x, y, TileType.Floor);
                }
            }
            
            // 5. 设置出入口位置（贴近墙壁边缘）
            // 左侧入口 - 贴近左墙
            roomData.startPos = new Vector2Int(wallThickness, groundLevel + 1);
            
            // 右侧出口（将被门封闭）- 贴近右墙
            roomData.endPos = new Vector2Int(roomData.width - wallThickness - 1, groundLevel + 1);
            
            // 5. 清空生成点列表（Boss房间不生成小怪）
            roomData.potentialSpawns.Clear();
            
            // 6. 添加Boss生成点（在房间中央偏右的地面上）
            Vector2Int bossSpawnPoint = new Vector2Int(
                roomData.width - wallThickness - 10,  // 距离右墙10格
                groundLevel + 1  // 地面上方1格
            );
            
            // 创建Boss生成点对象
            SpawnPointV2 bossSpawn = new SpawnPointV2(bossSpawnPoint, SpawnType.Boss);
            roomData.potentialSpawns.Add(bossSpawn);
            
            // 7. 标记需要在出口处放置门
            roomData.needsDoorAtExit = true;
            
            Debug.Log($"[SpecialRoomGenerator] Boss房间生成完成，Boss生成点: {bossSpawnPoint}");
        }
        
        #endregion
    }
}
