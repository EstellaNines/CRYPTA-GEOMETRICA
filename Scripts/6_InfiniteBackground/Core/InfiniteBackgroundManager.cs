using System.Collections.Generic;
using UnityEngine;
using CryptaGeometrica.InfiniteBackground.Data;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.InfiniteBackground.Core
{
    /// <summary>
    /// Manager class for the Infinite Background System.
    /// Handles the instantiation and updating of parallax layers.
    /// </summary>
    public class InfiniteBackgroundManager : MonoBehaviour
    {
        [Title("Configuration")]
        [SerializeField, Required] 
        private BackgroundThemeSO defaultTheme;

        [Title("Theme Presets")]
        [SerializeField] private BackgroundThemeSO redTheme;
        [SerializeField] private BackgroundThemeSO yellowTheme;
        [SerializeField] private BackgroundThemeSO blueTheme;

        [Title("References")]
        [SerializeField] 
        private Camera targetCamera;

        [Title("Runtime")]
        [ShowInInspector, ReadOnly]
        private List<ParallaxLayer> _activeLayers = new List<ParallaxLayer>();
        
        private bool _hasThemeBeenSet = false;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Start()
        {
            // Register message listener
            if (MessageManager.Instance != null)
            {
                MessageManager.Instance.Register<RoomColorThemeData>(MessageDefine.ROOM_COLOR_THEME_CHANGED, OnRoomColorThemeChanged);
            }

            // Only load default theme if no theme has been set and no layers exist
            if (!_hasThemeBeenSet && _activeLayers.Count == 0 && defaultTheme != null)
            {
                LoadTheme(defaultTheme);
            }
        }

        private void OnDestroy()
        {
            // Unregister message listener
            if (MessageManager.Instance != null)
            {
                MessageManager.Instance.Remove<RoomColorThemeData>(MessageDefine.ROOM_COLOR_THEME_CHANGED, OnRoomColorThemeChanged);
            }
        }

        private void LateUpdate()
        {
            foreach (var layer in _activeLayers)
            {
                layer.Refresh();
            }
        }

        /// <summary>
        /// Message handler for room color theme changes
        /// </summary>
        public void OnRoomColorThemeChanged(RoomColorThemeData data)
        {
            if (data == null) return;

            BackgroundThemeSO targetTheme = null;

            switch (data.colorTheme)
            {
                case RoomColorTheme.Red:
                    targetTheme = redTheme;
                    break;
                case RoomColorTheme.Yellow:
                    targetTheme = yellowTheme;
                    break;
                case RoomColorTheme.Blue:
                    targetTheme = blueTheme;
                    break;
                default:
                    targetTheme = defaultTheme;
                    break;
            }

            // Fallback to default if specific theme is missing
            if (targetTheme == null)
            {
                targetTheme = defaultTheme;
            }

            if (targetTheme != null)
            {
                Debug.Log($"[InfiniteBackgroundManager] Switching to theme: {targetTheme.themeName} (Color: {data.colorTheme})");
                LoadTheme(targetTheme);
                _hasThemeBeenSet = true; // Mark that theme has been explicitly set
            }
        }

        [Button("Load Theme")]
        public void LoadTheme(BackgroundThemeSO theme)
        {
            // Clear existing layers
            ClearLayers();

            if (theme == null || theme.layers == null) return;

            // Create new layers
            foreach (var layerConfig in theme.layers)
            {
                CreateLayer(layerConfig);
            }
        }

        private void CreateLayer(LayerConfig config)
        {
            GameObject layerObj = new GameObject($"Layer_{config.sortingOrder}");
            layerObj.transform.SetParent(transform);
            
            // Reset local transform
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localRotation = Quaternion.identity;
            layerObj.transform.localScale = Vector3.one;

            ParallaxLayer layer = layerObj.AddComponent<ParallaxLayer>();
            layer.Initialize(config, targetCamera);
            
            _activeLayers.Add(layer);
        }

        private void ClearLayers()
        {
            foreach (var layer in _activeLayers)
            {
                if (layer != null)
                {
                    if (Application.isPlaying)
                        Destroy(layer.gameObject);
                    else
                        DestroyImmediate(layer.gameObject);
                }
            }
            _activeLayers.Clear();
            
            // Also destroy any children that might not be in the list (cleanup)
            // Need to iterate backwards or use a list when destroying immediate
            int childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        /// <summary>
        /// Force update camera reference if needed (e.g. after scene load)
        /// </summary>
        public void SetCamera(Camera cam)
        {
            targetCamera = cam;
            // Re-initialize layers with new camera if needed
            // Ideally, layers should just reference the new camera transform
            // For now, simpler to reload theme or update references in layers if we add that method
        }
    }
}
