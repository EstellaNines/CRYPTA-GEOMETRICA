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
  - `void SetDebugMode(bool enabled)`
  - `void Reset()`

用法示例：
```csharp
var sm = new EnemyStateMachine();
sm.Initialize(owner, true);
sm.RegisterState(new GroundEnemyIdleState());
sm.RegisterState(new GroundEnemyPatrolState());
sm.TransitionTo("Idle");
```

## IEnemyState（接口）
- 成员
  - `string StateName { get; }`
  - `void OnEnter(EnemyController enemy)`
  - `void OnUpdate(EnemyController enemy)`
  - `void OnFixedUpdate(EnemyController enemy)`
  - `void OnExit(EnemyController enemy)`
  - `bool CanTransitionTo(string targetState, EnemyController enemy)`

## EnemyStateBase（抽象基类）
- 继承/实现：`IEnemyState`
- 关键受保护成员
  - `stateTimer : float`（逻辑时间）
  - `fixedStateTimer : float`（物理时间）
  - `isStateComplete : bool` / `isInterrupted : bool`
  - `debugMode : bool`
- 模板方法
  - `OnEnter/OnUpdate/OnFixedUpdate/OnExit` 已实现通用流程
  - 需由子类实现：
    - `protected abstract void UpdateState(EnemyController enemy)`
    - `protected abstract void CheckTransitionConditions(EnemyController enemy)`
  - 可重写：
    - `protected virtual void InitializeState(EnemyController enemy)`
    - `protected virtual void FixedUpdateState(EnemyController enemy)`
    - `protected virtual void CleanupState(EnemyController enemy)`
- 常用工具方法
  - `IsStateTimeout(float duration)` / `IsFixedStateTimeout(float duration)`
  - `GetStateProgress(float total)` / `GetFixedStateProgress(float total)`
  - `IsInTimeWindow(start, end)` / `IsInFixedTimeWindow(start, end)`

## EnemyController（抽象基类）
- 重要属性
  - `StateMachine : EnemyStateMachine`
  - `CurrentHealth : float`，`HealthPercentage : float`
  - `IsAlive : bool`，`CanAct : bool`，`IsFacingRight : bool`
  - `JustTookDamage : bool`
- 生命周期
  - `Awake/Start/Update/FixedUpdate` 内已对接状态机
- 抽象方法（子类必须实现）
  - `protected abstract void RegisterStates()`
  - `public abstract void PlayAnimation(string animationName)`
  - `public abstract void MoveTowards(Vector3 target, float speed)`
  - `public abstract void FaceTarget(Vector3 target)`
  - `public abstract void PerformAttack()`
  - `public abstract bool DetectPlayer(float range)`
  - `public abstract GameObject GetPlayerTarget()`
- 常用可重写方法
  - `TakeDamage(damage, source)` / `OnTakeDamage` / `OnDeath`
  - `PlaySound(name)`、`ApplyKnockback(force, dir)`
  - `SetFacingDirection(bool)`、`SetColliderEnabled(bool)`、`SetAIEnabled(bool)`
  - `SetAlpha(float)`、`DropLoot()`、`DestroyEnemy()`
  - 物理辅助：`MoveWithPhysics`、`ApplyGravity`、`CheckGroundCollision`、`CheckWallCollision`、`CheckPlatformEdge`、`ApplyJumpForce`
  - 工具：`GetDistanceToTarget(target)`、`IsTargetInLineOfSight(target, obstacleLayer)`、`Heal(amount)`、`SetMaxHealth(newMax)`

## GenericEnemyController（具体实现）
- 作用：可直接用于胶囊体敌人，内置状态工厂、检测与调试 Gizmos。
- 关键公有/序列化字段（节选）
  - `enabledStates : List<StateConfig>`（配置要注册的状态）
  - `initialState : string`
  - `playerDetectionRange, patrolSpeed, chaseSpeed, attackRange`
  - `idleTimeout, patrolDuration, attackCooldown`
  - `groundLayer, wallLayer, obstacleLayer`
- 工厂方法
  - `CreateIdleState()`，`CreatePatrolState()`：真实实现
  - `CreateChaseState()`，`CreateAttackState()`：占位实现（待替换）
- 重要重写
  - `PlayAnimation`（无 Animator 时以颜色代替）
  - `MoveTowards/FaceTarget/PerformAttack/DetectPlayer/GetPlayerTarget`

Inspector 快速操作（右键菜单）：
- `测试受伤`、`重置状态`、`切换到待机`、`切换到巡逻`、`列出所有状态`

## GroundEnemyIdleState（已实现）
- 字段
  - `idleTimeout, detectionRange, obstacleLayer`
  - `scanInterval, enableIdleMovement, idleMovementAmplitude, idleMovementSpeed`
- 行为
  - 周期性扫描玩家（遮挡判定），可微动。
  - 转换
    - 超时 -> `Patrol`
    - 发现玩家 -> 优先 `Chase`（若存在），否则 `Patrol`

## GroundEnemyPatrolState（已实现）
- 字段
  - `patrolSpeed, patrolDuration, detectionRange`
  - `groundLayer, wallLayer, obstacleLayer`（规划用于完善巡逻）
- 行为
  - 简化“移动-暂停”巡逻，周期性扫描玩家。
  - 转换
    - 巡逻时长结束 -> `Idle`
    - 发现玩家 -> `Chase`（若存在）

## 示例：自定义状态添加流程
```csharp
// 1) 新建状态
public class GroundEnemyChaseState : EnemyStateBase {
    public override string StateName => "Chase";
    protected override void UpdateState(EnemyController enemy) {
        var player = enemy.GetPlayerTarget();
        if (player != null) enemy.MoveTowards(player.transform.position, 4f);
    }
    protected override void CheckTransitionConditions(EnemyController enemy) {
        // 示例：接近进入Attack，丢失回 Patrol
    }
}

// 2) 在 GenericEnemyController.CreateStateInstance 增加 case "Chase"
// 3) 在 Inspector 的 enabledStates 勾选 "Chase"
```

> 注意：示例为演示接口形态，具体数值/判定逻辑请按设计补足。
