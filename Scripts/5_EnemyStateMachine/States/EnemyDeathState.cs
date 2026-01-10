using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 敌人死亡状态
    /// 敌人血量为0时进入此状态
    /// 流程：变黑 → 禁用碰撞体 → 等待2秒 → 闪烁消失 → 销毁
    /// </summary>
    public class EnemyDeathState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("死亡设置")]
        [SerializeField] internal float deathDelay = 2f;           // 死亡后等待时间
        [SerializeField] internal float fadeOutDuration = 1f;      // 闪烁消失持续时间
        [SerializeField] internal float flashInterval = 0.1f;      // 闪烁间隔
        
        [Header("视觉效果")]
        [SerializeField] internal Color deathColor = Color.black;  // 死亡颜色（黑色）
        
        [Header("掉落设置")]
        [SerializeField] internal bool dropLootOnDeath = true;     // 死亡时是否掉落物品
        
        #endregion
        
        #region 私有字段
        
        private Renderer[] cachedRenderers;     // 缓存的渲染器
        private Collider2D[] cachedColliders;   // 缓存的碰撞体
        private float fadeTimer;                // 消失计时器
        private float flashTimer;               // 闪烁计时器
        private bool isFading;                  // 是否正在消失
        private bool isVisible;                 // 当前是否可见
        private bool hasDroppedLoot;            // 是否已掉落物品
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Death";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 缓存组件
            cachedRenderers = enemy.GetComponentsInChildren<Renderer>(true);
            cachedColliders = enemy.GetComponentsInChildren<Collider2D>(true);
            
            // 初始化计时器
            fadeTimer = 0f;
            flashTimer = 0f;
            isFading = false;
            isVisible = true;
            hasDroppedLoot = false;
            
            // 立即变黑
            ApplyDeathColor(enemy);
            
            // 禁用与玩家的碰撞
            DisablePlayerCollision();
            
            // 停止所有移动
            StopMovement(enemy);
            
            // 禁用AI行为
            enemy.SetAIEnabled(false);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 进入死亡状态 - 等待{deathDelay}秒后消失");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            // 等待阶段
            if (!isFading)
            {
                if (stateTimer >= deathDelay)
                {
                    // 开始闪烁消失
                    isFading = true;
                    fadeTimer = 0f;
                    
                    // 掉落物品
                    if (dropLootOnDeath && !hasDroppedLoot)
                    {
                        enemy.DropLoot();
                        hasDroppedLoot = true;
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 开始闪烁消失");
                    }
                }
            }
            else
            {
                // 闪烁消失阶段
                fadeTimer += Time.deltaTime;
                flashTimer += Time.deltaTime;
                
                // 闪烁效果
                if (flashTimer >= flashInterval)
                {
                    flashTimer = 0f;
                    isVisible = !isVisible;
                    SetRenderersVisible(isVisible);
                }
                
                // 消失完成
                if (fadeTimer >= fadeOutDuration)
                {
                    // 销毁敌人
                    enemy.DestroyEnemy();
                }
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            // 死亡状态不执行物理更新
        }
        
        protected override void CheckTransitionConditions(EnemyController enemy)
        {
            // 死亡状态不能转换到其他状态
        }
        
        protected override void CleanupState(EnemyController enemy)
        {
            // 死亡状态通常不会退出，但以防万一
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 退出死亡状态");
            }
        }
        
        #endregion
        
        #region 视觉效果
        
        /// <summary>
        /// 应用死亡颜色（黑色）
        /// </summary>
        private void ApplyDeathColor(EnemyController enemy)
        {
            if (cachedRenderers == null) return;
            
            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = deathColor;
                }
            }
        }
        
        /// <summary>
        /// 设置渲染器可见性
        /// </summary>
        private void SetRenderersVisible(bool visible)
        {
            if (cachedRenderers == null) return;
            
            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
        
        #endregion
        
        #region 物理控制
        
        /// <summary>
        /// 禁用与玩家的碰撞（而非禁用所有碰撞体）
        /// </summary>
        private void DisablePlayerCollision()
        {
            if (cachedColliders == null) return;
            
            // 获取玩家的碰撞体
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(true);
            if (playerColliders == null || playerColliders.Length == 0) return;
            
            // 忽略敌人与玩家之间的碰撞
            foreach (var enemyCollider in cachedColliders)
            {
                if (enemyCollider == null) continue;
                
                foreach (var playerCollider in playerColliders)
                {
                    if (playerCollider != null)
                    {
                        Physics2D.IgnoreCollision(enemyCollider, playerCollider, true);
                    }
                }
            }
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        private void StopMovement(EnemyController enemy)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                
                // 如果是飞行敌人，保持位置不下落
                if (rb.gravityScale == 0f)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 死亡状态不能转换到任何其他状态
            return false;
        }
        
        #endregion
        
        #region 调试方法
        
        public void DrawDebugGizmos(EnemyController enemy)
        {
            if (!debugMode) return;
            
            // 绘制死亡标记
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(enemy.transform.position, 1f);
            
            // 绘制X标记
            Vector3 pos = enemy.transform.position;
            Gizmos.DrawLine(pos + new Vector3(-0.5f, -0.5f, 0), pos + new Vector3(0.5f, 0.5f, 0));
            Gizmos.DrawLine(pos + new Vector3(-0.5f, 0.5f, 0), pos + new Vector3(0.5f, -0.5f, 0));
        }
        
        #endregion
    }
}
