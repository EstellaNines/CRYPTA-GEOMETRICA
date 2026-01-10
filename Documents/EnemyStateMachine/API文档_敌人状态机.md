# 敌人状态机 API 文档

面向开发者的类与方法说明，基于当前仓库实际实现（C#）。

- 命名空间：`CryptaGeometrica.EnemyStateMachine` 及 `...States`
- 代码位置：`Assets/Scripts/5_EnemyStateMachine`

## EnemyStateMachine

- 字段/属性
  - `CurrentStateName : string`
  - `StateCount : int`
  - `IsInitialized : bool`
- 方法
  - `Initialize(EnemyController enemy, bool enableDebug = false)`
  - `bool RegisterState(IEnemyState state)`
  - `bool UnregisterState(string stateName)`
  - `bool HasState(string stateName)`
  - `bool TransitionTo(string stateName)`
  - `bool ForceTransitionTo(string stateName)`
  - `void Update()` / `void FixedUpdate()`
  - `string[] GetAllStateNames()`
  - `string[] GetStateHistory()` / `string GetPreviousState()`

## IEnemyState（接口）

- 成员
  - `string StateName { get; }`
  - `void OnEnter(EnemyController enemy)`
  - `void OnUpdate(EnemyController enemy)`
  - `void OnFixedUpdate(EnemyController enemy)`
  - `void OnExit(EnemyController enemy)`
  - `bool CanTransitionTo(string targetState, EnemyController enemy)`

## EnemyStateBase（抽象基类）

- 关键受保护成员
  - `stateTimer : float`（逻辑时间）
  - `fixedStateTimer : float`（物理时间）
  - `debugMode : bool`
- 需由子类实现：
  - `protected abstract void UpdateState(EnemyController enemy)`
  - `protected abstract void CheckTransitionConditions(EnemyController enemy)`
- 可重写：
  - `protected virtual void InitializeState(EnemyController enemy)`
  - `protected virtual void FixedUpdateState(EnemyController enemy)`
  - `protected virtual void CleanupState(EnemyController enemy)`

## GenericEnemyController

- 关键字段
  - `enemyType : EnemyType`（GroundEnemy/FlyingEnemy/BossEnemy）
  - `enabledStates : List<StateConfig>`
  - `initialState : string`
  - `playerDetectionRange, patrolSpeed, chaseSpeed, attackRange`
  - `groundLayer, wallLayer, obstacleLayer`
- 工厂方法
  - `CreateIdleState()` → GroundEnemyIdleState
  - `CreatePatrolState()` → GroundEnemyPatrolState / AirEnemyPatrolState
  - `CreateChaseState()` → GroundEnemyChaseState / AirEnemyChaseState
  - `CreateAttackState()` → GroundEnemyAttackState / AirEnemyAttackState


## 地面敌人状态

### GroundEnemyIdleState

- 字段：`idleTimeout, detectionRange, obstacleLayer, scanInterval`
- 转换：超时 -> `Patrol`，发现玩家 -> `Chase`

### GroundEnemyPatrolState

- 字段：`patrolSpeed, patrolDuration, detectionRange, groundLayer, wallLayer`
- 转换：巡逻结束 -> `Idle`，发现玩家 -> `Chase`

### GroundEnemyChaseState

- 字段：`chaseSpeed, attackRange, detectionRange, groundLayer, wallLayer, obstacleLayer`
- 转换：进入攻击范围 -> `Attack`，目标丢失 -> `Patrol/Idle`

### GroundEnemyAttackState

- 字段：`attackDuration, attackCooldown, attackRange, chaseRange, attackDamage`
- 字段：`attackHitboxSize, attackHitboxOffset, playerLayer`
- 方法：`OnAttackHitFrame(enemy)` - 动画事件回调
- 转换：
  - 玩家在攻击范围 -> 持续攻击
  - 玩家离开攻击范围但在追击范围 -> `Chase`
  - 玩家离开追击范围 -> `Patrol/Idle`

## 飞行敌人状态

### AirEnemyPatrolState

- 字段：`patrolSpeed, patrolDuration, detectionRange`
- 字段：`patrolMode`（Horizontal/Vertical/Circular/Random/Figure8）
- 字段：`patrolRadius, verticalAmplitude, boundaryMin, boundaryMax`
- 转换：巡逻结束 -> `Idle`，发现玩家 -> `Chase`

### AirEnemyChaseState

- 字段：`chaseSpeed, attackRange, detectionRange, obstacleLayer`
- 字段：`chaseMode`（Direct/Circling/KeepDistance/Dive）
- 字段：`circleRadius, preferredDistance, diveSpeed, diveAngle`
- 转换：进入攻击范围 -> `Attack`，目标丢失 -> `Patrol/Idle`

### AirEnemyAttackState

- 字段：`attackDuration, attackCooldown, attackRange, chaseRange, attackDamage`
- 字段：`attackMode`（Dive/Ranged/Swoop）
- 字段：`diveSpeed, diveAngle, pullUpHeight`（俯冲攻击）
- 字段：`projectilePrefab, projectileSpeed, firePoint`（远程攻击）
- 方法：`OnAttackHitFrame(enemy)` - 动画事件回调
- 转换：与地面攻击状态一致

## 示例：自定义状态添加流程

```csharp
// 1) 新建状态
public class GroundEnemyHurtState : EnemyStateBase {
    public override string StateName => "Hurt";
    
    protected override void UpdateState(EnemyController enemy) {
        // 受伤逻辑
    }
    
    protected override void CheckTransitionConditions(EnemyController enemy) {
        // 受伤结束后回到之前状态
        if (IsStateTimeout(hurtDuration)) {
            enemy.StateMachine.TransitionTo("Idle");
        }
    }
}

// 2) 在 GenericEnemyController.CreateStateInstance 增加 case "Hurt"
// 3) 在 Inspector 的 enabledStates 勾选 "Hurt"
```
