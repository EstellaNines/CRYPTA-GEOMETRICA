using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.SmallRoom
{
    public enum TileType
    {
        Wall = 0,   // 墙壁（实心）
        Floor = 1,  // 地面（可行走）
        Platform = 2 // 单向平台
    }

    public enum SpawnType
    {
        Ground, // 适合锐枪手、盾卫
        Air     // 适合飞蛾
    }

    [Serializable]
    public struct SpawnPoint
    {
        public Vector2Int position;
        public SpawnType type;
    }

    [Serializable]
    public class RoomGenParams
    {
        [Header("Basic Settings")]
        [LabelText("房间宽度")]
        public int roomWidth = 20;
        [LabelText("房间高度")]
        public int roomHeight = 15;
        [LabelText("随机种子")]
        public string seed = "";
        [LabelText("使用随机种子")]
        public bool useRandomSeed = true;

        [Header("Anchors Settings")]
        [LabelText("强制生成锚点")]
        public bool enforceAnchors = true;
        [LabelText("左侧入口Y坐标"), Tooltip("-1 表示随机")]
        public int entranceY = 5; // -1 for random
        [LabelText("右侧出口Y坐标"), Tooltip("-1 表示随机")]
        public int exitY = 5;     // -1 for random

        [Header("Walker Settings")]
        [LabelText("最大游走步数")]
        public int maxSteps = 100;
        [LabelText("通道宽度"), Tooltip("玩家占据2格宽，建议设为2")]
        public int pathWidth = 2;
        
        [LabelText("生成小厅概率"), Range(0f, 1f)]
        public float roomSpawnChance = 0.1f;
        [LabelText("小厅最小尺寸")]
        public int minRoomSize = 3;
        [LabelText("小厅最大尺寸")]
        public int maxRoomSize = 5;

        [LabelText("矿工数量")]
        public int walkerCount = 1;
        [LabelText("转向概率"), Range(0f, 1f), Tooltip("低=长走廊，高=方形区域")]
        public float turnProbability = 0.2f;
        [LabelText("允许对角线移动")]
        public bool allowDiagonal = false;

        [Header("Rules Settings")]
        [LabelText("移除孤立墙壁")]
        public bool removeSingleWalls = true;
        [LabelText("最大平台数量")]
        public int maxPlatforms = 4;
        [LabelText("平台最小宽度")]
        public int platformWidthMin = 3;
        [LabelText("平台最大宽度")]
        public int platformWidthMax = 5;
        
        [LabelText("最小平台尺寸")] // Keep specifically for logic check, maybe deprecate
        public int minPlatformSize = 2;
        [LabelText("边缘留空")]
        public int edgePadding = 1;

        [Header("Spawn Analysis")]
        [LabelText("最小地面连续长度"), Tooltip("用于判定地面敌人生成点")]
        public int minGroundSpan = 3;
        [LabelText("最小空中高度"), Tooltip("用于判定空中敌人生成点")]
        public int minAirHeight = 3;
        
        [Header("Spawning Constraints")]
        [LabelText("最大怪物数量")]
        public int maxEnemies = 4;
        [LabelText("怪物最小间距")]
        public int minSpawnDistance = 5;
        
        [Header("Structure")]
        [LabelText("初始大洞数量"), Tooltip("先挖几个大洞以减少墙壁占比")]
        public int initialHolesCount = 2;

        [LabelText("目标开阔度"), Range(0.1f, 0.8f), Tooltip("期望的地面/空间占比，越高墙壁越少")]
        public float targetOpenness = 0.4f;
    }
}
