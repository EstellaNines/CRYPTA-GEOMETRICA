using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.Enemies
{
    /// <summary>
    /// 敌人预制体注册表：维护 EnemyType 到 Prefab 的映射
    /// 用于关卡生成时根据敌人类型查找对应的预制体
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPrefabRegistry", menuName = "自制工具/敌人/Prefab Registry")]
    public class EnemyPrefabRegistrySO : ScriptableObject
    {
        /// <summary>
        /// 敌人类型与预制体的映射条目
        /// </summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("敌人类型")]
            public EnemyType enemyType;
            
            [Tooltip("对应的敌人预制体")]
            public GameObject prefab;
        }

        [Header("敌人预制体映射配置")]
        [Tooltip("配置每种敌人类型对应的预制体")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 按敌人类型获取对应的预制体
        /// </summary>
        /// <param name="type">敌人类型</param>
        /// <returns>对应的预制体，未找到返回null</returns>
        public GameObject GetPrefab(EnemyType type)
        {
            if (entries == null) return null;
            
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.enemyType == type && e.prefab != null)
                    return e.prefab;
            }
            return null;
        }

        /// <summary>
        /// 检查是否包含指定类型的预制体
        /// </summary>
        public bool HasPrefab(EnemyType type)
        {
            return GetPrefab(type) != null;
        }

        /// <summary>
        /// 获取所有已配置的敌人类型
        /// </summary>
        public List<EnemyType> GetConfiguredTypes()
        {
            var types = new List<EnemyType>();
            if (entries == null) return types;
            
            foreach (var e in entries)
            {
                if (e != null && e.prefab != null && !types.Contains(e.enemyType))
                {
                    types.Add(e.enemyType);
                }
            }
            return types;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器：验证配置完整性
        /// </summary>
        [ContextMenu("验证配置")]
        private void ValidateEntries()
        {
            var allTypes = new[] { 
                EnemyType.TriangleSharpshooter, 
                EnemyType.TriangleShieldbearer, 
                EnemyType.TriangleMoth, 
                EnemyType.CompositeGuardian 
            };
            
            foreach (var type in allTypes)
            {
                if (!HasPrefab(type))
                {
                    Debug.LogWarning($"[EnemyPrefabRegistry] 缺少敌人类型配置: {type}");
                }
            }
            Debug.Log($"[EnemyPrefabRegistry] 验证完成，已配置 {GetConfiguredTypes().Count}/{allTypes.Length} 种敌人");
        }
#endif
    }
}
