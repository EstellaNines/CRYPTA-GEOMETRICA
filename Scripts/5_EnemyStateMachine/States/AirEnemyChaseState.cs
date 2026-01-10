using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 飞行敌人追击状态
    /// 敌人在空中追击玩家，可以在3D空间内自由移动
    /// </summary>
    public class AirEnemyChaseState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("追击设置")]
        [SerializeField] internal float chaseSpeed = 5f; // 追击速度
        [SerializeField] internal float attackRange = 2.5f; // 攻击范围
        [SerializeField] internal float detectionRange = 10f; // 检测范围
        [SerializeField] internal float chaseDuration = 20f; // 最大追击时间
        
        [Header("目标丢失设置")]
        [SerializeField] internal float loseTargetDelay = 3f; // 丢失目标后的延迟时间
        [SerializeField] internal float lastSeenMemory = 5f; // 记住玩家最后位置的时间
        
        [Header("飞行控制")]
        [SerializeField] internal float minFlightHeight = 1f; // 最低飞行高度
        [SerializeField] internal float maxFlightHeight = 15f; // 最高飞行高度
        [SerializeField] internal float preferredHeightAbovePlayer = 1.5f; // 相对玩家的首选高度
        [SerializeField] internal float verticalChaseSpeed = 4f; // 垂直追击速度
        
        [Header("追击行为")]
        [SerializeField] internal ChasePattern chasePattern = ChasePattern.Direct; // 追击模式
        [SerializeField] internal float circleRadius = 3f; // 环绕半径（环绕模式）
        [SerializeField] internal float circleSpeed = 2f; // 环绕速度
        [SerializeField] internal float approachDistance = 4f; // 接近距离（保持距离模式）
        
        [Header("障碍物检测")]
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级
        [SerializeField] internal float obstacleAvoidanceDistance = 1.5f; // 障碍物避让距离
        
        [Header("移动控制")]
        [SerializeField] internal float accelerationRate = 8f; // 加速度
        [SerializeField] internal float decelerationRate = 12f; // 减速度
        [SerializeField] internal float smoothTime = 0.2f; // 平滑时间
        
        #endregion
        
        #region 枚举定义
        
        public enum ChasePattern
        {
            Direct,         // 直接追击
            Circling,       // 环绕追击
            KeepDistance,   // 保持距离
            Dive            // 俯冲攻击
        }
        
        #endregion
        
        #region 私有字段
        
        // 目标追踪
        private GameObject targetPlayer;
        private Vector3 lastKnownPosition;
        private float timeSinceLastSeen;
        private bool hasLineOfSight;
        
        // 移动控制
        private Vector2 currentVelocity;
        private Vector2 velocitySmooth;
        private float circleAngle;
        
        // 状态控制
        private bool isTargetLost;
        private float loseTargetTimer;
        private bool isDiving; // 俯冲状态
        private float diveStartHeight;
        
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
            }
            
            timeSinceLastSeen = 0f;
            isTargetLost = false;
            loseTargetTimer = 0f;
            currentVelocity = Vector2.zero;
            velocitySmooth = Vector2.zero;
            circleAngle = 0f;
            isDiving = false;
            
            // 确保不受重力影响
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
            }
            
            // 应用视觉效果
            ApplyChaseVisualEffect(enemy, true);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始飞行追击 - 模式: {chasePattern}, 目标: {targetPlayer?.name}");
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
            
            // 根据追击模式执行移动
            switch (chasePattern)
            {
                case ChasePattern.Direct:
                    PerformDirectChase(enemy);
                    break;
                case ChasePattern.Circling:
                    PerformCirclingChase(enemy);
                    break;
                case ChasePattern.KeepDistance:
                    PerformKeepDistanceChase(enemy);
                    break;
                case ChasePattern.Dive:
                    PerformDiveChase(enemy);
                    break;
            }
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
                rb.velocity = Vector2.zero;
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束飞行追击");
            }
        }
        
        #endregion
        
        #region 追击模式实现
        
        /// <summary>
        /// 直接追击 - 直线飞向玩家
        /// </summary>
        private void PerformDirectChase(EnemyController enemy)
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 currentPos = enemy.transform.position;
            
            // 计算目标方向
            Vector2 direction = (targetPos - currentPos).normalized;
            
            // 检测障碍物并避让
            direction = AvoidObstacles(enemy, direction);
            
            // 计算目标速度
            Vector2 targetVelocity = direction * chaseSpeed;
            
            // 平滑加速
            currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref velocitySmooth, smoothTime);
            
            // 应用移动
            ApplyMovement(enemy, currentVelocity);
        }
        
        /// <summary>
        /// 环绕追击 - 围绕玩家飞行
        /// </summary>
        private void PerformCirclingChase(EnemyController enemy)
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 currentPos = enemy.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);
            
            // 如果距离太远，先接近
            if (distanceToTarget > circleRadius * 1.5f)
            {
                PerformDirectChase(enemy);
                return;
            }
            
            // 更新环绕角度
            circleAngle += circleSpeed * Time.fixedDeltaTime;
            
            // 计算环绕位置
            float targetX = targetPos.x + Mathf.Cos(circleAngle) * circleRadius;
            float targetY = targetPos.y + preferredHeightAbovePlayer + Mathf.Sin(circleAngle * 0.5f) * 1f;
            
            // 限制高度
            targetY = Mathf.Clamp(targetY, minFlightHeight, maxFlightHeight);
            
            Vector3 circlePos = new Vector3(targetX, targetY, currentPos.z);
            
            // 移动到环绕位置
            Vector2 direction = (circlePos - currentPos).normalized;
            direction = AvoidObstacles(enemy, direction);
            
            Vector2 targetVelocity = direction * chaseSpeed;
            currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref velocitySmooth, smoothTime);
            
            ApplyMovement(enemy, currentVelocity);
        }
        
        /// <summary>
        /// 保持距离追击 - 保持一定距离跟随
        /// </summary>
        private void PerformKeepDistanceChase(EnemyController enemy)
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 currentPos = enemy.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);
            
            Vector2 directionToTarget = (targetPos - currentPos).normalized;
            Vector2 targetVelocity;
            
            if (distanceToTarget > approachDistance + 1f)
            {
                // 太远，接近
                targetVelocity = directionToTarget * chaseSpeed;
            }
            else if (distanceToTarget < approachDistance - 1f)
            {
                // 太近，后退
                targetVelocity = -directionToTarget * chaseSpeed * 0.5f;
            }
            else
            {
                // 保持距离，横向移动
                Vector2 perpendicular = new Vector2(-directionToTarget.y, directionToTarget.x);
                targetVelocity = perpendicular * chaseSpeed * 0.3f;
            }
            
            // 保持首选高度
            float targetHeight = targetPos.y + preferredHeightAbovePlayer;
            targetHeight = Mathf.Clamp(targetHeight, minFlightHeight, maxFlightHeight);
            
            if (Mathf.Abs(currentPos.y - targetHeight) > 0.5f)
            {
                float verticalDir = Mathf.Sign(targetHeight - currentPos.y);
                targetVelocity.y += verticalDir * verticalChaseSpeed;
            }
            
            targetVelocity = AvoidObstacles(enemy, targetVelocity.normalized) * targetVelocity.magnitude;
            currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref velocitySmooth, smoothTime);
            
            ApplyMovement(enemy, currentVelocity);
        }
        
        /// <summary>
        /// 俯冲追击 - 从高处俯冲攻击
        /// </summary>
        private void PerformDiveChase(EnemyController enemy)
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 currentPos = enemy.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);
            
            if (!isDiving)
            {
                // 准备俯冲：先飞到玩家上方
                float targetHeight = targetPos.y + 5f;
                targetHeight = Mathf.Clamp(targetHeight, minFlightHeight, maxFlightHeight);
                
                if (currentPos.y < targetHeight - 0.5f)
                {
                    // 上升
                    Vector2 upDirection = new Vector2(
                        (targetPos.x - currentPos.x) * 0.3f,
                        1f
                    ).normalized;
                    
                    currentVelocity = Vector2.SmoothDamp(currentVelocity, upDirection * chaseSpeed, ref velocitySmooth, smoothTime);
                    ApplyMovement(enemy, currentVelocity);
                }
                else if (Mathf.Abs(currentPos.x - targetPos.x) < 2f)
                {
                    // 位置合适，开始俯冲
                    isDiving = true;
                    diveStartHeight = currentPos.y;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 开始俯冲攻击！");
                    }
                }
                else
                {
                    // 水平调整位置
                    Vector2 horizontalDir = new Vector2(Mathf.Sign(targetPos.x - currentPos.x), 0f);
                    currentVelocity = Vector2.SmoothDamp(currentVelocity, horizontalDir * chaseSpeed * 0.5f, ref velocitySmooth, smoothTime);
                    ApplyMovement(enemy, currentVelocity);
                }
            }
            else
            {
                // 执行俯冲
                Vector2 diveDirection = (targetPos - currentPos).normalized;
                float diveSpeed = chaseSpeed * 1.5f; // 俯冲速度更快
                
                currentVelocity = diveDirection * diveSpeed;
                ApplyMovement(enemy, currentVelocity);
                
                // 俯冲结束条件
                if (distanceToTarget <= attackRange || currentPos.y <= targetPos.y - 0.5f)
                {
                    isDiving = false;
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取目标位置
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            if (hasLineOfSight && targetPlayer != null)
            {
                return targetPlayer.transform.position;
            }
            return lastKnownPosition;
        }
        
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
            Vector3 targetPos = GetTargetPosition();
            bool shouldFaceRight = targetPos.x > enemy.transform.position.x;
            
            if (enemy.IsFacingRight != shouldFaceRight)
            {
                enemy.SetFacingDirection(shouldFaceRight);
            }
        }
        
        /// <summary>
        /// 避让障碍物
        /// </summary>
        private Vector2 AvoidObstacles(EnemyController enemy, Vector2 desiredDirection)
        {
            Vector3 pos = enemy.transform.position;
            
            // 检测前方障碍物
            RaycastHit2D hit = Physics2D.Raycast(pos, desiredDirection, obstacleAvoidanceDistance, obstacleLayer);
            
            if (hit.collider != null)
            {
                // 计算避让方向
                Vector2 avoidDirection = Vector2.Perpendicular(hit.normal);
                
                // 选择更接近原方向的避让方向
                if (Vector2.Dot(avoidDirection, desiredDirection) < 0)
                {
                    avoidDirection = -avoidDirection;
                }
                
                return Vector2.Lerp(desiredDirection, avoidDirection, 0.7f).normalized;
            }
            
            return desiredDirection;
        }
        
        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement(EnemyController enemy, Vector2 velocity)
        {
            // 限制高度
            Vector3 newPos = enemy.transform.position + (Vector3)velocity * Time.fixedDeltaTime;
            newPos.y = Mathf.Clamp(newPos.y, minFlightHeight, maxFlightHeight);
            
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = velocity;
                
                // 额外的高度限制
                if (rb.position.y < minFlightHeight)
                {
                    rb.position = new Vector2(rb.position.x, minFlightHeight);
                }
                else if (rb.position.y > maxFlightHeight)
                {
                    rb.position = new Vector2(rb.position.x, maxFlightHeight);
                }
            }
            else
            {
                enemy.transform.position = newPos;
            }
        }
        
        /// <summary>
        /// 应用追击视觉效果
        /// </summary>
        private void ApplyChaseVisualEffect(EnemyController enemy, bool enable)
        {
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.2f, 0.4f, 1f, 1f)  // 飞行追击状态：深蓝色
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
                Debug.Log($"[{enemy.name}] 应用飞行追击视觉效果");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            return true;
        }
        
        #endregion
        
        #region 调试方法
        
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
            
            // 绘制环绕半径（环绕模式）
            if (chasePattern == ChasePattern.Circling && targetPlayer != null)
            {
                Gizmos.color = Color.cyan;
                DrawCircle(targetPlayer.transform.position, circleRadius, 32);
            }
            
            // 绘制保持距离（保持距离模式）
            if (chasePattern == ChasePattern.KeepDistance && targetPlayer != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(targetPlayer.transform.position, approachDistance);
            }
            
            // 绘制移动方向
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(enemy.transform.position, (Vector3)currentVelocity.normalized * 2f);
        }
        
        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
        
        #endregion
    }
}
