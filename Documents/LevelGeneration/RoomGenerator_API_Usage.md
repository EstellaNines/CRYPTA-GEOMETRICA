# 房间生成器 (Room Generator) v0.1 - API 使用文档

## 1. 快速开始

### 1.1 打开编辑器窗口

在 Unity 菜单栏选择：

```
自制工具 → 程序化关卡 → 程序化房间生成
```

### 1.2 创建配置文件

在 Project 窗口右键：

```
Create → 自制工具 → 程序化关卡 → 房间生成配置文件
```

### 1.3 基本工作流

1. 将创建的配置文件拖入窗口的 **Configuration Asset** 栏
2. 调整参数（见 2.2 节）
3. 点击 **Generate** 预览房间结构
4. 点击 **Bake** 将房间烘焙到场景

---

## 2. 配置参数详解

所有参数定义在 `RoomGenParams` 类中，通过 `RoomGenerationSettings` ScriptableObject 持久化。

### 2.1 基础设置 (Basic Settings)

| 参数名          | 类型   | 默认值 | 说明                     |
| --------------- | ------ | ------ | ------------------------ |
| `roomWidth`     | int    | 20     | 房间宽度（格子数）       |
| `roomHeight`    | int    | 15     | 房间高度（格子数）       |
| `seed`          | string | ""     | 随机种子，留空则自动生成 |
| `useRandomSeed` | bool   | true   | 是否每次使用新的随机种子 |

### 2.2 出入口设置 (Anchors Settings)

| 参数名           | 类型 | 默认值 | 说明                             |
| ---------------- | ---- | ------ | -------------------------------- |
| `enforceAnchors` | bool | true   | 强制生成 2x2 的出入口区域        |
| `entranceY`      | int  | 5      | 左侧入口的 Y 坐标（-1 表示随机） |
| `exitY`          | int  | 5      | 右侧出口的 Y 坐标（-1 表示随机） |

### 2.3 路径生成设置 (Walker Settings)

| 参数名            | 类型  | 默认值 | 说明                             |
| ----------------- | ----- | ------ | -------------------------------- |
| `maxSteps`        | int   | 100    | 随机游走的最大步数               |
| `pathWidth`       | int   | 2      | 通道宽度（建议 2，对应玩家宽度） |
| `walkerCount`     | int   | 1      | 同时进行的随机游走数量           |
| `turnProbability` | float | 0.2    | 转向概率（0=直线，1=频繁转向）   |
| `allowDiagonal`   | bool  | false  | 是否允许对角线移动               |

### 2.4 房间挖掘设置 (Random Rooms)

| 参数名              | 类型  | 默认值 | 说明                            |
| ------------------- | ----- | ------ | ------------------------------- |
| `roomSpawnChance`   | float | 0.1    | 生成小厅的概率（0~1）           |
| `minRoomSize`       | int   | 3      | 小厅最小尺寸                    |
| `maxRoomSize`       | int   | 5      | 小厅最大尺寸                    |
| `initialHolesCount` | int   | 2      | 初始挖掘的大洞数量              |
| `targetOpenness`    | float | 0.4    | 目标开阔度（地面占比，0.1~0.8） |

### 2.5 规则与平台设置 (Rules Settings)

| 参数名              | 类型 | 默认值 | 说明                   |
| ------------------- | ---- | ------ | ---------------------- |
| `removeSingleWalls` | bool | true   | 移除孤立的 1x1 墙壁    |
| `maxPlatforms`      | int  | 4      | 最多生成的单向平台数量 |
| `platformWidthMin`  | int  | 3      | 平台最小宽度           |
| `platformWidthMax`  | int  | 5      | 平台最大宽度           |
| `edgePadding`       | int  | 1      | 房间边缘留空距离       |

### 2.6 敌人生成点设置 (Spawn Analysis & Constraints)

| 参数名             | 类型 | 默认值 | 说明                             |
| ------------------ | ---- | ------ | -------------------------------- |
| `minGroundSpan`    | int  | 3      | 地面敌人生成点的最小连续地面长度 |
| `minAirHeight`     | int  | 3      | 空中敌人生成点的最小高度         |
| `maxEnemies`       | int  | 4      | 房间内最多敌人生成点数           |
| `minSpawnDistance` | int  | 5      | 敌人生成点之间的最小距离         |

---

## 3. 核心 API 接口

### 3.1 RoomGenerator 主类

#### 公共方法

```csharp
// 生成房间数据（不烘焙到场景）
public void GenerateRoom()

// 烘焙到 Tilemap 并广播消息
public void BakeToTilemap()

// 从编辑器窗口注入房间数据（用于预览）
public void SetRoomData(RoomData data)

// 随机选择一个主题
public void ForcePickTheme()
```

#### 公共属性

```csharp
// 获取当前生成的房间数据
public RoomData CurrentRoom { get; }

// 生成参数
public RoomGenParams parameters;

// 视觉主题列表
public List<RoomTheme> themes;

// 墙壁 Tilemap（包含地面、墙壁、细长结构）
public Tilemap targetTilemap;

// 平台 Tilemap（单向平台专用）
public Tilemap platformTilemap;
```

### 3.2 RoomData 数据类

```csharp
// 房间宽度和高度
public int width;
public int height;

// 左侧入口和右侧出口的网格坐标
public Vector2Int startPos;
public Vector2Int endPos;

// 所有地面瓦片的列表
public List<Vector2Int> floorTiles;

// 所有敌人生成点
public List<SpawnPoint> potentialSpawns;

// 获取指定位置的瓦片类型
public TileType GetTile(int x, int y)

// 设置指定位置的瓦片类型
public void SetTile(int x, int y, TileType type)

// 检查坐标是否有效
public bool IsValid(int x, int y)
```

### 3.3 RoomTheme 主题结构

```csharp
public string themeName;              // 主题名称
public TileBase wallTile;             // 墙壁瓦片（Rule Tile）
public TileBase platformTile;         // 平台瓦片（Rule Tile）
public TileBase singlePlatformTile;   // 单个平台瓦片（1x1）
public TileBase backgroundTile;       // 背景瓦片（可选）
```

### 3.4 消息广播 API

#### 消息定义

```csharp
// 消息 Key
MessageDefine.ROOM_ANCHORS_UPDATE

// 消息数据结构
public struct RoomAnchorsData
{
    public Vector2Int startGridPos;      // 入口网格坐标
    public Vector2Int endGridPos;        // 出口网格坐标
    public Vector3 startWorldPos;        // 入口世界坐标
    public Vector3 endWorldPos;          // 出口世界坐标
    public Vector2Int startDirection;    // 入口方位 (-1,0)=Left
    public Vector2Int endDirection;      // 出口方位 (1,0)=Right
}
```

#### 监听消息

```csharp
// 注册监听
MessageManager.Instance.Register<RoomAnchorsData>(
    MessageDefine.ROOM_ANCHORS_UPDATE,
    OnRoomGenerated
);

// 回调方法
void OnRoomGenerated(RoomAnchorsData data)
{
    Debug.Log($"入口: {data.startWorldPos}, 出口: {data.endWorldPos}");
    // 在此处放置角色、传送门等
}

// 取消监听
MessageManager.Instance.Remove<RoomAnchorsData>(
    MessageDefine.ROOM_ANCHORS_UPDATE,
    OnRoomGenerated
);
```

---

## 4. 编辑器窗口 UI 说明

### 4.1 配置文件区 (Configuration Asset)

- **用途**：选择或拖拽一个 `RoomGenerationSettings` 资源文件
- **自动加载**：窗口启动时自动查找默认配置或创建新配置

### 4.2 参数编辑区

所有参数通过 Odin Inspector 显示，支持实时编辑。

### 4.3 操作按钮

| 按钮              | 快捷键 | 功能                           |
| ----------------- | ------ | ------------------------------ |
| **Generate**      | -      | 生成房间预览（显示在小地图中） |
| **Save Settings** | -      | 保存当前配置到 SO 文件         |
| **Bake**          | -      | 烘焙到场景 Tilemap             |
| **Align Camera**  | -      | 自动对齐摄像机到房间中心       |

### 4.4 预览区

显示生成的房间结构小地图，颜色说明：

- **黑色**：墙壁
- **白色**：地面
- **蓝色**：平台
- **绿色**：出入口（2x2）
- **红色**：地面敌人生成点
- **黄色**：空中敌人生成点

---

## 5. 运行时集成示例

### 5.1 在场景中动态生成房间

```csharp
// 获取或创建 RoomGenerator 组件
RoomGenerator generator = GetComponent<RoomGenerator>();

// 配置参数
generator.parameters.roomWidth = 25;
generator.parameters.roomHeight = 20;
generator.parameters.targetOpenness = 0.45f;

// 生成房间
generator.GenerateRoom();

// 烘焙到 Tilemap
generator.BakeToTilemap();

// 监听出入口信息
MessageManager.Instance.Register<RoomAnchorsData>(
    MessageDefine.ROOM_ANCHORS_UPDATE,
    (data) => {
        Debug.Log($"房间已生成，入口在 {data.startWorldPos}");
    }
);
```

### 5.2 使用不同的主题

```csharp
// 在编辑器中配置多个主题，运行时随机选择
generator.ForcePickTheme();
generator.GenerateRoom();
generator.BakeToTilemap();
```

---

## 6. 常见问题

### Q: 如何确保出入口畅通？

A: 系统自动在生成前后各清理一次出入口区域，并在烘焙时向外延伸走廊。参数 `enforceAnchors = true` 可强制生成 2x2 的出入口。

### Q: 如何控制房间的"开放程度"？

A: 调整 `targetOpenness` 参数（0.1~0.8），值越高房间越开放，墙壁越少。

### Q: 平台和墙壁为什么分开？

A: 平台需要 `PlatformEffector2D` 支持单向通过，而墙壁需要 `CompositeCollider2D`。分开放置在不同 Tilemap 上可独立配置碰撞。

### Q: 如何自定义敌人生成点的逻辑？

A: 监听 `ROOM_ANCHORS_UPDATE` 消息或直接访问 `RoomData.potentialSpawns` 列表，根据 `SpawnType` 放置不同的敌人。

---

## 7. 性能建议

- **参数调优**：`maxSteps` 过大会增加计算时间，建议 50~200。
- **平台数量**：`maxPlatforms` 过多会影响烘焙速度，建议 3~5。
- **目标开阔度**：过高会导致迭代次数增加，建议 0.3~0.5。

---

_Last Updated: v0.1_
