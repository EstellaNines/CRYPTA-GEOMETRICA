using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 飞行敌人待机状态
    /// 敌人在空中悬浮，保持轻微的上下浮动效果，定期扫描玩家
    /// </summary>
    public class AirEnemyIdleState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("待机状态设置")]
        [SerializeField] internal float idleTimeout = 5f; // 待机超时时间
        [SerializeField] internal float detectionRange = 7f; // 玩家检测范围
        [SerializeField] private float scanInterval = 0.5f; // 扫描间隔
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级
        
        [Header("悬浮效果")]
        [SerializeField] private bool enableHoverEffect = true; // 启用悬浮效果
        [SerializeField] private float hoverAmplitude = 0.3f; // 悬浮幅度
        [SerializeField] private float hoverSpeed = 1.5f; // 悬浮速度
        [SerializeField] private float hoverDriftAmplitude = 0.15f; // 水平漂移幅度
        [SerializeField] private float hoverDriftSpeed = 0.8f; // 水平漂移速度
        
        [Header("高度控制")]
        [SerializeField] internal float minFlightHeight = 2f; // 最低飞行高度
        [SerializeField] internal float maxFlightHeight = 8f; // 最高飞行高度
        
        [Header("视觉效果")]
        [SerializeField] private bool enableIdleEffect = true; // 启用待机视觉效果
        
        #endregion
        
        #region 私有字段
        
        private float lastScanTime;
        private Vector3 originalPosition;
        private bool playerDetected = false;
        
        // 悬浮效果参数
        private float hoverPhase; // 悬浮相位
        private float driftPhase; // 漂移相位
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Idle";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 记录初始位置
            originalPosition = enemy.transform.position;
            
            // 确保初始位置在有效高度范围内
            if (originalPosition.y < minFlightHeight)
            {
                originalPosition.y = minFlightHeight;
                enemy.transform.position = originalPosition;
            }
            else if (originalPosition.y > maxFlightHeight)
            {
                originalPosition.y = maxFlightHeight;
                enemy.transform.position = originalPosition;
            }
            
            // 重置状态
            lastScanTime = 0f;
            playerDetected = false;
            
            // 随机初始化悬浮相位，避免所有敌人同步
            hoverPhase = Random.Range(0f, Mathf.PI * 2f);
            driftPhase = Random.Range(0f, Mathf.PI * 2f);
            
            // 停止物理速度
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                // 飞行敌人不受重力影响
                rb.gravityScale = 0f;
            }
            
            // 应用视觉效果
            if (enableIdleEffect && enemy.GetComponent<Animator>() == null)
            {
                ApplyIdleVisualEffect(enemy, true);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 进入飞行待机状态 - 检测范围: {detectionRange}m, 悬浮高度: {originalPosition.y:F2}m");
            }
        }
        
        protected override void UpdateState(EnemyController enemy)
        {
            // 定期扫描玩家
            if (Time.time - lastScanTime >= scanInterval)
            {
                ScanForPlayer(enemy);
                lastScanTime = Time.time;
            }
            
            // 应用悬浮效果
            if (enableHoverEffect)
            {
                ApplyHoverEffect(enemy);
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            // 确保飞行敌人不受重力影响
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null && rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
            }
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
                else if (enemy.StateMachine.HasState("Patrol"))
                {
                    // 如果没有追击状态，转换到巡逻状态
                    enemy.StateMachine.TransitionTo("Patrol");
                }
                return;
            }
            
            // 待机超时 -> 转换到巡逻状态
            if (IsStateTimeout(idleTimeout))
            {
                if (enemy.StateMachine.HasState("Patrol"))
                {
                    enemy.StateMachine.TransitionTo("Patrol");
                }
                else
                {
                    // 如果没有巡逻状态，重置待机时间
                    stateTimer = 0f;
                }
            }
        }
        
        protected override void CleanupState(EnemyController enemy)
        {
            playerDetected = false;
            
            // 清除视觉效果
            if (enableIdleEffect && enemy.GetComponent<Animator>() == null)
            {
                ApplyIdleVisualEffect(enemy, false);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 退出飞行待机状态");
            }
        }
        
        #endregion
        
        #region 私有方法
        
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
                        
                        if (debugMode)
                        {
                            Debug.Log($"[{enemy.name}] 玩家被障碍物遮挡: {hit.collider.name}");
                        }
                    }
                    else if (playerDetected)
                    {
                        // 面向玩家
                        bool shouldFaceRight = player.transform.position.x > enemy.transform.position.x;
                        enemy.SetFacingDirection(shouldFaceRight);
                        
                        if (debugMode)
                        {
                            Debug.Log($"[{enemy.name}] 飞行待机中检测到玩家！距离: {Vector3.Distance(enemy.transform.position, player.transform.position):F2}m");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 应用悬浮效果
        /// </summary>
        private void ApplyHoverEffect(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 更新相位
            hoverPhase += Time.deltaTime * hoverSpeed;
            driftPhase += Time.deltaTime * hoverDriftSpeed;
            
            // 计算垂直悬浮偏移（正弦波）
            float verticalOffset = Mathf.Sin(hoverPhase) * hoverAmplitude;
            
            // 计算水平漂移偏移（余弦波，频率不同以产生更自然的效果）
            float horizontalOffset = Mathf.Cos(driftPhase) * hoverDriftAmplitude;
            
            // 计算目标位置
            Vector3 targetPosition = originalPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
            
            // 限制高度范围
            targetPosition.y = Mathf.Clamp(targetPosition.y, minFlightHeight, maxFlightHeight);
            
            // 平滑移动到目标位置
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 使用物理系统平滑移动
                Vector2 desiredVelocity = (targetPosition - enemy.transform.position) * 2f;
                rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, Time.deltaTime * 5f);
            }
            else
            {
                // 直接插值移动
                enemy.transform.position = Vector3.Lerp(
                    enemy.transform.position,
                    targetPosition,
                    Time.deltaTime * 3f
                );
            }
        }
        
        /// <summary>
        /// 应用待机视觉效果（支持多层级结构）
        /// </summary>
        private void ApplyIdleVisualEffect(EnemyController enemy, bool enable)
        {
            // 获取所有渲染器（支持PSD导入的多层级结构）
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            
            Color targetColor = enable 
                ? new Color(0.8f, 0.6f, 1f, 0.8f)  // 飞行待机状态：淡紫色
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
                Debug.Log($"[{enemy.name}] 应用飞行待机视觉效果到 {renderers.Length} 个渲染器");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 飞行待机状态可以转换到任何状态
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
            
            // 绘制到玩家的射线
            GameObject player = enemy.GetPlayerTarget();
            if (player != null && playerDetected)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(enemy.transform.position, player.transform.position);
            }
            
            // 绘制原始悬浮位置
            if (enableHoverEffect)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.3f);
                
                // 绘制悬浮范围
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Vector3 hoverRangeSize = new Vector3(hoverDriftAmplitude * 2f, hoverAmplitude * 2f, 0.1f);
                Gizmos.DrawWireCube(originalPosition, hoverRangeSize);
            }
            
            // 绘制高度限制线
            Gizmos.color = Color.red;
            Vector3 minHeightPos = new Vector3(enemy.transform.position.x, minFlightHeight, 0f);
            Vector3 maxHeightPos = new Vector3(enemy.transform.position.x, maxFlightHeight, 0f);
            Gizmos.DrawLine(minHeightPos + Vector3.left * 0.5f, minHeightPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(maxHeightPos + Vector3.left * 0.5f, maxHeightPos + Vector3.right * 0.5f);
        }
        
        #endregion
    }
}
