using UnityEngine;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家攻击状态
    /// 播放攻击动画，通过动画事件触发伤害判定
    /// 动画播放完毕后自动返回 Idle 或 Walk 状态
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        /// <summary>
        /// 状态名称
        /// </summary>
        public override string StateName => "Attack";
        
        /// <summary>
        /// 攻击动画长度（秒）
        /// </summary>
        private float _attackDuration;
        
        /// <summary>
        /// 是否已触发攻击判定
        /// </summary>
        private bool _hasTriggeredHit;
        
        /// <summary>
        /// 进入状态 - 播放攻击动画
        /// </summary>
        public override void OnEnter(PlayerController player)
        {
            base.OnEnter(player);
            _hasTriggeredHit = false;
            
            // 播放攻击动画
            if (player.Animator != null)
            {
                player.Animator.CrossFade("Attack", 0.05f, 0);
                
                // 获取攻击动画长度
                var clips = player.Animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    if (clip.name == "Attack")
                    {
                        _attackDuration = clip.length;
                        break;
                    }
                }
                
                // 如果没找到动画，使用默认时长
                if (_attackDuration <= 0f)
                {
                    _attackDuration = 0.4f;
                }
            }
            else
            {
                _attackDuration = 0.4f;
            }
            
            // 标记攻击已触发（防止连续触发）
            player.ConsumeAttackInput();
        }
        
        /// <summary>
        /// 更新状态逻辑
        /// </summary>
        protected override void UpdateState(PlayerController player)
        {
            // 攻击状态下不处理移动，但保持当前速度惯性
        }
        
        /// <summary>
        /// 检查状态转换条件
        /// </summary>
        protected override void CheckTransitions(PlayerController player)
        {
            // 攻击动画播放完毕后，根据输入决定下一个状态
            if (stateTimer >= _attackDuration)
            {
                if (Mathf.Abs(player.MoveInput.x) > 0.01f)
                {
                    player.StateMachine.TransitionTo("Walk");
                }
                else
                {
                    player.StateMachine.TransitionTo("Idle");
                }
            }
        }
        
        /// <summary>
        /// 退出状态
        /// </summary>
        public override void OnExit(PlayerController player)
        {
            base.OnExit(player);
            _hasTriggeredHit = false;
        }
    }
}
