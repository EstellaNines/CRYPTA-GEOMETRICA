namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家状态接口
    /// 定义了状态机中每个状态必须实现的基本方法
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>
        /// 状态名称 - 用于状态机管理和调试
        /// </summary>
        string StateName { get; }
        
        /// <summary>
        /// 进入状态时调用 - 初始化状态相关数据
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        void OnEnter(PlayerController player);
        
        /// <summary>
        /// 状态更新 - 每帧调用，执行状态逻辑
        /// 处理动画播放、状态转换检查等非物理相关逻辑
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        void OnUpdate(PlayerController player);
        
        /// <summary>
        /// 物理更新 - 固定时间间隔调用，处理物理相关逻辑
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        void OnFixedUpdate(PlayerController player);
        
        /// <summary>
        /// 退出状态时调用 - 清理状态数据
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        void OnExit(PlayerController player);
    }
}
