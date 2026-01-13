using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;
using CryptaGeometrica.Enemies;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 生成点管理器
    /// 管理房间内所有的怪物生成点
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        #region 字段
        
        [Header("管理器信息")]
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        [SerializeField] private bool autoSpawnOnStart = false;
        [SerializeField] private float spawnDelay = 1f;
        [SerializeField] private EnemyPrefabRegistrySO enemyRegistry;
        
        [Header("敌人预制体")]
        [SerializeField] private GameObject sharpshooterPrefab;
        [SerializeField] private GameObject shieldbearerPrefab;
        [SerializeField] private GameObject mothPrefab;
        [SerializeField] private GameObject bossPrefab;
        
        #endregion
        
        #region 属性
        
        /// <summary>
        /// 所有生成点
        /// </summary>
        public List<SpawnPoint> SpawnPoints => spawnPoints;
        
        /// <summary>
        /// 激活的生成点数量
        /// </summary>
        public int ActiveSpawnPointCount => spawnPoints.FindAll(sp => sp.IsActive).Count;
        
        /// <summary>
        /// 已生成敌人的生成点数量
        /// </summary>
        public int SpawnedEnemyCount => spawnPoints.FindAll(sp => sp.HasSpawnedEnemy).Count;
        
        #endregion
        
        #region Unity生命周期
        
        private void Start()
        {
            if (autoSpawnOnStart)
            {
                SpawnAllEnemies();
            }
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 注册生成点
        /// </summary>
        public void RegisterSpawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
            {
                spawnPoints.Add(spawnPoint);
                // 注册生成点
            }
        }
        
        /// <summary>
        /// 初始化注册表
        /// </summary>
        public void Initialize(EnemyPrefabRegistrySO registry)
        {
            enemyRegistry = registry;
        }
        
        /// <summary>
        /// 设置是否在Start时自动生成
        /// </summary>
        public void SetAutoSpawnOnStart(bool value)
        {
            autoSpawnOnStart = value;
        }
        
        /// <summary>
        /// 注销生成点
        /// </summary>
        public void UnregisterSpawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint != null && spawnPoints.Contains(spawnPoint))
            {
                spawnPoints.Remove(spawnPoint);
                // 注销生成点
            }
        }
        
        /// <summary>
        /// 生成所有敌人
        /// </summary>
        public void SpawnAllEnemies()
        {
            if (spawnDelay > 0)
            {
                StartCoroutine(SpawnAllEnemiesWithDelay());
            }
            else
            {
                SpawnAllEnemiesImmediate();
            }
        }
        
        /// <summary>
        /// 立即生成所有敌人
        /// </summary>
        public void SpawnAllEnemiesImmediate()
        {
            int attempt = 0;
            int success = 0;
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint.IsActive && !spawnPoint.HasSpawnedEnemy)
                {
                    attempt++;
                    var go = SpawnEnemyAtPoint(spawnPoint);
                    if (go != null) success++;
                }
            }
            
            Debug.Log($"[SpawnPointManager] 已尝试生成 {attempt}，成功 {success}，总点位 {spawnPoints.Count}");
            // 生成所有敌人完成
        }
        
        /// <summary>
        /// 带延迟的生成所有敌人
        /// </summary>
        private System.Collections.IEnumerator SpawnAllEnemiesWithDelay()
        {
            int spawnedCount = 0;
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint.IsActive && !spawnPoint.HasSpawnedEnemy)
                {
                    SpawnEnemyAtPoint(spawnPoint);
                    spawnedCount++;
                    
                    if (spawnedCount < spawnPoints.Count)
                    {
                        yield return new WaitForSeconds(spawnDelay);
                    }
                }
            }
            
            // 延迟生成所有敌人完成
        }
        
        /// <summary>
        /// 在指定生成点生成敌人
        /// </summary>
        public GameObject SpawnEnemyAtPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null || !spawnPoint.IsActive || spawnPoint.HasSpawnedEnemy)
            {
                return null;
            }
            
            GameObject prefab = GetEnemyPrefab(spawnPoint.SpawnData.enemyType);
            if (prefab == null)
            {
                Debug.LogWarning($"[SpawnPointManager] 找不到敌人预制体: {spawnPoint.SpawnData.enemyType}");
                return null;
            }
            
            var go = spawnPoint.SpawnEnemy(prefab);
            if (go != null)
            {
                Debug.Log($"[SpawnPointManager] 生成 {spawnPoint.SpawnData.enemyType} 于 {spawnPoint.transform.position}");
            }
            return go;
        }
        
        /// <summary>
        /// 清除所有已生成的敌人
        /// </summary>
        public void ClearAllSpawnedEnemies()
        {
            foreach (var spawnPoint in spawnPoints)
            {
                spawnPoint.ClearSpawnedEnemy();
            }
            
            // 清除所有已生成的敌人
        }
        
        /// <summary>
        /// 根据敌人类型获取生成点
        /// </summary>
        public List<SpawnPoint> GetSpawnPointsByEnemyType(EnemyType enemyType)
        {
            return spawnPoints.FindAll(sp => sp.SpawnData.enemyType == enemyType);
        }
        
        /// <summary>
        /// 根据生成类型获取生成点
        /// </summary>
        public List<SpawnPoint> GetSpawnPointsBySpawnType(SpawnType spawnType)
        {
            return spawnPoints.FindAll(sp => sp.SpawnData.type == spawnType);
        }
        
        /// <summary>
        /// 激活/停用所有生成点
        /// </summary>
        public void SetAllSpawnPointsActive(bool active)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                spawnPoint.SetActive(active);
            }
            
            // 设置所有生成点激活状态
        }
        
        /// <summary>
        /// 获取管理器统计信息
        /// </summary>
        public string GetStatistics()
        {
            int sharpshooters = GetSpawnPointsByEnemyType(EnemyType.TriangleSharpshooter).Count;
            int shieldbearers = GetSpawnPointsByEnemyType(EnemyType.TriangleShieldbearer).Count;
            int moths = GetSpawnPointsByEnemyType(EnemyType.TriangleMoth).Count;
            int bosses = GetSpawnPointsByEnemyType(EnemyType.CompositeGuardian).Count;
            int unassigned = GetSpawnPointsByEnemyType(EnemyType.None).Count;
            
            return $"生成点统计: 锐枪手={sharpshooters}, 盾卫={shieldbearers}, 飞蛾={moths}, Boss={bosses}, 未分配={unassigned}, 总计={spawnPoints.Count}";
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 根据敌人类型获取预制体
        /// </summary>
        private GameObject GetEnemyPrefab(EnemyType enemyType)
        {
            // 优先从注册表获取
            if (enemyRegistry != null)
            {
                var regPrefab = enemyRegistry.GetPrefab(enemyType);
                if (regPrefab != null) return regPrefab;
            }
            
            switch (enemyType)
            {
                case EnemyType.TriangleSharpshooter:
                    return sharpshooterPrefab;
                case EnemyType.TriangleShieldbearer:
                    return shieldbearerPrefab;
                case EnemyType.TriangleMoth:
                    return mothPrefab;
                case EnemyType.CompositeGuardian:
                    return bossPrefab;
                default:
                    return null;
            }
        }
        
        #endregion
        
        #region Editor方法
        
        [ContextMenu("生成所有敌人")]
        private void EditorSpawnAllEnemies()
        {
            SpawnAllEnemies();
        }
        
        [ContextMenu("清除所有敌人")]
        private void EditorClearAllEnemies()
        {
            ClearAllSpawnedEnemies();
        }
        
        [ContextMenu("显示统计信息")]
        private void EditorShowStatistics()
        {
            Debug.Log($"[SpawnPointManager] {GetStatistics()}");
        }
        
        #endregion
    }
}
