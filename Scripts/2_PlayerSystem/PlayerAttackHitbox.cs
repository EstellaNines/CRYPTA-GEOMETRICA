using UnityEngine;
using CryptaGeometrica.EnemyStateMachine;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家攻击判定组件
    /// 使用 OverlapBox 检测攻击范围内的敌人并造成伤害
    /// </summary>
    public class PlayerAttackHitbox : MonoBehaviour
    {
        [Header("攻击参数")]
        [Tooltip("攻击伤害值")]
        [SerializeField] private float attackDamage = 10f;
        
        [Tooltip("攻击范围尺寸")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.5f, 1f);
        
        [Tooltip("攻击范围偏移（相对于玩家朝向）")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.8f, 0f);
        
        [Tooltip("敌人图层")]
        [SerializeField] private LayerMask enemyLayer;
        
        [Header("调试")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);
        
        /// <summary>
        /// 玩家 Transform 引用
        /// </summary>
        private Transform _playerTransform;
        
        private void Awake()
        {
            _playerTransform = transform;
        }
        
        /// <summary>
        /// 执行攻击判定（由动画事件调用）
        /// </summary>
        public void PerformAttack()
        {
            // 计算攻击范围中心点（考虑玩家朝向）
            float facingDirection = Mathf.Sign(_playerTransform.localScale.x);
            Vector2 center = (Vector2)_playerTransform.position + new Vector2(hitboxOffset.x * facingDirection, hitboxOffset.y);
            
            // 检测范围内的敌人
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, enemyLayer);
            
            foreach (var hit in hits)
            {
                // 尝试获取敌人控制器并造成伤害
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage, _playerTransform.position);
                }
            }
        }
        
        /// <summary>
        /// 编辑器绘制攻击范围
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            Transform t = _playerTransform != null ? _playerTransform : transform;
            float facingDirection = Mathf.Sign(t.localScale.x);
            Vector2 center = (Vector2)t.position + new Vector2(hitboxOffset.x * facingDirection, hitboxOffset.y);
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(center, hitboxSize);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            Gizmos.DrawCube(center, hitboxSize);
        }
    }
}
