using System;
using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.Enemies
{
    /// <summary>
    /// 敌人预制体注册表：维护 EnemyType 到 Prefab 的映射
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPrefabRegistry", menuName = "自制工具/敌人/Prefab Registry")]
    public class EnemyPrefabRegistrySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public EnemyType enemyType;
            public GameObject prefab;
        }

        [Header("映射配置")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 按敌人类型获取对应的预制体
        /// </summary>
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
    }
}
