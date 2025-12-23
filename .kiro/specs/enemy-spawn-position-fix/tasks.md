# Implementation Plan: Enemy Spawn Position Fix

## Overview

本实现计划将修复怪物生成位置问题，通过增加边界验证、环境验证和碰撞检测来确保敌人生成在有效位置。

## Tasks

- [x] 1. 创建 SpawnPointValidator 工具类
  - 创建新文件 `Scripts/3_LevelGeneration/SmallRoom v0.2/Utils/SpawnPointValidator.cs`
  - 实现 `IsWithinBounds` 方法
  - 实现 `ValidateGroundEnvironment` 方法
  - 实现 `ValidateAirEnvironment` 方法
  - 实现 `IsInSafeZone` 方法
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 5.1, 5.2_

- [x] 2. 修改 RoomGeneratorV2 生成点识别逻辑
  - [x] 2.1 修改 IdentifyGroundSpawns 方法
    - 增加边界验证调用
    - 增加环境验证调用
    - 增加左右空间检查
    - _Requirements: 1.1, 1.3, 2.1, 2.2_
  - [x] 2.2 修改 IdentifyAirSpawns 方法
    - 增加边界验证调用
    - 增加8方向环境验证
    - _Requirements: 1.2, 1.3, 2.3_
  - [x] 2.3 修改 FilterSpawnPoints 方法
    - 在分配敌人类型前先排除安全区内的点位
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 3. Checkpoint - 验证生成点识别逻辑
  - 确保所有修改编译通过
  - 在 Unity Editor 中测试房间生成
  - 验证生成点不再出现在88边界附近

- [x] 4. 修改 SpawnPoint 敌人实例化逻辑
  - [x] 4.1 添加碰撞检测方法
    - 实现 `ValidateSpawnPosition` 方法
    - 使用 Physics2D.OverlapBox 检测碰撞
    - _Requirements: 3.1_
  - [x] 4.2 添加位置调整逻辑
    - 实现 `TryFindValidPosition` 方法
    - 向上偏移查找有效位置（最多5格）
    - _Requirements: 3.2_
  - [x] 4.3 修改 SpawnEnemy 方法
    - 集成碰撞检测和位置调整
    - 添加失败时的警告日志
    - _Requirements: 3.3, 3.4_

- [x] 5. 改进 Gizmos 可视化
  - [x] 5.1 修改 SpawnPoint.OnDrawGizmos
    - 根据验证状态显示不同颜色
    - 显示边界检测范围
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 6. Final Checkpoint - 完整测试
  - 确保所有测试通过
  - 在 Unity Editor 中运行完整房间生成流程
  - 验证敌人不再卡在墙里或生成在房间外

## Notes

- 所有修改都应保持向后兼容，不破坏现有功能
- 边界验证使用的 edgePadding 默认值为 3，可通过参数配置
- 碰撞检测需要在 Physics2D 初始化后执行，注意时序问题
