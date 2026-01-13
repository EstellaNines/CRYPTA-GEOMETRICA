# Requirements Document

## Introduction

为玩家角色实现状态机系统，支持基于移动状态自动切换动画。当玩家站立不动时播放Idle动画，当玩家行走时播放Walk动画。该系统应与现有的PlayerController集成，并参考项目中已有的敌人状态机架构模式。

## Glossary

- **Player_State_Machine**: 玩家状态机管理器，负责状态的注册、转换和更新
- **Player_State**: 玩家状态接口/基类，定义状态的生命周期方法
- **Idle_State**: 站立状态，玩家静止时的状态
- **Walk_State**: 行走状态，玩家移动时的状态
- **Player_Controller**: 现有的玩家控制器，处理输入和移动逻辑
- **Animator**: Unity动画控制器组件，用于播放动画

## Requirements

### Requirement 1: 玩家状态机核心系统

**User Story:** As a developer, I want a player state machine system, so that I can manage player states in a structured and extensible way.

#### Acceptance Criteria

1. THE Player_State_Machine SHALL manage state registration, transitions, and updates
2. THE Player_State_Machine SHALL support registering multiple states by name
3. WHEN a state transition is requested, THE Player_State_Machine SHALL call OnExit on the current state and OnEnter on the new state
4. THE Player_State_Machine SHALL provide Update and FixedUpdate methods for state logic execution

### Requirement 2: 玩家状态接口和基类

**User Story:** As a developer, I want a player state interface and base class, so that I can create consistent state implementations.

#### Acceptance Criteria

1. THE Player_State interface SHALL define OnEnter, OnUpdate, OnFixedUpdate, and OnExit lifecycle methods
2. THE Player_State interface SHALL define a StateName property for identification
3. THE Player_State_Base class SHALL provide default implementations and common utilities
4. THE Player_State_Base class SHALL track state duration via a timer

### Requirement 3: Idle状态实现

**User Story:** As a player, I want my character to play an idle animation when standing still, so that the character feels alive and responsive.

#### Acceptance Criteria

1. WHEN the player has zero horizontal movement input, THE Idle_State SHALL be active
2. WHILE in Idle_State, THE Animator SHALL play the "Idle" animation
3. WHEN horizontal movement input becomes non-zero, THE Idle_State SHALL transition to Walk_State

### Requirement 4: Walk状态实现

**User Story:** As a player, I want my character to play a walk animation when moving, so that the movement feels natural and visually clear.

#### Acceptance Criteria

1. WHEN the player has non-zero horizontal movement input, THE Walk_State SHALL be active
2. WHILE in Walk_State, THE Animator SHALL play the "Walk" animation
3. WHEN horizontal movement input becomes zero, THE Walk_State SHALL transition to Idle_State

### Requirement 5: PlayerController集成

**User Story:** As a developer, I want the state machine integrated with the existing PlayerController, so that states can access movement data and control animations.

#### Acceptance Criteria

1. THE Player_Controller SHALL expose movement input data for states to read
2. THE Player_Controller SHALL hold a reference to the Player_State_Machine
3. THE Player_Controller SHALL call state machine Update in Update and FixedUpdate in FixedUpdate
4. THE Player_Controller SHALL hold a reference to the Animator component
5. WHEN the game starts, THE Player_Controller SHALL initialize the state machine with Idle and Walk states
6. WHEN the game starts, THE Player_State_Machine SHALL transition to Idle_State as the default state
