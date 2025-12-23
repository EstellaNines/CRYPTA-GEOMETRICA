using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 生成点验证工具类
    /// 提供边界验证、环境验证和安全区检测功能
    /// </summary>
    public static class SpawnPointValidator
    {
        /// <summary>
        /// 验证生成点是否在有效边界内
        /// </summary>
        /// <param name="position">生成点网格坐标</param>
        /// <param name="roomWidth">房间宽度</param>
        /// <param name="roomHeight">房间高度</param>
        /// <param name="edgePadding">边缘填充距离（默认3格）</param>
        /// <returns>是否在有效边界内</returns>
        public static bool IsWithinBounds(Vector2Int position, int roomWidth, int roomHeight, int edgePadding = 3)
        {
            return position.x >= edgePadding 
                && position.x < roomWidth - edgePadding 
                && position.y >= edgePadding 
                && position.y < roomHeight - edgePadding;
        }
        
        /// <summary>
        /// 验证地面生成点周围环境
        /// 检查上方是否有足够的头顶空间，左右是否有足够的移动空间
        /// </summary>
        /// <param name="position">生成点网格坐标</param>
        /// <param name="roomData">房间数据</param>
        /// <param name="requiredHeadroom">需要的头顶空间（默认3格）</param>
        /// <param name="requiredSideSpace">需要的左右空间（默认1格）</param>
        /// <returns>环境是否有效</returns>
        public static bool ValidateGroundEnvironment(
            Vector2Int position, 
            RoomDataV2 roomData, 
            int requiredHeadroom = 3, 
            int requiredSideSpace = 1)
        {
            if (roomData == null)
                return false;
            
            // 检查生成点下方是否是实心块（墙壁或平台）
            if (!roomData.IsSolid(position.x, position.y - 1))
                return false;
            
            // 检查上方连续空间
            for (int dy = 0; dy < requiredHeadroom; dy++)
            {
                int checkY = position.y + dy;
                if (!roomData.IsValid(position.x, checkY))
                    return false;
                
                TileType tile = roomData.GetTile(position.x, checkY);
                if (tile != TileType.Floor)
                    return false;
            }
            
            // 检查左侧空间
            for (int dx = 1; dx <= requiredSideSpace; dx++)
            {
                int checkX = position.x - dx;
                if (!roomData.IsValid(checkX, position.y))
                    return false;
                
                TileType tile = roomData.GetTile(checkX, position.y);
                if (tile != TileType.Floor)
                    return false;
            }
            
            // 检查右侧空间
            for (int dx = 1; dx <= requiredSideSpace; dx++)
            {
                int checkX = position.x + dx;
                if (!roomData.IsValid(checkX, position.y))
                    return false;
                
                TileType tile = roomData.GetTile(checkX, position.y);
                if (tile != TileType.Floor)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证空中生成点周围环境
        /// 检查周围更大范围是否都是Floor（空气），确保飞行怪物有足够空间
        /// </summary>
        /// <param name="position">生成点网格坐标</param>
        /// <param name="roomData">房间数据</param>
        /// <param name="radius">检测半径（默认2格，即5x5区域）</param>
        /// <returns>周围区域是否都是Floor</returns>
        public static bool ValidateAirEnvironment(Vector2Int position, RoomDataV2 roomData, int radius = 2)
        {
            if (roomData == null)
                return false;
            
            // 首先检查生成点本身是否是Floor
            if (roomData.GetTile(position.x, position.y) != TileType.Floor)
                return false;
            
            // 检查以position为中心的正方形区域（半径为radius）
            // 例如 radius=2 时，检查 5x5 = 25 格
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int checkX = position.x + dx;
                    int checkY = position.y + dy;
                    
                    // 检查坐标是否在房间范围内
                    if (!roomData.IsValid(checkX, checkY))
                        return false;
                    
                    // 检查是否是Floor
                    TileType tile = roomData.GetTile(checkX, checkY);
                    if (tile != TileType.Floor)
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查位置是否在安全区内（入口或出口附近）
        /// </summary>
        /// <param name="position">生成点网格坐标</param>
        /// <param name="entrancePos">入口位置</param>
        /// <param name="exitPos">出口位置</param>
        /// <param name="safeDistance">安全距离</param>
        /// <returns>是否在安全区内（true表示应该被排除）</returns>
        public static bool IsInSafeZone(
            Vector2Int position, 
            Vector2Int entrancePos, 
            Vector2Int exitPos, 
            int safeDistance)
        {
            // 计算与入口的曼哈顿距离
            int distanceToEntrance = Mathf.Abs(position.x - entrancePos.x) + Mathf.Abs(position.y - entrancePos.y);
            
            // 计算与出口的曼哈顿距离
            int distanceToExit = Mathf.Abs(position.x - exitPos.x) + Mathf.Abs(position.y - exitPos.y);
            
            // 如果距离任一出入口小于安全距离，则在安全区内
            return distanceToEntrance < safeDistance || distanceToExit < safeDistance;
        }
    }
}
