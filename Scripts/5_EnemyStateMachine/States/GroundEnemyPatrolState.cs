using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 地面敌人巡逻状态
    /// 敌人在地面上左右巡逻，检测边缘和墙壁进行转向
    /// </summary>
    public class GroundEnemyPatrolState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("巡逻移动设置")]
        [SerializeField] internal float patrolSpeed = 2f; // 巡逻移动速度
        [SerializeField] internal float patrolDuration = 8f; // 巡逻持续时间
        [SerializeField] internal float detectionRange = 6f; // 玩家检测范围
        
        [Header("地面检测设置")]
        [SerializeField] internal LayerMask groundLayer = 1; // 地面层级
        [SerializeField] internal LayerMask wallLayer = 1; // 墙壁层级
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级（用于视线检测）
        
        [Header("巡逻路径设置")]
        [SerializeField] internal float maxPatrolDistance = 5f; // 最大巡逻距离
        
        [Header("视觉效果")]
        [SerializeField] private string patrolAnimationName = "Walk";
        [SerializeField] private bool enablePatrolEffect = true; // 启用巡逻视觉效果
        
        #endregion
        
        #region 私有字段
        
        // 简化的巡逻状态
        private bool isMoving = true; // 是否正在移动
        private int patrolDirection = 1; // 巡逻方向：1为右，-1为左
        private float moveDuration = 3f; // 移动持续时间
        private float pauseDuration = 2f; // 暂停持续时间
        
        // 玩家检测
        private float lastPlayerScanTime; // 上次玩家扫描时间
        private float playerScanInterval = 0.3f; // 玩家扫描间隔
        private bool playerDetected = false;
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Patrol";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 随机初始方向
            patrolDirection = Random.Range(0, 2) == 0 ? -1 : 1;
            enemy.SetFacingDirection(patrolDirection > 0);
            
            // 初始化巡逻状态
            isMoving = true;
            stateTimer = 0f;
            
            // 重置玩家检测
            lastPlayerScanTime = 0f;
            playerDetected = false;
            
            // 播放巡逻动画
            if (enemy.GetComponent<Animator>() != null)
            {
                enemy.PlayAnimation(patrolAnimationName);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 开始简化巡逻 - 初始方向: {(patrolDirection > 0 ? "右" : "左")}, 移动时长: {moveDuration}秒, 暂停时长: {pauseDuration}秒");
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
            
            // 执行简化的巡逻逻辑
            PerformSimplePatrol(enemy);
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
                    // 如果没有待机状态，重置巡逻时间继续巡逻
                    stateTimer = 0f;
                    isMoving = true;
                }
            }
        }
        
        protected override void CleanupState(EnemyController enemy)
        {
            playerDetected = false;
            
            // 清除巡逻视觉效果
            if (enemy.GetComponent<Animator>() == null && enablePatrolEffect)
            {
                ApplyPatrolVisualEffect(enemy, false);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 结束巡逻");
            }
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 执行简化的巡逻逻辑
        /// </summary>
        private void PerformSimplePatrol(EnemyController enemy)
        {
            // 更新状态计时器
            stateTimer += Time.fixedDeltaTime;
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 巡逻状态 - 移动中: {isMoving}, 计时器: {stateTimer:F2}, 方向: {(patrolDirection > 0 ? "右" : "左")}");
            }
            
            if (isMoving)
            {
                // 移动阶段
                if (stateTimer >= moveDuration)
                {
                    // 移动时间结束，进入暂停阶段
                    isMoving = false;
                    stateTimer = 0f;
                    
                    // 停止移动
                    Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = new Vector2(0, rb.velocity.y);
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 移动结束，进入暂停阶段");
                    }
                }
                else
                {
                    // 继续移动
                    ApplyMovement(enemy);
                }
            }
            else
            {
                // 暂停阶段
                if (stateTimer >= pauseDuration)
                {
                    // 暂停时间结束，切换方向并开始移动
                    patrolDirection *= -1;
                    enemy.SetFacingDirection(patrolDirection > 0);
                    isMoving = true;
                    stateTimer = 0f;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{enemy.name}] 暂停结束，转向并开始移动 - 新方向: {(patrolDirection > 0 ? "右" : "左")}");
                    }
                }
                else
                {
                    // 保持静止
                    Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = new Vector2(0, rb.velocity.y);
                    }
                }
            }
        }
        
        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement(EnemyController enemy)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 使用物理移动，保持垂直速度
                Vector2 targetVelocity = new Vector2(patrolSpeed * patrolDirection, rb.velocity.y);
                rb.velocity = targetVelocity;
                
                if (debugMode && Time.fixedTime % 1f < 0.02f) // 每秒打印一次
                {
                    Debug.Log($"[{enemy.name}] 应用移动 - 速度: {targetVelocity.x:F2}, 方向: {patrolDirection}");
                }
            }
            else
            {
                // 直接移动
                Vector3 movement = Vector3.right * patrolDirection * patrolSpeed * Time.fixedDeltaTime;
                enemy.transform.position += movement;
                
                if (debugMode && Time.fixedTime % 1f < 0.02f) // 每秒打印一次
                {
                    Debug.Log($"[{enemy.name}] 直接移动 - 移动量: {movement.x:F3}, 方向: {patrolDirection}");
                }
            }
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
                    RaycastHit2D hit = Physics2D.Raycast(
                        enemy.transform.position,
                        directionToPlayer.normalized,
                        detectionRange,
                        obstacleLayer
                    );
                    
                    // 如果射线击中的不是玩家，说明视线被阻挡
                    if (hit.collider != null && hit.collider.gameObject != player)
                    {
                        playerDetected = false;
                    }
                    else if (playerDetected)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[{enemy.name}] 巡逻中检测到玩家！");
                        }
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
                ? Color.green * 0.8f  // 巡逻状态：绿色
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
                Debug.Log($"[{enemy.name}] 应用巡逻视觉效果到 {renderers.Length} 个渲染器");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 巡逻状态可以转换到任何状态
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
            
            // 绘制移动方向箭头
            Gizmos.color = Color.cyan;
            Vector3 arrowStart = enemy.transform.position + Vector3.up * 1f;
            Vector3 arrowEnd = arrowStart + Vector3.right * patrolDirection * 1f;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            // 绘制到玩家的连线
            GameObject player = enemy.GetPlayerTarget();
            if (player != null && playerDetected)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(enemy.transform.position, player.transform.position);
            }
        }
        
        #endregion
    }
}
