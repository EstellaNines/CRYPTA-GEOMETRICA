# Enemy State Machine Technical Document

This document provides a systematic explanation of the enemy state machine architecture, data flow, state design, usage and extension methods for developers and level/art designers.

- Code Root: `Assets/Scripts/5_EnemyStateMachine`
- Main Types:
  - `EnemyStateMachine` (State Machine Manager)
  - `IEnemyState`, `EnemyStateBase` (State Interface & Base Class)
  - `EnemyController` (Enemy Controller Abstract Base)
  - `GenericEnemyController` (Generic Enemy Controller)
  - Ground States: `GroundEnemyIdleState`, `GroundEnemyPatrolState`, `GroundEnemyChaseState`, `GroundEnemyAttackState`
  - Air States: `AirEnemyPatrolState`, `AirEnemyChaseState`, `AirEnemyAttackState`
  - Common States: `EnemyHurtState`, `EnemyDeathState`

**State Machine Visualizer Tool**: This system includes a Canvas-style visual editor window similar to Unity's Animator (shortcut `Ctrl+Shift+V`), providing real-time state node graphs, transition relationship visualization, runtime debugging panel, and interactive state switching functionality to help developers and designers intuitively understand and debug enemy behavior logic, supporting canvas dragging, zooming, auto-layout operations, with all states color-coded and icon-labeled, allowing runtime state forcing by clicking nodes for quick testing.

---

## 1. Class Diagram

```mermaid
classDiagram
    class EnemyController {
        <<abstract>>
        +StateMachine : EnemyStateMachine
        +CurrentHealth : float
        +MaxHealth : float
        +IsAlive : bool
        +CanAct : bool
        +IsFacingRight : bool
        #rigidBody : Rigidbody2D
        #animator : Animator
        +TakeDamage(damage, source)
        +SetFacingDirection(right)
        +ApplyKnockback(force, direction)
        #RegisterStates()*
        #PlayAnimation(name)*
        #MoveTowards(target, speed)*
        #DetectPlayer(range)* bool
        #GetPlayerTarget()* GameObject
        #OnTakeDamage(damage, source)*
        #OnDeath()*
    }

    class GenericEnemyController {
        -enemyType : EnemyType
        -enabledStates : List~StateConfig~
        -initialState : string
        -playerDetectionRange : float
        -patrolSpeed : float
        -chaseSpeed : float
        -attackRange : float
        -allRenderers : Renderer[]
        -originalColors : Dictionary
        +GetMainRenderer() Renderer
        +GetAllRenderers() Renderer[]
        -CreateIdleState() IEnemyState
        -CreatePatrolState() IEnemyState
        -CreateChaseState() IEnemyState
        -CreateAttackState() IEnemyState
        -CreateHurtState() IEnemyState
        -CreateDeathState() IEnemyState
    }

    class EnemyStateMachine {
        -states : Dictionary~string, IEnemyState~
        -currentState : IEnemyState
        -owner : EnemyController
        +CurrentStateName : string
        +StateCount : int
        +RegisterState(state) bool
        +HasState(name) bool
        +GetState(name) IEnemyState
        +TransitionTo(name) bool
        +ForceTransitionTo(name) bool
        +Update()
        +FixedUpdate()
    }

    class IEnemyState {
        <<interface>>
        +StateName : string
        +OnEnter(enemy)
        +OnUpdate(enemy)
        +OnFixedUpdate(enemy)
        +OnExit(enemy)
        +CanTransitionTo(target, enemy) bool
    }

    class EnemyStateBase {
        <<abstract>>
        +StateName : string*
        #stateTimer : float
        #debugMode : bool
        +OnEnter(enemy)
        +OnUpdate(enemy)
        +OnFixedUpdate(enemy)
        +OnExit(enemy)
        #InitializeState(enemy)*
        #UpdateState(enemy)*
        #FixedUpdateState(enemy)*
        #CheckTransitionConditions(enemy)*
        #CleanupState(enemy)*
    }

    class GroundEnemyIdleState {
        -idleTimeout : float
        -detectionRange : float
    }

    class GroundEnemyPatrolState {
        -patrolSpeed : float
        -patrolDuration : float
        -maxPatrolDistance : float
    }

    class GroundEnemyChaseState {
        -chaseSpeed : float
        -attackRange : float
        -detectionRange : float
    }

    class GroundEnemyAttackState {
        -attackRange : float
        -attackCooldown : float
        -chaseRange : float
    }

    class AirEnemyPatrolState {
        -patrolMode : AirPatrolMode
        -patrolRadius : float
    }

    class AirEnemyChaseState {
        -chaseMode : AirChaseMode
        -chaseSpeed : float
    }

    class AirEnemyAttackState {
        -attackMode : AirAttackMode
        -attackRange : float
    }

    class EnemyHurtState {
        -hurtDuration : float
        -flashDuration : float
        -flashCount : int
        -knockbackForce : float
        +DamageSource : Vector3
    }

    class EnemyDeathState {
        -deathDelay : float
        -fadeOutDuration : float
        -flashInterval : float
        -deathColor : Color
    }

    EnemyController <|-- GenericEnemyController
    EnemyController o--> EnemyStateMachine
    EnemyStateMachine --> IEnemyState : manages
    IEnemyState <|.. EnemyStateBase
    EnemyStateBase <|-- GroundEnemyIdleState
    EnemyStateBase <|-- GroundEnemyPatrolState
    EnemyStateBase <|-- GroundEnemyChaseState
    EnemyStateBase <|-- GroundEnemyAttackState
    EnemyStateBase <|-- AirEnemyPatrolState
    EnemyStateBase <|-- AirEnemyChaseState
    EnemyStateBase <|-- AirEnemyAttackState
    EnemyStateBase <|-- EnemyHurtState
    EnemyStateBase <|-- EnemyDeathState
```

---

## 2. State Diagram

```mermaid
stateDiagram-v2
    [*] --> Idle : Initialize

    state "Normal Behavior" as Normal {
        Idle --> Patrol : Idle Timeout
        Idle --> Chase : Player Detected
        
        Patrol --> Idle : Patrol Complete
        Patrol --> Chase : Player Detected
        
        Chase --> Attack : Enter Attack Range
        Chase --> Patrol : Target Lost
        
        Attack --> Chase : Exit Attack Range
        Attack --> Patrol : Exit Chase Range
    }

    state "Hurt Processing" as HurtProcess {
        Hurt : Flash Red Effect
        Hurt : Knockback Processing
    }

    state "Death Processing" as DeathProcess {
        Death : Turn Black
        Death : Disable Player Collision
        Death : Wait 2 Seconds
        Death : Flash Disappear
    }

    Normal --> HurtProcess : Take Damage
    HurtProcess --> Chase : Player In Range
    HurtProcess --> Patrol : Player Out of Range

    Normal --> DeathProcess : Health Zero
    DeathProcess --> [*] : Destroy Object
```

---

## 3. Sequence Diagrams

### 3.1 State Transition Sequence

```mermaid
sequenceDiagram
    participant Player as Player
    participant Enemy as Enemy Controller
    participant SM as State Machine
    participant OldState as Old State
    participant NewState as New State

    Player->>Enemy: Enter Detection Range
    Enemy->>SM: TransitionTo("Chase")
    SM->>SM: HasState("Chase")?
    SM->>OldState: OnExit(enemy)
    OldState->>OldState: CleanupState()
    SM->>NewState: OnEnter(enemy)
    NewState->>NewState: InitializeState()
    NewState->>Enemy: PlayAnimation("Chase")
    SM-->>Enemy: Transition Complete
```

### 3.2 Hurt Processing Sequence

```mermaid
sequenceDiagram
    participant Player as Player
    participant Enemy as Enemy Controller
    participant SM as State Machine
    participant HurtState as Hurt State
    participant Renderer as Renderer

    Player->>Enemy: Attack Hit
    Enemy->>Enemy: TakeDamage(damage, source)
    Enemy->>Enemy: OnTakeDamage()
    Enemy->>SM: ForceTransitionTo("Hurt")
    SM->>HurtState: OnEnter(enemy)
    
    loop Flash Red Effect x3
        HurtState->>Renderer: SetColor(Red)
        HurtState->>HurtState: Wait(0.1s)
        HurtState->>Renderer: SetColor(Original)
        HurtState->>HurtState: Wait(0.1s)
    end
    
    HurtState->>Enemy: ApplyKnockback()
    HurtState->>SM: TransitionTo(nextState)
```

### 3.3 Death Processing Sequence

```mermaid
sequenceDiagram
    participant Enemy as Enemy Controller
    participant SM as State Machine
    participant DeathState as Death State
    participant Renderer as Renderer
    participant Collider as Collider
    participant GO as GameObject

    Enemy->>Enemy: CurrentHealth <= 0
    Enemy->>Enemy: OnDeath()
    Enemy->>SM: ForceTransitionTo("Death")
    SM->>DeathState: OnEnter(enemy)
    
    DeathState->>Renderer: SetColor(Black)
    DeathState->>Collider: IgnoreCollision(Player)
    DeathState->>Enemy: StopMovement()
    
    DeathState->>DeathState: Wait(2s)
    
    loop Flash Disappear
        DeathState->>Renderer: SetEnabled(false)
        DeathState->>DeathState: Wait(0.1s)
        DeathState->>Renderer: SetEnabled(true)
        DeathState->>DeathState: Wait(0.1s)
    end
    
    DeathState->>GO: Destroy()
```

---

## 4. Entity-Relationship Diagram

```mermaid
erDiagram
    ENEMY_CONTROLLER ||--|| ENEMY_STATE_MACHINE : owns
    ENEMY_STATE_MACHINE ||--o{ ENEMY_STATE : manages
    ENEMY_CONTROLLER ||--o{ RENDERER : has
    ENEMY_CONTROLLER ||--o{ COLLIDER : has
    ENEMY_CONTROLLER }o--|| PLAYER : detects

    ENEMY_CONTROLLER {
        string enemyName
        float currentHealth
        float maxHealth
        bool isAlive
        bool canAct
        bool isFacingRight
        EnemyType enemyType
    }

    ENEMY_STATE_MACHINE {
        string currentStateName
        int stateCount
    }

    ENEMY_STATE {
        string stateName
        float stateTimer
        bool debugMode
    }

    RENDERER {
        Color color
        bool enabled
    }

    COLLIDER {
        bool enabled
        bool isTrigger
    }

    PLAYER {
        string tag
        Vector3 position
    }

    STATE_CONFIG ||--|| ENEMY_STATE : configures
    STATE_CONFIG {
        string stateName
        bool enabled
        string description
    }

    GROUND_STATE ||--|{ ENEMY_STATE : extends
    GROUND_STATE {
        LayerMask groundLayer
        LayerMask wallLayer
        float patrolSpeed
        float chaseSpeed
    }

    AIR_STATE ||--|{ ENEMY_STATE : extends
    AIR_STATE {
        AirPatrolMode patrolMode
        AirChaseMode chaseMode
        AirAttackMode attackMode
        float flyHeight
    }
```

---

## 5. Robustness Diagram

```mermaid
flowchart LR
    subgraph Actors
        Player((Player))
        Designer((Designer))
    end

    subgraph Boundary
        Inspector[Inspector Panel]
        Gizmos[Scene Gizmos]
        DebugLog[Debug Log]
    end

    subgraph Control
        EnemyController{Enemy Controller}
        StateMachine{State Machine}
        StateFactory{State Factory}
        DetectionSystem{Detection System}
        DamageSystem{Damage System}
    end

    subgraph Entity
        EnemyData[(Enemy Data)]
        StateData[(State Data)]
        ConfigData[(Config Data)]
    end

    Player -->|Attack| DamageSystem
    Player -->|Move| DetectionSystem
    Designer -->|Configure| Inspector

    Inspector --> ConfigData
    Inspector --> EnemyController
    
    EnemyController --> StateMachine
    EnemyController --> DetectionSystem
    EnemyController --> DamageSystem
    
    StateMachine --> StateFactory
    StateFactory --> StateData
    
    DetectionSystem --> EnemyData
    DamageSystem --> EnemyData
    
    StateMachine --> Gizmos
    StateMachine --> DebugLog
```

---

## 6. Gantt Chart - Development Progress

```mermaid
gantt
    title Enemy State Machine Development Progress
    dateFormat  YYYY-MM-DD
    section Core Architecture
    State Machine Core       :done, core, 2025-11-20, 3d
    State Interface Design   :done, interface, after core, 2d
    Controller Base Class    :done, controller, after interface, 3d
    
    section Ground Enemy States
    Idle State              :done, g_idle, 2025-12-02, 2d
    Patrol State            :done, g_patrol, after g_idle, 3d
    Chase State             :done, g_chase, after g_patrol, 3d
    Attack State            :done, g_attack, after g_chase, 3d
    
    section Flying Enemy States
    AirPatrol State         :done, a_patrol, 2025-12-10, 3d
    AirChase State          :done, a_chase, after a_patrol, 3d
    AirAttack State         :done, a_attack, after a_chase, 3d
    
    section Common States
    Hurt State              :done, hurt, 2025-12-19, 2d
    Death State             :done, death, after hurt, 2d
    
    section Future Plans
    Boss States             :active, boss, 2025-12-24, 5d
```

---

## 7. Activity Diagram - State Update Flow

```mermaid
flowchart TD
    Start([Update Start]) --> CheckAlive{Enemy Alive?}
    
    CheckAlive -->|No| End([End])
    CheckAlive -->|Yes| CheckCanAct{Can Act?}
    
    CheckCanAct -->|No| End
    CheckCanAct -->|Yes| UpdateTimer[Update State Timer]
    
    UpdateTimer --> CallUpdate[Call Current State Update]
    CallUpdate --> CheckTransition[Check Transition Conditions]
    
    CheckTransition --> HasTransition{Need Transition?}
    HasTransition -->|No| End
    HasTransition -->|Yes| GetTarget[Get Target State]
    
    GetTarget --> CanTransition{Can Transition?}
    CanTransition -->|No| End
    CanTransition -->|Yes| ExitOld[Exit Old State]
    
    ExitOld --> EnterNew[Enter New State]
    EnterNew --> PlayAnim[Play Animation]
    PlayAnim --> ApplyEffect[Apply Visual Effect]
    ApplyEffect --> End
```

---

## 8. Component Diagram

```mermaid
flowchart TB
    subgraph Unity["Unity Engine"]
        MonoBehaviour
        Rigidbody2D
        Collider2D
        Animator
        SpriteRenderer
    end

    subgraph EnemySystem["Enemy State Machine System"]
        subgraph Core["Core Components"]
            EnemyController
            EnemyStateMachine
            IEnemyState
            EnemyStateBase
        end

        subgraph States["State Components"]
            subgraph Ground["Ground States"]
                GroundIdle[IdleState]
                GroundPatrol[PatrolState]
                GroundChase[ChaseState]
                GroundAttack[AttackState]
            end

            subgraph Air["Air States"]
                AirPatrol[PatrolState]
                AirChase[ChaseState]
                AirAttack[AttackState]
            end

            subgraph Common["Common States"]
                HurtState
                DeathState
            end
        end

        subgraph Controllers["Controllers"]
            GenericEnemyController
        end
    end

    MonoBehaviour --> EnemyController
    EnemyController --> GenericEnemyController
    GenericEnemyController --> EnemyStateMachine
    EnemyStateMachine --> IEnemyState
    IEnemyState --> EnemyStateBase
    EnemyStateBase --> Ground
    EnemyStateBase --> Air
    EnemyStateBase --> Common

    GenericEnemyController -.-> Rigidbody2D
    GenericEnemyController -.-> Collider2D
    GenericEnemyController -.-> Animator
    GenericEnemyController -.-> SpriteRenderer
```

---

## 9. State Machine Visualizer Window

### 9.1 Overview

The State Machine Visualizer Window is a Canvas-style editor tool similar to Unity's Animator, designed for real-time visualization and debugging of enemy state machines.

**How to Open**:
- Menu Path: `Window > ????? > ??????? (State Machine Visualizer)`
- Shortcut: `Ctrl + Shift + V`

### 9.2 Main Features

#### State Node Visualization
- **Node Layout**: Automatically layouts all enabled state nodes including Idle, Patrol, Chase, Attack, Hurt, Death
- **Color Coding**: Each state uses a unique color for identification
  - Idle: Cyan
  - Patrol: Green
  - Chase: Blue
  - Attack: Light Blue
  - Hurt: Red
  - Death: Black/Gray
- **State Icons**: Each state has an emoji icon for quick recognition
  - ?? Idle | ?? Patrol | ?? Chase | ?? Attack | ?? Hurt | ?? Death

#### State Transition Visualization
- **Transition Lines**: Shows transition relationships between states with directional arrows
- **Transition Labels**: Displays transition conditions on lines (e.g., "Player Detected", "Enter Attack Range")
- **Special Transitions**: Hurt and Death states use dashed lines to indicate they can be entered from any state

#### Runtime Debugging
- **Real-time State Highlight**: Currently active state node is highlighted with a green indicator
- **Runtime Info Panel**: Displays key runtime data
  - State: Current state name
  - Count: Number of registered states
  - Health: Current health value and percentage (color-coded)
  - Alive: Alive status (?/?)
  - Can Act: Can act status (?/?)
- **State Switching**: Left-click on state nodes during runtime to force transition to that state (for testing)

#### Interactive Operations
- **Canvas Dragging**: Right-click drag to move the entire canvas view
- **Zoom Control**: Mouse scroll wheel to zoom canvas (0.5x - 2.0x)
- **Auto Layout**: Click "?? ????" button to reset node positions
- **Reset View**: Click "?? ??" button to restore default view and zoom

#### Legend and Tips
- **Color Legend**: Top bar displays color squares and names for all states
- **Operation Tips**: Bottom bar shows interaction instructions
  - ?? Right-drag canvas | Scroll to zoom | Left-click to switch state (runtime)

### 9.3 Use Cases

1. **State Machine Structure Preview**: View complete state machine structure in the editor
2. **Runtime Debugging**: Monitor enemy state transitions and property changes in real-time
3. **Quick Testing**: Quickly switch states by clicking nodes to test state behaviors
4. **Level Design**: Helps level designers understand enemy behavior logic

### 9.4 Technical Features

- **Auto Selection**: Automatically updates display when selecting a GameObject with `GenericEnemyController` in Hierarchy
- **Real-time Refresh**: Automatically refreshes display during runtime without manual updates
- **Grid Background**: Provides grid background for visual positioning assistance
- **Shadow Effects**: Nodes have shadow effects to enhance visual hierarchy

---

## 10. Enemy Type Configuration

| Type | Description | States Used |
| ---- | ----------- | ----------- |
| GroundEnemy | Ground-based enemy | GroundEnemyXXXState |
| FlyingEnemy | Flying enemy | AirEnemyXXXState |
| BossEnemy | Boss enemy | BossState (TBD) |

---

## 11. State Color Scheme

| State | Color | RGB Value |
| ----- | ----- | --------- |
| Idle/Patrol | Default White | (1, 1, 1) |
| Chase | Blue | (0.3, 0.5, 1) |
| Attack | Light Blue | (0.4, 0.6, 1) |
| Hurt | Flash Red | (1, 0, 0) |
| Death | Black | (0, 0, 0) |

---

## 12. Implementation Progress

| State | Ground Enemy | Flying Enemy |
| ----- | ------------ | ------------ |
| Idle | ✅ Complete | ✅ Complete |
| Patrol | ✅ Complete | ✅ Complete |
| Chase | ✅ Complete | ✅ Complete |
| Attack | ✅ Complete | ✅ Complete |
| Hurt | ✅ Complete | ✅ Complete |
| Death | ✅ Complete | ✅ Complete |

---

## 13. Acceptance Criteria

- Enemy exhibits stable Idle/Patrol cycle without clipping when no player present
- Correctly enters Chase upon player detection and transitions to Attack at attack range
- Returns to Patrol/Idle correctly when player leaves detection range
- Attack has complete timing sequence with effective cooldown and hitbox
- Hurt state correctly displays flash red effect and applies knockback
- Death state correctly executes turn black → disable collision → flash disappear sequence
- Logs and Gizmos clearly reflect current state
