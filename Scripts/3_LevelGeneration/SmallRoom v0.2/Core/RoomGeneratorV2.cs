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
            
            // Phase 2: BSP 空间分割
            GenerateBSP();
            
            // Phase 3: 房间生成
            PlaceRooms();
            
            // Phase 4: 图连接
            BuildRoomGraph();
            
            // Phase 5: 走廊生成
            GenerateCorridors();
            
            // Phase 6: 连通性保障（双向随机游走）
            EnsureConnectivity();
            
            // Phase 7: 平台注入
            InjectPlatforms();
            
            // Phase 8: 后处理
            PostProcess();
            
            // 重建地面列表
            currentRoom.RebuildFloorTiles();
            
            Debug.Log($"[RoomGeneratorV2] 房间生成完成: {currentRoom}");
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
                Debug.LogWarning("[RoomGeneratorV2] 无法烘焙：Tilemap 或房间数据为空");
                return;
            }
            
            // 清空 Tilemap
            targetTilemap.ClearAllTiles();
            if (platformTilemap != null)
            {
                platformTilemap.ClearAllTiles();
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
            
            // 广播消息
            BroadcastRoomAnchors();
            
            Debug.Log("[RoomGeneratorV2] 烘焙完成");
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

        #region Phase 2: BSP 空间分割
        
        private void GenerateBSP()
        {
            // 创建 BSP 生成器
            bspGenerator = new BSPGenerator(parameters, random);
            
            // 生成 BSP 树
            currentRoom.bspRoot = bspGenerator.Generate();
            
            Debug.Log($"[RoomGeneratorV2] BSP 分割完成: 总节点={bspGenerator.TotalNodes}, 叶节点={bspGenerator.LeafNodes}, 最大深度={bspGenerator.MaxDepthReached}");
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
            
            Debug.Log($"[RoomGeneratorV2] 房间放置完成，房间数: {currentRoom.roomGraph.RoomCount}");
        }
        
        #endregion

        #region Phase 4: 图连接
        
        private void BuildRoomGraph()
        {
            List<RoomRegion> rooms = currentRoom.roomGraph.rooms;
            
            if (rooms == null || rooms.Count < 2)
            {
                Debug.LogWarning("[RoomGeneratorV2] 房间数量不足，跳过图连接");
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
                Debug.LogWarning("[RoomGeneratorV2] 警告：图不连通，双向游走将修复");
            }
            
            Debug.Log($"[RoomGeneratorV2] 图连接完成: {currentRoom.roomGraph}");
        }
        
        #endregion

        #region Phase 5: 走廊生成
        
        private void GenerateCorridors()
        {
            // 创建走廊生成器
            corridorBuilder = new CorridorBuilder(parameters, random);
            
            // 生成所有走廊
            corridorBuilder.BuildCorridors(currentRoom.roomGraph, currentRoom);
            
            Debug.Log($"[RoomGeneratorV2] 走廊生成完成: {corridorBuilder.CorridorsBuilt}条走廊, {corridorBuilder.TotalTilesDug}个瓦片");
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
                Debug.Log($"[RoomGeneratorV2] 连通性验证通过: 正向={connectivityGuarantor.ForwardSteps}步, 反向={connectivityGuarantor.BackwardSteps}步");
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
            
            Debug.Log($"[RoomGeneratorV2] 平台注入完成: 放置={platformInjector.PlatformsPlaced}, 分析落差={platformInjector.GapsAnalyzed}, 修复不可达={platformInjector.UnreachableFixed}");
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
            
            Debug.Log("[RoomGeneratorV2] 后处理完成");
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
            
            // 地面生成点
            IdentifyGroundSpawns();
            
            // 空中生成点
            IdentifyAirSpawns();
            
            // 筛选
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
                            currentRoom.potentialSpawns.Add(new SpawnPointV2(
                                new Vector2Int(centerX, y),
                                SpawnType.Ground
                            ) { groundSpan = consecutiveFloor });
                        }
                        consecutiveFloor = 0;
                        startX = -1;
                    }
                }
                
                if (consecutiveFloor >= parameters.minGroundSpan)
                {
                    int centerX = (startX + parameters.roomWidth - 1) / 2;
                    currentRoom.potentialSpawns.Add(new SpawnPointV2(
                        new Vector2Int(centerX, y),
                        SpawnType.Ground
                    ) { groundSpan = consecutiveFloor });
                }
            }
        }
        
        private void IdentifyAirSpawns()
        {
            for (int x = 2; x < parameters.roomWidth - 2; x++)
            {
                for (int y = 2; y < parameters.roomHeight - 2; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Floor) continue;
                    
                    // 检查周围开放
                    if (currentRoom.GetTile(x + 1, y) != TileType.Floor ||
                        currentRoom.GetTile(x - 1, y) != TileType.Floor ||
                        currentRoom.GetTile(x, y + 1) != TileType.Floor ||
                        currentRoom.GetTile(x, y - 1) != TileType.Floor)
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
                            currentRoom.potentialSpawns.Add(new SpawnPointV2(
                                new Vector2Int(x, y),
                                SpawnType.Air
                            ) { heightAboveGround = distToGround });
                        }
                    }
                }
            }
        }
        
        private void FilterSpawnPoints()
        {
            // 随机打乱
            for (int i = 0; i < currentRoom.potentialSpawns.Count; i++)
            {
                int j = random.Next(i, currentRoom.potentialSpawns.Count);
                var temp = currentRoom.potentialSpawns[i];
                currentRoom.potentialSpawns[i] = currentRoom.potentialSpawns[j];
                currentRoom.potentialSpawns[j] = temp;
            }
            
            // 筛选
            List<SpawnPointV2> selected = new List<SpawnPointV2>();
            
            foreach (var spawn in currentRoom.potentialSpawns)
            {
                if (selected.Count >= parameters.maxEnemies) break;
                
                bool tooClose = false;
                foreach (var s in selected)
                {
                    if (Vector2Int.Distance(spawn.position, s.position) < parameters.minSpawnDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose) selected.Add(spawn);
            }
            
            currentRoom.potentialSpawns = selected;
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
