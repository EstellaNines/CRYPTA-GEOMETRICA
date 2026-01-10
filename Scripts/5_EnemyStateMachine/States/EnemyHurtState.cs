using UnityEngine;
using System.Collections;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 敌人受伤状态
    /// 敌人受到攻击时进入此状态，显示闪红效果
    /// 适用于地面敌人和飞行敌人
    /// </summary>
    public class EnemyHurtState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("受伤设置")]
        [SerializeField] internal float hurtDuration = 0.5f;      // 受伤状态持续时间
        [SerializeField] internal float knockbackForce = 3f;      // 击退力度
        [SerializeField] internal bool enableKnockback = true;    // 是否启用击退
        
        [Header("闪红效果")]
        [SerializeField] internal float flashDuration = 0.1f;     // 单次闪烁持续时间
        [SerializeField] internal int flashCount = 3;             // 闪烁次数
        [SerializeField] internal Color hurtColor = new Color(1f, 0f, 0f, 1f);  // 受伤颜色（红色）
        
        [Header("无敌时间")]
        [SerializeField] internal float invincibilityDuration = 0.8f;  // 无敌时间
        
        #endregion
        
        #region 私有字段
        
        private Vector3 damageSource;           // 伤害来源位置
        private float flashTimer;               // 闪烁计时器
        private int currentFlashCount;          // 当前闪烁次数
        private bool isFlashingRed;             // 当前是否显示红色
        private Color[] originalColors;         // 原始颜色缓存
        private Renderer[] cachedRenderers;     // 缓存的渲染器
        private string previousState;           // 进入受伤状态前的状态
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Hurt";
        
        /// <summary>
        /// 设置伤害来源位置（用于击退方向计算）
        /// </summary>
        public Vector3 DamageSource
        {
            get => damageSource;
            set => damageSource = value;
        }
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 记录进入前的状态
            previousState = enemy.StateMachine.GetPreviousState();
            
            // 初始化闪烁
            flashTimer = 0f;
            currentFlashCount = 0;
            isFlashingRed = true;
            
            // 缓存渲染器和原始颜色
            CacheRenderersAndColors(enemy);
            
            // 应用击退
            if (enableKnockback && damageSource != Vector3.zero)
            {
                ApplyKnockback(enemy);
            }
            
            // 停止水平移动（保留垂直速度用于重力）
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 飞行敌人完全停止，地面敌人保留Y轴速度
                if (rb.gravityScale == 0f)
                {
                    rb.velocity = Vector2.zero;
                }
                else
                {
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                }
            }
            
            // 立即应用红色
            ApplyHurtColor(enemy);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 进入受伤状态 - 持续: {hurtDuration}s, 闪烁: {flashCount}次");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 更新闪烁效果
            UpdateFlashEffect(enemy);
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            // 受伤时不主动移动
        }
        
        protected override void CheckTransitionConditions(EnemyController enemy)
        {
            // 受伤状态结束后返回之前的状态
            if (IsStateTimeout(hurtDuration))
            {
                // 优先返回之前的状态
                if (!string.IsNullOrEmpty(previousState) && enemy.StateMachine.HasState(previousState))
                {
                    // 如果之前是攻击或追击状态，检查玩家是否还在范围内
                    if (previousState == "Attack" || previousState == "Chase")
                    {
                        GameObject player = enemy.GetPlayerTarget();
                        if (player != null)
                        {
                            enemy.StateMachine.TransitionTo(previousState);
                            return;
                        }
                    }
                }
                
                // 默认返回巡逻或待机
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
            // 恢复原始颜色
            RestoreOriginalColors(enemy);
            
            // 清理
            damageSource = Vector3.zero;
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束受伤状态");
            }
        }
        
        #endregion

        
        #region 闪烁效果
        
        /// <summary>
        /// 缓存渲染器和原始颜色
        /// </summary>
        private void CacheRenderersAndColors(EnemyController enemy)
        {
            cachedRenderers = enemy.GetComponentsInChildren<Renderer>(true);
            
            if (cachedRenderers != null && cachedRenderers.Length > 0)
            {
                originalColors = new Color[cachedRenderers.Length];
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
                    {
                        originalColors[i] = cachedRenderers[i].material.color;
                    }
                    else
                    {
                        originalColors[i] = Color.white;
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新闪烁效果
        /// </summary>
        private void UpdateFlashEffect(EnemyController enemy)
        {
            if (currentFlashCount >= flashCount * 2) return; // 闪烁完成
            
            flashTimer += Time.deltaTime;
            
            if (flashTimer >= flashDuration)
            {
                flashTimer = 0f;
                currentFlashCount++;
                isFlashingRed = !isFlashingRed;
                
                if (isFlashingRed)
                {
                    ApplyHurtColor(enemy);
                }
                else
                {
                    RestoreOriginalColors(enemy);
                }
            }
        }
        
        /// <summary>
        /// 应用受伤颜色（红色）
        /// </summary>
        private void ApplyHurtColor(EnemyController enemy)
        {
            if (cachedRenderers == null) return;
            
            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = hurtColor;
                }
            }
        }
        
        /// <summary>
        /// 恢复原始颜色
        /// </summary>
        private void RestoreOriginalColors(EnemyController enemy)
        {
            if (cachedRenderers == null || originalColors == null) return;
            
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
                {
                    cachedRenderers[i].material.color = originalColors[i];
                }
            }
        }
        
        #endregion
        
        #region 击退效果
        
        /// <summary>
        /// 应用击退
        /// </summary>
        private void ApplyKnockback(EnemyController enemy)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            
            // 计算击退方向（从伤害来源指向敌人）
            Vector2 knockbackDirection = (enemy.transform.position - damageSource).normalized;
            
            // 如果是地面敌人，只在水平方向击退
            if (rb.gravityScale > 0f)
            {
                knockbackDirection.y = 0.3f; // 轻微向上
                knockbackDirection = knockbackDirection.normalized;
            }
            
            // 应用击退力
            rb.velocity = knockbackDirection * knockbackForce;
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 击退方向: {knockbackDirection}, 力度: {knockbackForce}");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 受伤状态可以被死亡状态打断
            if (targetState == "Death")
            {
                return true;
            }
            
            // 受伤状态期间不能被其他状态打断（除非受伤结束）
            return IsStateTimeout(hurtDuration);
        }
        
        #endregion
        
        #region 调试方法
        
        public void DrawDebugGizmos(EnemyController enemy)
        {
            if (!debugMode) return;
            
            // 绘制伤害来源
            if (damageSource != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(enemy.transform.position, damageSource);
                Gizmos.DrawWireSphere(damageSource, 0.3f);
            }
        }
        
        #endregion
    }
}
