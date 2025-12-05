using UnityEngine;
using UnityEngine.UI;

namespace CryptaGeometrica.Environment
{
    /// <summary>
    /// 基于 RawImage 的背景视差滚动/无限循环脚本
    /// 必须将纹理的 Wrap Mode 设置为 Repeat
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ParallaxRawImage : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("跟随的相机，为空则自动获取 MainCamera")]
        [SerializeField] private Transform targetCamera;

        [Header("Parallax Settings")]
        [Tooltip("X轴滚动速度系数 (0=不动, 1=完全跟随)")]
        [SerializeField] private float parallaxSpeedX = 0.1f;
        
        [Tooltip("Y轴滚动速度系数")]
        [SerializeField] private float parallaxSpeedY = 0.05f;

        [Tooltip("是否反向滚动 (通常背景移动方向与相机相反)")]
        [SerializeField] private bool invertDirection = false;

        [Header("Position Settings")]
        [Tooltip("是否锁定物理位置到相机 (防止相机移出背景范围)")]
        [SerializeField] private bool lockPositionToCamera = true;
        [Tooltip("是否自动居中对齐相机 (忽略初始位置偏差)")]
        [SerializeField] private bool autoCenter = true;
        [Tooltip("手动偏移量 (当 AutoCenter 关闭时生效)")]
        [SerializeField] private Vector3 lockOffset = new Vector3(0, 0, 10);

        private RawImage _rawImage;
        private Vector2 _startOffset;
        private Vector3 _startCameraPos;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                if (Camera.main != null)
                    targetCamera = Camera.main.transform;
                else
                    Debug.LogError("ParallaxRawImage: 未找到 MainCamera，请手动指定 Target Camera");
            }

            if (targetCamera != null)
            {
                _startCameraPos = targetCamera.position;
                _startOffset = _rawImage.uvRect.position;
                
                // 如果没有手动设置 lockOffset，则使用当前的相对距离
                if (lockPositionToCamera && !autoCenter && lockOffset == Vector3.zero)
                {
                    lockOffset = transform.position - targetCamera.position;
                }
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            // 1. 处理物理位置跟随 (确保背景永远在相机视野内)
            if (lockPositionToCamera)
            {
                if (autoCenter)
                {
                    // 强制对齐到相机中心，保留自身的 Z 轴
                    transform.position = new Vector3(targetCamera.position.x, targetCamera.position.y, transform.position.z);
                }
                else
                {
                    transform.position = targetCamera.position + lockOffset;
                }
            }

            // 2. 处理 UV 滚动 (制造移动错觉)
            // 计算相机移动距离
            Vector3 movement = targetCamera.position - _startCameraPos;

            // 计算新的 UV 偏移
            float direction = invertDirection ? -1f : 1f;
            // 注意：如果锁定了位置，这里的 movement 仍然是相对于世界原点的（因为我们用的是 startCameraPos），逻辑是正确的
            float offsetX = movement.x * parallaxSpeedX * direction;
            float offsetY = movement.y * parallaxSpeedY * direction;

            // 更新 UV Rect
            Rect uvRect = _rawImage.uvRect;
            
            // 注意：RawImage 的 UV 坐标通常较小，如果纹理很大，需要调整系数
            // 这里直接叠加到初始偏移上
            uvRect.x = _startOffset.x + offsetX;
            uvRect.y = _startOffset.y + offsetY;

            _rawImage.uvRect = uvRect;
        }
    }
}
