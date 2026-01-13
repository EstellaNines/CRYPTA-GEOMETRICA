using UnityEngine;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家站立状态
    /// 当玩家无水平移动输入时激活，播放 Idle 动画
    /// </summary>
    public class PlayerIdleState : PlayerStateBase
    {
        /// <summary>
        /// 状态名称
        /// </summary>
        public override string StateName => "Idle";
        
        /// <summary>
        /// 进入状态 - 播放 Idle 动画
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        public override void OnEnter(PlayerController player)
        {
            base.OnEnter(player);
            
            // 使用 CrossFade 平滑切换到 Idle 动画，绕过 Animator Controller 的状态转换
            if (player.Animator != null)
            {
                player.Animator.CrossFade("Idle", 0.1f, 0);
            }
        }
        
        /// <summary>
        /// 更新状态逻辑 - Idle 状态无特殊逻辑
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        protected override void UpdateState(PlayerController player)
        {
            // Idle 状态无需额外更新逻辑
        }
        
        /// <summary>
        /// 检查状态转换条件
        /// 当有水平移动输入时转换到 Walk 状态
        /// 当有攻击输入时转换到 Attack 状态
        /// </summary>
        /// <param name="player">玩家控制器引用</param>
        protected override void CheckTransitions(PlayerController player)
        {
            // 优先检查攻击输入
            if (player.HasAttackInput)
            {
                player.StateMachine.TransitionTo("Attack");
                return;
            }
            
            // 当水平输入绝对值大于阈值时，转换到 Walk 状态
            if (Mathf.Abs(player.MoveInput.x) > 0.01f)
            {
                player.StateMachine.TransitionTo("Walk");
            }
        }
    }
}
