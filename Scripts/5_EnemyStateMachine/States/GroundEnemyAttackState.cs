using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 地面敌人攻击状态 - 简单框架
    /// 等待攻击动画完成后再实现具体攻击逻辑
    /// </summary>
    public class GroundEnemyAttackState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("攻击设置")]
        [SerializeField] internal float attackDuration = 1f;      // 攻击持续时间
        [SerializeField] internal float attackCooldown = 1.5f;    // 攻击冷却时间
        [SerializeField] internal float attackRange = 2f;         // 攻击范围
        [SerializeField] internal float attackDamage = 10f;       // 攻击伤害
        
        [Header("范围设置")]
        [SerializeField] internal float chaseRange = 8f;          // 追击范围（超出后回到巡逻）
        
        [Header("攻击检测")]
        [SerializeField] internal Vector2 attackHitboxSize = new Vector2(1.5f, 1f);  // 攻击判定框大小
        [SerializeField] internal Vector2 attackHitboxOffset = new Vector2(1f, 0f);  // 攻击判定框偏移
        [SerializeField] internal LayerMask playerLayer = -1;     // 玩家层级
        
        #endregion
        
        #region 私有字段
        
        private GameObject targetPlayer;
        private float attackTimer;
        private bool hasDealtDamage;  // 本次攻击是否已造成伤害
        private bool isAttacking;     // 是否正在攻击动画中
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Attack";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            targetPlayer = enemy.GetPlayerTarget();
            attackTimer = 0f;
            hasDealtDamage = false;
            isAttacking = true;
            
            // 面向玩家
            if (targetPlayer != null)
            {
                bool shouldFaceRight = targetPlayer.transform.position.x > enemy.transform.position.x;
                enemy.SetFacingDirection(shouldFaceRight);
            }
            
            // 停止移动
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
            
            // 应用视觉效果
            ApplyAttackVisualEffect(enemy, true);
            
            // TODO: 播放攻击动画
            // enemy.PlayAnimation("Attack");
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始攻击 - 目标: {targetPlayer?.name}");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            attackTimer += Time.deltaTime;
            
            // TODO: 在动画特定帧触发伤害判定
            // 目前使用简单的时间判定（攻击持续时间的一半时造成伤害）
            if (!hasDealtDamage && attackTimer >= attackDuration * 0.5f)
            {
                PerformAttackHit(enemy);
                hasDealtDamage = true;
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            // 攻击时不移动
        }
        
        protected override void CheckTransitionConditions(EnemyController enemy)
        {
            // 更新目标玩家
            if (targetPlayer == null)
            {
                targetPlayer = enemy.GetPlayerTarget();
            }
            
            // 检查玩家距离
            if (targetPlayer != null)
            {
                float distance = Vector3.Distance(enemy.transform.position, targetPlayer.transform.position);
                
                // 玩家离开追击范围 → 回到巡逻/待机
                if (distance > chaseRange)
                {
                    if (enemy.StateMachine.HasState("Patrol"))
                    {
                        enemy.StateMachine.TransitionTo("Patrol");
                    }
                    else if (enemy.StateMachine.HasState("Idle"))
                    {
                        enemy.StateMachine.TransitionTo("Idle");
                    }
                    return;
                }
                
                // 玩家离开攻击范围但在追击范围内 → 追击
                if (distance > attackRange && !isAttacking)
                {
                    if (enemy.StateMachine.HasState("Chase"))
                    {
                        enemy.StateMachine.TransitionTo("Chase");
                    }
                    return;
                }
                
                // 玩家在攻击范围内，攻击完成后重新攻击
                if (attackTimer >= attackDuration)
                {
                    isAttacking = false;
                    
                    if (distance <= attackRange)
                    {
                        // 等待冷却后再次攻击
                        if (attackTimer >= attackDuration + attackCooldown)
                        {
                            attackTimer = 0f;
                            hasDealtDamage = false;
                            isAttacking = true;
                            
                            // 重新面向玩家
                            bool shouldFaceRight = targetPlayer.transform.position.x > enemy.transform.position.x;
                            enemy.SetFacingDirection(shouldFaceRight);
                            
                            if (debugMode)
                            {
                                Debug.Log($"[{enemy.name}] 再次攻击");
                            }
                        }
                    }
                }
            }
            else
            {
                // 没有目标，回到巡逻
                if (enemy.StateMachine.HasState("Patrol"))
                {
                    enemy.StateMachine.TransitionTo("Patrol");
                }
                else if (enemy.StateMachine.HasState("Idle"))
                {
                    enemy.StateMachine.TransitionTo("Idle");
                }
            }
        }
        
        protected override void CleanupState(EnemyController enemy)
        {
            ApplyAttackVisualEffect(enemy, false);
            isAttacking = false;
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束攻击");
            }
        }
        
        #endregion
        
        #region 攻击逻辑
        
        /// <summary>
        /// 执行攻击判定
        /// TODO: 后续接入动画事件触发
        /// </summary>
        private void PerformAttackHit(EnemyController enemy)
        {
            // 计算攻击判定框位置
            Vector2 hitboxCenter = (Vector2)enemy.transform.position + 
                new Vector2(attackHitboxOffset.x * (enemy.IsFacingRight ? 1 : -1), attackHitboxOffset.y);
            
            // 检测玩家
            Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, attackHitboxSize, 0f, playerLayer);
            
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    // TODO: 对玩家造成伤害
                    // var playerHealth = hit.GetComponent<PlayerHealth>();
                    // if (playerHealth != null)
                    // {
                    //     playerHealth.TakeDamage(attackDamage, enemy.transform.position);
                    // }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 攻击命中玩家！伤害: {attackDamage}");
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// 动画事件回调 - 攻击命中帧
        /// </summary>
        public void OnAttackHitFrame(EnemyController enemy)
        {
            if (!hasDealtDamage)
            {
                PerformAttackHit(enemy);
                hasDealtDamage = true;
            }
        }
        
        #endregion
        
        #region 视觉效果
        
        private void ApplyAttackVisualEffect(EnemyController enemy, bool enable)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.4f, 0.6f, 1f, 1f)  // 攻击状态：亮蓝色
                : Color.white;
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = targetColor;
                }
            }
        }
        
        #endregion
        
        #region 调试
        
        public void DrawDebugGizmos(EnemyController enemy)
        {
            if (!debugMode) return;
            
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, attackRange);
            
            // 绘制攻击判定框
            Gizmos.color = isAttacking ? Color.yellow : Color.gray;
            Vector2 hitboxCenter = (Vector2)enemy.transform.position + 
                new Vector2(attackHitboxOffset.x * (enemy.IsFacingRight ? 1 : -1), attackHitboxOffset.y);
            Gizmos.DrawWireCube(hitboxCenter, attackHitboxSize);
        }
        
        #endregion
    }
}
