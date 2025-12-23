using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine
{
    /// <summary>
    /// 敌人状态机管理器
    /// 负责状态的注册、转换和更新管理
    /// </summary>
    [System.Serializable]
    public class EnemyStateMachine
    {
        #region 私有字段
        
        /// <summary>
        /// 状态字典 - 存储所有注册的状态
        /// </summary>
        private Dictionary<string, IEnemyState> states;
        
        /// <summary>
        /// 当前活动状态
        /// </summary>
        private IEnemyState currentState;
        
        /// <summary>
        /// 状态机拥有者
        /// </summary>
        private EnemyController owner;
        
        /// <summary>
        /// 状态转换历史记录
        /// </summary>
        private Queue<string> stateHistory;
        
        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        private const int MAX_HISTORY_COUNT = 10;
        
        /// <summary>
        /// 调试模式
        /// </summary>
        private bool debugMode = false;
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 当前状态名称
        /// </summary>
        public string CurrentStateName => currentState?.StateName ?? "None";
        
        /// <summary>
        /// 是否处于指定状态
        /// </summary>
        public bool IsInState(string stateName) => CurrentStateName == stateName;
        
        /// <summary>
        /// 状态机是否已初始化
        /// </summary>
        public bool IsInitialized => owner != null && states != null;
        
        /// <summary>
        /// 注册的状态数量
        /// </summary>
        public int StateCount => states?.Count ?? 0;
        
        #endregion
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化状态机
        /// </summary>
        /// <param name="enemy">敌人控制器</param>
        /// <param name="enableDebug">是否启用调试模式</param>
        public void Initialize(EnemyController enemy, bool enableDebug = false)
        {
            owner = enemy;
            debugMode = enableDebug;
            states = new Dictionary<string, IEnemyState>();
            stateHistory = new Queue<string>();
            
            if (debugMode)
            {
                Debug.Log($"[{owner.name}] 状态机初始化完成");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 注册状态
        /// </summary>
        /// <param name="state">要注册的状态</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterState(IEnemyState state)
        {
            if (state == null)
            {
                Debug.LogError("[EnemyStateMachine] 尝试注册空状态");
                return false;
            }
            
            if (states.ContainsKey(state.StateName))
            {
                Debug.LogWarning($"[EnemyStateMachine] 状态 {state.StateName} 已存在，将被覆盖");
            }
            
            states[state.StateName] = state;
            
            if (debugMode)
            {
                Debug.Log($"[{owner.name}] 注册状态: {state.StateName}");
            }
            
            return true;
        }
        
        /// <summary>
        /// 注销状态
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterState(string stateName)
        {
            if (!states.ContainsKey(stateName))
            {
                return false;
            }
            
            // 如果正在使用该状态，先切换到默认状态
            if (CurrentStateName == stateName)
            {
                if (HasState("Idle"))
                {
                    TransitionTo("Idle");
                }
                else
                {
                    currentState?.OnExit(owner);
                    currentState = null;
                }
            }
            
            states.Remove(stateName);
            return true;
        }
        
        /// <summary>
        /// 检查状态是否存在
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <returns>状态是否存在</returns>
        public bool HasState(string stateName)
        {
            return states.ContainsKey(stateName);
        }
        
        #endregion
        
        #region 状态转换
        
        /// <summary>
        /// 转换到指定状态
        /// </summary>
        /// <param name="stateName">目标状态名称</param>
        /// <returns>转换是否成功</returns>
        public bool TransitionTo(string stateName)
        {
            // 验证目标状态存在
            if (!states.ContainsKey(stateName))
            {
                Debug.LogError($"[{owner.name}] 状态 {stateName} 不存在");
                return false;
            }
            
            // 检查当前状态是否允许转换
            if (currentState != null && !currentState.CanTransitionTo(stateName, owner))
            {
                if (debugMode)
                {
                    Debug.Log($"[{owner.name}] 状态转换被拒绝: {CurrentStateName} -> {stateName}");
                }
                return false;
            }
            
            // 记录状态转换历史
            RecordStateTransition(stateName);
            
            // 执行状态转换
            string previousState = CurrentStateName;
            
            // 退出当前状态
            currentState?.OnExit(owner);
            
            // 切换到新状态
            currentState = states[stateName];
            currentState.OnEnter(owner);
            
            if (debugMode)
            {
                Debug.Log($"[{owner.name}] 状态转换: {previousState} -> {stateName}");
            }
            
            return true;
        }
        
        /// <summary>
        /// 强制转换状态（忽略转换限制）
        /// </summary>
        /// <param name="stateName">目标状态名称</param>
        /// <returns>转换是否成功</returns>
        public bool ForceTransitionTo(string stateName)
        {
            if (!states.ContainsKey(stateName))
            {
                Debug.LogError($"[{owner.name}] 状态 {stateName} 不存在");
                return false;
            }
            
            string previousState = CurrentStateName;
            
            // 强制退出当前状态
            currentState?.OnExit(owner);
            
            // 切换到新状态
            currentState = states[stateName];
            currentState.OnEnter(owner);
            
            RecordStateTransition(stateName);
            
            if (debugMode)
            {
                Debug.Log($"[{owner.name}] 强制状态转换: {previousState} -> {stateName}");
            }
            
            return true;
        }
        
        #endregion
        
        #region 更新和历史记录
        
        /// <summary>
        /// 更新状态机 - 逻辑更新
        /// </summary>
        public void Update()
        {
            if (currentState == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[{owner.name}] 状态机没有当前状态");
                }
                return;
            }
            
            currentState.OnUpdate(owner);
        }
        
        /// <summary>
        /// 物理更新状态机 - 物理更新
        /// </summary>
        public void FixedUpdate()
        {
            if (currentState == null)
            {
                return;
            }
            
            currentState.OnFixedUpdate(owner);
        }
        
        /// <summary>
        /// 记录状态转换历史
        /// </summary>
        private void RecordStateTransition(string stateName)
        {
            stateHistory.Enqueue(stateName);
            
            // 限制历史记录数量
            while (stateHistory.Count > MAX_HISTORY_COUNT)
            {
                stateHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// 获取状态历史
        /// </summary>
        /// <returns>状态历史数组</returns>
        public string[] GetStateHistory()
        {
            return stateHistory.ToArray();
        }
        
        /// <summary>
        /// 获取上一个状态
        /// </summary>
        /// <returns>上一个状态名称</returns>
        public string GetPreviousState()
        {
            var history = stateHistory.ToArray();
            return history.Length >= 2 ? history[history.Length - 2] : "None";
        }
        
        #endregion
        
        #region 调试和工具方法
        
        /// <summary>
        /// 设置调试模式
        /// </summary>
        /// <param name="enabled">是否启用调试</param>
        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }
        
        /// <summary>
        /// 获取所有状态名称
        /// </summary>
        /// <returns>状态名称数组</returns>
        public string[] GetAllStateNames()
        {
            return states.Keys.ToArray();
        }
        
        /// <summary>
        /// 重置状态机
        /// </summary>
        public void Reset()
        {
            currentState?.OnExit(owner);
            currentState = null;
            stateHistory.Clear();
        }
        
        /// <summary>
        /// 获取状态机信息字符串
        /// </summary>
        /// <returns>状态机信息</returns>
        public override string ToString()
        {
            return $"StateMachine[Owner: {owner?.name}, Current: {CurrentStateName}, States: {StateCount}]";
        }
        
        #endregion
    }
}
