using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.InfiniteBackground.Data
{
    [CreateAssetMenu(fileName = "BackgroundTheme", menuName = "自制工具/无限背景/Theme")]
    public class BackgroundThemeSO : ScriptableObject
    {
        [Header("Theme Settings")]
        [Tooltip("The name of this background theme")]
        public string themeName;

        [Header("Layers Configuration")]
        [Tooltip("List of background layers from back to front")]
        public LayerConfig[] layers;
    }

    [System.Serializable]
    public class LayerConfig
    {
        [Header("Visual")]
        [Tooltip("The sprite for this background layer")]
        [PreviewField(60, ObjectFieldAlignment.Left)]
        public Sprite sprite;

        [Tooltip("Tint color for this layer")]
        public Color tintColor = Color.white;

        [Tooltip("Sorting order for rendering. Lower values are further back.")]
        public int sortingOrder = -10;

        [Header("Parallax Settings")]
        [Tooltip("Horizontal parallax factor (0-1). 1 moves with camera (foreground), 0 stays still (far background).")]
        [Range(0f, 1f)]
        public float parallaxFactorX = 0.5f;

        [Tooltip("Vertical parallax factor (0-1).")]
        [Range(0f, 1f)]
        public float parallaxFactorY = 0.5f;

        [Header("Positioning")]
        [Tooltip("Vertical offset from the center")]
        public float yOffset = 0f;
        
        [Tooltip("Scale multiplier for this layer")]
        public Vector2 scaleMultiplier = Vector2.one;

        [Header("Auto Scaling")]
        [Tooltip("If true, automatically scales the sprite to fit the camera height")]
        public bool autoFitHeight = false;

        [Tooltip("If true, updates the X scale to match Y scale when fitting height (maintains aspect ratio)")]
        [ShowIf("autoFitHeight")]
        public bool maintainAspectRatio = true;

        [Tooltip("Multiplier for the auto-fit height. >1 means the background will be larger than the screen to allow for vertical parallax movement.")]
        [ShowIf("autoFitHeight")]
        [Range(1f, 3f)]
        public float heightFitMultiplier = 1.0f;

        [Header("Infinite Settings")]
        [Tooltip("If true, the background will loop vertically as well (good for sky/space, bad for terrain).")]
        public bool repeatY = false;
    }
}
