using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间生成器 v0.2
    /// 使用 BSP 空间分割 + 图连接 + 双向随机游走 + 跳跃可达性分析
    /// </summary>
    public class RoomGeneratorV2 : MonoBehaviour
    {
        #region 字段
        
        [TitleGroup("生成参数", "Generation Parameters", TitleAlignments.Centered)]
        [HideLabel, InlineProperty]
        public RoomGenParamsV2 parameters;
        
        [TitleGroup("Tilemap 配置", "Tilemap Configuration", TitleAlignments.Centered)]
        [LabelText("墙壁层 Tilemap"), Tooltip("用于放置墙壁和地面")]
        public Tilemap targetTilemap;
        
        [TitleGroup("Tilemap 配置")]
        [LabelText("平台层 Tilemap"), Tooltip("用于放置单向平台")]
        public Tilemap platformTilemap;
        
        [TitleGroup("Tilemap 配置")]
        [LabelText("门层 Tilemap"), Tooltip("用于Boss房间的门（Boss击杀后消失）")]
        public Tilemap doorTilemap;
        
        [TitleGroup("视觉主题", "Visual Themes", TitleAlignments.Centered)]
        [LabelText("主题列表")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "themeName")]
        public List<RoomThemeV2> themes;
        
        // 当前生成的房间数据
        private RoomDataV2 currentRoom;
        
        // 当前选中的主题
        private RoomThemeV2 currentTheme;
        
        // 随机数生成器
        private System.Random random;
        
        // 子模块
        private BSPGenerator bspGenerator;
        private RoomPlacer roomPlacer;
        private CorridorBuilder corridorBuilder;
        private ConnectivityGuarantor connectivityGuarantor;
        private PlatformInjector platformInjector;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 获取当前生成的房间数据
        /// </summary>
        public RoomDataV2 CurrentRoom => currentRoom;
        
        /// <summary>
        /// 获取当前主题
        /// </summary>
        public RoomThemeV2 CurrentTheme => currentTheme;
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 生成房间（完整流程）
        /// </summary>
        [Button("生成房间", ButtonSizes.Large)]
        [TitleGroup("操作")]
        public void GenerateRoom()
        {
            if (parameters == null)
            {
                parameters = new RoomGenParamsV2();
            }
            
            parameters.Validate();
            
            // Phase 1: 初始化
            InitializeSeed();
            InitializeGrid();
            SelectTheme();
            
            // 根据房间类型选择生成流程
            switch (parameters.roomType)
            {
                case MultiRoom.RoomType.Entrance:
                    GenerateEntranceRoom();
                    break;
                    
                case MultiRoom.RoomType.Boss:
                    GenerateBossRoom();
                    break;
                    
                case MultiRoom.RoomType.Combat:
                default:
                    GenerateStandardRoom();
                    break;
            }
            
            // 重建地面列表
            currentRoom.RebuildFloorTiles();
            
            Debug.Log($"[RoomGeneratorV2] 房间生成完成: {currentRoom} (类型: {parameters.roomType})");
        }
        
        /// <summary>
        /// 烘焙到 Tilemap
        /// </summary>
        [Button("烘焙到 Tilemap", ButtonSizes.Large)]
        [TitleGroup("操作")]
        public void BakeToTilemap()
        {
            if (targetTilemap == null || currentRoom == null)
            {
                // 无法烘焙：Tilemap 或房间数据为空
                return;
            }
            
            // 清空 Tilemap
            targetTilemap.ClearAllTiles();
            if (platformTilemap != null)
            {
                platformTilemap.ClearAllTiles();
            }
            if (doorTilemap != null)
            {
                doorTilemap.ClearAllTiles();
            }
            
            // 烘焙填充：在房间数据外围额外画几圈墙
            int bakePadding = 3;
            
            for (int x = -bakePadding; x < currentRoom.width + bakePadding; x++)
            {
                for (int y = -bakePadding; y < currentRoom.height + bakePadding; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    
                    if (currentRoom.IsValid(x, y))
                    {
                        BakeTileAt(x, y, tilePos);
                    }
                    else
                    {
                        // Padding 区域处理
                        BakePaddingAt(x, y, tilePos);
                    }
                }
            }
            
            // 烘焙生成点可视化
            BakeSpawnPoints();
            
            // 烘焙Boss房间的门
            BakeBossDoor();
            
            // 广播消息
            BroadcastRoomAnchors();
            
            // 烘焙完成
        }
        
        /// <summary>
        /// 设置房间数据（用于编辑器预览）
        /// </summary>
        public void SetRoomData(RoomDataV2 data)
        {
            currentRoom = data;
        }
        
        /// <summary>
        /// 强制选择主题
        /// </summary>
        public void ForcePickTheme()
        {
            SelectTheme();
        }
        
        #endregion

        #region 初始化
        
        private void InitializeSeed()
        {
            if (parameters.useRandomSeed)
            {
                parameters.seed = System.DateTime.Now.Ticks.ToString();
            }
            
            random = new System.Random(parameters.seed.GetHashCode());
            Random.InitState(parameters.seed.GetHashCode());
        }
        
        private void InitializeGrid()
        {
            currentRoom = new RoomDataV2(parameters.roomWidth, parameters.roomHeight);
            currentRoom.seed = parameters.seed;
            
            // 初始化为全墙壁
            currentRoom.Fill(TileType.Wall);
            
            // 确定出入口位置
            int entY = parameters.entranceY == -1 
                ? random.Next(parameters.edgePadding + 2, parameters.roomHeight - parameters.edgePadding - 3)
                : parameters.entranceY;
            
            int extY = parameters.exitY == -1
                ? random.Next(parameters.edgePadding + 2, parameters.roomHeight - parameters.edgePadding - 3)
                : parameters.exitY;
            
            // 确保出入口有足够空间（玩家 2×2 + 1 格余量）
            entY = Mathf.Clamp(entY, parameters.edgePadding + 2, parameters.roomHeight - parameters.edgePadding - 4);
            extY = Mathf.Clamp(extY, parameters.edgePadding + 2, parameters.roomHeight - parameters.edgePadding - 4);
            
            currentRoom.startPos = new Vector2Int(0, entY);
            currentRoom.endPos = new Vector2Int(parameters.roomWidth - 1, extY);
        }
        
        private void SelectTheme()
        {
            if (themes != null && themes.Count > 0)
            {
                currentTheme = themes[random.Next(themes.Count)];
            }
        }
        
        #endregion

        #region 房间类型生成流程
        
        /// <summary>
        /// 生成标准战斗房间（原有流程）
        /// </summary>
        private void GenerateStandardRoom()
        {
            // Phase 2: BSP 空间分割
            GenerateBSP();
            
            // Phase 3: 房间生成
            PlaceRooms();
            
            // Phase 4: 图连接
            BuildRoomGraph();
            
            // Phase 5: 走廊生成
            GenerateCorridors();
            
            // Phase 6: 连通性保障
            EnsureConnectivity();
            
            // Phase 7: 平台注入
            InjectPlatforms();
            
            // Phase 8: 后处理
            PostProcess();
        }
        
        /// <summary>
        /// 生成入口房间
        /// </summary>
        private void GenerateEntranceRoom()
        {
            SpecialRoomGenerator.GenerateEntranceRoom(currentRoom, parameters, random);
            
            // 入口房间不需要后处理（无生成点、无平台注入）
        }
        
        /// <summary>
        /// 生成Boss房间
        /// </summary>
        private void GenerateBossRoom()
        {
            SpecialRoomGenerator.GenerateBossRoom(currentRoom, parameters, random);
            
            // Boss房间不需要后处理（生成点已在 SpecialRoomGenerator 中设置）
        }
        
        #endregion

        #region Phase 2: BSP 空间分割
        
        private void GenerateBSP()
        {
            // 创建 BSP 生成器
            bspGenerator = new BSPGenerator(parameters, random);
            
            // 生成 BSP 树
            currentRoom.bspRoot = bspGenerator.Generate();
            
            // BSP 分割完成
        }
        
        #endregion

        #region Phase 3: 房间生成
        
        private void PlaceRooms()
        {
            // 创建房间放置器
            roomPlacer = new RoomPlacer(parameters, random);
            
            // 在 BSP 叶节点中放置房间
            List<RoomRegion> rooms = roomPlacer.PlaceRooms(currentRoom.bspRoot, currentRoom);
            
            // 更新房间图
            currentRoom.roomGraph.rooms = rooms;
            
            // 标记入口和出口所在的房间
            roomPlacer.MarkEntranceExitRooms(rooms, currentRoom.startPos, currentRoom.endPos);
            
            // 房间放置完成
        }
        
        #endregion

        #region Phase 4: 图连接
        
        private void BuildRoomGraph()
        {
            List<RoomRegion> rooms = currentRoom.roomGraph.rooms;
            
            if (rooms == null || rooms.Count < 2)
            {
                // 房间数量不足，跳过图连接
                return;
            }
            
            // Step 1: Delaunay 三角剖分 - 生成所有潜在连接边
            List<RoomEdge> delaunayEdges = DelaunayTriangulation.Triangulate(rooms);
            currentRoom.roomGraph.allEdges = delaunayEdges;
            
            // Step 2: Kruskal 最小生成树 - 选择必要连接
            List<RoomEdge> mstEdges = MinimumSpanningTree.Kruskal(rooms, delaunayEdges);
            currentRoom.roomGraph.mstEdges = mstEdges;
            
            // Step 3: 选择额外边 - 形成环路
            List<RoomEdge> extraEdges = MinimumSpanningTree.SelectExtraEdges(
                delaunayEdges, 
                mstEdges, 
                parameters.extraEdgeRatio,
                random
            );
            currentRoom.roomGraph.extraEdges = extraEdges;
            
            // Step 4: 构建最终边列表
            currentRoom.roomGraph.BuildFinalEdges();
            
            // 验证连通性
            bool isConnected = MinimumSpanningTree.IsConnected(rooms, currentRoom.roomGraph.finalEdges);
            if (!isConnected)
            {
                // 警告：图不连通，双向游走将修复
            }
            
            // 图连接完成
        }
        
        #endregion

        #region Phase 5: 走廊生成
        
        private void GenerateCorridors()
        {
            // 创建走廊生成器
            corridorBuilder = new CorridorBuilder(parameters, random);
            
            // 生成所有走廊
            corridorBuilder.BuildCorridors(currentRoom.roomGraph, currentRoom);
            
            // 走廊生成完成
        }
        
        #endregion

        #region Phase 6: 连通性保障
        
        private void EnsureConnectivity()
        {
            // 创建连通性保障器
            connectivityGuarantor = new ConnectivityGuarantor(parameters, random);
            
            // 执行双向随机游走
            connectivityGuarantor.EnsureConnectivity(currentRoom);
            
            // 验证连通性
            bool isConnected = ConnectivityGuarantor.VerifyConnectivity(currentRoom);
            if (isConnected)
            {
                // 连通性验证通过
            }
            else
            {
                Debug.LogWarning("[RoomGeneratorV2] 警告：连通性验证失败！");
            }
        }
        
        #endregion

        #region Phase 7: 平台注入
        
        private void InjectPlatforms()
        {
            // 创建平台注入器
            platformInjector = new PlatformInjector(parameters, random);
            
            // 执行平台注入
            platformInjector.InjectPlatforms(currentRoom);
            
            // 平台注入完成
        }
        
        #endregion

        #region Phase 8: 后处理
        
        private void PostProcess()
        {
            // 孤岛移除
            RemoveDisconnectedIslands();
            
            // 出入口安全区清理
            ClearEntranceExitArea();
            
            // 敌人生成点识别
            IdentifySpawnPoints();
            
            // 后处理完成
        }
        
        private void RemoveDisconnectedIslands()
        {
            bool[,] reachable = new bool[parameters.roomWidth, parameters.roomHeight];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            
            queue.Enqueue(currentRoom.startPos);
            reachable[currentRoom.startPos.x, currentRoom.startPos.y] = true;
            
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            while (queue.Count > 0)
            {
                Vector2Int p = queue.Dequeue();
                
                foreach (var dir in dirs)
                {
                    Vector2Int n = p + dir;
                    if (currentRoom.IsValid(n.x, n.y) &&
                        currentRoom.GetTile(n.x, n.y) != TileType.Wall &&
                        !reachable[n.x, n.y])
                    {
                        reachable[n.x, n.y] = true;
                        queue.Enqueue(n);
                    }
                }
            }
            
            // 移除不可达区域
            for (int x = 0; x < parameters.roomWidth; x++)
            {
                for (int y = 0; y < parameters.roomHeight; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Wall && !reachable[x, y])
                    {
                        currentRoom.SetTile(x, y, TileType.Wall);
                    }
                }
            }
        }
        
        private void ClearEntranceExitArea()
        {
            int clearDepth = parameters.entranceClearDepth;
            int clearHeight = 3; // 玩家 2 + 1 余量
            
            // 清理入口
            for (int x = 0; x < clearDepth; x++)
            {
                for (int dy = 0; dy < clearHeight; dy++)
                {
                    int y = currentRoom.startPos.y + dy;
                    if (currentRoom.IsValid(x, y))
                    {
                        currentRoom.SetTile(x, y, TileType.Floor);
                    }
                }
                // 确保下方有地面
                if (currentRoom.startPos.y - 1 >= 0)
                {
                    currentRoom.SetTile(x, currentRoom.startPos.y - 1, TileType.Wall);
                }
            }
            
            // 清理出口
            for (int x = 0; x < clearDepth; x++)
            {
                int targetX = currentRoom.width - 1 - x;
                for (int dy = 0; dy < clearHeight; dy++)
                {
                    int y = currentRoom.endPos.y + dy;
                    if (currentRoom.IsValid(targetX, y))
                    {
                        currentRoom.SetTile(targetX, y, TileType.Floor);
                    }
                }
                // 确保下方有地面
                if (currentRoom.endPos.y - 1 >= 0)
                {
                    currentRoom.SetTile(targetX, currentRoom.endPos.y - 1, TileType.Wall);
                }
            }
        }
        
        private void IdentifySpawnPoints()
        {
            currentRoom.potentialSpawns.Clear();
            
            // 开始识别生成点
            
            // 入口房间不生成怪物
            if (parameters.roomType == MultiRoom.RoomType.Entrance)
            {
                // 入口房间不生成怪物生成点
                return;
            }
            
            // 地面生成点
            IdentifyGroundSpawns();
            
            // 空中生成点
            IdentifyAirSpawns();
            
            // 识别到潜在生成点
            
            // 筛选并分配敌人类型
            FilterSpawnPoints();
        }
        
        private void IdentifyGroundSpawns()
        {
            for (int y = 1; y < parameters.roomHeight - 1; y++)
            {
                int consecutiveFloor = 0;
                int startX = -1;
                
                for (int x = 0; x < parameters.roomWidth; x++)
                {
                    bool isFloor = currentRoom.GetTile(x, y) == TileType.Floor;
                    bool hasSolidBelow = currentRoom.IsSolid(x, y - 1);
                    bool hasHeadroom = currentRoom.GetTile(x, y + 1) == TileType.Floor &&
                                       currentRoom.GetTile(x, y + 2) == TileType.Floor;
                    
                    if (isFloor && hasSolidBelow && hasHeadroom)
                    {
                        if (startX == -1) startX = x;
                        consecutiveFloor++;
                    }
                    else
                    {
                        if (consecutiveFloor >= parameters.minGroundSpan)
                        {
                            int centerX = (startX + x - 1) / 2;
                            Vector2Int spawnPos = new Vector2Int(centerX, y);
                            
                            // 增加边界验证调用 (Requirements 1.1, 1.3)
                            if (!SpawnPointValidator.IsWithinBounds(spawnPos, parameters.roomWidth, parameters.roomHeight, parameters.edgePadding))
                            {
                                consecutiveFloor = 0;
                                startX = -1;
                                continue;
                            }
                            
                            // 增加环境验证调用 (Requirements 2.1, 2.2)
                            // 验证上方3格空间和左右各1格空间
                            if (!SpawnPointValidator.ValidateGroundEnvironment(spawnPos, currentRoom, 3, 1))
                            {
                                consecutiveFloor = 0;
                                startX = -1;
                                continue;
                            }
                            
                            var newSpawn = new SpawnPointV2(
                                spawnPos,
                                SpawnType.Ground
                            ) { 
                                groundSpan = consecutiveFloor,
                                isValid = true,
                                invalidReason = string.Empty
                            };
                            currentRoom.potentialSpawns.Add(newSpawn);
                        }
                        consecutiveFloor = 0;
                        startX = -1;
                    }
                }
                
                if (consecutiveFloor >= parameters.minGroundSpan)
                {
                    int centerX = (startX + parameters.roomWidth - 1) / 2;
                    Vector2Int spawnPos = new Vector2Int(centerX, y);
                    
                    // 增加边界验证调用 (Requirements 1.1, 1.3)
                    if (SpawnPointValidator.IsWithinBounds(spawnPos, parameters.roomWidth, parameters.roomHeight, parameters.edgePadding))
                    {
                        // 增加环境验证调用 (Requirements 2.1, 2.2)
                        if (SpawnPointValidator.ValidateGroundEnvironment(spawnPos, currentRoom, 3, 1))
                        {
                            var newSpawn = new SpawnPointV2(
                                spawnPos,
                                SpawnType.Ground
                            ) { 
                                groundSpan = consecutiveFloor,
                                isValid = true,
                                invalidReason = string.Empty
                            };
                            currentRoom.potentialSpawns.Add(newSpawn);
                        }
                    }
                }
            }
        }
        
        private void IdentifyAirSpawns()
        {
            // 使用更大的边界距离，确保空中生成点远离墙壁
            int airEdgePadding = Mathf.Max(parameters.edgePadding, 4);
            
            for (int x = airEdgePadding; x < parameters.roomWidth - airEdgePadding; x++)
            {
                for (int y = airEdgePadding; y < parameters.roomHeight - airEdgePadding; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Floor) continue;
                    
                    Vector2Int spawnPos = new Vector2Int(x, y);
                    
                    // 增加边界验证调用，使用更大的边界距离 (Requirements 1.2, 1.3)
                    if (!SpawnPointValidator.IsWithinBounds(spawnPos, parameters.roomWidth, parameters.roomHeight, airEdgePadding))
                        continue;
                    
                    // 增加更大范围的环境验证（5x5区域），确保飞行怪物有足够空间 (Requirements 2.3)
                    if (!SpawnPointValidator.ValidateAirEnvironment(spawnPos, currentRoom, 2))
                        continue;
                    
                    // 检查距地面高度
                    int distToGround = 0;
                    for (int dy = 1; y - dy >= 0; dy++)
                    {
                        if (currentRoom.IsSolid(x, y - dy))
                        {
                            distToGround = dy;
                            break;
                        }
                    }
                    
                    if (distToGround >= parameters.minAirHeight)
                    {
                        if (random.NextDouble() < 0.15)
                        {
                            var newSpawn = new SpawnPointV2(
                                spawnPos,
                                SpawnType.Air
                            ) { 
                                heightAboveGround = distToGround,
                                isValid = true,
                                invalidReason = string.Empty
                            };
                            currentRoom.potentialSpawns.Add(newSpawn);
                        }
                    }
                }
            }
        }
        
        private void FilterSpawnPoints()
        {
            // FilterSpawnPoints 开始执行
            
            // 在分配敌人类型前先排除安全区内的点位 (Requirements 5.1, 5.2, 5.3)
            int safeDistance = parameters.entranceClearDepth + 2;
            List<SpawnPointV2> validSpawns = new List<SpawnPointV2>();
            
            foreach (var spawn in currentRoom.potentialSpawns)
            {
                // 检查是否在安全区内
                if (!SpawnPointValidator.IsInSafeZone(spawn.position, currentRoom.startPos, currentRoom.endPos, safeDistance))
                {
                    validSpawns.Add(spawn);
                }
            }
            
            // 使用过滤后的生成点列表
            currentRoom.potentialSpawns = validSpawns;
            
            // 随机打乱
            for (int i = 0; i < currentRoom.potentialSpawns.Count; i++)
            {
                int j = random.Next(i, currentRoom.potentialSpawns.Count);
                var temp = currentRoom.potentialSpawns[i];
                currentRoom.potentialSpawns[i] = currentRoom.potentialSpawns[j];
                currentRoom.potentialSpawns[j] = temp;
            }
            
            // 分离地面和空中生成点
            List<SpawnPointV2> groundSpawns = new List<SpawnPointV2>();
            List<SpawnPointV2> airSpawns = new List<SpawnPointV2>();
            
            foreach (var spawn in currentRoom.potentialSpawns)
            {
                if (spawn.type == SpawnType.Ground)
                    groundSpawns.Add(spawn);
                else if (spawn.type == SpawnType.Air)
                    airSpawns.Add(spawn);
            }
            
            // 筛选生成点并分配敌人类型
            List<SpawnPointV2> selected = new List<SpawnPointV2>();
            
            // 设置每种敌人的数量范围（随机）
            int shieldbearerCount = random.Next(1, 3);  // 1-2个盾卫
            int sharpshooterCount = random.Next(2, 4);  // 2-3个锐枪手
            int mothCount = random.Next(1, 3);          // 1-2个飞蛾
            
            // 目标敌人数量设定
            // 可用生成点统计
            
            // 1. 分配盾卫（地面敌人，降低空间要求）
            int shieldbearersAssigned = 0;
            foreach (var spawn in groundSpawns)
            {
                if (shieldbearersAssigned >= shieldbearerCount) break;
                if (spawn.groundSpan >= 3 && !IsTooClose(spawn, selected))  // 降低从5到3
                {
                    var newSpawn = new SpawnPointV2(spawn.position, spawn.type, EnemyType.TriangleShieldbearer);
                    newSpawn.groundSpan = spawn.groundSpan;
                    newSpawn.heightAboveGround = spawn.heightAboveGround;
                    selected.Add(newSpawn);
                    shieldbearersAssigned++;
                }
            }
            
            // 2. 分配锐枪手（地面敌人）
            int sharpshootersAssigned = 0;
            foreach (var spawn in groundSpawns)
            {
                if (sharpshootersAssigned >= sharpshooterCount) break;
                if (!IsTooClose(spawn, selected))
                {
                    var newSpawn = new SpawnPointV2(spawn.position, spawn.type, EnemyType.TriangleSharpshooter);
                    newSpawn.groundSpan = spawn.groundSpan;
                    newSpawn.heightAboveGround = spawn.heightAboveGround;
                    selected.Add(newSpawn);
                    sharpshootersAssigned++;
                }
            }
            
            // 3. 分配飞蛾（优先空中生成点）
            int mothsAssigned = 0;
            foreach (var spawn in airSpawns)
            {
                if (mothsAssigned >= mothCount) break;
                if (!IsTooClose(spawn, selected))
                {
                    var newSpawn = new SpawnPointV2(spawn.position, spawn.type, EnemyType.TriangleMoth);
                    newSpawn.groundSpan = spawn.groundSpan;
                    newSpawn.heightAboveGround = spawn.heightAboveGround;
                    selected.Add(newSpawn);
                    mothsAssigned++;
                }
            }
            
            // 如果空中生成点不够，飞蛾也可以在地面生成点生成
            if (mothsAssigned < mothCount)
            {
                foreach (var spawn in groundSpawns)
                {
                    if (mothsAssigned >= mothCount) break;
                    if (!IsTooClose(spawn, selected))
                    {
                        var newSpawn = new SpawnPointV2(spawn.position, spawn.type, EnemyType.TriangleMoth);
                        newSpawn.groundSpan = spawn.groundSpan;
                        newSpawn.heightAboveGround = spawn.heightAboveGround;
                        selected.Add(newSpawn);
                        mothsAssigned++;
                    }
                }
            }
            
            currentRoom.potentialSpawns = selected;
            
            // 敌人配置完成
            
            // 输出每个分配的生成点详情
            foreach (var spawn in selected)
            {
                // 分配生成点
            }
        }
        
        /// <summary>
        /// 检查生成点是否与已选择的生成点距离过近
        /// </summary>
        private bool IsTooClose(SpawnPointV2 spawn, List<SpawnPointV2> selected)
        {
            // 降低距离限制，使用更宽松的距离要求
            float minDistance = Mathf.Max(3f, parameters.minSpawnDistance * 0.6f);
            
            foreach (var s in selected)
            {
                if (Vector2Int.Distance(spawn.position, s.position) < minDistance)
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region 烘焙
        
        private void BakeTileAt(int x, int y, Vector3Int tilePos)
        {
            TileType type = currentRoom.GetTile(x, y);
            
            if (type == TileType.Wall)
            {
                // 检测细长结构
                bool IsSolidWall(int tx, int ty)
                {
                    if (!currentRoom.IsValid(tx, ty)) return true;
                    return currentRoom.GetTile(tx, ty) == TileType.Wall;
                }
                
                bool isBeam = !IsSolidWall(x, y + 1) && !IsSolidWall(x, y - 1);
                bool isPillar = !IsSolidWall(x - 1, y) && !IsSolidWall(x + 1, y);
                
                if ((isBeam || isPillar) && currentTheme.singlePlatformTile != null)
                {
                    targetTilemap.SetTile(tilePos, currentTheme.singlePlatformTile);
                }
                else if (currentTheme.wallTile != null)
                {
                    targetTilemap.SetTile(tilePos, currentTheme.wallTile);
                }
            }
            else if (type == TileType.Platform)
            {
                Tilemap destMap = platformTilemap != null ? platformTilemap : targetTilemap;
                
                // 检测孤立平台
                bool isSingle = true;
                int[] dx = { 0, 0, -1, 1 };
                int[] dy = { 1, -1, 0, 0 };
                
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (currentRoom.IsValid(nx, ny) && currentRoom.GetTile(nx, ny) == TileType.Platform)
                    {
                        isSingle = false;
                        break;
                    }
                }
                
                if (isSingle && currentTheme.singlePlatformTile != null)
                {
                    destMap.SetTile(tilePos, currentTheme.singlePlatformTile);
                }
                else if (currentTheme.platformTile != null)
                {
                    destMap.SetTile(tilePos, currentTheme.platformTile);
                }
            }
            else if (type == TileType.Floor)
            {
                if (currentTheme.backgroundTile != null)
                {
                    targetTilemap.SetTile(tilePos, currentTheme.backgroundTile);
                }
                else
                {
                    targetTilemap.SetTile(tilePos, null);
                }
            }
        }
        
        private void BakePaddingAt(int x, int y, Vector3Int tilePos)
        {
            bool isEntrancePath = false;
            bool isExitPath = false;
            
            // 检查左侧入口通道
            if (x < 0)
            {
                if (y >= currentRoom.startPos.y && y < currentRoom.startPos.y + 3)
                {
                    isEntrancePath = true;
                }
            }
            
            // 检查右侧出口通道
            if (x >= currentRoom.width)
            {
                if (y >= currentRoom.endPos.y && y < currentRoom.endPos.y + 3)
                {
                    isExitPath = true;
                }
            }
            
            if (isEntrancePath || isExitPath)
            {
                targetTilemap.SetTile(tilePos, null);
            }
            else if (currentTheme.wallTile != null)
            {
                targetTilemap.SetTile(tilePos, currentTheme.wallTile);
            }
        }
        
        private void BakeBossDoor()
        {
            if (currentRoom == null || !currentRoom.needsDoorAtExit || doorTilemap == null) return;
            
            // 使用专门的门砖块，如果没有则使用墙壁砖块
            TileBase doorTileToUse = currentTheme.doorTile != null ? currentTheme.doorTile : currentTheme.wallTile;
            
            if (doorTileToUse == null)
            {
                Debug.LogWarning("[RoomGeneratorV2] 没有可用的门砖块");
                return;
            }
            
            // 在出口位置放置门：宽2格 x 高3格
            int doorX = currentRoom.endPos.x;
            int doorY = currentRoom.endPos.y;
            
            // 放置6个砖块（2列 x 3行）
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Vector3Int doorPos = new Vector3Int(doorX + x, doorY + y, 0);
                    doorTilemap.SetTile(doorPos, doorTileToUse);
                }
            }
            
            // 烘焙Boss门完成
        }
        
        private void BakeSpawnPoints()
        {
            if (currentRoom == null || currentRoom.potentialSpawns == null) return;
            
            // 清理旧的生成点
            Transform spawnContainer = transform.Find("SpawnPoints");
            if (spawnContainer != null)
            {
                DestroyImmediate(spawnContainer.gameObject);
            }
            
            // 创建总管理器容器
            GameObject spawnManager = new GameObject("SpawnPointManager");
            spawnManager.transform.SetParent(transform);
            spawnManager.transform.localPosition = Vector3.zero;
            
            // 添加管理器组件
            SpawnPointManager managerComponent = spawnManager.AddComponent<SpawnPointManager>();
            
            // 为每个生成点创建GameObject
            for (int i = 0; i < currentRoom.potentialSpawns.Count; i++)
            {
                SpawnPointV2 spawn = currentRoom.potentialSpawns[i];
                
                // 创建生成点GameObject
                GameObject spawnObj = new GameObject($"SpawnPoint_{i}_{spawn.enemyType}");
                spawnObj.transform.SetParent(spawnManager.transform);
                
                // 设置位置
                Vector3 worldPos = targetTilemap.CellToWorld(new Vector3Int(spawn.position.x, spawn.position.y, 0));
                spawnObj.transform.position = worldPos + new Vector3(0.5f, 0.5f, 0);
                
                // 添加生成点组件
                SpawnPoint spawnComponent = spawnObj.AddComponent<SpawnPoint>();
                spawnComponent.Initialize(spawn, currentRoom);
                
                // 创建可视化子对象
                GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObj.name = "Visual";
                visualObj.transform.SetParent(spawnObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = Vector3.one * 0.8f;
                
                // 根据敌人类型设置颜色
                Renderer renderer = visualObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    
                    switch (spawn.enemyType)
                    {
                        case EnemyType.CompositeGuardian:
                            mat.color = Color.yellow; // Boss：黄色
                            break;
                        case EnemyType.TriangleSharpshooter:
                            mat.color = new Color(1f, 0.5f, 0f); // 锐枪手：橙色
                            break;
                        case EnemyType.TriangleShieldbearer:
                            mat.color = Color.red; // 盾卫：红色
                            break;
                        case EnemyType.TriangleMoth:
                            mat.color = Color.green; // 飞蛾：绿色
                            break;
                        default:
                            mat.color = spawn.type == SpawnType.Air ? Color.magenta : Color.cyan;
                            break;
                    }
                    
                    renderer.material = mat;
                }
                
                // 移除碰撞体（保留触发器功能）
                Collider collider = visualObj.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
                
                // 注册到管理器
                managerComponent.RegisterSpawnPoint(spawnComponent);
            }
            
            // 烘焙生成点完成
        }
        
        private void BroadcastRoomAnchors()
        {
            if (currentRoom == null || targetTilemap == null) return;
            
            Vector3 startWorld = targetTilemap.CellToWorld(
                new Vector3Int(currentRoom.startPos.x, currentRoom.startPos.y, 0)
            ) + targetTilemap.tileAnchor;
            
            Vector3 endWorld = targetTilemap.CellToWorld(
                new Vector3Int(currentRoom.endPos.x, currentRoom.endPos.y, 0)
            ) + targetTilemap.tileAnchor;
            
            if (Application.isPlaying)
            {
                // 运行时广播消息
                var data = new RoomAnchorsData
                {
                    startGridPos = currentRoom.startPos,
                    endGridPos = currentRoom.endPos,
                    startWorldPos = startWorld,
                    endWorldPos = endWorld,
                    startDirection = Vector2Int.left,
                    endDirection = Vector2Int.right
                };
                
                MessageManager.Instance.Send(MessageDefine.ROOM_ANCHORS_UPDATE, data);
            }
            else
            {
                Debug.Log($"[RoomGeneratorV2] Room Generated.\n" +
                          $"[方位/Direction] Entrance: Left | Exit: Right\n" +
                          $"[瓦片坐标/Grid] Entrance: {currentRoom.startPos} | Exit: {currentRoom.endPos}\n" +
                          $"[世界坐标/World] Entrance: {startWorld} | Exit: {endWorld}");
            }
        }
        
        #endregion
        
        #region Gizmos 可视化
        
        private void OnDrawGizmos()
        {
            if (currentRoom == null || currentRoom.potentialSpawns == null || targetTilemap == null) return;
            
            // 绘制生成点
            foreach (var spawn in currentRoom.potentialSpawns)
            {
                Vector3 worldPos = targetTilemap.CellToWorld(new Vector3Int(spawn.position.x, spawn.position.y, 0));
                worldPos += new Vector3(0.5f, 0.5f, 0); // 居中
                
                // 根据类型设置颜色
                switch (spawn.type)
                {
                    case SpawnType.Boss:
                        Gizmos.color = Color.yellow; // Boss生成点：黄色
                        break;
                    case SpawnType.Air:
                        Gizmos.color = Color.magenta; // 空中小怪：粉色
                        break;
                    case SpawnType.Ground:
                        Gizmos.color = Color.cyan; // 地面小怪：青色
                        break;
                }
                
                // 绘制立方体
                Gizmos.DrawCube(worldPos, Vector3.one * 0.8f);
                
                // 绘制线框
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
            }
            
            // 绘制出入口
            if (currentRoom.startPos != Vector2Int.zero)
            {
                Vector3 startWorld = targetTilemap.CellToWorld(new Vector3Int(currentRoom.startPos.x, currentRoom.startPos.y, 0));
                startWorld += new Vector3(0.5f, 0.5f, 0);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startWorld, 0.5f);
            }
            
            if (currentRoom.endPos != Vector2Int.zero)
            {
                Vector3 endWorld = targetTilemap.CellToWorld(new Vector3Int(currentRoom.endPos.x, currentRoom.endPos.y, 0));
                endWorld += new Vector3(0.5f, 0.5f, 0);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(endWorld, 0.5f);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 房间锚点数据（用于消息广播）
    /// </summary>
    public struct RoomAnchorsData
    {
        public Vector2Int startGridPos;
        public Vector2Int endGridPos;
        public Vector3 startWorldPos;
        public Vector3 endWorldPos;
        public Vector2Int startDirection;
        public Vector2Int endDirection;
    }
}
