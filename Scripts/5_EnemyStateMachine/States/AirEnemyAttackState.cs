using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 飞行敌人攻击状态 - 简单框架
    /// 支持多种攻击模式：俯冲攻击、远程攻击等
    /// 等待攻击动画完成后再实现具体攻击逻辑
    /// </summary>
    public class AirEnemyAttackState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("攻击设置")]
        [SerializeField] internal float attackDuration = 1.2f;    // 攻击持续时间
        [SerializeField] internal float attackCooldown = 2f;      // 攻击冷却时间
        [SerializeField] internal float attackRange = 2.5f;       // 攻击范围
        [SerializeField] internal float attackDamage = 15f;       // 攻击伤害
        
        [Header("范围设置")]
        [SerializeField] internal float chaseRange = 10f;         // 追击范围（超出后回到巡逻）
        
        [Header("攻击模式")]
        [SerializeField] internal AttackMode attackMode = AttackMode.Dive;
        
        [Header("俯冲攻击设置")]
        [SerializeField] internal float diveSpeed = 12f;          // 俯冲速度
        [SerializeField] internal float diveAngle = 45f;          // 俯冲角度
        [SerializeField] internal float pullUpHeight = 2f;        // 拉起高度
        
        [Header("远程攻击设置")]
        [SerializeField] internal GameObject projectilePrefab;    // 投射物预制体
        [SerializeField] internal float projectileSpeed = 8f;     // 投射物速度
        [SerializeField] internal Transform firePoint;            // 发射点
        
        [Header("攻击检测")]
        [SerializeField] internal float attackHitRadius = 1f;     // 攻击判定半径
        [SerializeField] internal LayerMask playerLayer = -1;     // 玩家层级
        
        #endregion
        
        #region 枚举定义
        
        public enum AttackMode
        {
            Dive,       // 俯冲攻击
            Ranged,     // 远程攻击
            Swoop       // 掠过攻击
        }
        
        #endregion
        
        #region 私有字段
        
        private GameObject targetPlayer;
        private float attackTimer;
        private bool hasDealtDamage;
        private bool isAttacking;
        
        // 俯冲攻击相关
        private Vector3 diveStartPos;
        private Vector3 diveTargetPos;
        private bool isDiving;
        private bool isPullingUp;
        
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
            isDiving = false;
            isPullingUp = false;
            
            // 记录起始位置
            diveStartPos = enemy.transform.position;
            
            // 面向玩家
            if (targetPlayer != null)
            {
                bool shouldFaceRight = targetPlayer.transform.position.x > enemy.transform.position.x;
                enemy.SetFacingDirection(shouldFaceRight);
                diveTargetPos = targetPlayer.transform.position;
            }
            
            // 确保不受重力影响
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
            }
            
            // 应用视觉效果
            ApplyAttackVisualEffect(enemy, true);
            
            // TODO: 播放攻击动画
            // enemy.PlayAnimation("Attack");
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始飞行攻击 - 模式: {attackMode}, 目标: {targetPlayer?.name}");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            attackTimer += Time.deltaTime;
            
            // 更新目标位置
            if (targetPlayer != null && !isDiving)
            {
                diveTargetPos = targetPlayer.transform.position;
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            switch (attackMode)
            {
                case AttackMode.Dive:
                    PerformDiveAttack(enemy);
                    break;
                case AttackMode.Ranged:
                    PerformRangedAttack(enemy);
                    break;
                case AttackMode.Swoop:
                    PerformSwoopAttack(enemy);
                    break;
            }
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
                if (distance > attackRange && !isAttacking && !isDiving && !isPullingUp)
                {
                    if (enemy.StateMachine.HasState("Chase"))
                    {
                        enemy.StateMachine.TransitionTo("Chase");
                    }
                    return;
                }
                
                // 玩家在攻击范围内，攻击完成后重新攻击
                if (attackTimer >= attackDuration && !isDiving && !isPullingUp)
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
                            isDiving = false;
                            isPullingUp = false;
                            diveStartPos = enemy.transform.position;
                            
                            // 重新面向玩家
                            bool shouldFaceRight = targetPlayer.transform.position.x > enemy.transform.position.x;
                            enemy.SetFacingDirection(shouldFaceRight);
                            
                            if (debugMode)
                            {
                                Debug.Log($"[{enemy.name}] 再次飞行攻击");
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
            isDiving = false;
            
            // 停止移动
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束飞行攻击");
            }
        }
        
        #endregion
        
        #region 攻击模式实现
        
        /// <summary>
        /// 俯冲攻击
        /// </summary>
        private void PerformDiveAttack(EnemyController enemy)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            
            if (!isDiving && !isPullingUp)
            {
                // 开始俯冲
                isDiving = true;
                
                if (debugMode)
                {
                    Debug.Log($"[{enemy.name}] 开始俯冲！");
                }
            }
            
            if (isDiving)
            {
                // 计算俯冲方向
                Vector2 diveDirection = (diveTargetPos - enemy.transform.position).normalized;
                rb.velocity = diveDirection * diveSpeed;
                
                // 检测是否命中
                if (!hasDealtDamage)
                {
                    CheckDiveHit(enemy);
                }
                
                // 检测是否到达目标或低于目标
                if (enemy.transform.position.y <= diveTargetPos.y || 
                    Vector3.Distance(enemy.transform.position, diveTargetPos) < 0.5f)
                {
                    isDiving = false;
                    isPullingUp = true;
                }
            }
            
            if (isPullingUp)
            {
                // 拉起
                Vector2 pullUpDirection = new Vector2(enemy.IsFacingRight ? 1 : -1, 1).normalized;
                rb.velocity = pullUpDirection * diveSpeed * 0.7f;
                
                // 检测是否拉起到足够高度
                if (enemy.transform.position.y >= diveStartPos.y - pullUpHeight)
                {
                    isPullingUp = false;
                    rb.velocity = Vector2.zero;
                }
            }
        }
        
        /// <summary>
        /// 远程攻击
        /// </summary>
        private void PerformRangedAttack(EnemyController enemy)
        {
            // 悬停
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // 在攻击时间的一半时发射投射物
            if (!hasDealtDamage && attackTimer >= attackDuration * 0.5f)
            {
                FireProjectile(enemy);
                hasDealtDamage = true;
            }
        }
        
        /// <summary>
        /// 掠过攻击
        /// </summary>
        private void PerformSwoopAttack(EnemyController enemy)
        {
            // 类似俯冲但保持水平
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            
            if (targetPlayer != null)
            {
                Vector2 direction = new Vector2(
                    enemy.IsFacingRight ? 1 : -1,
                    0
                );
                
                rb.velocity = direction * diveSpeed * 0.8f;
                
                // 检测命中
                if (!hasDealtDamage)
                {
                    CheckDiveHit(enemy);
                }
            }
        }
        
        #endregion
        
        #region 攻击判定
        
        /// <summary>
        /// 检测俯冲命中
        /// </summary>
        private void CheckDiveHit(EnemyController enemy)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(enemy.transform.position, attackHitRadius, playerLayer);
            
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    // TODO: 对玩家造成伤害
                    hasDealtDamage = true;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 俯冲命中玩家！伤害: {attackDamage}");
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// 发射投射物
        /// </summary>
        private void FireProjectile(EnemyController enemy)
        {
            if (projectilePrefab == null || targetPlayer == null)
            {
                if (debugMode)
                {
                    Debug.Log($"[{enemy.name}] 发射投射物（无预制体，仅模拟）");
                }
                return;
            }
            
            // TODO: 实例化投射物
            // Vector3 spawnPos = firePoint != null ? firePoint.position : enemy.transform.position;
            // GameObject projectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            // 设置投射物方向和速度...
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 发射投射物！");
            }
        }
        
        /// <summary>
        /// 动画事件回调 - 攻击命中帧
        /// </summary>
        public void OnAttackHitFrame(EnemyController enemy)
        {
            if (!hasDealtDamage)
            {
                CheckDiveHit(enemy);
            }
        }
        
        #endregion
        
        #region 视觉效果
        
        private void ApplyAttackVisualEffect(EnemyController enemy, bool enable)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.5f, 0.7f, 1f, 1f)  // 飞行攻击状态：浅蓝色
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
            
            // 绘制攻击判定半径
            Gizmos.color = isAttacking ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(enemy.transform.position, attackHitRadius);
            
            // 绘制俯冲路径
            if (isDiving && targetPlayer != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(enemy.transform.position, diveTargetPos);
            }
        }
        
        #endregion
    }
}
