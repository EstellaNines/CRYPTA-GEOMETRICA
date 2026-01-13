# Implementation Plan: Player State Machine

## Overview

基于设计文档，按照接口→基类→具体状态→集成的顺序实现玩家状态机系统。每个任务都是独立可验证的代码单元。

## Tasks

- [x] 1. 创建玩家状态接口和基类
  - [x] 1.1 创建 IPlayerState 接口
    - 在 Scripts/2_PlayerSystem 目录下创建 IPlayerState.cs
    - 定义 StateName 属性和 OnEnter/OnUpdate/OnFixedUpdate/OnExit 方法
    - _Requirements: 2.1, 2.2_
  - [x] 1.2 创建 PlayerStateBase 抽象类
    - 在 Scripts/2_PlayerSystem 目录下创建 PlayerStateBase.cs
    - 实现 IPlayerState 接口，提供 stateTimer 和生命周期模板方法
    - 定义 UpdateState 和 CheckTransitions 抽象方法
    - _Requirements: 2.3, 2.4_

- [x] 2. 创建玩家状态机管理器
  - [x] 2.1 创建 PlayerStateMachine 类
    - 在 Scripts/2_PlayerSystem 目录下创建 PlayerStateMachine.cs
    - 实现状态字典、注册、转换和更新方法
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 3. 实现具体状态类
  - [x] 3.1 创建 PlayerIdleState 类
    - 在 Scripts/2_PlayerSystem/States 目录下创建 PlayerIdleState.cs
    - 继承 PlayerStateBase，实现 Idle 动画播放和转换逻辑
    - _Requirements: 3.1, 3.2, 3.3_
  - [x] 3.2 创建 PlayerWalkState 类
    - 在 Scripts/2_PlayerSystem/States 目录下创建 PlayerWalkState.cs
    - 继承 PlayerStateBase，实现 Walk 动画播放和转换逻辑
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 4. 集成到 PlayerController
  - [x] 4.1 修改 PlayerController 添加状态机支持
    - 添加 PlayerStateMachine、Animator 字段
    - 暴露 MoveInput、Animator、StateMachine 属性
    - 在 Awake 中初始化状态机并注册状态
    - 在 Update 中调用状态机 Update
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ] 5. Checkpoint - 验证功能
  - 确保代码编译通过
  - 在 Unity 编辑器中为 Player 对象添加 Animator 组件并关联 Player.controller
  - 运行游戏测试 Idle/Walk 动画切换
  - 如有问题请告知

## Notes

- 所有新文件放在 Scripts/2_PlayerSystem 目录下，保持项目结构一致
- 命名空间使用 CryptaGeometrica.PlayerSystem
- 状态类放在 States 子目录中，与敌人状态机结构保持一致
- 动画名称使用 "Idle" 和 "Walk"，与现有动画资源匹配
