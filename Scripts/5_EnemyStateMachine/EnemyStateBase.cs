using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine
{
    /// <summary>
    /// 敌人状态基类
    /// 提供状态的基础实现和通用功能
    /// </summary>
    public abstract class EnemyStateBase : IEnemyState
    {
        #region 基础属性
        
        /// <summary>
        /// 状态名称 - 子类必须实现
        /// </summary>
        public abstract string StateName { get; }
        
        /// <summary>
        /// 状态持续时间计时器（逻辑时间）
        /// </summary>
        protected float stateTimer = 0f;
        
        /// <summary>
        /// 物理时间计时器（固定时间步长）
        /// </summary>
        protected float fixedStateTimer = 0f;
        
        /// <summary>
        /// 状态是否完成标记
        /// </summary>
        protected bool isStateComplete = false;
        
        /// <summary>
        /// 状态是否被中断标记
        /// </summary>
        protected bool isInterrupted = false;
        
        /// <summary>
        /// 调试模式开关
        /// </summary>
        [SerializeField] protected bool debugMode = false;
        
        #endregion
        
        #region 生命周期方法
        
        /// <summary>
        /// 进入状态 - 基础初始化
        /// </summary>
        public virtual void OnEnter(EnemyController enemy)
        {
            // 重置基础状态
            stateTimer = 0f;
            fixedStateTimer = 0f;
            isStateComplete = false;
            isInterrupted = false;
            
            // 调试日志
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 进入状态: {StateName}");
            }
            
            // 调用子类初始化
            InitializeState(enemy);
        }
        
        /// <summary>
        /// 更新状态 - 基础更新逻辑
        /// </summary>
        public virtual void OnUpdate(EnemyController enemy)
        {
            // 更新逻辑计时器
            stateTimer += Time.deltaTime;
            
            // 执行状态逻辑
            UpdateState(enemy);
            
            // 检查转换条件
            CheckTransitionConditions(enemy);
            
            // 调试信息（每秒输出一次）
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{enemy.name}] {StateName} 状态运行中 - 时间: {stateTimer:F2}s");
            }
        }
        
        /// <summary>
        /// 物理更新 - 处理移动、碰撞等物理相关逻辑
        /// </summary>
        public virtual void OnFixedUpdate(EnemyController enemy)
        {
            // 更新物理计时器
            fixedStateTimer += Time.fixedDeltaTime;
            
            // 执行物理更新
            FixedUpdateState(enemy);
        }
        
        /// <summary>
        /// 退出状态 - 基础清理
        /// </summary>
        public virtual void OnExit(EnemyController enemy)
        {
            // 调试日志
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 退出状态: {StateName} - 持续时间: {stateTimer:F2}s");
            }
            
            // 调用子类清理
            CleanupState(enemy);
            
            // 重置标记
            isStateComplete = false;
            isInterrupted = false;
        }
        
        #endregion
        
        #region 抽象和虚方法 - 子类实现
        
        /// <summary>
        /// 初始化状态 - 子类实现具体初始化逻辑
        /// </summary>
        protected virtual void InitializeState(EnemyController enemy) { }
        
        /// <summary>
        /// 更新状态逻辑 - 子类实现核心状态行为
        /// 处理AI决策、动画播放、音效触发等非物理相关逻辑
        /// </summary>
        protected abstract void UpdateState(EnemyController enemy);
        
        /// <summary>
        /// 物理更新状态 - 子类实现物理相关行为
        /// 处理移动、碰撞、重力、击退等物理相关操作
        /// </summary>
        protected virtual void FixedUpdateState(EnemyController enemy) { }
        
        /// <summary>
        /// 检查状态转换条件 - 子类实现转换逻辑
        /// </summary>
        protected abstract void CheckTransitionConditions(EnemyController enemy);
        
        /// <summary>
        /// 清理状态 - 子类实现具体清理逻辑
        /// </summary>
        protected virtual void CleanupState(EnemyController enemy) { }
        
        #endregion
        
        #region 转换控制
        
        /// <summary>
        /// 检查是否可以转换到目标状态
        /// </summary>
        public virtual bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 默认允许所有转换，子类可重写实现特殊规则
            return true;
        }
        
        /// <summary>
        /// 强制完成状态
        /// </summary>
        protected void CompleteState()
        {
            isStateComplete = true;
        }
        
        /// <summary>
        /// 中断状态
        /// </summary>
        protected void InterruptState()
        {
            isInterrupted = true;
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 检查逻辑时间是否超时
        /// </summary>
        protected bool IsStateTimeout(float timeoutDuration)
        {
            return stateTimer >= timeoutDuration;
        }
        
        /// <summary>
        /// 检查物理时间是否超时
        /// </summary>
        protected bool IsFixedStateTimeout(float timeoutDuration)
        {
            return fixedStateTimer >= timeoutDuration;
        }
        
        /// <summary>
        /// 获取逻辑时间状态进度百分比
        /// </summary>
        protected float GetStateProgress(float totalDuration)
        {
            return Mathf.Clamp01(stateTimer / totalDuration);
        }
        
        /// <summary>
        /// 获取物理时间状态进度百分比
        /// </summary>
        protected float GetFixedStateProgress(float totalDuration)
        {
            return Mathf.Clamp01(fixedStateTimer / totalDuration);
        }
        
        /// <summary>
        /// 检查是否在逻辑时间的特定时间窗口内
        /// </summary>
        protected bool IsInTimeWindow(float startTime, float endTime)
        {
            return stateTimer >= startTime && stateTimer <= endTime;
        }
        
        /// <summary>
        /// 检查是否在物理时间的特定时间窗口内
        /// </summary>
        protected bool IsInFixedTimeWindow(float startTime, float endTime)
        {
            return fixedStateTimer >= startTime && fixedStateTimer <= endTime;
        }
        
        #endregion
    }
}
