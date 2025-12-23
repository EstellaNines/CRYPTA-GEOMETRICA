# Requirements Document

## Introduction

本文档定义了修复怪物生成位置问题的需求规范。当前系统存在怪物生成在墙壁内部或房间外部的问题，需要改进生成点识别和敌人实例化逻辑，确保所有敌人都生成在有效的可行走区域内。

## Glossary

- **Spawn_Point_System**: 负责识别、存储和管理怪物生成点的系统
- **Room_Generator**: 房间生成器，负责创建房间布局和识别生成点
- **Spawn_Point**: 单个怪物生成点组件，负责在指定位置实例化敌人
- **Grid_Coordinate**: 网格坐标，房间数据中的整数坐标系统
- **World_Coordinate**: 世界坐标，Unity 场景中的实际位置
- **Floor_Tile**: 地面瓦片，敌人可以站立的区域
- **Wall_Tile**: 墙壁瓦片，敌人不能进入的区域
- **Edge_Padding**: 边缘填充，房间边界到有效生成区域的最小距离
- **Ground_Spawn**: 地面生成点，敌人站在地面上
- **Air_Spawn**: 空中生成点，敌人悬浮在空中

## Requirements

### Requirement 1: 生成点边界验证

**User Story:** As a 关卡设计师, I want 生成点识别系统排除靠近房间边界的位置, so that 敌人不会生成在房间外部或墙壁边缘。

#### Acceptance Criteria

1. WHEN Room_Generator 识别地面生成点 THEN THE Spawn_Point_System SHALL 排除距离房间边界小于 Edge_Padding (默认3格) 的位置
2. WHEN Room_Generator 识别空中生成点 THEN THE Spawn_Point_System SHALL 排除距离房间边界小于 Edge_Padding 的位置
3. THE Spawn_Point_System SHALL 验证生成点的 Grid_Coordinate 在有效房间范围内 (x >= padding && x < width - padding && y >= padding && y < height - padding)

### Requirement 2: 生成点周围环境验证

**User Story:** As a 玩家, I want 敌人生成在有足够空间的位置, so that 敌人不会卡在墙壁或平台中。

#### Acceptance Criteria

1. WHEN 识别地面生成点 THEN THE Spawn_Point_System SHALL 验证生成点上方至少有3格连续的 Floor_Tile 空间
2. WHEN 识别地面生成点 THEN THE Spawn_Point_System SHALL 验证生成点左右各至少有1格 Floor_Tile 空间
3. WHEN 识别空中生成点 THEN THE Spawn_Point_System SHALL 验证生成点周围8个方向都是 Floor_Tile
4. IF 生成点周围环境验证失败 THEN THE Spawn_Point_System SHALL 跳过该位置不添加到生成点列表

### Requirement 3: 敌人实例化位置验证

**User Story:** As a 玩家, I want 敌人在实际生成时位置正确, so that 敌人不会卡在地形中。

#### Acceptance Criteria

1. WHEN Spawn_Point 实例化敌人 THEN THE Spawn_Point SHALL 使用 Physics2D.OverlapBox 检测目标位置是否有碰撞体
2. IF 目标位置存在碰撞体 THEN THE Spawn_Point SHALL 尝试向上偏移直到找到无碰撞位置（最多尝试5格）
3. IF 无法找到有效位置 THEN THE Spawn_Point SHALL 取消生成并记录警告日志
4. WHEN 地面敌人生成 THEN THE Spawn_Point SHALL 限制射线检测距离为当前位置到房间底部的距离

### Requirement 4: 生成点可视化调试

**User Story:** As a 开发者, I want 在编辑器中清晰看到生成点的有效性, so that 我可以快速识别和调试生成点问题。

#### Acceptance Criteria

1. WHEN 在编辑器中绘制 Gizmos THEN THE Spawn_Point SHALL 用绿色表示有效生成点
2. WHEN 在编辑器中绘制 Gizmos THEN THE Spawn_Point SHALL 用红色表示无效或被跳过的生成点
3. THE Spawn_Point SHALL 在 Gizmos 中显示生成点的边界检测范围

### Requirement 5: 入口出口安全区排除

**User Story:** As a 玩家, I want 入口和出口附近没有敌人, so that 我进入房间时有安全的缓冲区域。

#### Acceptance Criteria

1. THE Spawn_Point_System SHALL 排除距离入口位置小于 entranceClearDepth + 2 格的生成点
2. THE Spawn_Point_System SHALL 排除距离出口位置小于 entranceClearDepth + 2 格的生成点
3. WHEN 筛选生成点 THEN THE Spawn_Point_System SHALL 在分配敌人类型之前先排除安全区内的点位
