using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.SmallRoom
{
    [System.Serializable]
    public struct RoomTheme
    {
        public string themeName;
        public TileBase wallTile;     // 实体墙壁/地面 (Rule Tile)
        public TileBase platformTile; // 浮空平台 (Rule Tile 或普通 Tile)
        public TileBase singlePlatformTile; // 独立的 1x1 浮空平台 (可选)
        public TileBase backgroundTile; // 背景墙 (可选)
    }

    public class RoomGenerator : MonoBehaviour
    {
        public RoomGenParams parameters;
        
        [Tooltip("Assign the Tilemap for Solid Walls here")]
        public Tilemap targetTilemap; // 保持原名以兼容，实际上作为 Wall Layer
        
        [Tooltip("Assign a separate Tilemap for One-Way Platforms here")]
        public Tilemap platformTilemap; // 新增：专门用于平台的 Tilemap
        
        [Header("Visual Themes")]
        public List<RoomTheme> themes; // 在编辑器中配置 红/蓝/黄 主题
        
        // public TileBase wallTile; // Deprecated
        // public TileBase floorTile; // Deprecated

        private RoomData currentRoom;
        private RoomTheme currentTheme; // 当前选中的主题

        // For Editor access
        public RoomData CurrentRoom => currentRoom;

        // Allow injecting data (e.g. from Editor Window preview)
        public void SetRoomData(RoomData data)
        {
            this.currentRoom = data;
        }
        
        public void ForcePickTheme()
        {
            if (themes != null && themes.Count > 0)
            {
                // Only pick if not set, or always pick? 
                // Let's random pick to be safe, or maybe we want to keep the specific theme from preview?
                // The preview didn't strictly enforce a theme visual, just data. 
                // So random is fine.
                currentTheme = themes[Random.Range(0, themes.Count)];
            }
        }

        public void GenerateRoom()
        {
            if (parameters == null) parameters = new RoomGenParams();

            // 0. Select Theme
            if (themes != null && themes.Count > 0)
            {
                currentTheme = themes[Random.Range(0, themes.Count)];
            }

            InitializeSeed();
            InitializeGrid(); // Sets Start/End and clears grid
            
            // Ensure Entrances are clear BEFORE generating path
            ClearEntranceExitArea();

            // New Strategy: Main Path + Rooms
            GenerateMainPath();
            GenerateRandomRooms();
            
            // Phase 2: Post-processing
            // EnsureConnectivity(); // MainPath guarantees connectivity usually
            RemoveDisconnectedIslands();
            
            // Re-clear area to be absolutely sure nothing overwrote it
            ClearEntranceExitArea();
            
            PostProcessPlatforms(); // Minimal platform addition
            IdentifySpawnPoints();
            
            // BakeToTilemap(); // Can be called from Editor
        }

        private void ClearEntranceExitArea()
        {
            int clearDistance = 4; // 向内打通4格距离

            // Clear Start (Left)
            for (int x = 0; x < clearDistance; x++)
            {
                if (x >= currentRoom.width) break;
                for (int y = 0; y < 2; y++) // 2格高
                {
                    int targetY = currentRoom.startPos.y + y;
                    if (targetY < currentRoom.height)
                    {
                        currentRoom.SetTile(x, targetY, TileType.Floor);
                    }
                }
                // Ensure ground below
                if (currentRoom.startPos.y - 1 >= 0)
                {
                    currentRoom.SetTile(x, currentRoom.startPos.y - 1, TileType.Wall);
                }
            }

            // Clear End (Right)
            for (int x = 0; x < clearDistance; x++)
            {
                int targetX = currentRoom.width - 1 - x;
                if (targetX < 0) break;
                for (int y = 0; y < 2; y++)
                {
                    int targetY = currentRoom.endPos.y + y;
                    if (targetY < currentRoom.height)
                    {
                        currentRoom.SetTile(targetX, targetY, TileType.Floor);
                    }
                }
                // Ensure ground below
                if (currentRoom.endPos.y - 1 >= 0)
                {
                    currentRoom.SetTile(targetX, currentRoom.endPos.y - 1, TileType.Wall);
                }
            }
        }

        private void GenerateMainPath()
        {
            // Connect Start to End with a wide, walkable path
            Vector2Int current = currentRoom.startPos;
            Vector2Int target = currentRoom.endPos;
            
            // Safety counter
            int moves = 0;
            int maxMoves = 1000;

            while (current.x < target.x && moves < maxMoves)
            {
                moves++;
                
                // Always dig current pos
                Dig(current.x, current.y, parameters.pathWidth, parameters.pathWidth);

                // Decide next step
                // We want to move generally towards target.x, but y can fluctuate
                // We prioritize Horizontal movement to avoid shafts
                
                int dx = target.x - current.x;
                int dy = target.y - current.y;

                if (dx <= 0) break; // Reached x

                // Randomly choose to move X or Y, weighted to X
                float r = Random.value;
                
                if (r < 0.7f || dx == 0) // 70% Horizontal
                {
                    current.x++;
                }
                else // 30% Vertical adjustment
                {
                    // Move towards target Y, but maybe wander slightly?
                    // No, let's just steer towards target Y to ensure we get there
                    if (Mathf.Abs(dy) > 0)
                    {
                        // Ensure we don't create 1-tile shafts. 
                        // If we move Y, we should also move X (Diagonal/Stairs) 
                        // OR dig a wide vertical shaft.
                        
                        int yDir = System.Math.Sign(dy);
                        current.y += yDir;
                        
                        // If going up/down, dig the intermediate step to make it a "slope" if possible
                        // Actually Dig() at start of loop handles the new pos.
                        // But to prevent "Corner Cutting" where diagonal is blocked, we should dig the corner.
                        // (x, y) -> (x, y+1). Corner is (x, y+1) or (x+1, y)? 
                        // We just moved Y, X didn't change. So it's a vertical step.
                        // To make it walkable, we need a platform or stairs.
                        // Let's enforce "Stairs": If we move Y, we MUST move X next step.
                    }
                    else
                    {
                        // Already at target Y, force X
                        current.x++;
                    }
                }
                
                // Clamp
                current.y = Mathf.Clamp(current.y, parameters.edgePadding, parameters.roomHeight - parameters.edgePadding - 2);
            }
            
            // Ensure End is dug
            Dig(target.x - 1, target.y, 2, 2);
        }

        private void GenerateRandomRooms()
        {
            int totalTiles = parameters.roomWidth * parameters.roomHeight;
            int maxIterations = 100;
            int iteration = 0;

            // 1. Mandatory Initial Holes (Structure Variety)
            for (int i = 0; i < parameters.initialHolesCount; i++)
            {
                DigRandomRoom();
            }

            // 2. Fill to Target Openness
            while (iteration < maxIterations)
            {
                float currentRatio = CalculateOpenness();
                if (currentRatio >= parameters.targetOpenness) break;

                DigRandomRoom();
                iteration++;
            }
        }

        private float CalculateOpenness()
        {
            int floorCount = 0;
            for (int x = 0; x < parameters.roomWidth; x++)
            {
                for (int y = 0; y < parameters.roomHeight; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Wall) floorCount++;
                }
            }
            return (float)floorCount / (parameters.roomWidth * parameters.roomHeight);
        }

        private void DigRandomRoom()
        {
            int w = Random.Range(parameters.minRoomSize, parameters.maxRoomSize + 1);
            int h = Random.Range(parameters.minRoomSize, parameters.maxRoomSize + 1);
            
            // Pick a random X, Y
            int x = Random.Range(parameters.edgePadding + 1, parameters.roomWidth - parameters.edgePadding - w);
            int y = Random.Range(parameters.edgePadding + 1, parameters.roomHeight - parameters.edgePadding - h);
            
            // Dig
            Dig(x, y, w, h);
        }

        private void PostProcessPlatforms()
        {
            // 极简平台逻辑
            // 1. 最多 3 个平台
            // 2. 宽度仅限 3-5
            // 3. 仅在严格必要时添加（垂直间隙 > 3）
            // 4. 防止平台相互接触
            // 5. 保护出入口（门附近无平台）
            // 6. 区域排斥（防止聚类）

            int placed = 0;
            List<RectInt> exclusionZones = new List<RectInt>();
            int exclusionRadius = 4; // 强排斥半径，防止聚类

            // 第一遍：必要平台（修复间隙）
            int safetyMargin = 5; // 绝对安全距离（平台砖块不可进入的范围）
            int seedMargin = 6;   // 种子安全距离（平台中心点不可进入的范围，防止向左右扩展时太靠近）

            for (int x = 1; x < parameters.roomWidth - 1; x++)
            {
                if (placed >= 3) break;

                // 出入口保护（保持入口/出口畅通）
                // 增加安全距离，防止堵门
                if (Mathf.Abs(x - currentRoom.startPos.x) < seedMargin || Mathf.Abs(x - currentRoom.endPos.x) < seedMargin) continue;

                // 区域排斥检查
                
                int lastStandableY = -1; 
                for (int y = 0; y < parameters.roomHeight; y++)
                {
                    TileType t = currentRoom.GetTile(x, y);
                    if (t == TileType.Wall || t == TileType.Platform)
                    {
                        lastStandableY = y;
                    }
                    else
                    {
                        int gap = y - lastStandableY;
                        if (gap > 3)
                        {
                            // 尝试在 lastStandableY + 3 处放置平台
                            int platY = lastStandableY + 3;
                            
                            // 有效性检查：
                            // 1. 头顶空间
                            if (platY + 2 >= parameters.roomHeight) continue;
                            
                            // 2. 排斥区检查
                            if (IsInExclusionZone(x, platY, exclusionZones)) continue;

                            // 3. 避免垂直或水平接触其他平台（初始点）
                            if (IsTouchingPlatform(x, platY)) continue;

                            // 检查是否可以做得足够宽（3-5）
                            // 向左/右扫描空气（地面）空间，确保不撞到另一个平台
                            int l = 0, r = 0;
                            
                            // 向左扫描
                            for (int i = 1; i <= 2; i++)
                            {
                                int tx = x - i;
                                if (!currentRoom.IsValid(tx, platY) || currentRoom.GetTile(tx, platY) != TileType.Floor) break;
                                if (IsTouchingPlatform(tx, platY)) break; 
                                // 检查扩展时的出入口保护
                                if (Mathf.Abs(tx - currentRoom.startPos.x) < safetyMargin || Mathf.Abs(tx - currentRoom.endPos.x) < safetyMargin) break;
                                if (IsInExclusionZone(tx, platY, exclusionZones)) break; // 检查部分的排斥区
                                
                                // 【新增】下方净空检查：确保平台下有2格高空间供通行
                                if (currentRoom.GetTile(tx, platY - 1) != TileType.Floor || 
                                    currentRoom.GetTile(tx, platY - 2) != TileType.Floor) break;

                                l++;
                            }
                            
                            // 向右扫描
                            for (int i = 1; i <= 2; i++)
                            {
                                int tx = x + i;
                                if (!currentRoom.IsValid(tx, platY) || currentRoom.GetTile(tx, platY) != TileType.Floor) break;
                                if (IsTouchingPlatform(tx, platY)) break;
                                if (Mathf.Abs(tx - currentRoom.startPos.x) < safetyMargin || Mathf.Abs(tx - currentRoom.endPos.x) < safetyMargin) break;
                                if (IsInExclusionZone(tx, platY, exclusionZones)) break;

                                // 【新增】下方净空检查：确保平台下有2格高空间供通行
                                if (currentRoom.GetTile(tx, platY - 1) != TileType.Floor || 
                                    currentRoom.GetTile(tx, platY - 2) != TileType.Floor) break;

                                r++;
                            }
                            
                            int width = 1 + l + r;
                            if (width >= 3) // 只有能做成像样的平台才放置
                            {
                                currentRoom.SetTile(x, platY, TileType.Platform);
                                for(int i=1; i<=l; i++) currentRoom.SetTile(x-i, platY, TileType.Platform);
                                for(int i=1; i<=r; i++) currentRoom.SetTile(x+i, platY, TileType.Platform);
                                
                                // 添加到排斥区
                                // 区域覆盖平台 + 半径
                                int startX = x - l;
                                int startY = platY;
                                RectInt zone = new RectInt(startX - exclusionRadius, startY - exclusionRadius, 
                                                           width + exclusionRadius * 2, 1 + exclusionRadius * 2);
                                exclusionZones.Add(zone);

                                placed++;
                                lastStandableY = platY;
                            }
                        }
                    }
                }
            }
        }

        private bool IsInExclusionZone(int x, int y, List<RectInt> zones)
        {
            foreach (var z in zones)
            {
                if (z.Contains(new Vector2Int(x, y))) return true;
            }
            return false;
        }

        private bool IsTouchingPlatform(int x, int y)
        {
            // Check 4-neighbors (Up, Down, Left, Right)
            // Maybe check Diagonals too if we want strict separation? Let's stick to 4-neighbors for "linking"
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (currentRoom.IsValid(nx, ny) && currentRoom.GetTile(nx, ny) == TileType.Platform)
                    return true;
            }
            return false;
        }

        // Removed DigInitialHoles (Merged into GenerateRandomRooms/MainPath logic or deprecated)
        
        private void InitializeSeed()
        {
            if (parameters.useRandomSeed)
            {
                parameters.seed = System.DateTime.Now.Ticks.ToString();
            }
            Random.InitState(parameters.seed.GetHashCode());
        }

        private void InitializeGrid()
        {
            if (parameters.roomWidth < 5) parameters.roomWidth = 5;
            if (parameters.roomHeight < 5) parameters.roomHeight = 5;

            currentRoom = new RoomData(parameters.roomWidth, parameters.roomHeight);
            
            // Determine Anchors
            int entY = parameters.entranceY == -1 ? Random.Range(parameters.edgePadding, parameters.roomHeight - parameters.edgePadding) : parameters.entranceY;
            int extY = parameters.exitY == -1 ? Random.Range(parameters.edgePadding, parameters.roomHeight - parameters.edgePadding) : parameters.exitY;

            // Clamp anchors (ensure space for 2 tiles high)
            entY = Mathf.Clamp(entY, parameters.edgePadding, parameters.roomHeight - 2 - parameters.edgePadding);
            extY = Mathf.Clamp(extY, parameters.edgePadding, parameters.roomHeight - 2 - parameters.edgePadding);

            currentRoom.startPos = new Vector2Int(0, entY);
            currentRoom.endPos = new Vector2Int(parameters.roomWidth - 1, extY);

            // Ensure anchors are floors (2x2)
            if (parameters.enforceAnchors)
            {
                // Entrance (Left)
                Dig(currentRoom.startPos.x, currentRoom.startPos.y, 2, 2);
                
                // Exit (Right) - Shift left by 1 to fit 2x2 inside bounds if needed
                // Actually endPos is at width-1. 
                // To dig 2 wide ending at width-1, we start at width-2.
                Dig(currentRoom.endPos.x - 1, currentRoom.endPos.y, 2, 2);
            }
        }
        
        // Helper to dig brush (Square)
        private void Dig(int x, int y)
        {
            Dig(x, y, parameters.pathWidth, parameters.pathWidth);
        }

        // Helper to dig brush (Rect)
        private void Dig(int x, int y, int w, int h)
        {
            for (int dx = 0; dx < w; dx++)
            {
                for (int dy = 0; dy < h; dy++)
                {
                    if (currentRoom.IsValid(x + dx, y + dy))
                    {
                        currentRoom.SetTile(x + dx, y + dy, TileType.Floor);
                    }
                }
            }
        }

        private void RunRandomWalk()
        {
            for (int i = 0; i < parameters.walkerCount; i++)
            {
                Vector2Int currentPos = currentRoom.startPos;
                Vector2Int direction = Vector2Int.right; 

                for (int step = 0; step < parameters.maxSteps; step++)
                {
                    if (Random.value < parameters.turnProbability)
                    {
                        direction = GetRandomDirection();
                    }

                    Vector2Int nextPos = currentPos + direction;

                    // Bounds Check - adjust for path width
                    if (nextPos.x >= parameters.edgePadding && nextPos.x < parameters.roomWidth - parameters.edgePadding - parameters.pathWidth &&
                        nextPos.y >= parameters.edgePadding && nextPos.y < parameters.roomHeight - parameters.edgePadding - parameters.pathWidth)
                    {
                        currentPos = nextPos;
                        Dig(currentPos.x, currentPos.y);
                    }
                    else
                    {
                        direction = GetRandomDirection();
                    }
                }
            }
        }

        private void EnsureConnectivity()
        {
            if (IsConnected(currentRoom.startPos, currentRoom.endPos)) return;

            Vector2Int current = currentRoom.startPos;
            Vector2Int target = currentRoom.endPos;
            
            int safetyCounter = 0;
            int maxStitchSteps = parameters.roomWidth * parameters.roomHeight;

            while (Vector2Int.Distance(current, target) > parameters.pathWidth && safetyCounter < maxStitchSteps)
            {
                safetyCounter++;
                Dig(current.x, current.y);

                int dx = target.x - current.x;
                int dy = target.y - current.y;
                Vector2Int moveDir = Vector2Int.zero;

                float r = Random.value;
                
                // Improved Steering: Move horizontal more often to create walkable platforms
                // If moving vertical, try to zigzag or just rely on platforms being generated later
                
                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                {
                    if (r < 0.8f) moveDir = new Vector2Int(System.Math.Sign(dx), 0); // 80% chance horizontal
                    else moveDir = new Vector2Int(0, System.Math.Sign(dy));
                }
                else
                {
                    // Vertical gap
                    if (r < 0.6f) moveDir = new Vector2Int(0, System.Math.Sign(dy));
                    else moveDir = new Vector2Int(System.Math.Sign(dx) != 0 ? System.Math.Sign(dx) : 1, 0);
                }
                
                Vector2Int next = current + moveDir;
                next.x = Mathf.Clamp(next.x, 0, parameters.roomWidth - 1 - parameters.pathWidth);
                next.y = Mathf.Clamp(next.y, 0, parameters.roomHeight - 1 - parameters.pathWidth);
                current = next;
            }
            Dig(target.x, target.y);
        }
        
        private void RemoveDisconnectedIslands()
        {
            // Flood fill from StartPos to find all reachable tiles
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

            // Remove unreachable floor tiles
            for (int x = 0; x < parameters.roomWidth; x++)
            {
                for (int y = 0; y < parameters.roomHeight; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Wall && !reachable[x, y])
                    {
                        currentRoom.SetTile(x, y, TileType.Wall); // Fill dead end
                    }
                }
            }
            
            // Re-build floorTiles list
            currentRoom.floorTiles.Clear();
            for (int x = 0; x < parameters.roomWidth; x++)
            {
                for (int y = 0; y < parameters.roomHeight; y++)
                {
                    if (currentRoom.GetTile(x, y) == TileType.Floor)
                    {
                        currentRoom.floorTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private void IdentifySpawnPoints()
        {
            currentRoom.potentialSpawns.Clear();
            IdentifyGroundSpawns();
            IdentifyAirSpawns();
            
            // Filter: Max Count & Min Distance
            if (currentRoom.potentialSpawns.Count > 0)
            {
                // Shuffle list (simple Fisher-Yates)
                for (int i = 0; i < currentRoom.potentialSpawns.Count; i++)
                {
                    SpawnPoint temp = currentRoom.potentialSpawns[i];
                    int randomIndex = Random.Range(i, currentRoom.potentialSpawns.Count);
                    currentRoom.potentialSpawns[i] = currentRoom.potentialSpawns[randomIndex];
                    currentRoom.potentialSpawns[randomIndex] = temp;
                }

                List<SpawnPoint> selected = new List<SpawnPoint>();
                foreach (var candidate in currentRoom.potentialSpawns)
                {
                    if (selected.Count >= parameters.maxEnemies) break;

                    bool tooClose = false;
                    foreach (var s in selected)
                    {
                        if (Vector2Int.Distance(candidate.position, s.position) < parameters.minSpawnDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose) selected.Add(candidate);
                }
                currentRoom.potentialSpawns = selected;
            }
        }

        private void IdentifyGroundSpawns()
        {
            // Scan for horizontal floor segments
            for (int y = 0; y < parameters.roomHeight; y++)
            {
                int consecutiveFloor = 0;
                int startX = -1;

                for (int x = 0; x < parameters.roomWidth; x++)
                {
                    bool isFloor = currentRoom.GetTile(x, y) == TileType.Floor;
                    bool hasGroundBelow = y == 0 || currentRoom.GetTile(x, y - 1) == TileType.Wall || currentRoom.GetTile(x, y - 1) == TileType.Platform;
                    
                    // Check headroom (2 blocks above must be free)
                    bool hasHeadroom = true;
                    for (int h = 1; h <= 2; h++)
                    {
                        // 严格检查：必须是地板(空气)，不能是墙也不能是平台
                        if (!currentRoom.IsValid(x, y + h) || currentRoom.GetTile(x, y + h) != TileType.Floor)
                        {
                            hasHeadroom = false;
                            break;
                        }
                    }

                    // A valid ground spawn spot is: Floor (Air) + Solid Below + Headroom
                    // Wait, TileType.Floor means "Walkable Space" (Air), correct?
                    // Let's re-verify definitions.
                    // TileType.Floor = 1 (地面/可行走). In my visualization, Floor is white (empty space). Wall is black.
                    // So "Ground Spawn" means: Current tile is Floor (Air), Tile BELOW is Wall/Platform.
                    
                    if (isFloor && hasGroundBelow && hasHeadroom)
                    {
                        if (startX == -1) startX = x;
                        consecutiveFloor++;
                    }
                    else
                    {
                        // Sequence broken
                        if (consecutiveFloor >= parameters.minGroundSpan)
                        {
                            AddGroundSpawns(startX, x - 1, y);
                        }
                        consecutiveFloor = 0;
                        startX = -1;
                    }
                }
                
                // End of row check
                if (consecutiveFloor >= parameters.minGroundSpan)
                {
                    AddGroundSpawns(startX, parameters.roomWidth - 1, y);
                }
            }
        }

        private void AddGroundSpawns(int startX, int endX, int y)
        {
            // Add the middle point, or multiple points?
            // Let's add the center of the span for now
            int centerX = (startX + endX) / 2;
            currentRoom.potentialSpawns.Add(new SpawnPoint 
            { 
                position = new Vector2Int(centerX, y), 
                type = SpawnType.Ground 
            });
        }

        private void IdentifyAirSpawns()
        {
            for (int x = 1; x < parameters.roomWidth - 1; x++)
            {
                for (int y = 1; y < parameters.roomHeight - 1; y++)
                {
                    if (currentRoom.GetTile(x, y) != TileType.Floor) continue;

                    // Check surrounding openness (1 tile radius)
                    if (currentRoom.GetTile(x + 1, y) != TileType.Floor ||
                        currentRoom.GetTile(x - 1, y) != TileType.Floor ||
                        currentRoom.GetTile(x, y + 1) != TileType.Floor ||
                        currentRoom.GetTile(x, y - 1) != TileType.Floor)
                    {
                        continue;
                    }

                    // Check height from ground
                    int distToGround = 0;
                    bool groundFound = false;
                    for (int dy = 1; y - dy >= 0; dy++)
                    {
                        if (currentRoom.GetTile(x, y - dy) == TileType.Wall || 
                            currentRoom.GetTile(x, y - dy) == TileType.Platform)
                        {
                            distToGround = dy;
                            groundFound = true;
                            break;
                        }
                    }

                    if (groundFound && distToGround >= parameters.minAirHeight)
                    {
                        // Valid Air Spawn
                        // Avoid clustering? Simple grid based check for now.
                        // Let's just add it if it passes. 
                        // Maybe add a random check to reduce density?
                        if (Random.value < 0.2f) // 20% chance to mark a valid air spot
                        {
                            currentRoom.potentialSpawns.Add(new SpawnPoint 
                            { 
                                position = new Vector2Int(x, y), 
                                type = SpawnType.Air 
                            });
                        }
                    }
                }
            }
        }

        private bool IsConnected(Vector2Int start, Vector2Int end)
        {
            if (currentRoom.GetTile(start.x, start.y) == TileType.Wall || 
                currentRoom.GetTile(end.x, end.y) == TileType.Wall) return false;

            bool[,] visited = new bool[parameters.roomWidth, parameters.roomHeight];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (queue.Count > 0)
            {
                Vector2Int p = queue.Dequeue();
                if (p == end) return true;

                foreach (var dir in dirs)
                {
                    Vector2Int n = p + dir;
                    if (currentRoom.IsValid(n.x, n.y) && 
                        currentRoom.GetTile(n.x, n.y) != TileType.Wall && 
                        !visited[n.x, n.y])
                    {
                        visited[n.x, n.y] = true;
                        queue.Enqueue(n);
                    }
                }
            }
            return false;
        }

        private Vector2Int GetRandomDirection()
        {
            int dir = Random.Range(0, 4);
            switch (dir)
            {
                case 0: return Vector2Int.up;
                case 1: return Vector2Int.down;
                case 2: return Vector2Int.left;
                case 3: return Vector2Int.right;
                default: return Vector2Int.right;
            }
        }

        // Helper for Editor
        public void BakeToTilemap()
        {
            if (targetTilemap == null || currentRoom == null) return;

            targetTilemap.ClearAllTiles();
            if (platformTilemap != null) platformTilemap.ClearAllTiles();

            // 烘焙填充：在房间数据外围额外画几圈墙，以触发Rule Tile的正确连接规则
            int bakePadding = 3; 

            for (int x = -bakePadding; x < currentRoom.width + bakePadding; x++)
            {
                for (int y = -bakePadding; y < currentRoom.height + bakePadding; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    
                    // 检查坐标是否在房间数据范围内
                    if (currentRoom.IsValid(x, y))
                    {
                        TileType type = currentRoom.GetTile(x, y);
                        
                        if (type == TileType.Wall)
                        {
                            // 定义一个局部函数来检查某个位置是否是“实体墙” (越界也视为墙，保证边界厚度)
                            bool IsSolidWall(int tx, int ty)
                            {
                                if (!currentRoom.IsValid(tx, ty)) return true; // 边界外视为墙
                                return currentRoom.GetTile(tx, ty) == TileType.Wall;
                            }

                            // 检查是否是“细长”的墙壁结构 (横梁或立柱)
                            // 1. 横梁: 上下都不是墙
                            bool isBeam = !IsSolidWall(x, y + 1) && !IsSolidWall(x, y - 1);
                            // 2. 立柱: 左右都不是墙
                            bool isPillar = !IsSolidWall(x - 1, y) && !IsSolidWall(x + 1, y);

                            // 3. 孤立: 四周都不是墙 (已包含在上述逻辑中：如果是孤立，既是Beam也是Pillar)

                            if ((isBeam || isPillar) && currentTheme.singlePlatformTile != null)
                            {
                                // 使用独立瓦片渲染细长结构 (视为装饰性墙壁，通常放在 Wall 层，或者如果需要单向跳过也可以放 Platform 层？)
                                // 这里假设细长结构仍然是阻挡物，或者是背景装饰。如果是装饰，放在 Wall 层没问题。
                                targetTilemap.SetTile(tilePos, currentTheme.singlePlatformTile);
                            }
                            else
                            {
                                if (currentTheme.wallTile != null)
                                    targetTilemap.SetTile(tilePos, currentTheme.wallTile);
                            }
                        }
                        else if (type == TileType.Platform)
                        {
                            // 检查是否是孤立平台 (1x1)
                            bool isSingle = true;
                            // 检查上下左右
                            int[] dx = { 0, 0, -1, 1 };
                            int[] dy = { 1, -1, 0, 0 };
                            for(int i=0; i<4; i++)
                            {
                                int nx = x + dx[i];
                                int ny = y + dy[i];
                                // 如果邻居是平台，则不是孤立的
                                if (currentRoom.IsValid(nx, ny) && currentRoom.GetTile(nx, ny) == TileType.Platform)
                                {
                                    isSingle = false;
                                    break;
                                }
                            }

                            // 决定使用哪个 Tilemap
                            Tilemap destMap = platformTilemap != null ? platformTilemap : targetTilemap;

                            if (isSingle && currentTheme.singlePlatformTile != null)
                            {
                                destMap.SetTile(tilePos, currentTheme.singlePlatformTile);
                            }
                            else
                            {
                                if (currentTheme.platformTile != null)
                                    destMap.SetTile(tilePos, currentTheme.platformTile);
                            }
                        }
                        else if (type == TileType.Floor)
                        {
                             if (currentTheme.backgroundTile != null)
                                targetTilemap.SetTile(tilePos, currentTheme.backgroundTile);
                             else
                                targetTilemap.SetTile(tilePos, null);
                        }
                    }
                    else
                    {
                        // 范围外 (Padding 区域)
                        // 需要处理出入口的通道，不能把出入口堵死

                        bool isEntrancePath = false;
                        bool isExitPath = false;

                        // 检查左侧入口通道 (x < 0)
                        if (x < 0)
                        {
                            // 对应 StartPos 的 Y 轴范围 (高度2)
                            if (y >= currentRoom.startPos.y && y < currentRoom.startPos.y + 2)
                            {
                                isEntrancePath = true;
                            }
                        }

                        // 检查右侧出口通道 (x >= width)
                        if (x >= currentRoom.width)
                        {
                            // 对应 EndPos 的 Y 轴范围 (高度2)
                            if (y >= currentRoom.endPos.y && y < currentRoom.endPos.y + 2)
                            {
                                isExitPath = true;
                            }
                        }

                        if (isEntrancePath || isExitPath)
                        {
                            // 如果是通道路径，不填墙，留空 (null)
                            targetTilemap.SetTile(tilePos, null);
                        }
                        else
                        {
                            // 其他区域强制填充墙壁，作为边缘的支撑
                            if (currentTheme.wallTile != null)
                                targetTilemap.SetTile(tilePos, currentTheme.wallTile);
                        }
                    }
                }
            }
            
            // Broadcast Anchors Position (Run in both Play Mode and Editor Mode)
            BroadcastRoomAnchors();
        }

        private void BroadcastRoomAnchors()
        {
            if (currentRoom == null || targetTilemap == null) return;

            // Calculate World Positions
            Vector3 startWorld = targetTilemap.CellToWorld(new Vector3Int(currentRoom.startPos.x, currentRoom.startPos.y, 0)) + targetTilemap.tileAnchor;
            Vector3 endWorld = targetTilemap.CellToWorld(new Vector3Int(currentRoom.endPos.x, currentRoom.endPos.y, 0)) + targetTilemap.tileAnchor;

            // Directions: Left (-1,0), Right (1,0)
            Vector2Int startDir = Vector2Int.left;
            Vector2Int endDir = Vector2Int.right;

            RoomAnchorsData data = new RoomAnchorsData
            {
                startGridPos = currentRoom.startPos,
                endGridPos = currentRoom.endPos,
                startWorldPos = startWorld,
                endWorldPos = endWorld,
                startDirection = startDir,
                endDirection = endDir
            };

            if (Application.isPlaying)
            {
                MessageManager.Instance.Send(MessageDefine.ROOM_ANCHORS_UPDATE, data);
            }
            else
            {
                // Local helper to convert direction to text
                string ToDirText(Vector2Int dir)
                {
                    if (dir == Vector2Int.left) return "Left (左)";
                    if (dir == Vector2Int.right) return "Right (右)";
                    if (dir == Vector2Int.up) return "Up (上)";
                    if (dir == Vector2Int.down) return "Down (下)";
                    return "Unknown";
                }

                // In Editor Mode, log with formatted text
                Debug.Log($"[RoomGenerator] Room Generated.\n" +
                          $"[方位/Direction] Entrance: {ToDirText(data.startDirection)} | Exit: {ToDirText(data.endDirection)}\n" +
                          $"[瓦片坐标/Grid] Entrance: {data.startGridPos} | Exit: {data.endGridPos}\n" +
                          $"[世界坐标/World] Entrance: {data.startWorldPos} | Exit: {data.endWorldPos}");
            }
        }
    }
}
