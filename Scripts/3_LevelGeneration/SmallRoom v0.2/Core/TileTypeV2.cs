using System;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 瓦片类型枚举
    /// </summary>
    public enum TileType
    {
        /// <summary>
        /// 墙壁（实心，不可通行）
        /// </summary>
        Wall = 0,
        
        /// <summary>
        /// 地面（空气，可通行）
        /// </summary>
        Floor = 1,
        
        /// <summary>
        /// 单向平台（可从下方穿过）
        /// </summary>
        Platform = 2,
        
        /// <summary>
        /// 入口区域
        /// </summary>
        Entrance = 3,
        
        /// <summary>
        /// 出口区域
        /// </summary>
        Exit = 4
    }

    /// <summary>
    /// 敌人生成点类型
    /// </summary>
    public enum SpawnType
    {
        /// <summary>
        /// 地面敌人（锐枪手、盾卫等）
        /// </summary>
        Ground,
        
        /// <summary>
        /// 空中敌人（飞蛾等）
        /// </summary>
        Air,
        
        /// <summary>
        /// Boss 敌人
        /// </summary>
        Boss
    }

    /// <summary>
    /// BSP 分割方向
    /// </summary>
    public enum SplitDirection
    {
        /// <summary>
        /// 水平分割（上下分）
        /// </summary>
        Horizontal,
        
        /// <summary>
        /// 垂直分割（左右分）
        /// </summary>
        Vertical
    }

    // RoomType 已移至 CryptaGeometrica.LevelGeneration.MultiRoom 命名空间
    // 请使用 MultiRoom.RoomType
}
