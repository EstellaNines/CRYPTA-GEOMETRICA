using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;

namespace CryptaGeometrica.LevelGeneration.MultiRoom
{
    /// <summary>
    /// 关卡生成器
    /// 负责生成多房间关卡，包括房间放置、走廊连接和烘焙
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        #region 字段
        
        [TitleGroup("生成参数", "Generation Parameters", TitleAlignments.Centered)]
        [HideLabel, InlineProperty]
        public LevelGeneratorParams parameters = new LevelGeneratorParams();
        
        [TitleGroup("Tilemap 配置", "Tilemap Configuration", TitleAlignments.Centered)]
        [LabelText("墙壁层 Tilemap"), Tooltip("用于放置墙壁和地面")]
        public Tilemap wallTilemap;
        
        [TitleGroup("Tilemap 配置")]
        [LabelText("平台层 Tilemap"), Tooltip("用于放置单向平台")]
        public Tilemap platformTilemap;
        
        [TitleGroup("Tilemap 配置")]
        [LabelText("门层 Tilemap"), Tooltip("用于Boss房间的门")]
        public Tilemap doorTilemap;
        
        [TitleGroup("视觉主题", "Visual Themes", TitleAlignments.Centered)]
        [LabelText("主题配置"), Tooltip("引用房间主题配置 SO 文件")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public RoomThemeConfigSO themeConfig;
        
        [TitleGroup("布局配置", "Layout Configuration", TitleAlignments.Centered)]
        [LabelText("当前布局")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public LevelLayoutSO currentLayoutSO;
        
        [TitleGroup("摄像机配置", "Camera Configuration", TitleAlignments.Centered)]
        [LabelText("摄像机边界碰撞器"), Tooltip("用于限制虚拟摄像机移动范围的 PolygonCollider2D")]
        public PolygonCollider2D cameraBoundsCollider;
        
        [TitleGroup("摄像机配置")]
        [LabelText("边界边距"), Range(0, 20), Tooltip("摄像机边界相对于关卡边界的额外边距")]
        public int cameraBoundsPadding = 5;
        
        // 当前关卡数据
        private LevelData currentLevel;
        
        // 当前选中的主题
        private RoomThemeV2 currentTheme;
        
        // 是否已选择主题
        private bool hasTheme;
        
        // 随机数生成器
        private System.Random random;
        
        // 子模块
        private RoomSeedPool seedPool;
        private LinearRoomPlacer roomPlacer;
        private LCorridorBuilder corridorBuilder;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 获取当前关卡数据
        /// </summary>
        public LevelData CurrentLevel => currentLevel;
        
        /// <summary>
        /// 获取当前主题
        /// </summary>
        public RoomThemeV2 CurrentTheme => currentTheme;
        
        /// <summary>
        /// 关卡是否已生成
        /// </summary>
        public bool IsGenerated => currentLevel != null && currentLevel.RoomCount > 0;
        
        #endregion

        #region 公共方法 - 生成
        
        /// <summary>
        /// 生成关卡
        /// </summary>
        [Button("生成关卡", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        [TitleGroup("操作", "Actions", TitleAlignments.Centered)]
        public void GenerateLevel()
        {
            Debug.Log("[LevelGenerator] 开始生成关卡...");
            
            // 验证参数
            parameters.Validate();
            
            // 初始化种子
            InitializeSeed();
            
            // 选择主题
            SelectTheme();
            
            // 初始化子模块
            InitializeModules();
            
            // 生成种子池
            seedPool.GenerateSeedPool(parameters.combatRoomCount);
            
            // 放置房间
            currentLevel = roomPlacer.PlaceRooms(seedPool);
            
            // 生成走廊
            GenerateCorridors();
            
            // 更新摄像机边界
            UpdateCameraBounds();
            
            Debug.Log($"[LevelGenerator] 关卡生成完成: {currentLevel}");
        }
        
        /// <summary>
        /// 生成走廊
        /// </summary>
        [Button("生成走廊", ButtonSizes.Medium), GUIColor(0.6f, 0.8f, 0.6f)]
        [TitleGroup("操作")]
        public void GenerateCorridors()
        {
            if (currentLevel == null || currentLevel.RoomCount < 2)
            {
                Debug.LogWarning("[LevelGenerator] 房间数量不足，无法生成走廊");
                return;
            }
            
            // 初始化走廊生成器
            if (corridorBuilder == null)
            {
                corridorBuilder = new LCorridorBuilder(parameters, random);
            }
            
            // 生成走廊
            var corridors = corridorBuilder.BuildCorridors(currentLevel);
            currentLevel.SetCorridors(corridors);
            
            Debug.Log($"[LevelGenerator] 走廊生成完成: {currentLevel.CorridorCount} 条走廊");
        }
        
        #endregion

        #region 公共方法 - 烘焙
        
        /// <summary>
        /// 烘焙到 Tilemap
        /// </summary>
        [Button("烘焙到 Tilemap", ButtonSizes.Large), GUIColor(0.8f, 0.6f, 0.2f)]
        [TitleGroup("操作")]
        public void BakeToTilemap()
        {
            if (currentLevel == null || currentLevel.RoomCount == 0)
            {
                Debug.LogWarning("[LevelGenerator] 没有可用的关卡数据");
                return;
            }
            
            if (wallTilemap == null)
            {
                Debug.LogError("[LevelGenerator] 墙壁层 Tilemap 未设置");
                return;
            }
            
            Debug.Log("[LevelGenerator] 开始烘焙关卡...");
            
            // 清空所有 Tilemap
            ClearAllTilemaps();
            
            // 烘焙每个房间
            foreach (var room in currentLevel.rooms)
            {
                BakeRoom(room);
            }
            
            // 烘焙走廊
            if (currentLevel.corridors != null)
            {
                foreach (var corridor in currentLevel.corridors)
                {
                    BakeCorridor(corridor);
                }
            }
            
            Debug.Log("[LevelGenerator] 烘焙完成");
        }
        
        /// <summary>
        /// 清空所有 Tilemap
        /// </summary>
        [Button("清空 Tilemap", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
        [TitleGroup("操作")]
        public void ClearAllTilemaps()
        {
            wallTilemap?.ClearAllTiles();
            platformTilemap?.ClearAllTiles();
            doorTilemap?.ClearAllTiles();
        }
        
        #endregion

        #region 公共方法 - 布局保存/加载
        
        /// <summary>
        /// 保存当前布局到 ScriptableObject
        /// </summary>
        [Button("保存布局", ButtonSizes.Medium), GUIColor(0.5f, 0.7f, 1f)]
        [TitleGroup("操作")]
        public void SaveLayout()
        {
            if (currentLevel == null || currentLevel.RoomCount == 0)
            {
                Debug.LogWarning("[LevelGenerator] 没有可用的关卡数据");
                return;
            }
            
            if (currentLayoutSO == null)
            {
                Debug.LogWarning("[LevelGenerator] 请先指定布局配置文件");
                return;
            }
            
            currentLayoutSO.SaveFromLevelData(currentLevel, parameters);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentLayoutSO);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            
            Debug.Log($"[LevelGenerator] 布局已保存到: {currentLayoutSO.name}");
        }
        
        /// <summary>
        /// 从 ScriptableObject 加载布局
        /// </summary>
        [Button("加载布局", ButtonSizes.Medium), GUIColor(0.5f, 1f, 0.7f)]
        [TitleGroup("操作")]
        public void LoadLayout()
        {
            if (currentLayoutSO == null)
            {
                Debug.LogWarning("[LevelGenerator] 请先指定布局配置文件");
                return;
            }
            
            // 加载布局数据
            currentLevel = currentLayoutSO.LoadToLevelData();
            parameters = currentLayoutSO.generatorParams?.Clone() ?? new LevelGeneratorParams();
            
            // 初始化模块
            InitializeSeed();
            InitializeModules();
            
            // 重新生成房间数据
            roomPlacer.RegenerateAllRooms(currentLevel);
            
            // 重新生成走廊
            GenerateCorridors();
            
            Debug.Log($"[LevelGenerator] 布局已加载: {currentLayoutSO.name}");
        }
        
        #endregion

        #region 公共方法 - 房间操作
        
        /// <summary>
        /// 更新房间位置（用于编辑器拖拽）
        /// </summary>
        public void UpdateRoomPosition(int roomId, Vector2Int newPosition)
        {
            if (currentLevel == null) return;
            
            currentLevel.UpdateRoomPosition(roomId, newPosition);
            
            // 重新生成走廊
            GenerateCorridors();
        }
        
        /// <summary>
        /// 检测房间重叠
        /// </summary>
        public List<(int roomA, int roomB)> GetOverlappingRooms()
        {
            if (currentLevel == null) return new List<(int, int)>();
            return currentLevel.GetOverlappingRooms(0);
        }
        
        /// <summary>
        /// 检测指定房间是否重叠
        /// </summary>
        public bool IsRoomOverlapping(int roomId)
        {
            if (currentLevel == null) return false;
            return currentLevel.IsRoomOverlapping(roomId, 0);
        }
        
        #endregion

        #region 私有方法 - 初始化
        
        private void InitializeSeed()
        {
            if (parameters.useRandomSeed)
            {
                parameters.levelSeed = DateTime.Now.Ticks.ToString();
            }
            
            random = new System.Random(parameters.levelSeed.GetHashCode());
            UnityEngine.Random.InitState(parameters.levelSeed.GetHashCode());
        }
        
        private void InitializeModules()
        {
            // 初始化种子池
            seedPool = new RoomSeedPool(parameters.combatRoomParams, false);
            
            // 初始化房间放置器
            roomPlacer = new LinearRoomPlacer(parameters, random);
            
            // 初始化走廊生成器
            corridorBuilder = new LCorridorBuilder(parameters, random);
        }
        
        /// <summary>
        /// 更新摄像机边界碰撞器
        /// </summary>
        [Button("更新摄像机边界", ButtonSizes.Medium)]
        [TitleGroup("操作")]
        public void UpdateCameraBounds()
        {
            if (cameraBoundsCollider == null)
            {
                Debug.LogWarning("[LevelGenerator] 未设置摄像机边界碰撞器");
                return;
            }
            
            if (currentLevel == null || currentLevel.RoomCount == 0)
            {
                Debug.LogWarning("[LevelGenerator] 没有可用的关卡数据");
                return;
            }
            
            var bounds = currentLevel.TotalBounds;
            
            // 计算带边距的边界
            float minX = bounds.x - cameraBoundsPadding;
            float maxX = bounds.xMax + cameraBoundsPadding;
            float minY = bounds.y - cameraBoundsPadding;
            float maxY = bounds.yMax + cameraBoundsPadding;
            
            // 设置 PolygonCollider2D 的点（矩形）
            Vector2[] points = new Vector2[]
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            };
            
            cameraBoundsCollider.SetPath(0, points);
            
            Debug.Log($"[LevelGenerator] 摄像机边界已更新: ({minX}, {minY}) - ({maxX}, {maxY})");
        }
        
        private void SelectTheme()
        {
            if (themeConfig != null && themeConfig.ThemeCount > 0)
            {
                currentTheme = themeConfig.GetTheme(random.Next(themeConfig.ThemeCount));
                hasTheme = true;
            }
            else
            {
                hasTheme = false;
            }
        }
        
        #endregion

        #region 私有方法 - 烘焙
        
        /// <summary>
        /// 烘焙单个房间
        /// </summary>
        private void BakeRoom(PlacedRoom room)
        {
            if (room == null || room.roomData == null) return;
            
            var roomData = room.roomData;
            var offset = room.worldPosition;
            
            // 烘焙房间瓦片
            for (int x = 0; x < roomData.width; x++)
            {
                for (int y = 0; y < roomData.height; y++)
                {
                    Vector3Int tilePos = new Vector3Int(offset.x + x, offset.y + y, 0);
                    TileType tileType = roomData.GetTile(x, y);
                    
                    BakeTile(tilePos, tileType);
                }
            }
            
            // 烘焙Boss房间的门
            if (room.roomType == RoomType.Boss && roomData.needsDoorAtExit)
            {
                BakeBossDoor(room);
            }
        }
        
        /// <summary>
        /// 烘焙单个瓦片
        /// </summary>
        private void BakeTile(Vector3Int pos, TileType tileType)
        {
            if (!hasTheme) return;
            
            switch (tileType)
            {
                case TileType.Wall:
                    if (currentTheme.wallTile != null)
                    {
                        wallTilemap.SetTile(pos, currentTheme.wallTile);
                    }
                    break;
                    
                case TileType.Floor:
                    // 地面不放置瓦片（透明）
                    break;
                    
                case TileType.Platform:
                    if (platformTilemap != null && currentTheme.platformTile != null)
                    {
                        platformTilemap.SetTile(pos, currentTheme.platformTile);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 烘焙单个走廊
        /// </summary>
        private void BakeCorridor(CorridorData corridor)
        {
            if (corridor == null || !hasTheme) return;
            
            // 获取走廊所有瓦片并烘焙
            foreach (var (pos, tileType) in corridor.GetTiles())
            {
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                BakeTile(tilePos, tileType);
            }
            
            Debug.Log($"[LevelGenerator] 烘焙走廊 #{corridor.id}");
        }
        
        /// <summary>
        /// 烘焙Boss房间的门
        /// </summary>
        private void BakeBossDoor(PlacedRoom bossRoom)
        {
            if (doorTilemap == null || !hasTheme) return;
            
            var roomData = bossRoom.roomData;
            var offset = bossRoom.worldPosition;
            
            // 在出口位置放置门
            Vector2Int exitPos = roomData.endPos;
            
            // 门的高度（3格）
            for (int dy = 0; dy < 3; dy++)
            {
                Vector3Int doorPos = new Vector3Int(
                    offset.x + exitPos.x,
                    offset.y + exitPos.y + dy,
                    0
                );
                
                if (currentTheme.wallTile != null)
                {
                    doorTilemap.SetTile(doorPos, currentTheme.wallTile);
                }
            }
        }
        
        #endregion

        #region Unity 生命周期
        
        private void OnValidate()
        {
            parameters?.Validate();
        }
        
        #endregion
    }
}
