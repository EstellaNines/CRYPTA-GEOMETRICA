using UnityEngine;

namespace CryptaGeometrica.EnemyStateMachine.States
{
    /// <summary>
    /// 地面敌人待机状态
    /// 敌人在原地待机，定期扫描周围环境，检测玩家
    /// </summary>
    public class GroundEnemyIdleState : EnemyStateBase
    {
        #region 状态配置
        
        [Header("待机状态设置")]
        [SerializeField] internal float idleTimeout = 4f; // 待机超时时间
        [SerializeField] internal float detectionRange = 6f; // 玩家检测范围
        [SerializeField] private float scanInterval = 0.5f; // 扫描间隔
        [SerializeField] internal LayerMask obstacleLayer = -1; // 障碍物层级
        
        [Header("待机动画")]
        [SerializeField] private string idleAnimationName = "Idle";
        [SerializeField] private bool enableIdleMovement = true; // 是否启用待机时的轻微移动
        [SerializeField] private float idleMovementAmplitude = 0.1f; // 待机移动幅度
        [SerializeField] private float idleMovementSpeed = 1f; // 待机移动速度
        
        #endregion
        
        #region 私有字段
        
        private float lastScanTime;
        private Vector3 originalPosition;
        private bool playerDetected = false;
        
        #endregion
        
        #region 状态属性
        
        public override string StateName => "Idle";
        
        #endregion
        
        #region 生命周期方法
        
        protected override void InitializeState(EnemyController enemy)
        {
            // 记录初始位置
            originalPosition = enemy.transform.position;
            lastScanTime = 0f;
            playerDetected = false;
            
            // 尝试播放待机动画（如果有动画器的话）
            if (enemy.GetComponent<Animator>() != null)
            {
                enemy.PlayAnimation(idleAnimationName);
            }
            else
            {
                // 对于胶囊体敌人，使用颜色变化表示待机状态
                ApplyIdleVisualEffect(enemy, true);
            }
            
            // 停止移动
            if (enemy.GetComponent<Rigidbody2D>() != null)
            {
                enemy.GetComponent<Rigidbody2D>().velocity = new Vector2(0, enemy.GetComponent<Rigidbody2D>().velocity.y);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 进入待机状态 - 检测范围: {detectionRange}m");
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
            
            // 待机时的轻微移动效果
            if (enableIdleMovement)
            {
                ApplyIdleMovement(enemy);
            }
        }
        
        protected override void FixedUpdateState(EnemyController enemy)
        {
            // 地面敌人在待机状态下确保受重力影响
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 保持垂直速度，清除水平速度（除非有待机移动）
                if (!enableIdleMovement)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }
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
            
            // 清除待机视觉效果
            if (enemy.GetComponent<Animator>() == null)
            {
                ApplyIdleVisualEffect(enemy, false);
            }
            
            if (debugMode)
            {
                Debug.Log($"[{enemy.name}] 退出待机状态");
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
                            Debug.Log($"[{enemy.name}] 检测到玩家！距离: {Vector3.Distance(enemy.transform.position, player.transform.position):F2}m");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 应用待机时的轻微移动
        /// </summary>
        private void ApplyIdleMovement(EnemyController enemy)
        {
            if (!enemy.CanAct) return;
            
            // 使用正弦波创建轻微的左右摆动
            float movement = Mathf.Sin(Time.time * idleMovementSpeed) * idleMovementAmplitude;
            Vector3 targetPosition = originalPosition + Vector3.right * movement;
            
            // 平滑移动到目标位置
            enemy.transform.position = Vector3.Lerp(
                enemy.transform.position,
                new Vector3(targetPosition.x, enemy.transform.position.y, enemy.transform.position.z),
                Time.deltaTime * 2f
            );
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
                ? Color.cyan * 0.8f  // 待机状态：较暗的青色
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
                Debug.Log($"[{enemy.name}] 应用待机视觉效果到 {renderers.Length} 个渲染器");
            }
        }
        
        #endregion
        
        #region 状态转换控制
        
        public override bool CanTransitionTo(string targetState, EnemyController enemy)
        {
            // 待机状态可以转换到任何状态
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
            
            // 绘制原始位置
            if (enableIdleMovement)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.2f);
            }
        }
        
        #endregion
    }
}
