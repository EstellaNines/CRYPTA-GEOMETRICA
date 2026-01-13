using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家状态机管理器
    /// 负责状态的注册、转换和更新
    /// </summary>
    public class PlayerStateMachine
    {
        /// <summary>
        /// 状态字典 - 通过状态名称索引状态实例
        /// </summary>
        private Dictionary<string, IPlayerState> _states;
        
        /// <summary>
        /// 当前激活的状态
        /// </summary>
        private IPlayerState _currentState;
        
        /// <summary>
        /// 状态机所属的玩家控制器
        /// </summary>
        private PlayerController _owner;
        
        /// <summary>
        /// 当前状态名称 - 用于外部查询和调试
        /// </summary>
        public string CurrentStateName => _currentState?.StateName ?? "None";
        
        /// <summary>
        /// 初始化状态机
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        public void Initialize(PlayerController player)
        {
            _owner = player;
            _states = new Dictionary<string, IPlayerState>();
            _currentState = null;
        }
        
        /// <summary>
        /// 注册状态到状态机
        /// </summary>
        /// <param name="state">要注册的状态实例</param>
        public void RegisterState(IPlayerState state)
        {
            // 空状态检查
            if (state == null)
            {
                Debug.LogWarning("[PlayerStateMachine] 尝试注册空状态，已忽略");
                return;
            }
            
            // 状态名称检查
            if (string.IsNullOrEmpty(state.StateName))
            {
                Debug.LogWarning("[PlayerStateMachine] 状态名称为空，已忽略注册");
                return;
            }
            
            // 重复注册检查
            if (_states.ContainsKey(state.StateName))
            {
                Debug.LogWarning($"[PlayerStateMachine] 状态 '{state.StateName}' 已存在，将被覆盖");
            }
            
            _states[state.StateName] = state;
        }
        
        /// <summary>
        /// 转换到指定状态
        /// </summary>
        /// <param name="stateName">目标状态名称</param>
        /// <returns>转换是否成功</returns>
        public bool TransitionTo(string stateName)
        {
            // 状态名称检查
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogError("[PlayerStateMachine] 转换目标状态名称为空");
                return false;
            }
            
            // 状态存在性检查
            if (!_states.TryGetValue(stateName, out IPlayerState newState))
            {
                Debug.LogError($"[PlayerStateMachine] 状态 '{stateName}' 不存在，无法转换");
                return false;
            }
            
            // 相同状态检查（可选：避免重复进入同一状态）
            if (_currentState != null && _currentState.StateName == stateName)
            {
                return true; // 已经在目标状态，视为成功
            }
            
            // 执行状态转换
            // 1. 退出当前状态
            _currentState?.OnExit(_owner);
            
            // 2. 切换到新状态
            _currentState = newState;
            
            // 3. 进入新状态
            _currentState.OnEnter(_owner);
            
            return true;
        }
        
        /// <summary>
        /// 更新状态机 - 每帧调用
        /// </summary>
        public void Update()
        {
            // 状态机未初始化或无当前状态时静默返回
            if (_owner == null || _currentState == null)
            {
                return;
            }
            
            _currentState.OnUpdate(_owner);
        }
        
        /// <summary>
        /// 物理更新 - 固定时间间隔调用
        /// </summary>
        public void FixedUpdate()
        {
            // 状态机未初始化或无当前状态时静默返回
            if (_owner == null || _currentState == null)
            {
                return;
            }
            
            _currentState.OnFixedUpdate(_owner);
        }
    }
}
