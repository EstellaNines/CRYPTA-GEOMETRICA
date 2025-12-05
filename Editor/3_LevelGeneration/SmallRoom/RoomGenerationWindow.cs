using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.UIElements;
using CryptaGeometrica.LevelGeneration.SmallRoom;

namespace CryptaGeometrica.Tools // Changed namespace to avoid conflict
{
    public class RoomGenerationWindow : EditorWindow // Inherit from standard EditorWindow for UITK support
    {
        [MenuItem("自制工具/程序化关卡/程序化房间生成")]
        private static void OpenWindow()
        {
            var window = GetWindow<RoomGenerationWindow>();
            window.titleContent = new GUIContent("Room Generator");
            window.Show();
        }

        // --- Data & Parameters ---
        // [Title("生成设置 (Generation Settings)")] 
        // Moved to ScriptableObject
        
        [Title("配置文件 (Configuration Asset)")]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)] // Change to Boxed so you can see the file reference
        [Tooltip("Drag your RoomGenerationSettings asset here, or use the default one.")]
        public RoomGenerationSettings settings;

        private PropertyTree _propertyTree; // Odin Property Tree
        private RoomData cachedRoomData; // Cache for baking

        protected void OnEnable()
        {
            // 1. Try to load specific default asset first (most stable)
            string defaultPath = "Assets/Editor/RoomGeneratorConfig.asset";
            settings = AssetDatabase.LoadAssetAtPath<RoomGenerationSettings>(defaultPath);

            // 2. If not found, search for ANY settings asset
            if (settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:RoomGenerationSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<RoomGenerationSettings>(path);
                }
            }

            // 3. If still not found, create a new one
            if (settings == null)
            {
                settings = CreateInstance<RoomGenerationSettings>();
                if (!System.IO.Directory.Exists(Application.dataPath + "/Editor"))
                {
                    AssetDatabase.CreateFolder("Assets", "Editor");
                }
                
                AssetDatabase.CreateAsset(settings, defaultPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created new RoomGenerationSettings at " + defaultPath);
            }

            _propertyTree = PropertyTree.Create(this); 
        }
        
        [Button("保存配置 (Save Settings)", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 1f)]
        public void SaveSettings()
        {
            if (settings != null)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("Settings Saved!");
            }
        }

        [Title("操作 (Actions)")]
        [Button("生成预览 (Generate)", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Generate()
        {
            if (settings == null) return;

            GameObject tempGO = new GameObject("TempGenerator");
            var gen = tempGO.AddComponent<RoomGenerator>();
            gen.parameters = settings.parameters;
            gen.themes = settings.themes; // Pass themes
            
            try 
            {
                gen.GenerateRoom();
                cachedRoomData = gen.CurrentRoom; // Cache the data
                UpdatePreviewTexture(gen.CurrentRoom);
            }
            finally
            {
                DestroyImmediate(tempGO);
            }
        }

        [Button("烘焙到场景 (Bake)", ButtonSizes.Medium)]
        public void Bake()
        {
            if (settings == null) return;

            var generator = FindObjectOfType<RoomGenerator>();
            if (generator != null)
            {
                generator.parameters = settings.parameters;
                generator.themes = settings.themes; // Sync themes too

                if (cachedRoomData != null)
                {
                    // Use cached data from preview
                    generator.SetRoomData(cachedRoomData);
                    generator.ForcePickTheme(); 
                }
                else
                {
                    // No preview generated, generate new
                    generator.GenerateRoom();
                }

                generator.BakeToTilemap();
                Debug.Log("Baked to Scene Tilemap");
                
                // 自动对齐摄像机
                AlignCamera(generator.CurrentRoom);
            }
            else
            {
                Debug.LogError("No RoomGenerator found in scene!");
            }
        }

        [Button("对齐摄像机 (Align Camera)", ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.8f)]
        public void AlignCameraBtn()
        {
             var generator = FindObjectOfType<RoomGenerator>();
             if (generator != null && generator.CurrentRoom != null)
             {
                 AlignCamera(generator.CurrentRoom);
             }
        }

        private void AlignCamera(RoomData room)
        {
            if (room == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            // 1. Center Camera
            // Room coordinates are (0,0) to (width, height)
            // Center is (width/2, height/2)
            // But we added BakePadding (3), so the visual bounds are larger.
            // Let's center on the logical room.
            float centerX = room.width / 2f;
            float centerY = room.height / 2f;
            
            cam.transform.position = new Vector3(centerX, centerY, -10f);

            // 2. Adjust Size (Zoom)
            // Orthographic Size is half of the vertical viewing volume height.
            // Height needed = room.height + padding
            float padding = 4f; // Extra margin
            float targetHeight = (room.height + padding * 2) / 2f;
            
            // Check width as well (based on aspect ratio)
            float aspect = cam.aspect;
            float targetWidthSize = ((room.width + padding * 2) / aspect) / 2f;

            cam.orthographicSize = Mathf.Max(targetHeight, targetWidthSize);
        }

        // --- Preview Data ---
        // Note: PreviewTexture is now handled by UITK Image, so we don't need [PreviewField] here strictly,
        // but we keep the variable for texture management.
        private Texture2D previewTexture;

        private Image previewImage;

        protected void OnDisable()
        {
            if (_propertyTree != null) _propertyTree.Dispose();
        }

        protected void OnDestroy()
        {
            if (previewTexture != null) DestroyImmediate(previewTexture);
        }
        
        protected void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // Container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            root.Add(container);

            // 1. Sidebar (Odin Inspector)
            var sidebar = new ScrollView();
            sidebar.style.width = 350;
            // sidebar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f); // Removed to fix font contrast issues
            sidebar.style.borderRightWidth = 1;
            sidebar.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            
            // Embed Odin's IMGUI drawing into this container
            var odinContainer = new IMGUIContainer(() => {
                _propertyTree.Draw(false); // Draw the Odin tree for 'this' object
            });
            sidebar.Add(odinContainer);
            container.Add(sidebar);

            // 2. Content Area (Preview)
            // Change to ScrollView to handle large preview images
            var content = new ScrollView();
            content.style.flexGrow = 1;
            content.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f); // Dark background for better map visibility
            // content.style.justifyContent = Justify.FlexStart; // ScrollView handles layout
            // content.style.alignItems = Align.Center;
            
            var titleLabel = new Label("房间结构预览");
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 14;
            titleLabel.style.marginTop = 10;
            titleLabel.style.marginLeft = 10; // Add margin since alignment changed
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            content.Add(titleLabel);
            
            previewImage = new Image();
            // previewImage.style.flexGrow = 1; // Let it size naturally or constrained
            previewImage.style.width = Length.Percent(95); // Fill width with margin
            previewImage.style.alignSelf = Align.Center; // Center in ScrollView
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            previewImage.style.marginTop = 10;
            previewImage.style.marginBottom = 20;
            
            content.Add(previewImage);

            // 3. Legend
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.flexWrap = Wrap.Wrap;
            legend.style.paddingLeft = 10;
            legend.style.paddingBottom = 10;
            legend.style.justifyContent = Justify.Center;

            legend.Add(CreateLegendItem("墙壁 (Wall)", new Color(0.1f, 0.1f, 0.1f)));
            legend.Add(CreateLegendItem("地面 (Floor)", new Color(0.8f, 0.8f, 0.8f)));
            legend.Add(CreateLegendItem("出入口 (Anchor)", new Color(0.2f, 1.0f, 0.2f)));
            legend.Add(CreateLegendItem("平台 (Platform)", new Color(0.3f, 0.3f, 0.9f)));
            legend.Add(CreateLegendItem("地面怪点 (G-Spawn)", new Color(1f, 0.2f, 0.2f)));
            legend.Add(CreateLegendItem("空中怪点 (A-Spawn)", new Color(1f, 0.9f, 0.2f)));

            content.Add(legend);
            
            container.Add(content);
        }

        private VisualElement CreateLegendItem(string name, Color color)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginRight = 15;
            item.style.marginBottom = 5;

            var icon = new VisualElement();
            icon.style.width = 12;
            icon.style.height = 12;
            icon.style.backgroundColor = color;
            icon.style.marginRight = 5;
            
            // Border
            icon.style.borderTopWidth = 1;
            icon.style.borderBottomWidth = 1;
            icon.style.borderLeftWidth = 1;
            icon.style.borderRightWidth = 1;
            icon.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
            icon.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
            icon.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            icon.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);

            var label = new Label(name);
            label.style.color = Color.white;
            label.style.fontSize = 11;

            item.Add(icon);
            item.Add(label);
            return item;
        }

        private void UpdatePreviewTexture(RoomData data)
        {
            if (data == null) return;

            int scale = 16; // Scale up for grid visibility
            int width = data.width * scale;
            int height = data.height * scale;

            if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
            {
                previewTexture = new Texture2D(width, height);
                previewTexture.filterMode = FilterMode.Point;
                previewTexture.wrapMode = TextureWrapMode.Clamp;
            }

            Color[] pixels = new Color[width * height];
            Color wallColor = new Color(0.1f, 0.1f, 0.1f);
            Color floorColor = new Color(0.8f, 0.8f, 0.8f);
            Color anchorColor = new Color(0.2f, 1.0f, 0.2f);
            Color platformColor = new Color(0.3f, 0.3f, 0.9f);
            Color gridColor = new Color(0.25f, 0.25f, 0.25f);

            for (int y = 0; y < data.height; y++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    TileType type = data.GetTile(x, y);
                    Color c = wallColor;

                    if (type == TileType.Floor) c = floorColor;
                    else if (type == TileType.Platform) c = platformColor;

                    // Draw Anchors (2x2)
                    // StartPos: bottom-left of 2x2
                    if (x >= data.startPos.x && x < data.startPos.x + 2 &&
                        y >= data.startPos.y && y < data.startPos.y + 2)
                    {
                        c = anchorColor;
                    }
                    // EndPos: generator uses (endPos.x-1, endPos.y) as bottom-left of 2x2
                    else if (x >= data.endPos.x - 1 && x < data.endPos.x + 1 &&
                             y >= data.endPos.y && y < data.endPos.y + 2)
                    {
                         c = anchorColor;
                    }

                    // Fill scaled block
                    int BaseIndexY = y * scale;
                    int BaseIndexX = x * scale;

                    for (int py = 0; py < scale; py++)
                    {
                        for (int px = 0; px < scale; px++)
                        {
                            // Grid lines on edges
                            if (px == 0 || py == 0) 
                            {
                                pixels[(BaseIndexY + py) * width + (BaseIndexX + px)] = gridColor;
                            }
                            else
                            {
                                pixels[(BaseIndexY + py) * width + (BaseIndexX + px)] = c;
                            }
                        }
                    }
                }
            }

            // Draw Spawns (Overlay)
            Color groundSpawnColor = new Color(1f, 0.2f, 0.2f); // Red
            Color airSpawnColor = new Color(1f, 0.9f, 0.2f);    // Yellow

            foreach (var spawn in data.potentialSpawns)
            {
                int sx = spawn.position.x;
                int sy = spawn.position.y;
                Color sc = (spawn.type == SpawnType.Ground) ? groundSpawnColor : airSpawnColor;

                // Paint center of the tile
                int BaseIndexY = sy * scale;
                int BaseIndexX = sx * scale;

                for (int py = 2; py < scale - 2; py++) // Padding to show underlying tile type
                {
                    for (int px = 2; px < scale - 2; px++)
                    {
                        pixels[(BaseIndexY + py) * width + (BaseIndexX + px)] = sc;
                    }
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();

            // Update UITK Image
            if (previewImage != null)
            {
                previewImage.image = previewTexture;
            }
        }
    }
}
