# 房间生成器 V2 (RoomGeneratorV2)

## 功能概述

房间生成器 V2 是一个基于 **BSP 空间分割 + 图连接 + 双向随机游走 + 跳跃可达性分析** 的程序化房间生成系统。它能够生成具有多个子房间、走廊连接、单向平台和敌人生成点的复杂 2D 横版关卡。

---

## 目录

1. [核心特性](#核心特性)
2. [生成流程](#生成流程)
3. [房间类型](#房间类型)
4. [瓦片类型图例](#瓦片类型图例)
5. [生成点类型图例](#生成点类型图例)
6. [编辑器使用指南](#编辑器使用指南)
7. [参数配置说明](#参数配置说明)

---

## 核心特性

| 特性 | 描述 |
|------|------|
| **BSP 空间分割** | 使用二叉空间分割算法将房间区域递归划分为更小的子区域 |
| **Delaunay 三角剖分** | 生成房间之间的潜在连接边，确保连接的合理性 |
| **Kruskal 最小生成树** | 从潜在连接中选择必要的连接，保证所有房间连通 |
| **双向随机游走** | 入口→出口 + 出口→入口 双向游走，确保玩家可通行 |
| **跳跃可达性分析** | 基于玩家跳跃能力分析垂直落差，自动注入单向平台 |
| **敌人生成点识别** | 自动识别适合地面敌人和空中敌人的生成位置 |
| **多房间类型支持** | 支持战斗房、入口房、Boss房等不同类型的房间生成 |

---

## 生成流程

### 阶段 1-4：结构生成

```mermaid
flowchart LR
    subgraph Phase1[Phase 1: 初始化]
        P1_1[初始化种子]
        P1_2[初始化网格]
        P1_3[选择主题]
    end
    
    subgraph Phase2[Phase 2: BSP 分割]
        P2_1[递归分割空间]
        P2_2[生成 BSP 树]
        P2_3[确定叶节点]
    end
    
    subgraph Phase3[Phase 3: 房间放置]
        P3_1[在叶节点放置房间]
        P3_2[挖掘房间区域]
        P3_3[标记入口/出口房]
    end
    
    subgraph Phase4[Phase 4: 图连接]
        P4_1[Delaunay 三角剖分]
        P4_2[Kruskal MST]
        P4_3[选择额外边]
    end
    
    Phase1 --> Phase2 --> Phase3 --> Phase4
```

### 阶段 5-8：细节处理

```mermaid
flowchart LR
    subgraph Phase5[Phase 5: 走廊生成]
        P5_1[L形走廊生成]
        P5_2[拐角处理]
        P5_3[走廊宽度控制]
    end
    
    subgraph Phase6[Phase 6: 连通性保障]
        P6_1[双向随机游走]
        P6_2[连通性验证]
        P6_3[路径修复]
    end
    
    subgraph Phase7[Phase 7: 平台注入]
        P7_1[垂直落差分析]
        P7_2[水平跳跃分析]
        P7_3[可达性修复]
    end
    
    subgraph Phase8[Phase 8: 后处理]
        P8_1[孤岛移除]
        P8_2[出入口清理]
        P8_3[生成点识别]
    end
    
    Phase5 --> Phase6 --> Phase7 --> Phase8
```

### 标准战斗房间生成流程详解

1. **Phase 1: 初始化**
   - 初始化随机种子（支持固定种子重现）
   - 创建网格数据结构，填充为全墙壁
   - 确定出入口 Y 坐标位置
   - 随机选择视觉主题

2. **Phase 2: BSP 空间分割**
   - 使用二叉空间分割算法递归划分空间
   - 根据宽高比决定水平或垂直分割
   - 在配置的比例范围内随机选择分割位置
   - 达到最小尺寸或目标房间数时停止

3. **Phase 3: 房间放置**
   - 在每个 BSP 叶节点内创建房间区域
   - 房间尺寸基于填充率计算，带随机变化
   - 挖掘房间区域（设置为地面瓦片）
   - 标记包含入口/出口的房间

4. **Phase 4: 图连接**
   - 对房间中心点执行 Delaunay 三角剖分
   - 使用 Kruskal 算法计算最小生成树
   - 按配置比例选择额外边形成环路
   - 构建最终连接边列表

5. **Phase 5: 走廊生成**
   - 根据连接边在房间之间生成走廊
   - 支持 L 形走廊（先水平后垂直或反之）
   - 在拐角处额外挖掘确保转弯顺畅
   - 走廊宽度可配置（默认 3 格）

6. **Phase 6: 连通性保障**
   - 执行入口→出口方向的随机游走
   - 执行出口→入口方向的随机游走
   - 使用 BFS 验证连通性
   - 游走时只挖掘墙壁，保留已有平台

7. **Phase 7: 平台注入**
   - 在每个房间内部注入阶梯式平台
   - 按列分析垂直落差，在超过跳跃高度处注入平台
   - 分析水平跳跃距离，在过宽处添加平台
   - 验证可达性并修复不可达区域

8. **Phase 8: 后处理**
   - 使用 BFS 移除从入口不可达的孤岛
   - 清理出入口安全区（确保玩家可通行）
   - 识别地面敌人生成点（连续地面 + 头顶空间）
   - 识别空中敌人生成点（开放空间 + 距地面高度）

---

## 房间类型

### 1. 战斗房间 (Combat)

标准的程序化生成房间，包含完整的 8 阶段生成流程。

```mermaid
graph TB
    subgraph CombatRoom[战斗房间结构]
        ENTRANCE((入口)) --> R0[Room 0<br/>入口房间]
        R0 <-->|走廊| R1[Room 1]
        R0 <-->|走廊| R2[Room 2]
        R1 <-->|走廊| R3[Room 3<br/>出口房间]
        R2 <-->|走廊| R3
        R3 --> EXIT((出口))
    end
```

**图例说明：**
| 符号 | 含义 |
|------|------|
| 圆形节点 | 出入口 |
| 矩形框 | 房间区域 |
| 双向箭头 | 走廊连接 |

### 2. 入口房间 (Entrance)

简单矩形房间，完整平坦地面，无平台，无怪物生成点。

```mermaid
graph LR
    ENTRANCE((入口)) --> ROOM[简单矩形房间<br/>平坦地面<br/>无平台<br/>无生成点]
    ROOM --> EXIT((出口))
```

**特点：**
- 简单矩形结构
- 完整平坦地面
- 无平台
- 无怪物生成点
- 左右两侧开放

### 3. Boss 房间 (Boss)

大型竞技场，完整平坦地面，只有 Boss 生成点，出口有门封闭。

```mermaid
graph LR
    ENTRANCE((入口)) --> ARENA[大型竞技场<br/>平坦地面<br/>无平台]
    ARENA --> BOSS{{Boss生成点}}
    BOSS --> DOOR[/门/]
    DOOR -.->|击杀后开启| EXIT((出口))
```

**特点：**
- 大型矩形竞技场
- 完整平坦地面
- 只有 Boss 生成点
- 右侧出口有门封闭
- 门在 Boss 击杀后消失

---

## 瓦片类型图例

### 瓦片类型定义

```mermaid
classDiagram
    class TileType {
        <<enumeration>>
        Wall = 0
        Floor = 1
        Platform = 2
        Entrance = 3
        Exit = 4
    }
```

### 瓦片类型说明

| 类型 | 枚举值 | 描述 | 特性 |
|------|--------|------|------|
| **Wall** | 0 | 墙壁 | 实心，不可通行，用于房间边界和地面 |
| **Floor** | 1 | 地面/空气 | 空气区域，可通行，玩家和敌人可自由移动 |
| **Platform** | 2 | 单向平台 | 可从下方穿过，玩家可站立，可下跳穿过 |
| **Entrance** | 3 | 入口 | 标记房间的入口位置 |
| **Exit** | 4 | 出口 | 标记房间的出口位置 |

### 单向平台工作原理

```mermaid
graph TB
    subgraph PlatformBehavior[单向平台行为]
        UP[从下方] -->|可跳跃穿过| PLATFORM[平台]
        PLATFORM -->|可站立| STAND[站在平台上]
        PLATFORM -->|按下+跳跃| DOWN[向下穿过]
    end
```

**玩家可以：**
- 从下方跳跃穿过平台
- 站在平台上
- 按下+跳跃从平台下落

### 房间结构示意

```mermaid
graph TB
    subgraph RoomStructure[房间结构]
        WALL_TOP[墙壁顶部]
        subgraph Interior[内部空间]
            AIR1[空气区域]
            PLAT1[平台层1]
            AIR2[空气区域]
            PLAT2[平台层2]
            AIR3[空气区域]
        end
        WALL_BOTTOM[墙壁底部/地面]
    end
    
    WALL_TOP --> Interior --> WALL_BOTTOM
```

**说明：**
- 外围是墙壁边界
- 内部是可通行空气
- 平台呈阶梯式分布

---

## 生成点类型图例

### 生成点类型定义

```mermaid
classDiagram
    class SpawnType {
        <<enumeration>>
        Ground
        Air
        Boss
    }
```

### 生成点类型说明

| 类型 | 颜色 | 条件 | 适合敌人 |
|------|------|------|----------|
| **Ground** | 青色 (Cyan) | 位于地面上方，有足够连续地面长度 (≥ minGroundSpan)，头顶有足够空间 (≥ 2 格) | 锐枪手、盾卫等地面单位 |
| **Air** | 粉色 (Magenta) | 周围开放（上下左右都是空气），距地面高度 ≥ minAirHeight，随机概率筛选 (15%) | 飞蛾等空中单位 |
| **Boss** | 黄色 (Yellow) | 仅在 Boss 房间中生成，位于房间中央偏右，距离右墙约 10 格 | Boss 单位 |

### 生成点分布示意

```mermaid
graph TB
    subgraph SpawnDistribution[生成点分布]
        subgraph AirZone[空中区域]
            AIR_SPAWN((空中生成点))
        end
        subgraph GroundZone[地面区域]
            GROUND_SPAWN1((地面生成点1))
            GROUND_SPAWN2((地面生成点2))
        end
        GROUND[地面]
    end
    
    AirZone --> GroundZone --> GROUND
```

### 生成点筛选规则

```mermaid
flowchart TD
    START[收集所有潜在生成点] --> SHUFFLE[随机打乱]
    SHUFFLE --> CHECK_COUNT{数量 < maxEnemies?}
    CHECK_COUNT -->|是| CHECK_DIST{与已选点距离 >= minSpawnDistance?}
    CHECK_DIST -->|是| ADD[添加到最终列表]
    CHECK_DIST -->|否| SKIP[跳过]
    ADD --> CHECK_COUNT
    SKIP --> CHECK_COUNT
    CHECK_COUNT -->|否| END[完成筛选]
```

---

## 编辑器使用指南

### 打开编辑器窗口

**菜单路径:** `自制工具 → 程序化关卡 → 程序化房间生成V2 → Room Generator V2`

### 界面布局

```mermaid
graph TB
    subgraph EditorWindow[房间生成器 V2 编辑器窗口]
        subgraph LeftPanel[左侧面板]
            CONFIG[配置区域<br/>配置文件<br/>目标 Tilemap]
            PARAMS[生成参数<br/>房间类型/尺寸<br/>BSP/走廊/平台设置]
            ACTIONS[操作按钮<br/>生成房间/烘焙<br/>清空/复制种子]
            DEBUG[调试显示<br/>BSP分割/房间区域<br/>连接图/生成点]
        end
        subgraph RightPanel[右侧面板]
            PREVIEW[房间预览<br/>实时显示生成结果]
            OPTIONS[显示选项<br/>房间/连接/平台/生成点/BSP]
            STATS[统计信息<br/>种子/尺寸/数量/开放度/耗时]
        end
    end
    
    CONFIG --> PARAMS --> ACTIONS --> DEBUG
```

### 操作流程

```mermaid
flowchart TD
    STEP1[1. 配置 Tilemap<br/>指定墙壁层/平台层/门层]
    STEP2[2. 配置主题<br/>选择或创建主题配置文件]
    STEP3[3. 调整参数<br/>设置房间尺寸/BSP/走廊参数]
    STEP4[4. 生成房间<br/>点击生成房间按钮]
    STEP5[5. 预览结果<br/>在预览区和Scene视图查看]
    STEP6{满意?}
    STEP7[6. 烘焙到 Tilemap<br/>点击烘焙按钮]
    
    STEP1 --> STEP2 --> STEP3 --> STEP4 --> STEP5 --> STEP6
    STEP6 -->|否| STEP3
    STEP6 -->|是| STEP7
```

---

## 参数配置说明

### 基础设置

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| roomType | RoomType | Combat | - | 房间类型（战斗/入口/Boss） |
| roomWidth | int | 40 | 20-100 | 房间宽度（格子数） |
| roomHeight | int | 25 | 15-60 | 房间高度（格子数） |
| seed | string | "" | - | 随机种子 |
| useRandomSeed | bool | true | - | 是否使用随机种子 |

### 出入口设置

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| enforceAnchors | bool | true | - | 确保出入口区域被清理 |
| entranceY | int | -1 | - | 左侧入口Y坐标（-1表示随机） |
| exitY | int | -1 | - | 右侧出口Y坐标（-1表示随机） |
| entranceClearDepth | int | 5 | 3-8 | 出入口清理深度 |

### BSP 空间分割

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| targetRoomCount | int | 4 | 3-8 | 目标房间数量 |
| minBSPSize | int | 8 | 6-16 | BSP 叶节点最小尺寸 |
| maxBSPDepth | int | 6 | 3-8 | 最大分割深度 |
| splitRatioRange | Vector2 | (0.35, 0.65) | 0.3-0.7 | 分割比例范围 |

### 房间生成

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| roomFillRatio | float | 0.65 | 0.5-0.95 | 房间占 BSP 叶节点的比例 |
| roomPadding | int | 2 | 1-4 | 房间与 BSP 边界的最小距离 |

### 图连接

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| extraEdgeRatio | float | 0.2 | 0-0.5 | 在 MST 基础上额外添加的边比例 |

### 走廊生成

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| corridorWidth | int | 3 | 2-5 | 走廊宽度（玩家2×2，建议至少3） |
| lShapeCorridorChance | float | 0.7 | 0-1 | L形走廊概率 |

### 连通性保障

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| enableBidirectionalWalk | bool | true | - | 启用双向游走 |
| walkBrushSize | int | 3 | 2-5 | 游走刷子尺寸 |
| horizontalBias | float | 0.7 | 0.5-0.9 | 水平移动偏好 |

### 平台注入

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| enableJumpAnalysis | bool | true | - | 启用跳跃可达性分析 |
| maxJumpHeight | int | 5 | 3-8 | 玩家最大跳跃高度 |
| maxJumpDistance | int | 7 | 4-10 | 玩家最大跳跃距离 |
| maxPlatforms | int | 6 | 0-12 | 最大平台数量 |
| minPlatformWidth | int | 3 | 2-5 | 最小平台宽度 |
| maxPlatformWidth | int | 6 | 4-10 | 最大平台宽度 |
| platformExclusionRadius | int | 4 | 3-8 | 平台排斥半径 |
| maxHorizontalJump | int | 5 | 3-8 | 最大水平跳跃距离 |
| playerJumpForce | float | 8 | 5-12 | 玩家跳跃力 |
| hasDoubleJump | bool | true | - | 允许二段跳 |

### 敌人生成

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| minGroundSpan | int | 4 | 3-8 | 地面敌人生成点需要的最小连续地面 |
| minAirHeight | int | 4 | 3-8 | 空中敌人生成点距地面的最小高度 |
| maxEnemies | int | 5 | 0-10 | 最大敌人数量 |
| minSpawnDistance | int | 6 | 3-10 | 敌人最小间距 |

### 安全区设置

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| edgePadding | int | 2 | 1-4 | 边缘留空 |

---

## 版本信息

- **版本**: v0.2
- **命名空间**: `CryptaGeometrica.LevelGeneration.SmallRoomV2`
- **依赖**: Unity 2021.3+, Odin Inspector, Sirenix

---

