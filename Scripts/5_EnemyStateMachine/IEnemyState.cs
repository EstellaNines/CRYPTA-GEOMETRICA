using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine
{
    /// <summary>
    /// 敌人状态接口
    /// 定义了状态机中每个状态必须实现的基本方法
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// 状态名称 - 用于状态机管理和调试
        /// </summary>
        string StateName { get; }
        
        /// <summary>
        /// 进入状态时调用 - 初始化状态相关数据
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        void OnEnter(EnemyController enemy);
        
        /// <summary>
        /// 状态更新 - 每帧调用，执行状态逻辑
        /// 处理AI决策、动画播放、音效触发等非物理相关逻辑
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        void OnUpdate(EnemyController enemy);
        
        /// <summary>
        /// 物理更新 - 固定时间间隔调用，处理物理相关逻辑
        /// 处理移动、碰撞、重力、击退等物理相关操作
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        void OnFixedUpdate(EnemyController enemy);
        
        /// <summary>
        /// 退出状态时调用 - 清理状态数据
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        void OnExit(EnemyController enemy);
        
        /// <summary>
        /// 检查是否可以转换到目标状态
        /// 用于实现状态转换的权限控制
        /// </summary>
        /// <param name="targetState">目标状态名称</param>
        /// <param name="enemy">敌人控制器引用</param>
        /// <returns>是否允许转换</returns>
        bool CanTransitionTo(string targetState, EnemyController enemy);
    }
}
