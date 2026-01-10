using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 地面敌人追击状态
    /// 敌人在地面上追击玩家，进入攻击范围后切换到攻击状态
    /// </summary>
    public class GroundEnemyChaseState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("追击设置")]
        [SerializeField] internal float chaseSpeed = 4f; // 追击速度
        [SerializeField] internal float attackRange = 2f; // 攻击范围（进入后切换到Attack）
        [SerializeField] internal float detectionRange = 8f; // 检测范围（超出后丢失目标）
        [SerializeField] internal float chaseDuration = 15f; // 最大追击时间
        
        [Header("目标丢失设置")]
        [SerializeField] internal float loseTargetDelay = 2f; // 丢失目标后的延迟时间
        [SerializeField] internal float lastSeenMemory = 3f; // 记住玩家最后位置的时间
        
        [Header("物理检测")]
        [SerializeField] internal LayerMask groundLayer = 1; // 地面层级
        [SerializeField] internal LayerMask wallLayer = 1; // 墙壁层级
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级（视线检测）
        
        [Header("移动控制")]
        [SerializeField] internal bool usePhysicsMovement = true; // 使用物理移动
        [SerializeField] internal float accelerationRate = 10f; // 加速度
        [SerializeField] internal float turnSpeed = 5f; // 转向速度
        
        #endregion
        
        #region 私有字段
        
        // 目标追踪
        private GameObject targetPlayer;
        private Vector3 lastKnownPosition; // 玩家最后已知位置
        private float timeSinceLastSeen; // 自上次看到玩家的时间
        private bool hasLineOfSight; // 是否有视线
        
        // 移动控制
        private int moveDirection; // 移动方向：1右，-1左
        private float currentSpeed; // 当前速度
        
        // 状态控制
        private bool isTargetLost; // 目标是否丢失
        private float loseTargetTimer; // 丢失目标计时器
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Chase";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 获取玩家目标
            targetPlayer = enemy.GetPlayerTarget();
            
            // 初始化追踪状态
            if (targetPlayer != null)
            {
                lastKnownPosition = targetPlayer.transform.position;
                hasLineOfSight = true;
                
                // 初始朝向玩家
                bool shouldFaceRight = targetPlayer.transform.position.x > enemy.transform.position.x;
                enemy.SetFacingDirection(shouldFaceRight);
                moveDirection = shouldFaceRight ? 1 : -1;
            }
            else
            {
                // 如果没有玩家，使用当前朝向
                moveDirection = enemy.IsFacingRight ? 1 : -1;
                hasLineOfSight = false;
            }
            
            timeSinceLastSeen = 0f;
            isTargetLost = false;
            loseTargetTimer = 0f;
            currentSpeed = 0f;
            
            // 应用视觉效果
            ApplyChaseVisualEffect(enemy, true);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始追击 - 目标: {targetPlayer?.name}, 速度: {chaseSpeed}, 方向: {moveDirection}");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 更新目标追踪
            UpdateTargetTracking(enemy);
            
            // 更新朝向
            UpdateFacing(enemy);
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 确保有目标玩家
            if (targetPlayer == null)
            {
                targetPlayer = enemy.GetPlayerTarget();
                if (targetPlayer == null)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[{enemy.name}] Chase状态: 无法获取玩家目标！");
                    }
                    return;
                }
            }
            
            // 执行追击移动
            PerformChaseMovement(enemy);
        }
        
        protected override void CheckTransitionConditions(EnemyController enemy)
        {
            // 目标丢失 -> 回到巡逻或待机
            if (isTargetLost)
            {
                loseTargetTimer += Time.deltaTime;
                
                if (loseTargetTimer >= loseTargetDelay)
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
            }
            
            // 进入攻击范围 -> 切换到攻击状态
            if (targetPlayer != null && hasLineOfSight)
            {
                float distanceToTarget = Vector3.Distance(enemy.transform.position, targetPlayer.transform.position);
                
                if (distanceToTarget <= attackRange)
                {
                    if (enemy.StateMachine.HasState("Attack"))
                    {
                        enemy.StateMachine.TransitionTo("Attack");
                    }
                    return;
                }
            }
            
            // 追击超时 -> 回到巡逻
            if (IsStateTimeout(chaseDuration))
            {
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
            // 清除视觉效果
            ApplyChaseVisualEffect(enemy, false);
            
            // 停止移动
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束追击");
            }
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 更新目标追踪
        /// </summary>
        private void UpdateTargetTracking(EnemyController enemy)
        {
            if (targetPlayer == null)
            {
                targetPlayer = enemy.GetPlayerTarget();
                if (targetPlayer == null)
                {
                    isTargetLost = true;
                    return;
                }
            }
            
            Vector3 enemyPos = enemy.transform.position;
            Vector3 playerPos = targetPlayer.transform.position;
            float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);
            
            // 检查是否超出检测范围
            if (distanceToPlayer > detectionRange)
            {
                hasLineOfSight = false;
                timeSinceLastSeen += Time.deltaTime;
                
                if (timeSinceLastSeen >= lastSeenMemory)
                {
                    isTargetLost = true;
                }
                return;
            }
            
            // 检查视线 - 只有当obstacleLayer不是-1时才进行遮挡检测
            if (obstacleLayer.value != -1 && obstacleLayer.value != 0)
            {
                Vector3 directionToPlayer = (playerPos - enemyPos).normalized;
                RaycastHit2D hit = Physics2D.Raycast(enemyPos, directionToPlayer, distanceToPlayer, obstacleLayer);
                
                if (hit.collider != null && hit.collider.gameObject != targetPlayer)
                {
                    // 视线被阻挡
                    hasLineOfSight = false;
                    timeSinceLastSeen += Time.deltaTime;
                    
                    if (timeSinceLastSeen >= lastSeenMemory)
                    {
                        isTargetLost = true;
                    }
                    return;
                }
            }
            
            // 有视线或不需要视线检测
            hasLineOfSight = true;
            timeSinceLastSeen = 0f;
            lastKnownPosition = playerPos;
            isTargetLost = false;
            loseTargetTimer = 0f;
        }
        
        /// <summary>
        /// 更新朝向
        /// </summary>
        private void UpdateFacing(EnemyController enemy)
        {
            Vector3 targetPos = hasLineOfSight && targetPlayer != null 
                ? targetPlayer.transform.position 
                : lastKnownPosition;
            
            bool shouldFaceRight = targetPos.x > enemy.transform.position.x;
            
            // 始终更新移动方向
            moveDirection = shouldFaceRight ? 1 : -1;
            
            if (enemy.IsFacingRight != shouldFaceRight)
            {
                enemy.SetFacingDirection(shouldFaceRight);
            }
        }
        
        /// <summary>
        /// 执行追击移动
        /// </summary>
        private void PerformChaseMovement(EnemyController enemy)
        {
            // 只有当层级正确配置时才进行墙壁和边缘检测
            bool shouldCheckObstacles = wallLayer.value != 1 && wallLayer.value != 0 && wallLayer.value != -1;
            bool shouldCheckEdge = groundLayer.value != 1 && groundLayer.value != 0 && groundLayer.value != -1;
            
            if (shouldCheckObstacles)
            {
                bool hitWall = CheckWallAhead(enemy);
                if (hitWall)
                {
                    currentSpeed = 0f;
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 追击受阻 - 墙壁");
                    }
                    return;
                }
            }
            
            if (shouldCheckEdge)
            {
                bool atEdge = CheckPlatformEdge(enemy);
                if (atEdge)
                {
                    currentSpeed = 0f;
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 追击受阻 - 边缘");
                    }
                    return;
                }
            }
            
            // 加速到目标速度
            currentSpeed = Mathf.MoveTowards(currentSpeed, chaseSpeed, accelerationRate * Time.fixedDeltaTime);
            
            // 应用移动
            if (usePhysicsMovement)
            {
                Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = new Vector2(currentSpeed * moveDirection, rb.velocity.y);
                }
            }
            else
            {
                Vector3 movement = Vector3.right * currentSpeed * moveDirection * Time.fixedDeltaTime;
                enemy.transform.position += movement;
            }
        }
        
        /// <summary>
        /// 检查前方墙壁
        /// </summary>
        private bool CheckWallAhead(EnemyController enemy)
        {
            Vector2 direction = moveDirection > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D hit = Physics2D.Raycast(enemy.transform.position, direction, 0.6f, wallLayer);
            return hit.collider != null;
        }
        
        /// <summary>
        /// 检查平台边缘
        /// </summary>
        private bool CheckPlatformEdge(EnemyController enemy)
        {
            Vector3 checkPos = enemy.transform.position + Vector3.right * moveDirection * 0.6f;
            RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1.5f, groundLayer);
            return hit.collider == null;
        }
        
        /// <summary>
        /// 应用追击视觉效果
        /// </summary>
        private void ApplyChaseVisualEffect(EnemyController enemy, bool enable)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.3f, 0.5f, 1f, 1f)  // 追击状态：蓝色
                : Color.white;
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = targetColor;
                }
            }
            
            if (debugMode && enable)
            {
                Debug.Log($"[{enemy.name}] 应用追击视觉效果");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 追击状态可以转换到任何状态
            return true;
        }
        
        #endregion
        
        #region 调试方法
        
        /// <summary>
        /// 绘制调试Gizmos
        /// </summary>
        public void DrawDebugGizmos(EnemyController enemy)
        {
            if (!debugMode) return;
            
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position, detectionRange);
            
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, attackRange);
            
            // 绘制到目标的连线
            if (targetPlayer != null)
            {
                Gizmos.color = hasLineOfSight ? Color.green : Color.gray;
                Gizmos.DrawLine(enemy.transform.position, targetPlayer.transform.position);
            }
            
            // 绘制最后已知位置
            if (!hasLineOfSight)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
            }
            
            // 绘制移动方向
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(enemy.transform.position, Vector3.right * moveDirection * 1.5f);
        }
        
        #endregion
    }
}
