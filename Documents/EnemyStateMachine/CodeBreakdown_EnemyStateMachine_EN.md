# Enemy State Machine Code Breakdown

This document provides a file-by-file analysis of the current implementation, explaining responsibilities, call relationships, common pitfalls, and extension points.

## Directory Structure

```text
Assets/
└── Scripts/5_EnemyStateMachine/
    ├── EnemyStateMachine.cs           # State machine core: register/transition/update/history
    ├── IEnemyState.cs                 # State interface
    ├── EnemyStateBase.cs              # State base class: template method + utility functions
    ├── EnemyController.cs             # Enemy controller abstract: lifecycle & interface integration
    ├── GenericEnemyController.cs      # Generic controller: factory/detection/Gizmos
    └── States/
        ├── GroundEnemyIdleState.cs    # Ground idle: scan player/micro-movement/timeout to patrol
        ├── GroundEnemyPatrolState.cs  # Ground patrol: move-pause alternation/scan player
        ├── GroundEnemyChaseState.cs   # Ground chase: track player/wall & edge detection
        ├── GroundEnemyAttackState.cs  # Ground attack: attack framework/cooldown/range transitions
        ├── AirEnemyPatrolState.cs     # Air patrol: 3D space movement/multiple patrol modes
        ├── AirEnemyChaseState.cs      # Air chase: multiple chase modes/obstacle avoidance
        ├── AirEnemyAttackState.cs     # Air attack: dive/ranged/swoop attack modes
        ├── EnemyHurtState.cs          # Hurt state: flash red effect/knockback
        └── EnemyDeathState.cs         # Death state: turn black/disable collision/fade out
```

## 1) EnemyStateMachine.cs

- Maintains `states` dictionary and `currentState`, with owner as `EnemyController`.
- `TransitionTo/ForceTransitionTo`: Exit old state -> Enter new state -> Record history.
- `Update/FixedUpdate`: Delegates to current state.
- Debug-friendly: `CurrentStateName`, `GetAllStateNames()`, history tracking, etc.

## 2) IEnemyState.cs / EnemyStateBase.cs

- Interface defines state lifecycle and transition authorization via `CanTransitionTo`.
- Base class encapsulation:
  - Template flow for enter/update/physics/exit with timer management.
  - Subclasses implement `UpdateState` and `CheckTransitionConditions`.
  - Common utilities: timeout, percentage progress, time windows, etc.

## 3) EnemyController.cs

- Abstract base class:
  - Initializes state machine in `Awake` and calls `RegisterStates()`.
  - Enters `Idle` state in `Start` (if exists).
  - Advances state machine in `Update/FixedUpdate`.
- Abstract interfaces implemented by concrete enemies: animation, movement, facing, attack, player detection & target acquisition.
- Provides numerous overridable utility methods (knockback, sound, physics detection, Gizmos, etc.).

## 4) GenericEnemyController.cs

- Purpose: Out-of-the-box generic enemy controller supporting ground and flying enemies.
- Key features:
  - Inspector-configurable `enabledStates` and `initialState`.
  - `enemyType` enum distinguishes `GroundEnemy`/`FlyingEnemy`/`BossEnemy`.
  - `RegisterStates()` iterates configuration, factory methods create states based on enemy type.
  - Factory: `CreateIdleState/PatrolState/ChaseState/AttackState/HurtState/DeathState` all implemented.
  - Physics detection: `CheckGroundCollision/CheckWallCollision/CheckPlatformEdge` implemented.
  - Gizmos: Detection range/patrol range/player connection line, etc.

## 5) GroundEnemyIdleState.cs

- Initialization: Records original position; applies "idle color" when no Animator; stops horizontal velocity.
- Update: Periodic `ScanForPlayer` (raycast occlusion), optional "micro-movement".
- Transitions:
  - Player detected -> `Chase` (if exists) else `Patrol`
  - Idle timeout -> `Patrol`

## 6) GroundEnemyPatrolState.cs

- Simplified patrol: Alternates between movement and pause phases; advances via Rigidbody2D or direct position.
- Periodic `ScanForPlayer` (occlusion check).
- Transitions:
  - Player detected -> `Chase` (if exists)
  - Patrol duration complete -> `Idle`

## 7) GroundEnemyChaseState.cs (Implemented)

- Chase player: Continuously moves toward player using `chaseSpeed`.
- Wall/edge detection: Stops chase when encountering walls or platform edges.
- Line of sight: Configurable `obstacleLayer` for occlusion detection.
- Transitions:
  - Enter attack range -> `Attack`
  - Target lost/out of detection range -> `Patrol` or `Idle`

## 8) GroundEnemyAttackState.cs (Framework)

- Attack framework: Supports attack hitbox, cooldown, animation event callbacks.
- Range transition logic:
  - Player in attack range -> Continue attacking (repeat after cooldown)
  - Player exits attack range but within chase range -> `Chase`
  - Player exits chase range -> `Patrol` or `Idle`
- TODO: Integrate specific damage logic after attack animation completion.

## 9) AirEnemyPatrolState.cs (Implemented)

- Flying patrol: Free 3D space movement, unaffected by gravity.
- Multiple patrol modes: `Horizontal`/`Vertical`/`Circular`/`Random`/`Figure8`.
- Boundary constraints: Configurable patrol area boundaries.
- Transitions:
  - Player detected -> `Chase`
  - Patrol duration complete -> `Idle`

## 10) AirEnemyChaseState.cs (Implemented)

- Multiple chase modes: `Direct`/`Circling`/`KeepDistance`/`Dive`.
- Obstacle avoidance: Configurable avoidance distance and layers.
- Transitions:
  - Enter attack range -> `Attack`
  - Target lost/out of detection range -> `Patrol` or `Idle`

## 11) AirEnemyAttackState.cs (Framework)

- Multiple attack modes: `Dive` (dive attack)/`Ranged` (projectile)/`Swoop` (swoop attack).
- Range transition logic: Same as ground attack state.
- TODO: Integrate after attack animation and projectile system completion.

## 12) EnemyHurtState.cs (Implemented)

- Universal state for all enemy types.
- Flash red effect: Default 3 flashes, 0.1s each.
- Knockback: Configurable knockback force and direction.
- Transitions:
  - Hurt complete + player in range -> `Chase`
  - Hurt complete + player out of range -> `Patrol`

## 13) EnemyDeathState.cs (Implemented)

- Universal state for all enemy types.
- Death sequence: Turn black -> Disable player collision -> Wait 2s -> Flash disappear -> Destroy.
- Cannot transition to other states.
- Supports loot drop configuration.

## 14) Common Pitfalls & Constraints

- Forgetting to register state in `RegisterStates()` or not checking in `enabledStates` -> Initial state transition fails.
- Not adding corresponding case in `CreateStateInstance` -> Cannot create new state instance.
- Incorrect physics detection layer configuration (`groundLayer/wallLayer/obstacleLayer`) -> Detection/occlusion fails.
- `GetPlayerTarget()` requires scene to have `Player` Tag, otherwise falls back to main camera as target.
- Flying enemies need `enemyType = FlyingEnemy`, otherwise ground states will be created.

## 15) Current Implementation Progress

| State | Ground Enemy | Flying Enemy |
|-------|--------------|--------------|
| Idle | ✅ Complete | ✅ Complete |
| Patrol | ✅ Complete | ✅ Complete |
| Chase | ✅ Complete | ✅ Complete |
| Attack | ✅ Complete | ✅ Complete |
| Hurt | ✅ Complete | ✅ Complete |
| Death | ✅ Complete | ✅ Complete |

## 16) Next Upgrade Path

1. Enhance attack states (integrate animation events and damage system).
2. Projectile system (flying enemy ranged attacks).
3. Boss-specific states and behaviors.

After completing the above, the AI combat loop will reach playable quality.
