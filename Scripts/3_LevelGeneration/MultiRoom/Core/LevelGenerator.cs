using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;
using CryptaGeometrica.InfiniteBackground.Core;
using CryptaGeometrica.Enemies;

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
        
        [TitleGroup("运行时配置", "Runtime Configuration", TitleAlignments.Centered)]
        [LabelText("运行时自动生成"), Tooltip("在运行时自动调用GenerateLevel()来确保背景主题正确切换")]
        public bool autoGenerateOnStart = true;
        
        [TitleGroup("敌人配置", "Enemy Prefabs", TitleAlignments.Centered)]
        [LabelText("敌人预制体注册表"), Tooltip("用于按 EnemyType 查找对应的敌人Prefab")] 
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public EnemyPrefabRegistrySO enemyRegistry;
        
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

        #region Unity 生命周期
        
        /// <summary>
        /// Unity Start 方法 - 在运行时自动生成关卡（如果启用）
        /// </summary>
        private void Start()
        {
            // 只在运行时且启用自动生成时执行
            if (Application.isPlaying && autoGenerateOnStart)
            {
                Debug.Log("[LevelGenerator] 运行时自动生成关卡已启用，正在生成关卡...");
                GenerateLevel();
                
                // 自动烘焙到Tilemap以应用主题
                Debug.Log("[LevelGenerator] 正在烘焙关卡到Tilemap...");
                BakeToTilemap();
            }
        }
        
        #endregion

        #region 公共方法 - 生成
        
        /// <summary>
        /// 生成关卡
        /// </summary>
        [Button("生成关卡", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        [TitleGroup("操作", "Actions", TitleAlignments.Centered)]
        public void GenerateLevel()
        {
            // 开始生成关卡
            
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
            
            // 生成详细的总览信息
            string levelOverview = GenerateLevelOverview();
            Debug.Log($"[LevelGenerator] 多房间生成已完成\n{levelOverview}");
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
                // 房间数量不足，无法生成走廊
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
            
            // 走廊生成完成
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
                // 墙壁层 Tilemap 未设置
                return;
            }
            
            // 开始烘焙关卡
            
            // 清空所有 Tilemap
            ClearAllTilemaps();
            
            // 烘焙每个房间
            foreach (var room in currentLevel.rooms)
            {
                BakeRoom(room);
            }
            
            // 烘焙走廊（统一处理重叠区域）
            if (currentLevel.corridors != null)
            {
                BakeCorridorsWithOverlapHandling(currentLevel.corridors);
            }
            
            // 生成详细的烘焙总览信息
            string bakeOverview = GenerateBakeOverview();
            Debug.Log($"[LevelGenerator] 烘焙完成\n{bakeOverview}");
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
            
            // 布局已保存
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
            
            // 布局已加载
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
                // 未设置摄像机边界碰撞器
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
            
            // 摄像机边界已更新
        }
        
        private void SelectTheme()
        {
            if (themeConfig != null && themeConfig.ThemeCount > 0)
            {
                currentTheme = themeConfig.GetTheme(random.Next(themeConfig.ThemeCount));
                hasTheme = true;
                
                // 使用MessageManager发送房间颜色主题消息给背景管理器
                SendRoomColorThemeMessage(currentTheme);
                Debug.Log($"[LevelGenerator] 已选择主题: {currentTheme.themeName}，并通过MessageManager发送房间颜色主题消息");
            }
            else
            {
                hasTheme = false;
                Debug.LogWarning("[LevelGenerator] 没有可用的主题配置");
            }
        }
        
        /// <summary>
        /// 发送房间颜色主题消息
        /// 根据当前主题生成对应的颜色主题消息并广播
        /// </summary>
        private void SendRoomColorThemeMessage(RoomThemeV2 theme)
        {
            if (string.IsNullOrEmpty(theme.themeName))
            {
                Debug.LogWarning("[LevelGenerator] 主题名称为空，无法发送颜色主题消息");
                return;
            }
            
            // 根据主题名称映射到颜色主题枚举
            RoomColorTheme colorTheme = MapThemeNameToColorTheme(theme.themeName);
            
            // 创建房间颜色主题消息数据
            var colorThemeData = new RoomColorThemeData(
                colorTheme,
                theme.infiniteBackgroundTexture, // 使用主题的无限背景纹理
                theme.themeColor,
                $"多房间关卡 - {theme.themeName}",
                1.0f // 默认过渡时间1秒
            );
            
            // 发送消息
            if (Application.isPlaying && MessageManager.Instance != null)
            {
                MessageManager.Instance.Send(MessageDefine.ROOM_COLOR_THEME_CHANGED, colorThemeData);
            }
            
            // 尝试直接查找并更新 InfiniteBackgroundManager (确保在编辑器/烘焙模式下也能生效)
            var bgManager = FindObjectOfType<InfiniteBackgroundManager>();
            if (bgManager != null)
            {
                bgManager.OnRoomColorThemeChanged(colorThemeData);
            }
            
            Debug.Log($"[LevelGenerator] 多房间主题: {theme.themeName} -> {colorTheme} ({colorThemeData.themeColor})");
        }
        
        /// <summary>
        /// 将主题名称映射到颜色主题枚举
        /// </summary>
        private RoomColorTheme MapThemeNameToColorTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
                return RoomColorTheme.Red;
                
            string lowerThemeName = themeName.ToLower();
            
            // 单字符映射（R=Red, B=Blue, Y=Yellow）
            if (lowerThemeName == "r")
                return RoomColorTheme.Red;
            else if (lowerThemeName == "b")
                return RoomColorTheme.Blue;
            else if (lowerThemeName == "y")
                return RoomColorTheme.Yellow;
            // Theme + 数字格式映射
            else if (lowerThemeName.Contains("theme") && lowerThemeName.Contains("0"))
                return RoomColorTheme.Red;
            else if (lowerThemeName.Contains("theme") && lowerThemeName.Contains("1"))
                return RoomColorTheme.Blue;
            else if (lowerThemeName.Contains("theme") && lowerThemeName.Contains("2"))
                return RoomColorTheme.Yellow;
            // 颜色名称映射
            else if (lowerThemeName.Contains("red") || lowerThemeName.Contains("红"))
                return RoomColorTheme.Red;
            else if (lowerThemeName.Contains("blue") || lowerThemeName.Contains("蓝"))
                return RoomColorTheme.Blue;
            else if (lowerThemeName.Contains("yellow") || lowerThemeName.Contains("黄"))
                return RoomColorTheme.Yellow;
            else
            {
                // 记录未识别的主题名称以便调试
                Debug.Log($"[LevelGenerator] 未识别的主题名称: '{themeName}'，使用默认红色主题");
                return RoomColorTheme.Red; // 默认红色主题
            }
        }
        
        /// <summary>
        /// 生成关卡总览信息
        /// </summary>
        private string GenerateLevelOverview()
        {
            if (currentLevel == null) return "无关卡数据";
            
            var overview = new System.Text.StringBuilder();
            overview.AppendLine("=== 多房间关卡总览 ===");
            overview.AppendLine($"主题: {(hasTheme ? currentTheme.themeName : "无主题")}");
            overview.AppendLine($"房间总数: {currentLevel.RoomCount}");
            overview.AppendLine($"走廊总数: {currentLevel.CorridorCount}");
            
            // 房间类型统计
            int entranceCount = 0, combatCount = 0, bossCount = 0;
            int totalSpawnPoints = 0;
            
            foreach (var room in currentLevel.rooms)
            {
                switch (room.roomType)
                {
                    case RoomType.Entrance: entranceCount++; break;
                    case RoomType.Combat: combatCount++; break;
                    case RoomType.Boss: bossCount++; break;
                }
                
                if (room.roomData != null && room.roomType != RoomType.Entrance)
                {
                    totalSpawnPoints += room.roomData.potentialSpawns.Count;
                }
            }
            
            overview.AppendLine($"房间分布: 入口×{entranceCount}, 战斗×{combatCount}, Boss×{bossCount}");
            overview.AppendLine($"刷怪点总数: {totalSpawnPoints}");
            
            // 关卡尺寸
            var bounds = currentLevel.TotalBounds;
            overview.AppendLine($"关卡尺寸: {bounds.width}×{bounds.height} (位置: {bounds.x}, {bounds.y})");
            
            return overview.ToString();
        }
        
        /// <summary>
        /// 生成烘焙总览信息
        /// </summary>
        private string GenerateBakeOverview()
        {
            if (currentLevel == null) return "无关卡数据";
            
            var overview = new System.Text.StringBuilder();
            overview.AppendLine("=== 烘焙完成总览 ===");
            
            // 基本统计
            int totalRooms = currentLevel.RoomCount;
            int totalCorridors = currentLevel.CorridorCount;
            int totalSpawnPoints = 0;
            
            // 房间类型和刷怪点统计
            int entranceRooms = 0, combatRooms = 0, bossRooms = 0;
            int shieldbearers = 0, sharpshooters = 0, moths = 0, bosses = 0;
            
            foreach (var room in currentLevel.rooms)
            {
                switch (room.roomType)
                {
                    case RoomType.Entrance: entranceRooms++; break;
                    case RoomType.Combat: combatRooms++; break;
                    case RoomType.Boss: bossRooms++; break;
                }
                
                if (room.roomData != null && room.roomData.potentialSpawns != null)
                {
                    foreach (var spawn in room.roomData.potentialSpawns)
                    {
                        totalSpawnPoints++;
                        switch (spawn.enemyType)
                        {
                            case EnemyType.TriangleShieldbearer: shieldbearers++; break;
                            case EnemyType.TriangleSharpshooter: sharpshooters++; break;
                            case EnemyType.TriangleMoth: moths++; break;
                            case EnemyType.None: bosses++; break;
                        }
                    }
                }
            }
            
            overview.AppendLine($"房间烘焙: {totalRooms}个 (入口×{entranceRooms}, 战斗×{combatRooms}, Boss×{bossRooms})");
            overview.AppendLine($"走廊烘焙: {totalCorridors}条");
            overview.AppendLine($"刷怪点烘焙: {totalSpawnPoints}个");
            overview.AppendLine($"敌人分布: 盾卫×{shieldbearers}, 锐枪手×{sharpshooters}, 飞蛾×{moths}, Boss×{bosses}");
            overview.AppendLine($"主题应用: {(hasTheme ? currentTheme.themeName : "无主题")}");
            
            return overview.ToString();
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
            
            // 烘焙生成点GameObject
            BakeRoomSpawnPoints(room);
        }
        
        /// <summary>
        /// 烘焙房间生成点GameObject
        /// </summary>
        private void BakeRoomSpawnPoints(PlacedRoom room)
        {
            if (room == null || room.roomData == null || room.roomData.potentialSpawns == null) return;
            
            // 清理旧的生成点
            Transform existingManager = transform.Find($"Room_{room.id}_SpawnPointManager");
            if (existingManager != null)
            {
                DestroyImmediate(existingManager.gameObject);
            }
            
            // 入口房间不生成怪物生成点
            if (room.roomType == RoomType.Entrance)
            {
                // 入口房间跳过生成点烘焙
                return;
            }
            
            // 创建房间专用的生成点管理器
            GameObject spawnManager = new GameObject($"Room_{room.id}_SpawnPointManager");
            spawnManager.transform.SetParent(transform);
            spawnManager.transform.localPosition = Vector3.zero;
            
            // 添加管理器组件
            var managerComponent = spawnManager.AddComponent<CryptaGeometrica.LevelGeneration.SmallRoomV2.SpawnPointManager>();
            // 注入敌人注册表并关闭Start自动生成
            if (enemyRegistry != null)
            {
                managerComponent.Initialize(enemyRegistry);
            }
            managerComponent.SetAutoSpawnOnStart(false);
            
            var offset = room.worldPosition;
            
            // 为每个生成点创建GameObject
            for (int i = 0; i < room.roomData.potentialSpawns.Count; i++)
            {
                var spawn = room.roomData.potentialSpawns[i];
                
                // 创建生成点GameObject
                GameObject spawnObj = new GameObject($"Room_{room.id}_SpawnPoint_{i}_{spawn.enemyType}");
                spawnObj.transform.SetParent(spawnManager.transform);
                
                // 设置世界位置（考虑房间偏移）
                Vector3 worldPos = wallTilemap.CellToWorld(new Vector3Int(offset.x + spawn.position.x, offset.y + spawn.position.y, 0));
                spawnObj.transform.position = worldPos + new Vector3(0.5f, 0.5f, 0);
                
                // 添加生成点组件
                var spawnComponent = spawnObj.AddComponent<CryptaGeometrica.LevelGeneration.SmallRoomV2.SpawnPoint>();
                spawnComponent.Initialize(spawn);
                
                // 注册到管理器
                managerComponent.RegisterSpawnPoint(spawnComponent);
            }
            
            // 清理旧的触发器
            Transform existingTrigger = transform.Find($"Room_{room.id}_Trigger");
            if (existingTrigger != null)
            {
                DestroyImmediate(existingTrigger.gameObject);
            }
            
            // 创建房间进入触发器（一次性刷怪）
            var triggerObj = new GameObject($"Room_{room.id}_Trigger");
            triggerObj.transform.SetParent(transform);
            
            // 计算房间世界中心与尺寸（单位：格=米）
            var bounds = room.WorldBounds;
            Vector3 worldMin = wallTilemap.CellToWorld(new Vector3Int(bounds.x, bounds.y, 0));
            Vector3 worldCenter = worldMin + new Vector3(bounds.width * 0.5f, bounds.height * 0.5f, 0f);
            triggerObj.transform.position = worldCenter;
            
            var box = triggerObj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(bounds.width, bounds.height);
            
            var activation = triggerObj.AddComponent<RoomActivationTrigger>();
            activation.spawnManager = managerComponent;
            activation.clearOnExit = false;
            
            // 房间生成点烘焙完成
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
        /// 烘焙多个走廊并处理重叠区域
        /// </summary>
        private void BakeCorridorsWithOverlapHandling(List<CorridorData> corridors)
        {
            if (corridors == null || corridors.Count == 0 || !hasTheme) return;
            
            // 收集所有走廊的瓦片
            var allTiles = new Dictionary<Vector2Int, TileType>();
            
            foreach (var corridor in corridors)
            {
                foreach (var (pos, tileType) in corridor.GetTiles())
                {
                    // 重叠区域优先级：Floor > Platform > Wall
                    // 确保通行空间不被墙壁阻塞
                    if (!allTiles.ContainsKey(pos))
                    {
                        allTiles[pos] = tileType;
                    }
                    else
                    {
                        var existingType = allTiles[pos];
                        var newType = tileType;
                        
                        // 应用优先级规则：Floor（空气）优先级最高，确保通行性
                        if (newType == TileType.Floor || existingType == TileType.Wall)
                        {
                            allTiles[pos] = newType;
                        }
                        else if (newType == TileType.Platform && existingType != TileType.Floor)
                        {
                            allTiles[pos] = newType;
                        }
                        // Wall 优先级最低，只有在位置为空时才放置
                    }
                }
            }
            
            // 统一烘焙所有瓦片
            foreach (var kvp in allTiles)
            {
                Vector3Int tilePos = new Vector3Int(kvp.Key.x, kvp.Key.y, 0);
                BakeTile(tilePos, kvp.Value);
            }
            
            Debug.Log($"[LevelGenerator] 走廊烘焙完成，处理了 {allTiles.Count} 个瓦片位置，包含重叠区域优化");
        }
        
        /// <summary>
        /// 烘焙单个走廊（已弃用，使用 BakeCorridorsWithOverlapHandling 代替）
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
            
            // 走廊烘焙完成
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
