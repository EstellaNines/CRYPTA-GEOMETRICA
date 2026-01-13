namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 房间类型枚举
    /// </summary>
    public enum RoomType
    {
        /// <summary>
        /// 入口房间（简单平台，玩家出生点）
        /// </summary>
        Entrance,
        
        /// <summary>
        /// 战斗房间（由 RoomGeneratorV2 生成）
        /// </summary>
        Combat,
        
        /// <summary>
        /// Boss 房间（平台 + 外墙）
        /// </summary>
        Boss,
        
        /// <summary>
        /// 连接房（走廊/过渡区域）
        /// </summary>
        Connector,
        
        /// <summary>
        /// 休息房（安全区域，无敌人）
        /// </summary>
        Rest,
        
        /// <summary>
        /// 出口房
        /// </summary>
        Exit
    }
}
