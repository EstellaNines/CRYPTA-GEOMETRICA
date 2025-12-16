# 房间生成器 V2 技术设计文档

## 1. 系统架构

### 1.1 整体架构图

```mermaid
graph TB
    subgraph Core[核心模块]
        RGV2[RoomGeneratorV2<br/>主控制器]
        RGP[RoomGenParamsV2<br/>生成参数]
        RDV2[RoomDataV2<br/>房间数据]
        RTV2[RoomThemeV2<br/>视觉主题]
    end
    
    subgraph Data[数据结构]
        BSP[BSPNode<br/>BSP节点]
        RG[RoomGraph<br/>房间图]
        RR[RoomRegion<br/>房间区域]
        RE[RoomEdge<br/>连接边]
        SP[SpawnPointV2<br/>生成点]
    end
    
    subgraph Generators[生成器模块]
        BSPG[BSPGenerator<br/>BSP空间分割]
        RP[RoomPlacer<br/>房间放置]
        CB[CorridorBuilder<br/>走廊生成]
        CG[ConnectivityGuarantor<br/>连通性保障]
        PI[PlatformInjector<br/>平台注入]
        SRG[SpecialRoomGenerator<br/>特殊房间]
    end
    
    subgraph Utils[工具模块]
        DT[DelaunayTriangulation<br/>三角剖分]
        MST[MinimumSpanningTree<br/>最小生成树]
    end
    
    RGV2 --> RGP
    RGV2 --> RDV2
    RGV2 --> RTV2
    RGV2 --> Generators
    
    RDV2 --> BSP
    RDV2 --> RG
    RDV2 --> SP
    
    RG --> RR
    RG --> RE
    BSP --> RR
    
    BSPG --> BSP
    RP --> RR
    CB --> RE
    
    Generators --> Utils
```

### 1.2 目录结构

```
SmallRoom v0.2/
├── Core/                           # 核心模块
│   ├── RoomGeneratorV2.cs          # 主生成器控制器
│   ├── RoomGenParamsV2.cs          # 生成参数配置
│   └── TileTypeV2.cs               # 瓦片类型枚举
│
├── Data/                           # 数据结构
│   ├── RoomDataV2.cs               # 房间数据容器
│   ├── BSPNode.cs                  # BSP树节点
│   ├── RoomGraph.cs                # 房间连接图
│   ├── RoomRegion.cs               # 房间区域
│   └── SpawnPointV2.cs             # 敌人生成点
│
├── Generators/                     # 生成器模块
│   ├── BSPGenerator.cs             # BSP空间分割
│   ├── RoomPlacer.cs               # 房间放置器
│   ├── CorridorBuilder.cs          # 走廊生成器
│   ├── ConnectivityGuarantor.cs    # 连通性保障器
│   ├── PlatformInjector.cs         # 平台注入器
│   └── SpecialRoomGenerator.cs     # 特殊房间生成器
│
├── Settings/                       # 配置模块
│   └── RoomGenerationSettingsV2.cs # ScriptableObject配置
│
├── Utils/                          # 工具模块
│   ├── DelaunayTriangulation.cs    # Delaunay三角剖分
│   └── MinimumSpanningTree.cs      # 最小生成树算法
│
└── Documentation/                  # 文档
    ├── README_RoomGeneratorV2.md   # 功能说明文档
    ├── TECHNICAL_DESIGN.md         # 技术设计文档（本文档）
    ├── RULES.md                    # 规则文档
    └── API_REFERENCE.md            # API参考文档
```

---

## 2. 核心数据结构

### 2.1 RoomDataV2 (房间数据)

```mermaid
classDiagram
    class RoomDataV2 {
        +int[,] grid
        +int width
        +int height
        +Vector2Int startPos
        +Vector2Int endPos
        +List~Vector2Int~ floorTiles
        +List~SpawnPointV2~ potentialSpawns
        +BSPNode bspRoot
        +RoomGraph roomGraph
        +string seed
        +bool needsDoorAtExit
        +SetTile(x, y, type)
        +GetTile(x, y) TileType
        +IsValid(x, y) bool
        +IsWalkable(x, y) bool
        +IsSolid(x, y) bool
        +Fill(type)
        +FillRect(rect, type)
        +DigRect(rect)
        +DigBrush(x, y, size)
        +RebuildFloorTiles()
    }
```

**数据流向图：**

```mermaid
flowchart LR
    subgraph Init[初始化]
        GRID1[grid<br/>全墙壁]
    end
    
    subgraph BSP[BSP分割]
        BSPROOT[bspRoot<br/>BSP树]
    end
    
    subgraph Place[房间放置]
        ROOMS[roomGraph.rooms]
        GRID2[grid<br/>挖掘房间]
    end
    
    subgraph Connect[图连接]
        EDGES[roomGraph.edges]
        GRID3[grid<br/>挖掘走廊]
    end
    
    subgraph Ensure[连通性保障]
        GRID4[grid<br/>游走挖掘]
    end
    
    subgraph Platform[平台注入]
        GRID5[grid<br/>平台注入]
    end
    
    subgraph Post[后处理]
        SPAWNS[potentialSpawns]
    end
    
    Init --> BSP --> Place --> Connect --> Ensure --> Platform --> Post
```

### 2.2 BSPNode (BSP树节点)

```mermaid
classDiagram
    class BSPNode {
        +RectInt bounds
        +BSPNode left
        +BSPNode right
        +RoomRegion room
        +int depth
        +SplitDirection splitDirection
        +int splitPosition
        +IsLeaf bool
        +Center Vector2Int
        +Width int
        +Height int
        +GetLeaves() List~BSPNode~
        +GetRooms() List~RoomRegion~
        +Contains(point) bool
    }
    
    BSPNode --> BSPNode : left
    BSPNode --> BSPNode : right
    BSPNode --> RoomRegion : room
```

**BSP树结构示意：**

```mermaid
graph TB
    ROOT[Root<br/>depth=0<br/>bounds: 0,0,40,25<br/>split: Vertical@20]
    
    LEFT[Left<br/>depth=1<br/>bounds: 0,0,20,25<br/>split: Horizontal@12]
    RIGHT[Right<br/>depth=1<br/>bounds: 20,0,20,25<br/>split: Horizontal@12]
    
    LEAF0[Leaf 0<br/>Room 0]
    LEAF1[Leaf 1<br/>Room 1]
    LEAF2[Leaf 2<br/>Room 2]
    LEAF3[Leaf 3<br/>Room 3]
    
    ROOT --> LEFT
    ROOT --> RIGHT
    LEFT --> LEAF0
    LEFT --> LEAF1
    RIGHT --> LEAF2
    RIGHT --> LEAF3
```

### 2.3 RoomGraph (房间连接图)

```mermaid
classDiagram
    class RoomGraph {
        +List~RoomRegion~ rooms
        +List~RoomEdge~ allEdges
        +List~RoomEdge~ mstEdges
        +List~RoomEdge~ extraEdges
        +List~RoomEdge~ finalEdges
        +RoomCount int
        +EdgeCount int
        +GetRoom(id) RoomRegion
        +GetConnectedRooms(roomId) List~RoomRegion~
        +AreConnected(roomA, roomB) bool
        +AddEdge(edge)
        +BuildFinalEdges()
    }
    
    class RoomEdge {
        +int roomA
        +int roomB
        +float distance
        +bool isMST
    }
    
    RoomGraph --> RoomRegion : rooms
    RoomGraph --> RoomEdge : edges
```

**图连接示意：**

```mermaid
graph LR
    subgraph Delaunay[Delaunay三角剖分<br/>所有潜在连接]
        D_R0((R0)) --- D_R1((R1))
        D_R0 --- D_R2((R2))
        D_R0 --- D_R3((R3))
        D_R1 --- D_R3
        D_R2 --- D_R3
    end
    
    subgraph MST[最小生成树<br/>必要连接]
        M_R0((R0)) --- M_R1((R1))
        M_R0 --- M_R2((R2))
        M_R2 --- M_R3((R3))
    end
    
    subgraph Final[最终连接图<br/>MST + 额外边]
        F_R0((R0)) --- F_R1((R1))
        F_R0 --- F_R2((R2))
        F_R1 -.- F_R3((R3))
        F_R2 --- F_R3
    end
    
    Delaunay --> MST --> Final
```

### 2.4 RoomRegion (房间区域)

```mermaid
classDiagram
    class RoomRegion {
        +int id
        +RectInt bounds
        +Vector2Int center
        +List~Vector2Int~ floorTiles
        +bool isEntrance
        +bool isExit
        +RectInt bspBounds
        +RoomType roomType
        +Width int
        +Height int
        +Area int
        +Contains(point) bool
        +Overlaps(rect) bool
        +DistanceTo(other) float
        +GetClosestPointTo(target) Vector2Int
    }
```

---

## 3. 核心算法

### 3.1 BSP空间分割算法

```mermaid
flowchart TD
    START[Split bounds, depth] --> CREATE[创建节点 node]
    CREATE --> CHECK_TERM{检查终止条件}
    
    CHECK_TERM -->|depth >= maxDepth| RETURN_LEAF[返回叶节点]
    CHECK_TERM -->|size < minSize*2| RETURN_LEAF
    CHECK_TERM -->|rooms >= target & random < 0.7| RETURN_LEAF
    CHECK_TERM -->|继续分割| DECIDE_DIR{决定分割方向}
    
    DECIDE_DIR -->|aspectRatio > 1.25| VERTICAL[垂直分割]
    DECIDE_DIR -->|aspectRatio < 0.8| HORIZONTAL[水平分割]
    DECIDE_DIR -->|0.8~1.25| RANDOM[随机选择]
    
    VERTICAL --> CALC_POS[计算分割位置]
    HORIZONTAL --> CALC_POS
    RANDOM --> CALC_POS
    
    CALC_POS --> VALIDATE{验证分割有效性}
    VALIDATE -->|无效| RETURN_LEAF
    VALIDATE -->|有效| SPLIT[执行分割]
    
    SPLIT --> RECURSE_L[递归分割左子节点]
    SPLIT --> RECURSE_R[递归分割右子节点]
    
    RECURSE_L --> RETURN[返回 node]
    RECURSE_R --> RETURN
```

**分割方向决策：**

```mermaid
graph LR
    subgraph Tall[太高 aspectRatio < 0.8]
        T_BEFORE[高矩形] --> T_AFTER[水平分割<br/>上下分]
    end
    
    subgraph Normal[正常 0.8~1.25]
        N_BEFORE[接近正方形] --> N_AFTER[随机选择<br/>水平或垂直]
    end
    
    subgraph Wide[太宽 aspectRatio > 1.25]
        W_BEFORE[宽矩形] --> W_AFTER[垂直分割<br/>左右分]
    end
```

### 3.2 Delaunay三角剖分 (Bowyer-Watson算法)

```mermaid
flowchart TD
    START[输入: 点集 points] --> SUPER[创建超级三角形]
    SUPER --> INIT[triangles = 超级三角形]
    
    INIT --> LOOP{遍历每个点}
    LOOP -->|有点| FIND_BAD[找到外接圆包含该点的三角形]
    FIND_BAD --> FIND_POLY[找到多边形边界<br/>非共享边]
    FIND_POLY --> REMOVE[移除坏三角形]
    REMOVE --> CREATE_NEW[用新点创建新三角形]
    CREATE_NEW --> LOOP
    
    LOOP -->|完成| CLEANUP[移除包含超级三角形顶点的三角形]
    CLEANUP --> EXTRACT[提取边并转换为 RoomEdge]
    EXTRACT --> RETURN[返回边列表]
```

**外接圆判断：**

```mermaid
graph TB
    subgraph CircumcircleTest[外接圆测试]
        TRI[三角形 ABC]
        CENTER((圆心))
        POINT[测试点 P]
        
        TRI --> CENTER
        CENTER -->|distance <= radius| INSIDE[点在圆内]
        CENTER -->|distance > radius| OUTSIDE[点在圆外]
    end
```

### 3.3 Kruskal最小生成树算法

```mermaid
flowchart TD
    START[输入: rooms, edges] --> INIT_UF[初始化并查集]
    INIT_UF --> SORT[按距离排序边]
    SORT --> INIT_MST[mst = 空列表]
    
    INIT_MST --> LOOP{遍历排序后的边}
    LOOP -->|有边| CHECK{两端点是否已连通?}
    CHECK -->|否| ADD[添加边到 MST<br/>合并两个集合]
    CHECK -->|是| SKIP[跳过]
    
    ADD --> CHECK_DONE{MST边数 = 房间数-1?}
    SKIP --> LOOP
    CHECK_DONE -->|否| LOOP
    CHECK_DONE -->|是| RETURN[返回 MST]
    LOOP -->|完成| RETURN
```

**并查集操作：**

```mermaid
graph LR
    subgraph UnionFind[并查集]
        FIND[Find x<br/>查找根节点<br/>带路径压缩]
        UNION[Union x,y<br/>合并集合<br/>按秩合并]
        CONNECTED[Connected x,y<br/>检查是否同集合]
    end
```

### 3.4 双向随机游走算法

```mermaid
flowchart TD
    START[EnsureConnectivity] --> FORWARD[正向游走: 入口 → 出口]
    FORWARD --> BACKWARD[反向游走: 出口 → 入口]
    BACKWARD --> VERIFY[验证连通性]
    
    subgraph RandomWalk[RandomWalkPath]
        RW_START[current = start] --> RW_LOOP{未到达目标?}
        RW_LOOP -->|是| DIG[挖掘当前位置<br/>只挖墙壁保留平台]
        DIG --> CALC_DIR[计算下一步方向]
        CALC_DIR --> MOVE[应用移动<br/>带边界检查]
        MOVE --> RW_LOOP
        RW_LOOP -->|否| RW_END[完成]
    end
```

**移动方向决策：**

```mermaid
flowchart TD
    START[计算方向] --> CHECK_BIAS{random < horizontalBias<br/>且 dx != 0?}
    CHECK_BIAS -->|是| HORIZONTAL[水平移动<br/>sign dx, 0]
    CHECK_BIAS -->|否| CHECK_DY{dy != 0?}
    CHECK_DY -->|是| VERTICAL[垂直移动<br/>0, sign dy]
    CHECK_DY -->|否| DEFAULT[默认方向<br/>preferRight ? 右 : 左]
```

### 3.5 跳跃可达性分析算法

```mermaid
flowchart TD
    START[InjectPlatforms] --> PROTECT[添加入口/出口保护区]
    PROTECT --> ROOM_PLAT[在每个房间内部注入阶梯式平台]
    ROOM_PLAT --> COLUMN[按列分析垂直落差]
    COLUMN --> HORIZONTAL[分析水平跳跃距离]
    HORIZONTAL --> VERIFY[验证可达性并修复]
    
    subgraph ColumnAnalysis[列分析]
        COL_SCAN[扫描列] --> FIND_GAP{找到落差?}
        FIND_GAP -->|gap > maxJumpHeight| INJECT[注入平台]
        FIND_GAP -->|gap <= maxJumpHeight| NEXT[下一列]
        INJECT --> NEXT
    end
```

**平台注入示意：**

```mermaid
graph TB
    subgraph VerticalGap[垂直落差分析]
        TOP[顶部]
        GAP1[落差区域<br/>gap > maxJumpHeight]
        PLAT1[注入平台1]
        GAP2[落差区域]
        PLAT2[注入平台2]
        BOTTOM[底部]
        
        TOP --> GAP1 --> PLAT1 --> GAP2 --> PLAT2 --> BOTTOM
    end
    
    subgraph HorizontalGap[水平落差分析]
        LEFT[左侧平台]
        H_GAP[水平落差<br/>gap > maxHorizontalJump]
        MID_PLAT[中间平台]
        RIGHT[右侧平台]
        
        LEFT --> H_GAP --> MID_PLAT --> RIGHT
    end
```

---

## 4. 模块职责

### 4.1 模块依赖关系

```mermaid
graph TB
    RGV2[RoomGeneratorV2] --> RGP[RoomGenParamsV2]
    RGV2 --> RDV2[RoomDataV2]
    RGV2 --> RTV2[RoomThemeV2]
    
    RGV2 --> BSPG[BSPGenerator]
    RGV2 --> RP[RoomPlacer]
    RGV2 --> CB[CorridorBuilder]
    RGV2 --> CG[ConnectivityGuarantor]
    RGV2 --> PI[PlatformInjector]
    RGV2 --> SRG[SpecialRoomGenerator]
    
    RP --> DT[DelaunayTriangulation]
    RP --> MST[MinimumSpanningTree]
```

### 4.2 各模块职责

| 模块 | 职责 | 输入 | 输出 |
|------|------|------|------|
| **RoomGeneratorV2** | 协调各生成器模块的执行顺序 | RoomGenParamsV2 | RoomDataV2 |
| **BSPGenerator** | 递归分割空间生成BSP树 | RoomGenParamsV2, Random | BSPNode |
| **RoomPlacer** | 在BSP叶节点内创建房间区域 | BSPNode, RoomDataV2 | List\<RoomRegion\> |
| **CorridorBuilder** | 根据RoomGraph生成走廊 | RoomGraph, RoomDataV2 | 修改后的grid |
| **ConnectivityGuarantor** | 执行双向随机游走确保连通性 | RoomDataV2 | 修改后的grid |
| **PlatformInjector** | 分析落差并注入平台 | RoomDataV2 | 修改后的grid |
| **SpecialRoomGenerator** | 生成入口房间和Boss房间 | RoomDataV2, RoomGenParamsV2 | 修改后的RoomDataV2 |

---

## 5. 性能考虑

### 5.1 时间复杂度

| 阶段 | 算法 | 时间复杂度 | 说明 |
|------|------|------------|------|
| BSP分割 | 递归分割 | O(n) | n = 叶节点数 |
| 房间放置 | 遍历叶节点 | O(n) | n = 叶节点数 |
| Delaunay | Bowyer-Watson | O(n²) | n = 房间数 |
| MST | Kruskal | O(E log E) | E = 边数 |
| 走廊生成 | 遍历边 | O(E × W) | W = 走廊宽度 |
| 连通性 | 随机游走 | O(W × H) | W,H = 房间尺寸 |
| 平台注入 | 列扫描 | O(W × H) | W,H = 房间尺寸 |
| 后处理 | BFS | O(W × H) | W,H = 房间尺寸 |

### 5.2 空间复杂度

| 数据结构 | 空间复杂度 | 说明 |
|----------|------------|------|
| grid | O(W × H) | 二维网格 |
| BSP树 | O(2n - 1) | n = 叶节点数 |
| RoomGraph | O(n + E) | n = 房间数, E = 边数 |
| floorTiles | O(W × H) | 最坏情况 |
| potentialSpawns | O(maxEnemies) | 有上限 |

---

## 6. 扩展点

### 6.1 新增房间类型

```mermaid
flowchart TD
    ENUM[1. 在 RoomType 枚举中添加新类型]
    SWITCH[2. 在 GenerateRoom 中添加分支]
    IMPL[3. 在 SpecialRoomGenerator 中实现生成逻辑]
    
    ENUM --> SWITCH --> IMPL
```

### 6.2 新增生成器模块

```mermaid
flowchart TD
    CREATE[1. 创建新的生成器类]
    INTEGRATE[2. 在 RoomGeneratorV2 中集成]
    CALL[3. 在生成流程中调用]
    
    CREATE --> INTEGRATE --> CALL
```

---

## 7. 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v0.1 | - | 初始版本，基础随机游走 |
| v0.2 | - | 重构：BSP分割 + 图连接 + 跳跃分析 |

---

