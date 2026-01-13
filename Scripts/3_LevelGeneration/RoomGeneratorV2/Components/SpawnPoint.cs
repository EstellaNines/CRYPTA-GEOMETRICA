using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 怪物生成点组件
    /// 管理单个生成点的数据和行为
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        #region 字段
        
        [Header("生成点信息")]
        [SerializeField] private SpawnPointV2 spawnData;
        [SerializeField] private bool isActive = true;
        [SerializeField] private GameObject spawnedEnemy;
        
        [Header("碰撞检测配置")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask platformLayer;
        
        // 房间数据引用（用于计算射线距离）
        private RoomDataV2 roomData;
        
        // 合并的碰撞检测层
        private LayerMask collisionLayers;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 生成点数据
        /// </summary>
        public SpawnPointV2 SpawnData => spawnData;
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }
        
        /// <summary>
        /// 已生成的敌人
        /// </summary>
        public GameObject SpawnedEnemy
        {
            get => spawnedEnemy;
            set => spawnedEnemy = value;
        }
        
        /// <summary>
        /// 是否已生成敌人
        /// </summary>
        public bool HasSpawnedEnemy => spawnedEnemy != null;
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 初始化生成点
        /// </summary>
        public void Initialize(SpawnPointV2 data, RoomDataV2 room = null)
        {
            spawnData = data;
            isActive = true;
            spawnedEnemy = null;
            roomData = room;
            
            // 初始化 LayerMask（如果没有在 Inspector 中设置）
            if (groundLayer == 0)
                groundLayer = LayerMask.GetMask("Ground");
            if (platformLayer == 0)
                platformLayer = LayerMask.GetMask("Platforms");
            
            // 合并碰撞检测层
            collisionLayers = groundLayer | platformLayer;
            
            // 更新GameObject名称
            gameObject.name = $"SpawnPoint_{data.enemyType}_{data.type}";
            
            // 初始化生成点
        }
        
        /// <summary>
        /// 验证生成位置是否有碰撞体
        /// </summary>
        /// <param name="position">要检测的位置</param>
        /// <param name="boxHalfSize">检测盒子的半尺寸（half extents）</param>
        /// <returns>如果位置无碰撞返回true，否则返回false</returns>
        private bool ValidateSpawnPosition(Vector2 position, Vector2 boxHalfSize)
        {
            // 确保 LayerMask 已初始化
            if (collisionLayers == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
                platformLayer = LayerMask.GetMask("Platforms");
                collisionLayers = groundLayer | platformLayer;
            }
            
            // 使用 Physics2D.OverlapBox 检测目标位置是否有碰撞体
            // 只检测 Ground 和 Platforms 层
            Collider2D[] colliders = Physics2D.OverlapBoxAll(position, boxHalfSize * 2f, 0f, collisionLayers);
            
            foreach (var col in colliders)
            {
                // 忽略触发器
                if (col.isTrigger)
                    continue;
                
                // 忽略敌人自身的碰撞体（如果有的话）
                if (col.gameObject == spawnedEnemy)
                    continue;
                
                // 检测到实体碰撞体，位置无效
                #if UNITY_EDITOR
                Debug.Log($"[SpawnPoint] 碰撞检测失败 at {position}, 检测到碰撞体: {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)}), 半尺寸: {boxHalfSize}");
                #endif
                
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 尝试查找有效的生成位置
        /// 按照以下顺序尝试：原始位置 -> 向上1-5格 -> 左1-2格 -> 右1-2格
        /// </summary>
        /// <param name="originalPosition">原始位置</param>
        /// <param name="boxHalfSize">检测盒子的半尺寸（half extents）</param>
        /// <param name="validPosition">找到的有效位置</param>
        /// <returns>是否找到有效位置</returns>
        private bool TryFindValidPosition(Vector2 originalPosition, Vector2 boxHalfSize, out Vector2 validPosition)
        {
            validPosition = originalPosition;
            const float gridSize = 1f; // 假设每格为1单位
            
            // 1. 首先检查原始位置
            if (ValidateSpawnPosition(originalPosition, boxHalfSize))
            {
                return true;
            }
            
            // 2. 向上偏移查找有效位置（最多尝试5格）
            const int maxUpAttempts = 5;
            for (int i = 1; i <= maxUpAttempts; i++)
            {
                Vector2 testPosition = originalPosition + Vector2.up * (gridSize * i);
                
                if (ValidateSpawnPosition(testPosition, boxHalfSize))
                {
                    validPosition = testPosition;
                    #if UNITY_EDITOR
                    Debug.Log($"[SpawnPoint] 向上偏移 {i} 格找到有效位置: {validPosition}");
                    #endif
                    return true;
                }
            }
            
            // 3. 向左偏移查找有效位置（最多尝试2格）
            const int maxHorizontalAttempts = 2;
            for (int i = 1; i <= maxHorizontalAttempts; i++)
            {
                Vector2 testPosition = originalPosition + Vector2.left * (gridSize * i);
                
                if (ValidateSpawnPosition(testPosition, boxHalfSize))
                {
                    validPosition = testPosition;
                    #if UNITY_EDITOR
                    Debug.Log($"[SpawnPoint] 向左偏移 {i} 格找到有效位置: {validPosition}");
                    #endif
                    return true;
                }
            }
            
            // 4. 向右偏移查找有效位置（最多尝试2格）
            for (int i = 1; i <= maxHorizontalAttempts; i++)
            {
                Vector2 testPosition = originalPosition + Vector2.right * (gridSize * i);
                
                if (ValidateSpawnPosition(testPosition, boxHalfSize))
                {
                    validPosition = testPosition;
                    #if UNITY_EDITOR
                    Debug.Log($"[SpawnPoint] 向右偏移 {i} 格找到有效位置: {validPosition}");
                    #endif
                    return true;
                }
            }
            
            // 5. 尝试对角线方向（左上、右上各2格）
            Vector2[] diagonalOffsets = new Vector2[]
            {
                new Vector2(-1, 1),  // 左上1格
                new Vector2(1, 1),   // 右上1格
                new Vector2(-2, 1),  // 左上2格（左2上1）
                new Vector2(2, 1),   // 右上2格（右2上1）
                new Vector2(-1, 2),  // 左上2格（左1上2）
                new Vector2(1, 2),   // 右上2格（右1上2）
            };
            
            foreach (var offset in diagonalOffsets)
            {
                Vector2 testPosition = originalPosition + offset * gridSize;
                
                if (ValidateSpawnPosition(testPosition, boxHalfSize))
                {
                    validPosition = testPosition;
                    #if UNITY_EDITOR
                    Debug.Log($"[SpawnPoint] 对角线偏移 {offset} 找到有效位置: {validPosition}");
                    #endif
                    return true;
                }
            }
            
            // 无法找到有效位置
            return false;
        }
        
        /// <summary>
        /// 生成敌人
        /// </summary>
        public GameObject SpawnEnemy(GameObject enemyPrefab)
        {
            if (!isActive || HasSpawnedEnemy || enemyPrefab == null)
            {
                return null;
            }
            
            // 获取敌人碰撞体大小（用于碰撞检测）
            var tempEnemy = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
            var col2D = tempEnemy.GetComponentInChildren<Collider2D>();
            // 注意：Physics2D.OverlapBox 需要的是半尺寸（extents），不是完整尺寸（size）
            Vector2 boxHalfSize = col2D != null ? (Vector2)col2D.bounds.extents : new Vector2(0.5f, 0.5f);
            float extY = col2D != null ? col2D.bounds.extents.y : 0.5f;
            
            #if UNITY_EDITOR
            Debug.Log($"[SpawnPoint] 准备生成 {spawnData.enemyType} at {transform.position}, 碰撞体半尺寸: {boxHalfSize}");
            #endif
            
            Destroy(tempEnemy);
            
            // 计算初始生成位置
            Vector2 targetPosition = transform.position;
            
            if (spawnData.type == SpawnType.Ground)
            {
                // 地面敌人：计算射线检测距离（限制为当前位置到房间底部的距离）
                float maxRayDistance = 10f;
                if (roomData != null)
                {
                    // 计算当前位置到房间底部的距离
                    float roomBottom = 0f; // 假设房间底部为y=0
                    maxRayDistance = Mathf.Max(1f, transform.position.y - roomBottom);
                }
                
                // 向下射线查找地面，只检测 Ground 和 Platforms 层
                RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, Vector2.down, maxRayDistance, collisionLayers);
                if (hit.collider != null)
                {
                    targetPosition.y = hit.point.y + extY;
                    
                    #if UNITY_EDITOR
                    Debug.Log($"[SpawnPoint] 地面射线检测成功，击中: {hit.collider.gameObject.name}, 目标位置: {targetPosition}");
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning($"[SpawnPoint] 地面射线检测失败，使用原始位置: {targetPosition}");
                    #endif
                }
            }
            else if (spawnData.type == SpawnType.Air)
            {
                // 空中敌人：若有记录距地高度，依据地面高度修正
                if (spawnData.heightAboveGround > 0)
                {
                    // 只检测 Ground 和 Platforms 层
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 20f, collisionLayers);
                    if (hit.collider != null)
                    {
                        targetPosition.y = hit.point.y + spawnData.heightAboveGround;
                    }
                }
            }
            
            // 碰撞检测和位置调整
            Vector2 validPosition;
            if (!TryFindValidPosition(targetPosition, boxHalfSize, out validPosition))
            {
                // 无法找到有效位置，取消生成并记录警告
                Debug.LogWarning($"[SpawnPoint] 无法为 {spawnData.enemyType} 找到有效生成位置 at {transform.position}. 取消生成。");
                return null;
            }
            
            // 在有效位置实例化敌人
            spawnedEnemy = Instantiate(enemyPrefab, validPosition, Quaternion.identity);
            spawnedEnemy.name = $"{enemyPrefab.name}_{spawnData.enemyType}";
            
            return spawnedEnemy;
        }
        
        /// <summary>
        /// 清除已生成的敌人
        /// </summary>
        public void ClearSpawnedEnemy()
        {
            if (spawnedEnemy != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedEnemy);
                }
                else
                {
                    DestroyImmediate(spawnedEnemy);
                }
                spawnedEnemy = null;
            }
        }
        
        /// <summary>
        /// 激活/停用生成点
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }
        
        #endregion
        
        #region Unity生命周期
        
        private void OnDrawGizmos()
        {
            if (!isActive) return;
            
            // 根据验证状态设置基础颜色
            Color baseColor;
            if (!spawnData.isValid)
            {
                // 无效生成点显示为红色
                baseColor = new Color(1f, 0f, 0f, 0.8f);
            }
            else
            {
                // 有效生成点显示为绿色
                baseColor = new Color(0f, 1f, 0f, 0.8f);
            }
            
            // 根据敌人类型调整颜色（仅在有效时）
            if (spawnData.isValid)
            {
                switch (spawnData.enemyType)
                {
                    case EnemyType.TriangleSharpshooter:
                        baseColor = new Color(1f, 0.5f, 0f, 0.8f); // 橙色
                        break;
                    case EnemyType.TriangleShieldbearer:
                        baseColor = new Color(1f, 0.2f, 0.2f, 0.8f); // 红色
                        break;
                    case EnemyType.TriangleMoth:
                        baseColor = new Color(0.2f, 1f, 0.2f, 0.8f); // 绿色
                        break;
                    case EnemyType.CompositeGuardian:
                        baseColor = new Color(1f, 1f, 0f, 0.8f); // 黄色
                        break;
                    default:
                        baseColor = spawnData.type == SpawnType.Air 
                            ? new Color(0.8f, 0.2f, 1f, 0.8f)  // 紫色
                            : new Color(0.2f, 0.8f, 0.8f, 0.8f); // 青色
                        break;
                }
            }
            
            Gizmos.color = baseColor;
            
            // 绘制生成点核心
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // 显示边界检测范围（使用半透明立方体）
            const float edgePadding = 3f; // 默认边界填充
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
            
            if (spawnData.type == SpawnType.Ground)
            {
                // 地面生成点：显示上方3格空间（包括生成点本身）和左右1格空间
                // 验证逻辑检查: position.y+0, position.y+1, position.y+2 都是 Floor
                // 所以 Gizmos 应该从 position.y 开始，向上延伸3格
                Vector3 boxSize = new Vector3(3f, 3f, 1f); // 宽度3格（左1+中1+右1），高度3格
                Vector3 boxCenter = transform.position + Vector3.up * 1f; // 向上偏移1格（3格的中心）
                Gizmos.DrawCube(boxCenter, boxSize);
                
                // 绘制边框
                Gizmos.color = baseColor;
                Gizmos.DrawWireCube(boxCenter, boxSize);
            }
            else if (spawnData.type == SpawnType.Air)
            {
                // 空中生成点：显示5x5的检测区域（半径2格）
                Vector3 boxSize = new Vector3(5f, 5f, 1f); // 5x5的检测区域
                Gizmos.DrawCube(transform.position, boxSize);
                
                // 绘制边框
                Gizmos.color = baseColor;
                Gizmos.DrawWireCube(transform.position, boxSize);
            }
            
            // 如果已生成敌人，绘制连接线
            if (HasSpawnedEnemy)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, spawnedEnemy.transform.position);
            }
            
            // 在Scene视图中显示验证失败原因（如果有）
            #if UNITY_EDITOR
            if (!spawnData.isValid && !string.IsNullOrEmpty(spawnData.invalidReason))
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 1f, 
                    $"Invalid: {spawnData.invalidReason}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.red },
                        fontSize = 10
                    }
                );
            }
            #endif
        }
        
        #endregion
    }
}
