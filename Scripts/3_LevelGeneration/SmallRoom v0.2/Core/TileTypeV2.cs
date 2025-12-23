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
    /// 敌人生成点类型（位置类型）
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
    /// 敌人类型枚举
    /// </summary>
    public enum EnemyType
    {
        /// <summary>
        /// 无敌人（空生成点）
        /// </summary>
        None,
        
        /// <summary>
        /// 三角锐枪手（远程敌人，低血量高伤害）
        /// </summary>
        TriangleSharpshooter,
        
        /// <summary>
        /// 三角盾卫（近战敌人，高血量低伤害）
        /// </summary>
        TriangleShieldbearer,
        
        /// <summary>
        /// 三角飞蛾（飞行骚扰敌人，极低血量）
        /// </summary>
        TriangleMoth,
        
        /// <summary>
        /// 复合守卫者（Boss）
        /// </summary>
        CompositeGuardian
    }
    
    /// <summary>
    /// 房间难度等级
    /// </summary>
    public enum RoomDifficulty
    {
        /// <summary>
        /// 简单房间：2-3个锐枪手
        /// </summary>
        Easy,
        
        /// <summary>
        /// 标准房间：1盾卫 + 2锐枪手
        /// </summary>
        Normal,
        
        /// <summary>
        /// 困难房间：1盾卫 + 1锐枪手 + 3-4飞蛾
        /// </summary>
        Hard,
        
        /// <summary>
        /// 精英房间：2盾卫 + 2锐枪手 + 2飞蛾
        /// </summary>
        Elite
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
