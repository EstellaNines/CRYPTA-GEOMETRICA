using System;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 敌人生成点数据
    /// </summary>
    [Serializable]
    public struct SpawnPointV2
    {
        /// <summary>
        /// 生成点位置（网格坐标）
        /// </summary>
        public Vector2Int position;
        
        /// <summary>
        /// 生成点类型（地面/空中）
        /// </summary>
        public SpawnType type;
        
        /// <summary>
        /// 分配的敌人类型
        /// </summary>
        public EnemyType enemyType;
        
        /// <summary>
        /// 可用的连续地面长度（仅地面类型有效）
        /// </summary>
        public int groundSpan;
        
        /// <summary>
        /// 距地面高度（仅空中类型有效）
        /// </summary>
        public int heightAboveGround;
        
        /// <summary>
        /// 生成点是否通过验证
        /// </summary>
        public bool isValid;
        
        /// <summary>
        /// 验证失败原因（用于调试）
        /// </summary>
        public string invalidReason;

        public SpawnPointV2(Vector2Int pos, SpawnType spawnType)
        {
            position = pos;
            type = spawnType;
            enemyType = EnemyType.None;
            groundSpan = 0;
            heightAboveGround = 0;
            isValid = true;
            invalidReason = string.Empty;
        }
        
        public SpawnPointV2(Vector2Int pos, SpawnType spawnType, EnemyType enemy)
        {
            position = pos;
            type = spawnType;
            enemyType = enemy;
            groundSpan = 0;
            heightAboveGround = 0;
            isValid = true;
            invalidReason = string.Empty;
        }

        public override string ToString()
        {
            return $"SpawnPoint({position}, {type}, {enemyType})";
        }
    }
}
