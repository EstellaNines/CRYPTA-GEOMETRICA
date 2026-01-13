using UnityEngine;
using CryptaGeometrica.InfiniteBackground.Data;

namespace CryptaGeometrica.InfiniteBackground.Core
{
    /// <summary>
    /// Handles a single background layer with 3 tiles for infinite scrolling
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        private SpriteRenderer[] _tiles;
        private float _textureUnitSizeX;
        private float _textureUnitSizeY;
        private float _parallaxFactorX;
        private float _parallaxFactorY;
        private Transform _cameraTransform;
        private float _startPositionX;
        private float _initialYOffset;
        private float _startCameraY;
        private float _startPositionY;
        private bool _repeatY;

        public void Initialize(LayerConfig config, Camera cam)
        {
            _cameraTransform = cam.transform;
            _parallaxFactorX = config.parallaxFactorX;
            _parallaxFactorY = config.parallaxFactorY;
            _initialYOffset = config.yOffset;
            _repeatY = config.repeatY;

            // Record initial camera state
            _startCameraY = cam.transform.position.y;
            
            // Calculate start Y position (aligned with camera + offset)
            _startPositionY = _startCameraY + config.yOffset;

            // Handle Auto Fit Height
            Vector2 finalScale = config.scaleMultiplier;
            if (config.autoFitHeight && config.sprite != null)
            {
                float camHeight = 2f * cam.orthographicSize;
                // Apply height multiplier (buffer)
                camHeight *= config.heightFitMultiplier > 0 ? config.heightFitMultiplier : 1f;
                
                float spriteHeight = config.sprite.bounds.size.y;
                float scaleFactor = camHeight / spriteHeight;
                
                finalScale.y = scaleFactor;
                if (config.maintainAspectRatio)
                {
                    finalScale.x = scaleFactor;
                }
            }
            
            // Create Tiles with dynamic count
            CreateTiles(config, cam, finalScale);
            
            _startPositionX = transform.position.x;
            
            // Initial positioning
            Refresh();
        }

        private void CreateTiles(LayerConfig config, Camera cam, Vector2 scale)
        {
            if (config.sprite == null) return;

            // Clear existing children if any
            int childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // Calculate tile width in world units
            _textureUnitSizeX = config.sprite.texture.width / config.sprite.pixelsPerUnit * scale.x;

            // Calculate needed tiles to cover screen width
            float screenHeight = 2f * cam.orthographicSize;
            float screenWidth = screenHeight * cam.aspect;
            
            // X Axis Tile Count
            int tilesNeededX = Mathf.CeilToInt(screenWidth / _textureUnitSizeX) + 2;
            if (tilesNeededX % 2 == 0) tilesNeededX += 1;
            if (tilesNeededX < 3) tilesNeededX = 3;

            // Y Axis Tile Count
            int tilesNeededY = 1;
            if (_repeatY)
            {
                tilesNeededY = Mathf.CeilToInt(screenHeight / _textureUnitSizeY) + 2;
                if (tilesNeededY % 2 == 0) tilesNeededY += 1;
                if (tilesNeededY < 3) tilesNeededY = 3;
            }

            _tiles = new SpriteRenderer[tilesNeededX * tilesNeededY];
            
            // Generate Grid
            int halfTilesX = tilesNeededX / 2;
            int halfTilesY = tilesNeededY / 2;
            int count = 0;

            for (int y = 0; y < tilesNeededY; y++)
            {
                for (int x = 0; x < tilesNeededX; x++)
                {
                    int offsetX = x - halfTilesX;
                    int offsetY = y - halfTilesY;
                    
                    GameObject tileObj = new GameObject($"Tile_{offsetX}_{offsetY}");
                    tileObj.transform.SetParent(transform);
                    
                    float xPos = offsetX * _textureUnitSizeX;
                    float yPos = offsetY * _textureUnitSizeY;
                    
                    tileObj.transform.localPosition = new Vector3(xPos, yPos, 0);
                    tileObj.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                    tileObj.transform.localRotation = Quaternion.identity;

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = config.sprite;
                    sr.color = config.tintColor;
                    sr.sortingOrder = config.sortingOrder;
                    sr.drawMode = SpriteDrawMode.Simple; 
                    
                    _tiles[count] = sr;
                    count++;
                }
            }
        }

        public void Refresh()
        {
            if (_cameraTransform == null) return;

            // --- X Axis Logic (Infinite) ---
            // temp is the position relative to the camera (inverse parallax)
            float tempX = (_cameraTransform.position.x * (1 - _parallaxFactorX));
            float distX = (_cameraTransform.position.x * _parallaxFactorX);

            // Check boundaries for infinite scrolling X
            if (tempX > _startPositionX + _textureUnitSizeX)
            {
                _startPositionX += _textureUnitSizeX;
            }
            else if (tempX < _startPositionX - _textureUnitSizeX)
            {
                _startPositionX -= _textureUnitSizeX;
            }

            // --- Y Axis Logic ---
            float finalY;
            
            if (_repeatY)
            {
                // Infinite Y Logic (Similar to X)
                float tempY = (_cameraTransform.position.y * (1 - _parallaxFactorY));
                float distY = (_cameraTransform.position.y * _parallaxFactorY);
                
                // Note: _startPositionY logic for Infinite mode is dynamic, similar to _startPositionX
                // But we initialized _startPositionY based on camera start.
                // We need to treat it as a floating anchor like _startPositionX.
                
                if (tempY > _startPositionY + _textureUnitSizeY)
                {
                    _startPositionY += _textureUnitSizeY;
                }
                else if (tempY < _startPositionY - _textureUnitSizeY)
                {
                    _startPositionY -= _textureUnitSizeY;
                }
                
                finalY = _startPositionY + distY;
            }
            else
            {
                // Standard Relative Parallax (Clamped/Non-looping)
                float camDeltaY = _cameraTransform.position.y - _startCameraY;
                float distY = camDeltaY * _parallaxFactorY;
                finalY = _startPositionY + distY;
            }

            transform.position = new Vector3(_startPositionX + distX, finalY, transform.position.z);
        }
    }
}
