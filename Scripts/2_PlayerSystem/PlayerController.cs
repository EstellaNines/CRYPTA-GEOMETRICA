using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CryptaGeometrica.PlayerSystem
{
    /// <summary>
    /// 玩家控制器
    /// 负责处理玩家的输入并控制角色的移动和翻转
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("移动速度")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Jump Settings")]
        [Tooltip("跳跃力度")]
        [SerializeField] private float jumpForce = 12f;
        [Tooltip("最大连续跳跃次数")]
        [SerializeField] private int maxJumpCount = 2;
        [Tooltip("地面图层，必须设置正确否则无法跳跃")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Platform Settings")]
        [Tooltip("平台图层（单向平台）")]
        [SerializeField] private LayerMask platformLayer;
        [Tooltip("下落穿透持续时间")]
        [SerializeField] private float platformDropDuration = 0.25f;

        [Header("Safety Settings")]
        [Tooltip("掉落重置高度（低于此Y值时重置）")]
        [SerializeField] private float fallThreshold = -20f;
        [Tooltip("重生点（为空则重置到 (0,0,0)）")]
        [SerializeField] private Transform respawnPoint;

        private PlayerInputSystemManager _inputActions;
        private Rigidbody2D _rb;
        private Collider2D _col;
        private Vector2 _moveInput;
        private bool _isFacingRight = true;
        private bool _isGrounded;
        private int _remainingJumps;
        private bool _wasGrounded; // 用于检测落地瞬间
        private bool _isDropping; // 是否正在下落穿透平台

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _remainingJumps = maxJumpCount;
            
            // 初始化输入系统
            _inputActions = new PlayerInputSystemManager();
            
            // 绑定移动事件
            _inputActions.GamePlay.Movement.performed += OnMovementPerformed;
            _inputActions.GamePlay.Movement.canceled += OnMovementCanceled;
        }

        private void OnEnable()
        {
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Disable();
        }

        private void FixedUpdate()
        {
            CheckGrounded();
            CheckFall();
            HandleMovement();
        }

        #endregion

        #region Safety Logic

        private void CheckFall()
        {
            if (transform.position.y < fallThreshold)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            // 重置速度
            _rb.velocity = Vector2.zero;
            
            // 重置位置
            if (respawnPoint != null)
            {
                transform.position = respawnPoint.position;
            }
            else
            {
                transform.position = Vector3.zero;
            }
        }

        #endregion

        #region Movement Logic

        private void OnMovementPerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            
            // 检测跳跃输入 (Y > 0) 且还有跳跃次数
            if (_moveInput.y > 0.5f && _remainingJumps > 0)
            {
                Jump();
            }
            
            // 检测下落穿透输入 (Y < 0)，在平台上时穿透下落
            if (_moveInput.y < -0.5f && _isGrounded && !_isDropping)
            {
                TryDropThroughPlatform();
            }
            
            CheckFlip();
        }

        private void OnMovementCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void HandleMovement()
        {
            // 直接修改 velocity 实现移动，保留原有的 y 轴速度（如重力影响）
            _rb.velocity = new Vector2(_moveInput.x * moveSpeed, _rb.velocity.y);
        }

        private void Jump()
        {
            // 消耗一次跳跃机会
            _remainingJumps--;
            
            // 施加向上速度，保留当前 X 轴速度
            // 重置 Y 轴速度为 0 再施加力，确保二段跳手感一致，不会受到下落速度的影响
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
        }

        private void CheckGrounded()
        {
            // 使用 BoxCast 检测脚底（同时检测地面和平台）
            Bounds bounds = _col.bounds;
            Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.1f);
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.05f);

            // 合并地面和平台图层进行检测
            LayerMask combinedLayers = groundLayer | platformLayer;
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, 0.05f, combinedLayers);
            _isGrounded = hit.collider != null;

            // 只有在接触地面且未处于上升状态（避免起跳瞬间立刻重置）时重置跳跃次数
            if (_isGrounded && _rb.velocity.y <= 0.1f)
            {
                _remainingJumps = maxJumpCount;
            }
        }

        private void CheckFlip()
        {
            // 如果输入向右且当前未向右，或者输入向左且当前向右，则翻转
            if (_moveInput.x > 0 && !_isFacingRight)
            {
                Flip();
            }
            else if (_moveInput.x < 0 && _isFacingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            _isFacingRight = !_isFacingRight;

            // 修改 transform.localScale.x 实现翻转
            Vector3 currentScale = transform.localScale;
            currentScale.x *= -1;
            transform.localScale = currentScale;
        }

        #endregion

        #region Platform Drop Logic

        /// <summary>
        /// 尝试穿透平台下落
        /// </summary>
        private void TryDropThroughPlatform()
        {
            // 使用 OverlapBox 检测脚底区域是否有平台
            Bounds bounds = _col.bounds;
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y);
            Vector2 size = new Vector2(bounds.size.x * 0.8f, 0.3f);
            
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, platformLayer);
            
            if (hit != null)
            {
                // 获取平台的 PlatformEffector2D
                PlatformEffector2D effector = hit.GetComponent<PlatformEffector2D>();
                if (effector == null)
                {
                    // 如果是 CompositeCollider2D，尝试从父对象获取
                    effector = hit.GetComponentInParent<PlatformEffector2D>();
                }
                
                if (effector != null)
                {
                    StartCoroutine(DropThroughPlatformCoroutine(effector));
                }
            }
        }

        /// <summary>
        /// 平台穿透协程：临时修改 PlatformEffector2D 的旋转偏移
        /// </summary>
        private IEnumerator DropThroughPlatformCoroutine(PlatformEffector2D effector)
        {
            _isDropping = true;
            
            // 保存原始旋转偏移
            float originalOffset = effector.rotationalOffset;
            
            // 将旋转偏移设为 180，让碰撞弧形朝下，允许从上往下穿透
            effector.rotationalOffset = 180f;
            
            // 等待玩家穿过平台
            yield return new WaitForSeconds(platformDropDuration);
            
            // 恢复原始旋转偏移
            if (effector != null)
            {
                effector.rotationalOffset = originalOffset;
            }
            
            _isDropping = false;
        }

        #endregion
    }
}
