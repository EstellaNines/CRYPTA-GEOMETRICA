# 程序化房间生成器 (Procedural Room Generator) - v0.1

## 简介 (Introduction)

本分支 (`feature/room-generator-v0.1`) 包含了一套完整的 2D 程序化房间生成系统。该系统旨在快速生成结构化、连通且具备多样性的关卡房间，支持自动铺设 Tilemap、生成单向平台以及与游戏运行时的消息系统集成。

v0.1 版本专注于**单一房间**的内部结构生成，确保从入口到出口的主路径绝对畅通，并在此基础上进行随机扩展。

## 核心特性 (Features)

### 1. 生成算法

- **路径优先 (Path-First)**：优先生成连通 Start 和 End 的主路径，杜绝死路。
- **混合策略**：结合“随机游走 (Random Walk)”与“随机挖掘 (Random Room)”算法，兼顾线性体验与空间探索感。
- **智能平台**：自动识别垂直落差，在合适位置生成单向平台 (One-Way Platform)。

### 2. 编辑器工具

- **可视化窗口**：提供 `Room Generation Window`，支持实时预览房间结构。
- **配置持久化**：使用 `ScriptableObject` 保存生成参数和视觉主题，方便管理不同的关卡风格。
- **自动烘焙**：一键将预览结果烘焙到场景中的 Tilemap，并自动处理摄像机对齐。

### 3. 系统集成

- **双层 Tilemap**：自动将墙壁和平台分层处理 (Wall Layer / Platform Layer)。
- **出入口保护**：强制清理出入口区域，并向外延伸形成走廊，防止生成物堵塞。
- **消息广播**：生成完成后通过 `MessageManager` 广播出入口坐标和方位 (`ROOM_ANCHORS_UPDATE`)，便于其他系统（如怪物生成、传送门）接入。

## 快速开始 (Getting Started)

### 1. 打开工具

在 Unity 菜单栏中选择：
`自制工具` -> `程序化关卡` -> `程序化房间生成`

### 2. 创建配置

1. 在 Project 窗口右键：`Create` -> `自制工具` -> `程序化关卡` -> `房间生成配置文件`。
2. 将创建的配置文件拖入窗口的 **Configuration Asset** 栏。

### 3. 调整参数

- **Room Size**：设置房间宽高（建议 20x15）。
- **Anchors**：设置出入口 Y 轴位置。
- **Themes**：配置 Wall Tile 和 Platform Tile。

### 4. 生成与烘焙

1. 点击 **Generate** 查看预览。
2. 点击 **Bake** 将房间实例化到场景中。
3. 观察 Console 控制台输出的日志，确认出入口坐标信息。

## 目录结构 (File Structure)

- **Core Logic**: `Assets/Scripts/3_LevelGeneration/SmallRoom/`
  - `RoomGenerator.cs`: 核心生成器组件
  - `RoomData.cs`: 房间数据结构
  - `RoomGenParams.cs`: 参数定义
  - `RoomGenerationSettings.cs`: 配置文件资源
- **Editor**: `Assets/Editor/3_LevelGeneration/SmallRoom/`
  - `RoomGenerationWindow.cs`: 可视化编辑器窗口
- **Message System**: `Assets/Scripts/0_MessageManager/`
  - `RoomMessageData.cs`: 广播数据定义

## 详细文档 (Documentation)

关于算法实现细节、类图及开发管线流程，请参阅详细开发文档：

- [RoomGenerator_v0.1_Doc.md](./RoomGenerator_v0.1_Doc.md)

---

_Generated for Crypta Geometrica - v0.1_
