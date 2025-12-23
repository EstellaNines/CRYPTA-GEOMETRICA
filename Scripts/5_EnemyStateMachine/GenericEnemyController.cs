using UnityEngine;
using CryptaGeometrica.EnemyStateMachine.States;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.EnemyStateMachine
{
    /// <summary>
    /// 通用敌人状态机控制器
    /// 可直接挂载到敌人预制体上，支持所有已实现的状态
    /// 支持PSD导入的多层级结构（父对象为空GameObject，子对象包含SpriteRenderer）
    /// 设计为可扩展架构，添加新状态时无需修改此脚本
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class GenericEnemyController : EnemyController
    {
        #region 敌人基础设置
        
        [FoldoutGroup("基本信息")]
        [LabelText("敌人名称")]
        [SerializeField] private string enemyName = "GenericEnemy";
        
        [FoldoutGroup("基本信息")]
        [LabelText("敌人类型")]
        [SerializeField] private EnemyType enemyType = EnemyType.GroundEnemy;
        
        [FoldoutGroup("状态配置")]
        [LabelText("启用的状态")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] public List<StateConfig> enabledStates = new List<StateConfig>();
        
        [FoldoutGroup("状态配置")]
        [LabelText("初始状态")]
        [ValueDropdown("GetAvailableStates")]
        [SerializeField] private string initialState = "Idle";
        
        [FoldoutGroup("检测设置")]
        [LabelText("玩家检测范围 (米)")]
        [Range(1f, 20f)]
        [OnValueChanged("OnDetectionRangeChanged")]
        [InfoBox("敌人能够检测到玩家的最大距离")]
        [SerializeField] private float playerDetectionRange = 6f;
        
        [FoldoutGroup("检测设置")]
        [LabelText("地面层级")]
        [InfoBox("用于地面检测的Unity层级")]
        [SerializeField] private LayerMask groundLayer = 1;
        
        [FoldoutGroup("检测设置")]
        [LabelText("墙壁层级")]
        [InfoBox("用于墙壁碰撞检测的Unity层级")]
        [SerializeField] private LayerMask wallLayer = 1;
        
        [FoldoutGroup("检测设置")]
        [LabelText("障碍物层级")]
        [InfoBox("用于视线遮挡检测的Unity层级")]
        [SerializeField] private LayerMask obstacleLayer = -1;
        
        [FoldoutGroup("Scene调试显示")]
        [LabelText("🎯 显示检测范围")]
        [InfoBox("在Scene视图中显示敌人的检测范围圆圈")]
        [SerializeField] private bool showDetectionRange = true;
        
        [FoldoutGroup("Scene调试显示")]
        [LabelText("🛤️ 显示巡逻路径")]
        [InfoBox("在Scene视图中显示敌人的巡逻路径线")]
        [SerializeField] private bool showPatrolPath = true;
        
        [FoldoutGroup("Scene调试显示")]
        [LabelText("📍 显示状态信息")]
        [InfoBox("在Scene视图中显示敌人当前状态和面向")]
        [SerializeField] private bool showStateInfo = true;
        
        [FoldoutGroup("Scene调试显示")]
        [LabelText("🎨 调试显示颜色")]
        [InfoBox("Scene视图中调试图形的颜色")]
        [SerializeField] private Color gizmosColor = Color.yellow;
        
        [FoldoutGroup("移动设置")]
        [LabelText("巡逻速度 (m/s)")]
        [Range(0.5f, 10f)]
        [InfoBox("敌人巡逻时的移动速度")]
        [SerializeField] private float patrolSpeed = 2f;
        
        [FoldoutGroup("移动设置")]
        [LabelText("最大巡逻距离 (m)")]
        [Range(1f, 20f)]
        [InfoBox("敌人巡逻的最大距离范围")]
        [SerializeField] private float maxPatrolDistance = 5f;
        
        [FoldoutGroup("移动设置")]
        [LabelText("追击速度 (m/s)")]
        [Range(1f, 15f)]
        [InfoBox("敌人追击玩家时的移动速度")]
        [SerializeField] private float chaseSpeed = 4f;
        
        [FoldoutGroup("移动设置")]
        [LabelText("攻击范围 (m)")]
        [Range(0.5f, 10f)]
        [InfoBox("敌人可以攻击玩家的距离")]
        [SerializeField] private float attackRange = 2f;
        
        [FoldoutGroup("时间设置")]
        [LabelText("待机超时时间 (秒)")]
        [Range(1f, 20f)]
        [InfoBox("敌人在待机状态的最长持续时间")]
        [SerializeField] private float idleTimeout = 4f;
        
        [FoldoutGroup("时间设置")]
        [LabelText("巡逻持续时间 (秒)")]
        [Range(2f, 30f)]
        [InfoBox("敌人单次巡逻的持续时间")]
        [SerializeField] private float patrolDuration = 8f;
        
        [FoldoutGroup("时间设置")]
        [LabelText("攻击冷却时间 (秒)")]
        [Range(0.5f, 10f)]
        [InfoBox("敌人攻击后的冷却等待时间")]
        [SerializeField] private float attackCooldown = 2f;
        
        [FoldoutGroup("视觉效果")]
        [LabelText("✨ 启用视觉效果")]
        [InfoBox("启用后敌人会根据当前状态改变颜色")]
        [SerializeField] private bool enableVisualEffects = true;
        
        [FoldoutGroup("视觉效果")]
        [LabelText("🔵 待机状态颜色")]
        [ShowIf("enableVisualEffects")]
        [InfoBox("敌人处于待机状态时的显示颜色")]
        [SerializeField] private Color idleColor = Color.cyan;
        
        [FoldoutGroup("视觉效果")]
        [LabelText("🟢 巡逻状态颜色")]
        [ShowIf("enableVisualEffects")]
        [InfoBox("敌人处于巡逻状态时的显示颜色")]
        [SerializeField] private Color patrolColor = Color.green;
        
        [FoldoutGroup("视觉效果")]
        [LabelText("🔴 追击状态颜色")]
        [ShowIf("enableVisualEffects")]
        [InfoBox("敌人处于追击状态时的显示颜色")]
        [SerializeField] private Color chaseColor = Color.red;
        
        [FoldoutGroup("视觉效果")]
        [LabelText("🟡 攻击状态颜色")]
        [ShowIf("enableVisualEffects")]
        [InfoBox("敌人处于攻击状态时的显示颜色")]
        [SerializeField] private Color attackColor = Color.yellow;
        
        #endregion
        
        #region 状态配置枚举
        
        public enum EnemyType
        {
            GroundEnemy,    // 地面敌人
            FlyingEnemy,    // 飞行敌人
            BossEnemy       // Boss敌人
        }
        
        /// <summary>
        /// 状态配置类
        /// </summary>
        [System.Serializable]
        public class StateConfig
        {
            [LabelText("🎯 状态名称")]
            [ValueDropdown("@UnityEngine.Resources.FindObjectsOfTypeAll<GenericEnemyController>().FirstOrDefault()?.GetAvailableStates()")]
            [InfoBox("选择要配置的敌人状态")]
            public string stateName;
            
            [LabelText("✅ 启用此状态")]
            [InfoBox("勾选后此状态将在状态机中可用")]
            public bool enabled = true;
            
            [LabelText("📝 状态描述")]
            [TextArea(2, 4)]
            [InfoBox("描述此状态的功能和行为")]
            public string description;
            
            public string GetStateConfigLabel()
            {
                return $"{stateName} {(enabled ? "✓" : "✗")}";
            }
        }
        
        #endregion
        
        #region 私有字段
        
        private GameObject cachedPlayer;
        private Camera mainCamera;
        private Dictionary<string, Color> stateColors;
        
        /// <summary>
        /// 缓存的渲染器组件（支持子对象查找）
        /// </summary>
        private Renderer cachedRenderer;
        
        /// <summary>
        /// 所有子对象的渲染器（用于PSD导入的多层级结构）
        /// </summary>
        private Renderer[] allRenderers;
        
        /// <summary>
        /// 原始颜色缓存（用于恢复颜色）
        /// </summary>
        private Dictionary<Renderer, Color> originalColors;
        
        #endregion
        
        #region Unity生命周期
        
        protected override void Awake()
        {
            base.Awake();
            
            // 初始化缓存
            InitializeCaches();
            
            // 设置默认启用状态
            SetupDefaultStates();
            
            // 初始化颜色和音效映射
            InitializeEffectMappings();
        }
        
        protected override void Start()
        {
            base.Start();
            
            // 启动初始状态
            StartInitialState();
            
            Debug.Log($"[{enemyName}] 通用敌人控制器初始化完成 - 类型: {enemyType}, 状态数: {StateMachine.StateCount}");
        }
        
        #endregion
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitializeCaches()
        {
            // 获取主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            // 初始化渲染器缓存（支持PSD导入的多层级结构）
            InitializeRendererCache();
        }
        
        /// <summary>
        /// 初始化渲染器缓存
        /// 支持两种结构：
        /// 1. 单一对象带Renderer
        /// 2. PSD导入的多层级结构（父对象为空，子对象包含SpriteRenderer）
        /// </summary>
        private void InitializeRendererCache()
        {
            // 首先尝试获取自身的Renderer
            cachedRenderer = GetComponent<Renderer>();
            
            // 获取所有子对象的Renderer（包括自身）
            allRenderers = GetComponentsInChildren<Renderer>(true);
            
            // 如果自身没有Renderer但子对象有，使用第一个子对象的Renderer作为主渲染器
            if (cachedRenderer == null && allRenderers.Length > 0)
            {
                cachedRenderer = allRenderers[0];
                
                if (enableStateMachineDebug)
                {
                    Debug.Log($"[{enemyName}] 使用子对象渲染器: {cachedRenderer.gameObject.name}，共找到 {allRenderers.Length} 个渲染器");
                }
            }
            
            // 缓存所有渲染器的原始颜色
            originalColors = new Dictionary<Renderer, Color>();
            foreach (var renderer in allRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    originalColors[renderer] = renderer.material.color;
                }
            }
            
            if (allRenderers.Length == 0)
            {
                Debug.LogWarning($"[{enemyName}] 未找到任何Renderer组件，视觉效果将不可用");
            }
        }
        
        /// <summary>
        /// 设置默认启用状态
        /// </summary>
        private void SetupDefaultStates()
        {
            if (enabledStates.Count == 0)
            {
                // 默认启用待机和巡逻状态
                enabledStates.Add(new StateConfig 
                { 
                    stateName = "Idle", 
                    enabled = true, 
                    description = "待机状态 - 敌人原地待机并扫描玩家" 
                });
                
                enabledStates.Add(new StateConfig 
                { 
                    stateName = "Patrol", 
                    enabled = true, 
                    description = "巡逻状态 - 敌人左右巡逻移动" 
                });
                
                // 根据敌人类型添加其他状态
                switch (enemyType)
                {
                    case EnemyType.GroundEnemy:
                        // 地面敌人可以添加追击、攻击等状态
                        break;
                    case EnemyType.FlyingEnemy:
                        // 飞行敌人可以添加俯冲攻击等状态
                        break;
                    case EnemyType.BossEnemy:
                        // Boss敌人可以添加特殊技能状态
                        break;
                }
            }
        }
        
        /// <summary>
        /// 初始化效果映射
        /// </summary>
        private void InitializeEffectMappings()
        {
            // 初始化状态颜色映射
            stateColors = new Dictionary<string, Color>
            {
                { "Idle", idleColor * 0.8f },
                { "Patrol", patrolColor * 0.8f },
                { "Chase", chaseColor * 0.8f },
                { "Attack", attackColor * 0.8f }
            };
        }
        
        /// <summary>
        /// 启动初始状态
        /// </summary>
        private void StartInitialState()
        {
            if (StateMachine.HasState(initialState))
            {
                StateMachine.TransitionTo(initialState);
            }
            else if (StateMachine.HasState("Idle"))
            {
                StateMachine.TransitionTo("Idle");
            }
            else
            {
                Debug.LogWarning($"[{enemyName}] 找不到初始状态 '{initialState}' 或默认状态 'Idle'");
            }
        }
        
        #endregion
        
        #region 状态注册系统
        
        /// <summary>
        /// 注册敌人状态 - 自动注册所有启用的状态
        /// </summary>
        protected override void RegisterStates()
        {
            foreach (var stateConfig in enabledStates)
            {
                if (!stateConfig.enabled) continue;
                
                IEnemyState state = CreateStateInstance(stateConfig.stateName);
                if (state != null)
                {
                    StateMachine.RegisterState(state);
                    
                    if (enableStateMachineDebug)
                    {
                        Debug.Log($"[{enemyName}] 注册状态: {stateConfig.stateName} - {stateConfig.description}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{enemyName}] 无法创建状态: {stateConfig.stateName}");
                }
            }
            
            Debug.Log($"[{enemyName}] 状态注册完成，共 {StateMachine.StateCount} 个状态");
        }
        
        /// <summary>
        /// 创建状态实例 - 工厂方法模式
        /// </summary>
        private IEnemyState CreateStateInstance(string stateName)
        {
            switch (stateName)
            {
                case "Idle":
                    return CreateIdleState();
                    
                case "Patrol":
                    return CreatePatrolState();
                    
                // 未来可以在这里添加更多状态
                case "Chase":
                    return CreateChaseState();
                case "Attack":
                    return CreateAttackState();
                // case "Hurt":
                //     return CreateHurtState();
                // case "Death":
                //     return CreateDeathState();
                    
                default:
                    Debug.LogWarning($"[{enemyName}] 未知状态类型: {stateName}");
                    return null;
            }
        }
        
        /// <summary>
        /// 创建待机状态
        /// </summary>
        private GroundEnemyIdleState CreateIdleState()
        {
            var idleState = new GroundEnemyIdleState();
            
            // 使用反射设置状态参数
            ConfigureStateParameters(idleState, new Dictionary<string, object>
            {
                { "idleTimeout", idleTimeout },
                { "detectionRange", playerDetectionRange },
                { "obstacleLayer", obstacleLayer }
            });
            
            return idleState;
        }
        
        /// <summary>
        /// 创建巡逻状态
        /// </summary>
        private GroundEnemyPatrolState CreatePatrolState()
        {
            var patrolState = new GroundEnemyPatrolState();
            
            // 使用反射设置状态参数
            ConfigureStateParameters(patrolState, new Dictionary<string, object>
            {
                { "patrolSpeed", patrolSpeed },
                { "patrolDuration", patrolDuration },
                { "detectionRange", playerDetectionRange },
                { "maxPatrolDistance", maxPatrolDistance },
                { "groundLayer", groundLayer },
                { "wallLayer", wallLayer },
                { "obstacleLayer", obstacleLayer }
            });
            
            return patrolState;
        }
        
        /// <summary>
        /// 创建追击状态（占位实现）
        /// </summary>
        private IEnemyState CreateChaseState()
        {
            // 目前返回巡逻状态作为占位，使用追击速度
            var chaseState = new GroundEnemyPatrolState();
            
            ConfigureStateParameters(chaseState, new Dictionary<string, object>
            {
                { "patrolSpeed", chaseSpeed }, // 使用追击速度
                { "patrolDuration", patrolDuration },
                { "detectionRange", playerDetectionRange },
                { "maxPatrolDistance", maxPatrolDistance },
                { "groundLayer", groundLayer },
                { "wallLayer", wallLayer },
                { "obstacleLayer", obstacleLayer }
            });
            
            if (enableStateMachineDebug)
            {
                Debug.Log($"[{enemyName}] 创建追击状态（使用巡逻状态，速度: {chaseSpeed}）");
            }
            
            return chaseState;
        }
        
        /// <summary>
        /// 创建攻击状态（占位实现）
        /// </summary>
        private IEnemyState CreateAttackState()
        {
            // 目前返回待机状态作为占位，记录攻击冷却时间
            var attackState = new GroundEnemyIdleState();
            
            ConfigureStateParameters(attackState, new Dictionary<string, object>
            {
                { "idleTimeout", attackCooldown }, // 使用攻击冷却时间作为待机时间
                { "detectionRange", attackRange }, // 使用攻击范围作为检测范围
                { "obstacleLayer", obstacleLayer }
            });
            
            if (enableStateMachineDebug)
            {
                Debug.Log($"[{enemyName}] 创建攻击状态（使用待机状态，冷却: {attackCooldown}）");
            }
            
            return attackState;
        }
        
        /// <summary>
        /// 配置状态参数（使用反射）
        /// </summary>
        private void ConfigureStateParameters(object state, Dictionary<string, object> parameters)
        {
            var stateType = state.GetType();
            
            foreach (var param in parameters)
            {
                var field = stateType.GetField(param.Key, 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (field != null && field.FieldType == param.Value.GetType())
                {
                    field.SetValue(state, param.Value);
                    
                    if (enableStateMachineDebug)
                    {
                        Debug.Log($"[{enemyName}] 配置状态参数: {param.Key} = {param.Value}");
                    }
                }
            }
        }
        
        #endregion
        
        #region 抽象方法实现
        
        /// <summary>
        /// 播放动画
        /// </summary>
        public override void PlayAnimation(string animationName)
        {
            if (animator != null)
            {
                animator.Play(animationName);
            }
            else if (enableVisualEffects)
            {
                // 对于胶囊体，使用颜色变化表示状态
                ApplyStateVisualEffect(animationName);
            }
            
            if (enableStateMachineDebug)
            {
                Debug.Log($"[{enemyName}] 播放动画/效果: {animationName}");
            }
        }
        
        /// <summary>
        /// 向目标移动
        /// </summary>
        public override void MoveTowards(Vector3 target, float speed)
        {
            if (!CanAct) return;
            
            Vector3 direction = (target - transform.position).normalized;
            
            if (rigidBody != null)
            {
                rigidBody.velocity = new Vector2(direction.x * speed, rigidBody.velocity.y);
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }
            
            if (direction.x != 0)
            {
                SetFacingDirection(direction.x > 0);
            }
        }
        
        /// <summary>
        /// 面向目标
        /// </summary>
        public override void FaceTarget(Vector3 target)
        {
            bool shouldFaceRight = target.x > transform.position.x;
            SetFacingDirection(shouldFaceRight);
        }
        
        /// <summary>
        /// 执行攻击
        /// </summary>
        public override void PerformAttack()
        {
            PlayAnimation("Attack");
            PlaySound("Attack");
            
            // 这里可以添加具体的攻击逻辑
            Debug.Log($"[{enemyName}] 执行攻击！");
        }
        
        /// <summary>
        /// 检测玩家
        /// </summary>
        public override bool DetectPlayer(float range)
        {
            GameObject player = GetPlayerTarget();
            if (player == null) return false;
            
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool inRange = distance <= range;
            
            if (inRange && enableStateMachineDebug)
            {
                Debug.Log($"[{enemyName}] 检测到玩家，距离: {distance:F2}m");
            }
            
            return inRange;
        }
        
        /// <summary>
        /// 获取玩家目标
        /// </summary>
        public override GameObject GetPlayerTarget()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = GameObject.FindGameObjectWithTag("Player");
                
                if (cachedPlayer == null && mainCamera != null)
                {
                    cachedPlayer = mainCamera.gameObject;
                    
                    if (enableStateMachineDebug)
                    {
                        Debug.Log($"[{enemyName}] 使用主摄像机作为玩家目标");
                    }
                }
            }
            
            return cachedPlayer;
        }
        
        #endregion
        
        #region 物理检测实现
        
        /// <summary>
        /// 检查地面碰撞
        /// </summary>
        public override bool CheckGroundCollision()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, groundLayer);
            return hit.collider != null;
        }
        
        /// <summary>
        /// 检查墙壁碰撞
        /// </summary>
        public override bool CheckWallCollision()
        {
            Vector3 direction = IsFacingRight ? Vector3.right : Vector3.left;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f, wallLayer);
            return hit.collider != null;
        }
        
        /// <summary>
        /// 检查平台边缘
        /// </summary>
        public override bool CheckPlatformEdge()
        {
            Vector3 direction = IsFacingRight ? Vector3.right : Vector3.left;
            Vector3 checkPos = transform.position + direction * 0.6f;
            RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, groundLayer);
            return hit.collider == null;
        }
        
        #endregion
        
        #region 视觉和音效系统
        
        /// <summary>
        /// 应用状态视觉效果
        /// 支持单一Renderer和多层级PSD结构
        /// </summary>
        private void ApplyStateVisualEffect(string stateName)
        {
            if (!enableVisualEffects) return;
            
            Color targetColor = Color.white;
            if (stateColors.ContainsKey(stateName))
            {
                targetColor = stateColors[stateName];
            }
            
            // 应用到所有渲染器
            ApplyColorToAllRenderers(targetColor);
        }
        
        /// <summary>
        /// 应用颜色到所有渲染器
        /// </summary>
        private void ApplyColorToAllRenderers(Color color)
        {
            if (allRenderers == null || allRenderers.Length == 0) return;
            
            foreach (var renderer in allRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
        
        /// <summary>
        /// 恢复所有渲染器的原始颜色
        /// </summary>
        private void RestoreOriginalColors()
        {
            if (originalColors == null || allRenderers == null) return;
            
            foreach (var renderer in allRenderers)
            {
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    renderer.material.color = originalColors[renderer];
                }
            }
        }
        
        /// <summary>
        /// 获取主渲染器（兼容单一对象和多层级结构）
        /// </summary>
        public Renderer GetMainRenderer()
        {
            return cachedRenderer;
        }
        
        /// <summary>
        /// 获取所有渲染器
        /// </summary>
        public Renderer[] GetAllRenderers()
        {
            return allRenderers;
        }
        
        /// <summary>
        /// 播放音效（简化实现）
        /// </summary>
        public override void PlaySound(string soundName)
        {
            // 简化实现：仅输出调试日志
            if (enableStateMachineDebug)
            {
                Debug.Log($"[{enemyName}] 播放音效: {soundName}");
            }
        }
        
        #endregion
        
        #region 事件处理重写
        
        /// <summary>
        /// 受伤处理
        /// </summary>
        protected override void OnTakeDamage(float damage, Vector3 damageSource)
        {
            base.OnTakeDamage(damage, damageSource);
            
            PlaySound("Hurt");
            
            if (damageSource != Vector3.zero)
            {
                Vector3 knockbackDirection = (transform.position - damageSource).normalized;
                ApplyKnockback(5f, knockbackDirection);
            }
            
            Debug.Log($"[{enemyName}] 受到伤害: {damage}, 剩余生命: {CurrentHealth}");
        }
        
        /// <summary>
        /// 死亡处理
        /// </summary>
        protected override void OnDeath()
        {
            base.OnDeath();
            
            PlaySound("Death");
            
            // 死亡视觉效果 - 使用缓存的渲染器
            if (enableVisualEffects)
            {
                ApplyColorToAllRenderers(Color.black);
            }
            
            Debug.Log($"[{enemyName}] 死亡");
            
            // 延迟销毁
            Invoke(nameof(DestroyEnemy), 2f);
        }
        
        #endregion
        
        #region 调试和工具方法
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // 绘制玩家检测范围
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
            
            // 绘制巡逻范围
            Gizmos.color = Color.blue;
            Vector3 leftBound = transform.position + Vector3.left * maxPatrolDistance;
            Vector3 rightBound = transform.position + Vector3.right * maxPatrolDistance;
            Gizmos.DrawLine(leftBound, rightBound);
            
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 绘制到玩家的连线
            GameObject player = GetPlayerTarget();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                Gizmos.color = distance <= playerDetectionRange ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }
        
        /// <summary>
        /// 获取当前状态信息
        /// </summary>
        public string GetStateInfo()
        {
            if (StateMachine == null) return "状态机未初始化";
            
            return $"当前状态: {StateMachine.CurrentStateName}, 生命值: {CurrentHealth}/{maxHealth}, 可行动: {CanAct}";
        }
        
        #endregion
        
        #region 右键菜单测试方法
        
        [ContextMenu("测试受伤")]
        public void TestTakeDamage()
        {
            TakeDamage(10f, Vector3.left);
        }
        
        [ContextMenu("重置状态")]
        public void ResetEnemy()
        {
            currentHealth = maxHealth;
            canMove = true;
            
            if (StateMachine != null)
            {
                StateMachine.ForceTransitionTo(initialState);
            }
            
            Debug.Log($"[{enemyName}] 状态已重置");
        }
        
        [ContextMenu("切换到待机")]
        public void SwitchToIdle()
        {
            if (StateMachine != null && StateMachine.HasState("Idle"))
            {
                StateMachine.ForceTransitionTo("Idle");
            }
        }
        
        [ContextMenu("切换到巡逻")]
        public void SwitchToPatrol()
        {
            if (StateMachine != null && StateMachine.HasState("Patrol"))
            {
                StateMachine.ForceTransitionTo("Patrol");
            }
        }
        
        [ContextMenu("显示状态信息")]
        public void ShowStateInfo()
        {
            Debug.Log($"[{enemyName}] {GetStateInfo()}");
        }
        
        [ContextMenu("列出所有状态")]
        public void ListAllStates()
        {
            if (StateMachine != null)
            {
                string[] states = StateMachine.GetAllStateNames();
                Debug.Log($"[{enemyName}] 已注册状态: {string.Join(", ", states)}");
            }
        }
        
        #endregion
        
        #region Odin Inspector支持方法
        
        /// <summary>
        /// 获取可用状态列表（用于下拉菜单）
        /// </summary>
        private IEnumerable<string> GetAvailableStates()
        {
            return GetAllPossibleStates();
        }
        
        /// <summary>
        /// 获取所有可能的状态名称
        /// </summary>
        public IEnumerable<string> GetAllPossibleStates()
        {
            return new string[] { "Idle", "Patrol", "Chase", "Attack", "Hurt", "Death" };
        }
        
        /// <summary>
        /// 获取状态配置标签
        /// </summary>
        private string GetStateConfigLabel(StateConfig config)
        {
            if (config == null) return "未配置";
            return config.GetStateConfigLabel();
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// 添加默认状态配置按钮
        /// </summary>
        [FoldoutGroup("状态配置")]
        [Button("⚡ 添加默认状态 (待机+巡逻)", ButtonSizes.Medium)]
        [GUIColor(0.7f, 1f, 0.7f)]
        private void AddDefaultStates()
        {
            enabledStates.Clear();
            
            enabledStates.Add(new StateConfig 
            { 
                stateName = "Idle", 
                enabled = true, 
                description = "待机状态 - 敌人原地待机并扫描玩家" 
            });
            
            enabledStates.Add(new StateConfig 
            { 
                stateName = "Patrol", 
                enabled = true, 
                description = "巡逻状态 - 敌人左右巡逻移动" 
            });
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// 添加完整状态配置按钮
        /// </summary>
        [FoldoutGroup("状态配置")]
        [Button("🎯 添加完整状态 (全部6种)", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.9f, 1f)]
        private void AddFullStates()
        {
            enabledStates.Clear();
            
            var allStates = new (string name, string desc)[]
            {
                ("Idle", "待机状态 - 敌人原地待机并扫描玩家"),
                ("Patrol", "巡逻状态 - 敌人左右巡逻移动"),
                ("Chase", "追击状态 - 敌人追击玩家"),
                ("Attack", "攻击状态 - 敌人攻击玩家"),
                ("Hurt", "受伤状态 - 敌人受到伤害"),
                ("Death", "死亡状态 - 敌人死亡")
            };
            
            foreach (var (name, desc) in allStates)
            {
                enabledStates.Add(new StateConfig 
                { 
                    stateName = name, 
                    enabled = name == "Idle" || name == "Patrol", // 默认只启用基础状态
                    description = desc 
                });
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// 清空状态配置按钮
        /// </summary>
        [FoldoutGroup("状态配置")]
        [Button("🗑️ 清空所有状态配置", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.7f)]
        private void ClearStates()
        {
            enabledStates.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// 运行时状态信息显示
        /// </summary>
        [FoldoutGroup("运行时信息")]
        [ShowInInspector]
        [DisplayAsString]
        [ShowIf("@Application.isPlaying")]
        [LabelText("当前状态")]
        private string CurrentStateDisplay => StateMachine?.CurrentStateName ?? "未初始化";
        
        [FoldoutGroup("运行时信息")]
        [ShowInInspector]
        [DisplayAsString]
        [ShowIf("@Application.isPlaying")]
        [LabelText("状态数量")]
        private string StateCountDisplay => StateMachine?.StateCount.ToString() ?? "0";
        
        [FoldoutGroup("运行时信息")]
        [ShowInInspector]
        [DisplayAsString]
        [ShowIf("@Application.isPlaying")]
        [LabelText("生命值")]
        private string HealthDisplay => $"{CurrentHealth:F0} ({HealthPercentage * 100:F0}%)";
        
        [FoldoutGroup("运行时信息")]
        [ShowInInspector]
        [DisplayAsString]
        [ShowIf("@Application.isPlaying")]
        [LabelText("状态标记")]
        private string StatusDisplay => $"存活:{IsAlive} | 可行动:{CanAct} | 面向:{(IsFacingRight ? "右" : "左")}";
        
        /// <summary>
        /// 打开状态机可视化窗口按钮
        /// </summary>
        [FoldoutGroup("状态机可视化")]
        [Button("🎨 打开状态机可视化窗口", ButtonSizes.Large)]
        [GUIColor(0.7f, 1f, 0.7f)]
        private void OpenStateMachineVisualizer()
        {
            #if UNITY_EDITOR
            // 使用更简单的反射方法
            try
            {
                // 查找所有程序集中的StateMachineVisualizerWindow类型
                System.Type windowType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    windowType = assembly.GetType("CryptaGeometrica.EnemyStateMachine.Editor.StateMachineVisualizerWindow");
                    if (windowType != null) break;
                }
                
                if (windowType != null)
                {
                    // 调用静态ShowWindow方法
                    var showWindowMethod = windowType.GetMethod("ShowWindow", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (showWindowMethod != null)
                    {
                        showWindowMethod.Invoke(null, null);
                        UnityEngine.Debug.Log("[状态机可视化] 窗口已打开");
                    }
                    else
                    {
                        // 如果没有ShowWindow方法，使用EditorWindow.GetWindow
                        var window = UnityEditor.EditorWindow.GetWindow(windowType, false, "状态机可视化器");
                        window.minSize = new Vector2(800, 600);
                        window.Show();
                        window.Focus();
                        UnityEngine.Debug.Log("[状态机可视化] 窗口已通过GetWindow打开");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("找不到StateMachineVisualizerWindow类。请检查：\n1. Editor脚本是否在正确的文件夹中\n2. 类名和命名空间是否正确\n3. 是否有编译错误");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"打开状态机可视化窗口时发生错误: {e.Message}");
            }
            #endif
        }
        #endif
        
        #endregion
        
        #region Gizmos调试绘制
        
        /// <summary>
        /// 绘制Gizmos调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDetectionRange && !showPatrolPath && !showStateInfo) return;
            
            Gizmos.color = gizmosColor;
            
            // 绘制检测范围
            if (showDetectionRange)
            {
                DrawDetectionRangeGizmos();
            }
            
            // 绘制巡逻路径
            if (showPatrolPath)
            {
                DrawPatrolPathGizmos();
            }
            
            // 绘制状态信息
            if (showStateInfo)
            {
                DrawStateInfoGizmos();
            }
        }
        
        /// <summary>
        /// 绘制检测范围Gizmos
        /// </summary>
        private void DrawDetectionRangeGizmos()
        {
            // 绘制检测圆圈
            Gizmos.color = new Color(gizmosColor.r, gizmosColor.g, gizmosColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, playerDetectionRange);
            
            // 绘制检测范围边框
            Gizmos.color = gizmosColor;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        }
        
        /// <summary>
        /// 绘制巡逻路径Gizmos
        /// </summary>
        private void DrawPatrolPathGizmos()
        {
            Vector3 leftPoint = transform.position + Vector3.left * maxPatrolDistance;
            Vector3 rightPoint = transform.position + Vector3.right * maxPatrolDistance;
            
            // 绘制巡逻线
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftPoint, rightPoint);
            
            // 绘制巡逻端点
            Gizmos.DrawWireCube(leftPoint, Vector3.one * 0.5f);
            Gizmos.DrawWireCube(rightPoint, Vector3.one * 0.5f);
        }
        
        /// <summary>
        /// 绘制状态信息Gizmos
        /// </summary>
        private void DrawStateInfoGizmos()
        {
            if (!Application.isPlaying || StateMachine == null) return;
            
            // 根据当前状态设置颜色
            string currentState = StateMachine.CurrentStateName ?? "Unknown";
            Color stateColor = GetStateColor(currentState);
            
            // 绘制状态指示器
            Gizmos.color = stateColor;
            Vector3 indicatorPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireCube(indicatorPos, Vector3.one * 0.8f);
            
            // 绘制面向方向箭头
            Vector3 direction = IsFacingRight ? Vector3.right : Vector3.left;
            Vector3 arrowStart = transform.position + Vector3.up * 1.5f;
            Vector3 arrowEnd = arrowStart + direction * 1f;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            // 绘制箭头头部
            Vector3 arrowHead1 = arrowEnd + (Vector3.left + Vector3.down) * 0.3f * (IsFacingRight ? 1 : -1);
            Vector3 arrowHead2 = arrowEnd + (Vector3.left + Vector3.up) * 0.3f * (IsFacingRight ? 1 : -1);
            Gizmos.DrawLine(arrowEnd, arrowHead1);
            Gizmos.DrawLine(arrowEnd, arrowHead2);
        }
        
        /// <summary>
        /// 获取状态对应的颜色
        /// </summary>
        private Color GetStateColor(string stateName)
        {
            switch (stateName)
            {
                case "Idle": return Color.cyan;
                case "Patrol": return Color.green;
                case "Chase": return Color.red;
                case "Attack": return Color.yellow;
                case "Hurt": return Color.magenta;
                case "Death": return Color.gray;
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// 检测范围变化回调
        /// </summary>
        #if UNITY_EDITOR
        private void OnDetectionRangeChanged()
        {
            // 在编辑器中实时更新Gizmos显示
            UnityEditor.SceneView.RepaintAll();
        }
        #endif
        
        #endregion
    }
}
