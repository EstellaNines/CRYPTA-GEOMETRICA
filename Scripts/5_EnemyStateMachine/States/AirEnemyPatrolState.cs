using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 飞行敌人巡逻状态
    /// 敌人在空中自由移动，可以在X和Y轴上巡逻
    /// </summary>
    public class AirEnemyPatrolState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("巡逻移动设置")]
        [SerializeField] internal float patrolSpeed = 2.5f; // 巡逻移动速度
        [SerializeField] internal float patrolDuration = 10f; // 巡逻持续时间
        [SerializeField] internal float detectionRange = 7f; // 玩家检测范围
        
        [Header("飞行路径设置")]
        [SerializeField] internal float maxHorizontalPatrolDistance = 6f; // 水平巡逻最大距离
        [SerializeField] internal float maxVerticalPatrolDistance = 4f; // 垂直巡逻最大距离
        [SerializeField] internal float minFlightHeight = 1f; // 最低飞行高度（世界坐标）
        [SerializeField] internal float maxFlightHeight = 15f; // 最高飞行高度（世界坐标）
        [SerializeField] internal bool useAbsoluteHeightLimits = false; // 是否使用绝对高度限制
        
        [Header("智能路径规划")]
        [SerializeField] internal bool enableSmartPatrol = true; // 启用智能巡逻（基于环境检测）
        [SerializeField] internal float boundaryDetectionDistance = 2f; // 边界检测距离
        [SerializeField] internal int pathfindingRays = 8; // 路径探测射线数量
        [SerializeField] internal float minWaypointDistance = 1.5f; // 最小路径点距离
        
        [Header("障碍物检测")]
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级（用于视线和碰撞检测）
        [SerializeField] internal float obstacleDetectionDistance = 1.5f; // 障碍物检测距离
        
        [Header("巡逻模式")]
        [SerializeField] internal PatrolMode patrolMode = PatrolMode.RandomWaypoint; // 巡逻模式
        [SerializeField] internal float waypointReachThreshold = 0.5f; // 到达路径点的阈值
        [SerializeField] internal float pauseDurationMin = 0.5f; // 最小暂停时间
        [SerializeField] internal float pauseDurationMax = 2f; // 最大暂停时间
        
        [Header("视觉效果")]
        [SerializeField] private bool enablePatrolEffect = true; // 启用巡逻视觉效果
        
        #endregion
        
        #region 枚举定义
        
        public enum PatrolMode
        {
            RandomWaypoint,  // 随机路径点模式
            Circular,        // 圆形巡逻
            HorizontalOnly   // 仅水平巡逻
        }
        
        #endregion
        
        #region 私有字段
        
        // 巡逻状态
        private Vector3 startPosition; // 起始位置
        private Vector3 currentWaypoint; // 当前目标路径点
        private bool hasReachedWaypoint = false; // 是否到达路径点
        
        // 移动控制
        private Vector2 moveDirection; // 当前移动方向
        private float pauseTimer = 0f; // 暂停计时器
        private float currentPauseDuration = 1.5f; // 当前暂停持续时间
        private bool isPaused = false; // 是否暂停中
        
        // 玩家检测
        private float lastPlayerScanTime; // 上次玩家扫描时间
        private float playerScanInterval = 0.4f; // 玩家扫描间隔
        private bool playerDetected = false;
        
        // 圆形巡逻参数
        private float circularAngle = 0f; // 圆形巡逻角度
        private float circularRadius = 3f; // 圆形巡逻半径
        private float circularCenterY = 0f; // 圆形巡逻中心Y坐标
        
        // 智能巡逻参数
        private Vector2[] validDirections; // 有效移动方向缓存
        private float lastBoundaryScanTime; // 上次边界扫描时间
        private float boundaryScanInterval = 0.5f; // 边界扫描间隔
        private Bounds movableBounds; // 可移动区域边界
        private bool boundsInitialized = false; // 边界是否已初始化
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Patrol";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 记录起始位置
            startPosition = enemy.transform.position;
            
            // 重置状态
            stateTimer = 0f;
            pauseTimer = 0f;
            isPaused = false;
            hasReachedWaypoint = false;
            
            // 重置玩家检测
            lastPlayerScanTime = 0f;
            playerDetected = false;
            
            // 初始化智能巡逻
            if (enableSmartPatrol)
            {
                InitializeSmartPatrol(enemy);
            }
            
            // 根据巡逻模式初始化
            InitializePatrolMode(enemy);
            
            // 应用视觉效果
            if (enablePatrolEffect && enemy.GetComponent<Animator>() == null)
            {
                ApplyPatrolVisualEffect(enemy, true);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始飞行巡逻 - 模式: {patrolMode}, 智能巡逻: {enableSmartPatrol}, 起始位置: {startPosition}");
            }
        }
        
        /// <summary>
        /// 初始化智能巡逻（检测可移动区域边界）
        /// </summary>
        private void InitializeSmartPatrol(EnemyController enemy)
        {
            validDirections = new Vector2[pathfindingRays];
            boundsInitialized = false;
            
            // 扫描周围环境，确定可移动边界
            ScanMovableBounds(enemy);
        }
        
        /// <summary>
        /// 扫描可移动区域边界
        /// </summary>
        private void ScanMovableBounds(EnemyController enemy)
        {
            Vector3 pos = enemy.transform.position;
            
            // 向8个方向发射射线，检测边界
            float maxDistance = Mathf.Max(maxHorizontalPatrolDistance, maxVerticalPatrolDistance) * 2f;
            
            float minX = pos.x, maxX = pos.x;
            float minY = pos.y, maxY = pos.y;
            
            // 检测左边界
            RaycastHit2D hitLeft = Physics2D.Raycast(pos, Vector2.left, maxDistance, obstacleLayer);
            minX = hitLeft.collider != null ? hitLeft.point.x + boundaryDetectionDistance : pos.x - maxHorizontalPatrolDistance;
            
            // 检测右边界
            RaycastHit2D hitRight = Physics2D.Raycast(pos, Vector2.right, maxDistance, obstacleLayer);
            maxX = hitRight.collider != null ? hitRight.point.x - boundaryDetectionDistance : pos.x + maxHorizontalPatrolDistance;
            
            // 检测下边界
            RaycastHit2D hitDown = Physics2D.Raycast(pos, Vector2.down, maxDistance, obstacleLayer);
            minY = hitDown.collider != null ? hitDown.point.y + boundaryDetectionDistance : pos.y - maxVerticalPatrolDistance;
            
            // 检测上边界
            RaycastHit2D hitUp = Physics2D.Raycast(pos, Vector2.up, maxDistance, obstacleLayer);
            maxY = hitUp.collider != null ? hitUp.point.y - boundaryDetectionDistance : pos.y + maxVerticalPatrolDistance;
            
            // 创建可移动边界
            movableBounds = new Bounds(
                new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f),
                new Vector3(maxX - minX, maxY - minY, 1f)
            );
            
            boundsInitialized = true;
            lastBoundaryScanTime = Time.time;
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 扫描可移动边界: X[{minX:F1}, {maxX:F1}], Y[{minY:F1}, {maxY:F1}]");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            // 定期扫描玩家
            if (Time.time - lastPlayerScanTime >= playerScanInterval)
            {
                ScanForPlayer(enemy);
                lastPlayerScanTime = Time.time;
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 执行巡逻逻辑
            PerformAirPatrol(enemy);
        }
        
        protected override void CheckTransitionConditions(EnemyController enemy)
        {
            // 检测到玩家 -> 转换到追击状态
            if (playerDetected)
            {
                if (enemy.StateMachine.HasState("Chase"))
                {
                    enemy.StateMachine.TransitionTo("Chase");
                }
                return;
            }
            
            // 巡逻时间结束 -> 转换到待机状态
            if (IsStateTimeout(patrolDuration))
            {
                if (enemy.StateMachine.HasState("Idle"))
                {
                    enemy.StateMachine.TransitionTo("Idle");
                }
                else
                {
                    // 如果没有待机状态，重置巡逻
                    stateTimer = 0f;
                    InitializePatrolMode(enemy);
                }
            }
        }
        
        protected override void CleanupState(EnemyController enemy)
        {
            playerDetected = false;
            isPaused = false;
            
            // 清除视觉效果
            if (enablePatrolEffect && enemy.GetComponent<Animator>() == null)
            {
                ApplyPatrolVisualEffect(enemy, false);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束飞行巡逻");
            }
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 初始化巡逻模式
        /// </summary>
        private void InitializePatrolMode(EnemyController enemy)
        {
            switch (patrolMode)
            {
                case PatrolMode.RandomWaypoint:
                    GenerateRandomWaypoint(enemy);
                    break;
                    
                case PatrolMode.Circular:
                    circularAngle = Random.Range(0f, Mathf.PI * 2f);
                    circularRadius = Random.Range(2f, maxHorizontalPatrolDistance);
                    circularCenterY = startPosition.y; // 圆心高度可以变化
                    break;
                    
                case PatrolMode.HorizontalOnly:
                    GenerateHorizontalWaypoint(enemy);
                    break;
            }
        }
        
        /// <summary>
        /// 执行飞行巡逻
        /// </summary>
        private void PerformAirPatrol(EnemyController enemy)
        {
            // 处理暂停状态
            if (isPaused)
            {
                pauseTimer += Time.fixedDeltaTime;
                if (pauseTimer >= currentPauseDuration)
                {
                    isPaused = false;
                    pauseTimer = 0f;
                    
                    // 暂停结束，生成新路径点
                    if (patrolMode == PatrolMode.RandomWaypoint)
                    {
                        GenerateRandomWaypoint(enemy);
                    }
                    else if (patrolMode == PatrolMode.HorizontalOnly)
                    {
                        GenerateHorizontalWaypoint(enemy);
                    }
                }
                
                // 暂停时保持悬浮（轻微上下浮动）
                ApplyHoverEffect(enemy);
                return;
            }
            
            // 根据巡逻模式执行移动
            switch (patrolMode)
            {
                case PatrolMode.RandomWaypoint:
                case PatrolMode.HorizontalOnly:
                    MoveTowardsWaypoint(enemy);
                    break;
                    
                case PatrolMode.Circular:
                    PerformCircularPatrol(enemy);
                    break;
            }
        }
        
        /// <summary>
        /// 生成随机路径点（支持智能巡逻）
        /// </summary>
        private void GenerateRandomWaypoint(EnemyController enemy)
        {
            Vector3 currentPos = enemy.transform.position;
            Vector3 targetWaypoint;
            
            if (enableSmartPatrol && boundsInitialized)
            {
                // 智能巡逻：在检测到的可移动边界内生成路径点
                targetWaypoint = GenerateSmartWaypoint(enemy, currentPos);
            }
            else
            {
                // 传统方式：基于固定参数生成
                targetWaypoint = GenerateBasicWaypoint(enemy, currentPos);
            }
            
            currentWaypoint = targetWaypoint;
            hasReachedWaypoint = false;
            
            // 随机暂停时间
            currentPauseDuration = Random.Range(pauseDurationMin, pauseDurationMax);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 生成路径点: {currentWaypoint}, 距离: {Vector3.Distance(currentPos, currentWaypoint):F2}");
            }
        }
        
        /// <summary>
        /// 智能生成路径点（基于环境检测）
        /// </summary>
        private Vector3 GenerateSmartWaypoint(EnemyController enemy, Vector3 currentPos)
        {
            // 定期重新扫描边界
            if (Time.time - lastBoundaryScanTime > boundaryScanInterval * 5f)
            {
                ScanMovableBounds(enemy);
            }
            
            // 在可移动边界内生成随机点
            float targetX = Random.Range(movableBounds.min.x, movableBounds.max.x);
            float targetY = Random.Range(movableBounds.min.y, movableBounds.max.y);
            
            Vector3 targetWaypoint = new Vector3(targetX, targetY, currentPos.z);
            
            // 确保路径点与当前位置有足够距离
            float distance = Vector3.Distance(currentPos, targetWaypoint);
            if (distance < minWaypointDistance)
            {
                // 如果太近，尝试找一个更远的点
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                targetWaypoint = currentPos + new Vector3(randomDir.x, randomDir.y, 0f) * minWaypointDistance * 2f;
                
                // 限制在边界内
                targetWaypoint.x = Mathf.Clamp(targetWaypoint.x, movableBounds.min.x, movableBounds.max.x);
                targetWaypoint.y = Mathf.Clamp(targetWaypoint.y, movableBounds.min.y, movableBounds.max.y);
            }
            
            // 验证路径是否可达（射线检测）
            Vector2 direction = (targetWaypoint - currentPos).normalized;
            float checkDistance = Vector3.Distance(currentPos, targetWaypoint);
            RaycastHit2D hit = Physics2D.Raycast(currentPos, direction, checkDistance, obstacleLayer);
            
            if (hit.collider != null)
            {
                // 路径被阻挡，使用碰撞点前方作为目标
                targetWaypoint = hit.point - direction * boundaryDetectionDistance;
            }
            
            return targetWaypoint;
        }
        
        /// <summary>
        /// 基础路径点生成（传统方式）
        /// </summary>
        private Vector3 GenerateBasicWaypoint(EnemyController enemy, Vector3 currentPos)
        {
            // 在当前位置周围生成随机路径点
            float randomX = Random.Range(-maxHorizontalPatrolDistance, maxHorizontalPatrolDistance);
            float randomY = Random.Range(-maxVerticalPatrolDistance, maxVerticalPatrolDistance);
            
            Vector3 targetWaypoint = currentPos + new Vector3(randomX, randomY, 0f);
            
            // 应用高度限制
            if (useAbsoluteHeightLimits)
            {
                targetWaypoint.y = Mathf.Clamp(targetWaypoint.y, minFlightHeight, maxFlightHeight);
            }
            else
            {
                float minY = startPosition.y - maxVerticalPatrolDistance;
                float maxY = startPosition.y + maxVerticalPatrolDistance;
                targetWaypoint.y = Mathf.Clamp(targetWaypoint.y, minY, maxY);
            }
            
            // 确保不会飞出水平巡逻范围
            float distanceFromStart = Vector2.Distance(
                new Vector2(targetWaypoint.x, targetWaypoint.y),
                new Vector2(startPosition.x, startPosition.y)
            );
            
            if (distanceFromStart > maxHorizontalPatrolDistance * 1.5f)
            {
                Vector3 directionToStart = (startPosition - targetWaypoint).normalized;
                targetWaypoint += directionToStart * (distanceFromStart - maxHorizontalPatrolDistance);
            }
            
            return targetWaypoint;
        }
        
        /// <summary>
        /// 生成水平路径点（保持当前高度）
        /// </summary>
        private void GenerateHorizontalWaypoint(EnemyController enemy)
        {
            Vector3 currentPos = enemy.transform.position;
            Vector3 targetWaypoint;
            
            if (enableSmartPatrol && boundsInitialized)
            {
                // 智能巡逻：在检测到的水平边界内生成
                float targetX = Random.Range(movableBounds.min.x, movableBounds.max.x);
                targetWaypoint = new Vector3(targetX, currentPos.y, currentPos.z);
            }
            else
            {
                // 传统方式
                float randomX = Random.Range(-maxHorizontalPatrolDistance, maxHorizontalPatrolDistance);
                targetWaypoint = currentPos + new Vector3(randomX, 0f, 0f);
                
                float distanceFromStartX = Mathf.Abs(targetWaypoint.x - startPosition.x);
                if (distanceFromStartX > maxHorizontalPatrolDistance)
                {
                    targetWaypoint.x = startPosition.x + Mathf.Sign(targetWaypoint.x - startPosition.x) * maxHorizontalPatrolDistance;
                }
            }
            
            currentWaypoint = targetWaypoint;
            hasReachedWaypoint = false;
            currentPauseDuration = Random.Range(pauseDurationMin, pauseDurationMax);
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 生成水平路径点: {currentWaypoint}");
            }
        }
        
        /// <summary>
        /// 向路径点移动
        /// </summary>
        private void MoveTowardsWaypoint(EnemyController enemy)
        {
            Vector3 currentPos = enemy.transform.position;
            float distanceToWaypoint = Vector3.Distance(currentPos, currentWaypoint);
            
            // 检查是否到达路径点
            if (distanceToWaypoint <= waypointReachThreshold)
            {
                if (!hasReachedWaypoint)
                {
                    hasReachedWaypoint = true;
                    isPaused = true;
                    pauseTimer = 0f;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 到达路径点，开始暂停");
                    }
                }
                return;
            }
            
            // 计算移动方向
            moveDirection = (currentWaypoint - currentPos).normalized;
            
            // 检测前方障碍物
            if (DetectObstacleAhead(enemy, moveDirection))
            {
                // 遇到障碍物，生成新路径点
                if (patrolMode == PatrolMode.RandomWaypoint)
                {
                    GenerateRandomWaypoint(enemy);
                }
                else
                {
                    GenerateHorizontalWaypoint(enemy);
                }
                return;
            }
            
            // 应用移动
            ApplyMovement(enemy, moveDirection);
            
            // 更新朝向
            if (Mathf.Abs(moveDirection.x) > 0.1f)
            {
                enemy.SetFacingDirection(moveDirection.x > 0);
            }
        }
        
        /// <summary>
        /// 执行圆形巡逻（支持3D螺旋运动）
        /// </summary>
        private void PerformCircularPatrol(EnemyController enemy)
        {
            // 更新角度
            circularAngle += patrolSpeed * Time.fixedDeltaTime / circularRadius;
            
            // 计算圆形路径上的位置（水平面）
            float x = startPosition.x + Mathf.Cos(circularAngle) * circularRadius;
            
            // 添加垂直方向的正弦波动，形成螺旋效果
            float verticalOscillation = Mathf.Sin(circularAngle * 0.5f) * maxVerticalPatrolDistance * 0.5f;
            float y = circularCenterY + verticalOscillation;
            
            // 应用高度限制
            if (useAbsoluteHeightLimits)
            {
                y = Mathf.Clamp(y, minFlightHeight, maxFlightHeight);
            }
            else
            {
                float minY = startPosition.y - maxVerticalPatrolDistance;
                float maxY = startPosition.y + maxVerticalPatrolDistance;
                y = Mathf.Clamp(y, minY, maxY);
            }
            
            Vector3 targetPosition = new Vector3(x, y, enemy.transform.position.z);
            
            // 计算移动方向
            moveDirection = (targetPosition - enemy.transform.position).normalized;
            
            // 检测障碍物
            if (!DetectObstacleAhead(enemy, moveDirection))
            {
                // 平滑移动到目标位置
                enemy.transform.position = Vector3.MoveTowards(
                    enemy.transform.position,
                    targetPosition,
                    patrolSpeed * Time.fixedDeltaTime
                );
                
                // 更新朝向
                if (Mathf.Abs(moveDirection.x) > 0.1f)
                {
                    enemy.SetFacingDirection(moveDirection.x > 0);
                }
            }
            else
            {
                // 遇到障碍物，反向旋转并调整高度
                circularAngle += Mathf.PI;
                circularCenterY = enemy.transform.position.y;
            }
        }
        
        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement(EnemyController enemy, Vector2 direction)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            
            if (rb != null)
            {
                // 使用物理移动
                Vector2 targetVelocity = direction * patrolSpeed;
                rb.velocity = targetVelocity;
            }
            else
            {
                // 直接移动
                Vector3 movement = direction * patrolSpeed * Time.fixedDeltaTime;
                enemy.transform.position += movement;
            }
        }
        
        /// <summary>
        /// 应用悬浮效果（暂停时的轻微上下浮动）
        /// </summary>
        private void ApplyHoverEffect(EnemyController enemy)
        {
            // 使用正弦波创建轻微的上下浮动
            float hoverOffset = Mathf.Sin(Time.time * 2f) * 0.1f;
            
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(0f, hoverOffset);
            }
        }
        
        /// <summary>
        /// 检测前方障碍物
        /// </summary>
        private bool DetectObstacleAhead(EnemyController enemy, Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                enemy.transform.position,
                direction,
                obstacleDetectionDistance,
                obstacleLayer
            );
            
            if (hit.collider != null)
            {
                if (debugMode)
                {
                    Debug.Log($"[{enemy.name}] 检测到前方障碍物: {hit.collider.name}");
                }
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 扫描玩家
        /// </summary>
        private void ScanForPlayer(EnemyController enemy)
        {
            playerDetected = enemy.DetectPlayer(detectionRange);
            
            if (playerDetected)
            {
                GameObject player = enemy.GetPlayerTarget();
                if (player != null)
                {
                    // 检查视线是否被阻挡
                    Vector3 directionToPlayer = player.transform.position - enemy.transform.position;
                    float distanceToPlayer = directionToPlayer.magnitude;
                    
                    // 只有当obstacleLayer不是-1（所有层级）时才进行视线遮挡检测
                    // 否则直接认为有视线
                    if (obstacleLayer.value != -1 && obstacleLayer.value != 0)
                    {
                        RaycastHit2D hit = Physics2D.Raycast(
                            enemy.transform.position,
                            directionToPlayer.normalized,
                            distanceToPlayer,
                            obstacleLayer
                        );
                        
                        // 如果射线击中了障碍物（且不是玩家），说明视线被阻挡
                        if (hit.collider != null && hit.collider.gameObject != player)
                        {
                            playerDetected = false;
                            return;
                        }
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 飞行巡逻中检测到玩家！距离: {distanceToPlayer:F2}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 应用巡逻视觉效果（支持多层级结构）
        /// </summary>
        private void ApplyPatrolVisualEffect(EnemyController enemy, bool enable)
        {
            // 获取所有渲染器（支持PSD导入的多层级结构）
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.5f, 0.8f, 1f, 0.8f)  // 飞行巡逻状态：浅蓝色
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
                Debug.Log($"[{enemy.name}] 应用飞行巡逻视觉效果到 {renderers.Length} 个渲染器");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 飞行巡逻状态可以转换到任何状态
            return true;
        }
        
        #endregion
        
        #region 调试方法
        
        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        public void DrawDebugGizmos(EnemyController enemy)
        {
            if (!debugMode) return;
            
            // 绘制检测范围
            Gizmos.color = playerDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position, detectionRange);
            
            // 绘制可移动边界（智能巡逻）
            if (enableSmartPatrol && boundsInitialized)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireCube(movableBounds.center, movableBounds.size);
                
                // 绘制边界检测射线
                Gizmos.color = Color.green;
                Vector3 pos = enemy.transform.position;
                float maxDist = Mathf.Max(maxHorizontalPatrolDistance, maxVerticalPatrolDistance) * 2f;
                Gizmos.DrawRay(pos, Vector3.left * maxDist);
                Gizmos.DrawRay(pos, Vector3.right * maxDist);
                Gizmos.DrawRay(pos, Vector3.up * maxDist);
                Gizmos.DrawRay(pos, Vector3.down * maxDist);
            }
            else
            {
                // 绘制传统巡逻范围
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(startPosition, maxHorizontalPatrolDistance);
                
                // 绘制垂直巡逻范围
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Vector3 patrolAreaSize = new Vector3(
                    maxHorizontalPatrolDistance * 2f,
                    maxVerticalPatrolDistance * 2f,
                    0.1f
                );
                Gizmos.DrawWireCube(startPosition, patrolAreaSize);
            }
            
            // 绘制当前路径点
            if (!isPaused && (patrolMode == PatrolMode.RandomWaypoint || patrolMode == PatrolMode.HorizontalOnly))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentWaypoint, waypointReachThreshold);
                Gizmos.DrawLine(enemy.transform.position, currentWaypoint);
            }
            
            // 绘制圆形巡逻路径
            if (patrolMode == PatrolMode.Circular)
            {
                Gizmos.color = Color.magenta;
                DrawCircle(new Vector3(startPosition.x, circularCenterY, startPosition.z), circularRadius, 32);
            }
            
            // 绘制移动方向
            if (!isPaused)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(enemy.transform.position, (Vector3)moveDirection * 1.5f);
            }
            
            // 绘制到玩家的连线
            GameObject player = enemy.GetPlayerTarget();
            if (player != null && playerDetected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(enemy.transform.position, player.transform.position);
            }
        }
        
        /// <summary>
        /// 绘制圆形
        /// </summary>
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
