using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.EnemyStateMachine
{
    /// <summary>
    /// 敌人控制器基类
    /// 提供敌人AI的基础框架和状态机集成
    /// </summary>
    public abstract class EnemyController : SerializedMonoBehaviour
    {
        #region 状态机相关
        
        [Header("状态机设置")]
        [SerializeField] protected bool enableStateMachineDebug = false;
        
        /// <summary>
        /// 敌人状态机
        /// </summary>
        public EnemyStateMachine StateMachine { get; private set; }
        
        #endregion
        
        #region 基础属性
        
        [Header("生命值设置")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        
        [Header("检测设置")]
        [SerializeField] protected LayerMask playerLayer = 1 << 8; // 假设玩家在第8层
        [SerializeField] protected Transform detectionPoint;
        [SerializeField] protected float baseDetectionRange = 5f;
        
        [Header("移动设置")]
        [SerializeField] protected float baseMovementSpeed = 3f;
        [SerializeField] protected bool canMove = true;
        
        [Header("组件引用")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Rigidbody2D rigidBody;
        [SerializeField] protected Collider2D mainCollider;
        
        #endregion
        
        #region 状态标记
        
        /// <summary>
        /// 刚刚受到伤害标记
        /// </summary>
        public bool JustTookDamage { get; private set; }
        
        /// <summary>
        /// 当前生命值
        /// </summary>
        public float CurrentHealth => currentHealth;
        
        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        
        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => currentHealth > 0;
        
        /// <summary>
        /// 是否可以行动
        /// </summary>
        public bool CanAct => IsAlive && canMove;
        
        /// <summary>
        /// 是否面向右侧
        /// </summary>
        public bool IsFacingRight { get; protected set; } = true;
        
        #endregion
        
        #region Unity生命周期
        
        protected virtual void Awake()
        {
            // 初始化状态机
            StateMachine = new EnemyStateMachine();
            StateMachine.Initialize(this, enableStateMachineDebug);
            
            // 初始化生命值
            currentHealth = maxHealth;
            
            // 获取组件引用
            InitializeComponents();
            
            // 注册状态
            RegisterStates();
        }
        
        protected virtual void Start()
        {
            // 启动状态机，默认进入待机状态
            if (StateMachine.HasState("Idle"))
            {
                StateMachine.TransitionTo("Idle");
            }
            else
            {
                Debug.LogWarning($"[{name}] 没有找到Idle状态，请确保已注册");
            }
        }
        
        protected virtual void Update()
        {
            // 更新状态机 - 处理逻辑
            StateMachine.Update();
            
            // 重置每帧标记
            JustTookDamage = false;
        }
        
        /// <summary>
        /// 物理更新 - 处理物理相关状态逻辑
        /// </summary>
        protected virtual void FixedUpdate()
        {
            // 更新状态机 - 处理物理
            StateMachine.FixedUpdate();
        }
        
        #endregion
        
        #region 抽象方法 - 子类必须实现
        
        /// <summary>
        /// 注册敌人特定的状态
        /// </summary>
        protected abstract void RegisterStates();
        
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        public abstract void PlayAnimation(string animationName);
        
        /// <summary>
        /// 向目标位置移动
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="speed">移动速度</param>
        public abstract void MoveTowards(Vector3 target, float speed);
        
        /// <summary>
        /// 面向目标
        /// </summary>
        /// <param name="target">目标位置</param>
        public abstract void FaceTarget(Vector3 target);
        
        /// <summary>
        /// 执行攻击
        /// </summary>
        public abstract void PerformAttack();
        
        /// <summary>
        /// 检测玩家
        /// </summary>
        /// <param name="range">检测范围</param>
        /// <returns>是否检测到玩家</returns>
        public abstract bool DetectPlayer(float range);
        
        /// <summary>
        /// 获取玩家目标
        /// </summary>
        /// <returns>玩家GameObject</returns>
        public abstract GameObject GetPlayerTarget();
        
        #endregion
        
        #region 虚方法 - 子类可重写
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        protected virtual void InitializeComponents()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody2D>();
            if (mainCollider == null)
                mainCollider = GetComponent<Collider2D>();
            if (detectionPoint == null)
                detectionPoint = transform;
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        public virtual void TakeDamage(float damage, Vector3 damageSource = default)
        {
            if (!IsAlive) return;
            
            currentHealth -= damage;
            JustTookDamage = true;
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDeath();
            }
            else
            {
                OnTakeDamage(damage, damageSource);
            }
        }
        
        /// <summary>
        /// 受伤时调用
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        protected virtual void OnTakeDamage(float damage, Vector3 damageSource)
        {
            // 子类可重写实现特殊受伤效果
        }
        
        /// <summary>
        /// 死亡时调用
        /// </summary>
        protected virtual void OnDeath()
        {
            // 子类可重写实现特殊死亡效果
        }
        
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundName">音效名称</param>
        public virtual void PlaySound(string soundName)
        {
            // 默认实现为空，子类可重写
        }
        
        /// <summary>
        /// 应用击退效果
        /// </summary>
        /// <param name="force">击退力度</param>
        /// <param name="direction">击退方向</param>
        public virtual void ApplyKnockback(float force, Vector3 direction = default)
        {
            if (rigidBody != null && direction != Vector3.zero)
            {
                rigidBody.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            }
        }
        
        /// <summary>
        /// 设置面向方向
        /// 支持两种翻转方式：
        /// 1. 单一SpriteRenderer：使用flipX
        /// 2. 多层级结构（PSD导入）：翻转localScale.x
        /// </summary>
        /// <param name="facingRight">是否面向右侧</param>
        public virtual void SetFacingDirection(bool facingRight)
        {
            if (IsFacingRight != facingRight)
            {
                IsFacingRight = facingRight;
                
                // 优先使用localScale翻转（支持多层级结构）
                Vector3 scale = transform.localScale;
                scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
                
                // 如果有单独的SpriteRenderer且不是多层级结构，也可以使用flipX
                // if (spriteRenderer != null)
                // {
                //     spriteRenderer.flipX = !facingRight;
                // }
            }
        }
        
        /// <summary>
        /// 设置碰撞体启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public virtual void SetColliderEnabled(bool enabled)
        {
            if (mainCollider != null)
            {
                mainCollider.enabled = enabled;
            }
        }
        
        /// <summary>
        /// 设置AI启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public virtual void SetAIEnabled(bool enabled)
        {
            canMove = enabled;
        }
        
        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="alpha">透明度值</param>
        public virtual void SetAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = Mathf.Clamp01(alpha);
                spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 掉落战利品
        /// </summary>
        public virtual void DropLoot()
        {
            // 默认实现为空，子类可重写
        }
        
        /// <summary>
        /// 销毁敌人
        /// </summary>
        public virtual void DestroyEnemy()
        {
            Destroy(gameObject);
        }
        
        #endregion
        
        #region 物理相关方法
        
        /// <summary>
        /// 使用物理系统移动
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="speed">移动速度</param>
        public virtual void MoveWithPhysics(Vector3 target, float speed)
        {
            if (rigidBody != null && CanAct)
            {
                Vector3 direction = (target - transform.position).normalized;
                rigidBody.velocity = new Vector2(direction.x * speed, rigidBody.velocity.y);
            }
        }
        
        /// <summary>
        /// 应用重力
        /// </summary>
        public virtual void ApplyGravity()
        {
            // 默认由Unity的Rigidbody2D处理重力
            // 子类可重写实现自定义重力逻辑
        }
        
        /// <summary>
        /// 检查地面碰撞
        /// </summary>
        /// <returns>是否在地面上</returns>
        public virtual bool CheckGroundCollision()
        {
            // 默认实现，子类应重写
            return true;
        }
        
        /// <summary>
        /// 检查墙壁碰撞
        /// </summary>
        /// <returns>是否碰到墙壁</returns>
        public virtual bool CheckWallCollision()
        {
            // 默认实现，子类应重写
            return false;
        }
        
        /// <summary>
        /// 检查平台边缘
        /// </summary>
        /// <returns>是否在平台边缘</returns>
        public virtual bool CheckPlatformEdge()
        {
            // 默认实现，子类应重写
            return false;
        }
        
        /// <summary>
        /// 应用跳跃力
        /// </summary>
        /// <param name="jumpForce">跳跃力度</param>
        public virtual void ApplyJumpForce(float jumpForce)
        {
            if (rigidBody != null)
            {
                rigidBody.velocity = new Vector2(rigidBody.velocity.x, jumpForce);
            }
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 获取到目标的距离
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <returns>距离</returns>
        protected float GetDistanceToTarget(Vector3 target)
        {
            return Vector3.Distance(transform.position, target);
        }
        
        /// <summary>
        /// 检查目标是否在视线内
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="obstacleLayer">障碍物层级</param>
        /// <returns>是否在视线内</returns>
        protected bool IsTargetInLineOfSight(Vector3 target, LayerMask obstacleLayer)
        {
            Vector3 direction = target - detectionPoint.position;
            RaycastHit2D hit = Physics2D.Raycast(detectionPoint.position, direction.normalized, direction.magnitude, obstacleLayer);
            return hit.collider == null;
        }
        
        /// <summary>
        /// 恢复生命值
        /// </summary>
        /// <param name="amount">恢复量</param>
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }
        
        /// <summary>
        /// 设置最大生命值
        /// </summary>
        /// <param name="newMaxHealth">新的最大生命值</param>
        public void SetMaxHealth(float newMaxHealth)
        {
            float healthRatio = HealthPercentage;
            maxHealth = newMaxHealth;
            currentHealth = maxHealth * healthRatio;
        }
        
        #endregion
        
        #region 调试方法
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(detectionPoint != null ? detectionPoint.position : transform.position, baseDetectionRange);
            
            // 绘制面向方向
            Gizmos.color = IsFacingRight ? Color.green : Color.red;
            Vector3 direction = IsFacingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
        
        #endregion
    }
}
