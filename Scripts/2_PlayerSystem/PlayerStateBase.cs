using UnityEngine;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家状态基类
    /// 提供状态的基础实现和通用功能
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        /// <summary>
        /// 状态名称 - 子类必须实现
        /// </summary>
        public abstract string StateName { get; }
        
        /// <summary>
        /// 状态持续时间计时器
        /// </summary>
        protected float stateTimer = 0f;
        
        /// <summary>
        /// 进入状态 - 基础初始化
        /// </summary>
        public virtual void OnEnter(PlayerController player)
        {
            // 重置计时器
            stateTimer = 0f;
        }
        
        /// <summary>
        /// 更新状态 - 基础更新逻辑
        /// </summary>
        public virtual void OnUpdate(PlayerController player)
        {
            // 更新计时器
            stateTimer += Time.deltaTime;
            
            // 执行状态逻辑
            UpdateState(player);
            
            // 检查转换条件
            CheckTransitions(player);
        }
        
        /// <summary>
        /// 物理更新 - 默认空实现
        /// </summary>
        public virtual void OnFixedUpdate(PlayerController player) { }
        
        /// <summary>
        /// 退出状态 - 默认空实现
        /// </summary>
        public virtual void OnExit(PlayerController player) { }
        
        /// <summary>
        /// 更新状态逻辑 - 子类实现核心状态行为
        /// </summary>
        protected abstract void UpdateState(PlayerController player);
        
        /// <summary>
        /// 检查状态转换条件 - 子类实现转换逻辑
        /// </summary>
        protected abstract void CheckTransitions(PlayerController player);
    }
}
