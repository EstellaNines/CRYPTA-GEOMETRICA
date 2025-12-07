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
        /// 可用的连续地面长度（仅地面类型有效）
        /// </summary>
        public int groundSpan;
        
        /// <summary>
        /// 距地面高度（仅空中类型有效）
        /// </summary>
        public int heightAboveGround;

        public SpawnPointV2(Vector2Int pos, SpawnType spawnType)
        {
            position = pos;
            type = spawnType;
            groundSpan = 0;
            heightAboveGround = 0;
        }

        public override string ToString()
        {
            return $"SpawnPoint({position}, {type})";
        }
    }
}
