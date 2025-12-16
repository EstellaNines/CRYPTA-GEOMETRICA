using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.UIElements;
using CryptaGeometrica.LevelGeneration.MultiRoom;
using SmallRoomV2 = CryptaGeometrica.LevelGeneration.SmallRoomV2;
using RoomType = CryptaGeometrica.LevelGeneration.MultiRoom.RoomType;

namespace CryptaGeometrica.Tools.LevelGeneration
{
    /// <summary>
    /// 多房间关卡生成器 Editor 窗口
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        #region 菜单
        
        [MenuItem("自制工具/程序化关卡/多房间关卡生成")]
        private static void OpenWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>();
            window.titleContent = new GUIContent("Level Generator", EditorGUIUtility.IconContent("d_Terrain Icon").image);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        #endregion

        #region 字段
        
        [Title("关卡生成器配置")]
        [LabelText("场景中的生成器")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public LevelGenerator generator;
        
        [Title("布局配置文件")]
        [LabelText("当前布局")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public LevelLayoutSO layoutSO;
        
        private PropertyTree propertyTree;
        private Vector2 scrollPosition;
        private Texture2D previewTexture;
        private Image previewImage;
        
        // 预览缩放
        private float previewScale = 2f;
        
        #endregion

        #region Unity 生命周期
        
        private void OnEnable()
        {
            // 尝试查找场景中的 LevelGenerator
            FindGenerator();
            
            // 创建属性树
            propertyTree = PropertyTree.Create(this);
            
            // 订阅场景变化事件
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        
        private void OnDisable()
        {
            propertyTree?.Dispose();
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }
        }
        
        private void OnHierarchyChanged()
        {
            if (generator == null)
            {
                FindGenerator();
            }
        }
        
        #endregion

        #region GUI
        
        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            
            // 主容器
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            root.Add(container);
            
            // 左侧面板 - 参数配置
            var leftPanel = new ScrollView();
            leftPanel.style.width = 400;
            leftPanel.style.borderRightWidth = 1;
            leftPanel.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            
            // Odin Inspector 容器
            var odinContainer = new IMGUIContainer(() => {
                DrawLeftPanel();
            });
            leftPanel.Add(odinContainer);
            container.Add(leftPanel);
            
            // 右侧面板 - 预览
            var rightPanel = new ScrollView();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            
            // 预览标题
            var titleLabel = new Label("关卡布局预览");
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 14;
            titleLabel.style.marginTop = 10;
            titleLabel.style.marginLeft = 10;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightPanel.Add(titleLabel);
            
            // 预览图像
            previewImage = new Image();
            previewImage.style.width = Length.Percent(95);
            previewImage.style.alignSelf = Align.Center;
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            previewImage.style.marginTop = 10;
            previewImage.style.marginBottom = 20;
            rightPanel.Add(previewImage);
            
            // 图例
            var legend = CreateLegend();
            rightPanel.Add(legend);
            
            container.Add(rightPanel);
        }
        
        private void DrawLeftPanel()
        {
            EditorGUILayout.Space(10);
            
            // 生成器引用
            EditorGUILayout.LabelField("场景生成器", EditorStyles.boldLabel);
            generator = (LevelGenerator)EditorGUILayout.ObjectField("LevelGenerator", generator, typeof(LevelGenerator), true);
            
            if (generator == null)
            {
                EditorGUILayout.HelpBox("请在场景中创建 LevelGenerator 组件，或点击下方按钮创建", MessageType.Warning);
                
                if (GUILayout.Button("创建 LevelGenerator", GUILayout.Height(30)))
                {
                    CreateGenerator();
                }
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // 布局配置文件
            EditorGUILayout.LabelField("布局配置", EditorStyles.boldLabel);
            layoutSO = (LevelLayoutSO)EditorGUILayout.ObjectField("布局文件", layoutSO, typeof(LevelLayoutSO), false);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新建布局"))
            {
                CreateNewLayoutSO();
            }
            if (layoutSO != null && GUILayout.Button("应用到生成器"))
            {
                generator.currentLayoutSO = layoutSO;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("生成关卡", GUILayout.Height(40)))
            {
                generator.GenerateLevel();
                UpdatePreview();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.8f, 0.6f);
            if (GUILayout.Button("生成走廊", GUILayout.Height(30)))
            {
                generator.GenerateCorridors();
                UpdatePreview();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("刷新预览", GUILayout.Height(30)))
            {
                UpdatePreview();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
            if (GUILayout.Button("烘焙到 Tilemap", GUILayout.Height(35)))
            {
                generator.BakeToTilemap();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
            if (GUILayout.Button("保存布局", GUILayout.Height(30)))
            {
                if (layoutSO != null)
                {
                    generator.currentLayoutSO = layoutSO;
                    generator.SaveLayout();
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择或创建布局配置文件", "确定");
                }
            }
            
            GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
            if (GUILayout.Button("加载布局", GUILayout.Height(30)))
            {
                if (layoutSO != null)
                {
                    generator.currentLayoutSO = layoutSO;
                    generator.LoadLayout();
                    UpdatePreview();
                    SceneView.RepaintAll();
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择布局配置文件", "确定");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // 关卡信息
            if (generator.CurrentLevel != null && generator.CurrentLevel.RoomCount > 0)
            {
                DrawLevelInfo();
            }
            
            EditorGUILayout.Space(10);
            
            // 重叠检测
            DrawOverlapWarnings();
            
            EditorGUILayout.Space(20);
            
            // 参数配置 - 使用 Odin Inspector 绘制以显示中文标签
            EditorGUILayout.LabelField("生成参数", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("详细参数请在 Inspector 中选中 LevelGenerator 对象查看", MessageType.Info);
            
            if (generator != null)
            {
                // 显示关键参数的快捷设置
                EditorGUI.BeginChangeCheck();
                
                generator.parameters.combatRoomCount = EditorGUILayout.IntSlider(
                    "战斗房间数量", generator.parameters.combatRoomCount, 1, 10);
                    
                generator.parameters.roomSpacing = EditorGUILayout.IntSlider(
                    "房间间距", generator.parameters.roomSpacing, 4, 20);
                    
                // 走廊功能已删除
                
                generator.parameters.yOffsetRange = EditorGUILayout.Vector2IntField(
                    "Y偏移范围", generator.parameters.yOffsetRange);
                
                generator.parameters.useRandomSeed = EditorGUILayout.Toggle(
                    "使用随机种子", generator.parameters.useRandomSeed);
                
                if (!generator.parameters.useRandomSeed)
                {
                    generator.parameters.levelSeed = EditorGUILayout.TextField(
                        "关卡种子", generator.parameters.levelSeed);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(generator);
                }
            }
        }
        
        private void DrawLevelInfo()
        {
            EditorGUILayout.LabelField("关卡信息", EditorStyles.boldLabel);
            
            var level = generator.CurrentLevel;
            var bounds = level.TotalBounds;
            
            EditorGUILayout.LabelField($"房间数量: {level.RoomCount}");
            EditorGUILayout.LabelField($"走廊数量: {level.CorridorCount}");
            EditorGUILayout.LabelField($"关卡尺寸: {bounds.width} x {bounds.height}");
            EditorGUILayout.LabelField($"种子: {level.levelSeed}");
            
            // 房间列表
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("房间列表:", EditorStyles.miniLabel);
            
            foreach (var room in level.rooms)
            {
                string typeIcon = room.roomType switch
                {
                    RoomType.Entrance => "🚪",
                    RoomType.Combat => "⚔️",
                    RoomType.Boss => "👹",
                    _ => "📦"
                };
                
                EditorGUILayout.LabelField($"  {typeIcon} #{room.id} [{room.roomType}] - 位置:({room.worldPosition.x}, {room.worldPosition.y}) 尺寸:{room.width}x{room.height}");
            }
        }
        
        private void DrawOverlapWarnings()
        {
            if (generator?.CurrentLevel == null) return;
            
            var overlaps = generator.GetOverlappingRooms();
            
            if (overlaps.Count > 0)
            {
                EditorGUILayout.HelpBox($"检测到 {overlaps.Count} 对房间重叠！", MessageType.Error);
                
                foreach (var (roomA, roomB) in overlaps)
                {
                    EditorGUILayout.LabelField($"  ⚠️ 房间 #{roomA} 与 #{roomB} 重叠", EditorStyles.miniLabel);
                }
            }
        }
        
        private VisualElement CreateLegend()
        {
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.flexWrap = Wrap.Wrap;
            legend.style.paddingLeft = 10;
            legend.style.paddingBottom = 10;
            legend.style.justifyContent = Justify.Center;
            
            legend.Add(CreateLegendItem("入口房间", new Color(0.2f, 0.8f, 0.2f)));
            legend.Add(CreateLegendItem("战斗房间", new Color(0.8f, 0.5f, 0.2f)));
            legend.Add(CreateLegendItem("Boss房间", new Color(0.8f, 0.2f, 0.2f)));
            legend.Add(CreateLegendItem("走廊", new Color(0.4f, 0.4f, 0.8f)));
            legend.Add(CreateLegendItem("重叠警告", new Color(1f, 0f, 0f)));
            
            return legend;
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
        
        #endregion

        #region 预览
        
        private void UpdatePreview()
        {
            if (generator?.CurrentLevel == null || generator.CurrentLevel.RoomCount == 0)
            {
                if (previewImage != null)
                {
                    previewImage.image = null;
                }
                return;
            }
            
            var level = generator.CurrentLevel;
            var bounds = level.TotalBounds;
            
            // 计算预览尺寸
            int padding = 10;
            int width = Mathf.Max(100, (int)((bounds.width + padding * 2) * previewScale));
            int height = Mathf.Max(100, (int)((bounds.height + padding * 2) * previewScale));
            
            // 限制最大尺寸
            width = Mathf.Min(width, 2048);
            height = Mathf.Min(height, 2048);
            
            // 创建纹理
            if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
            {
                if (previewTexture != null)
                {
                    DestroyImmediate(previewTexture);
                }
                previewTexture = new Texture2D(width, height);
                previewTexture.filterMode = FilterMode.Point;
            }
            
            // 填充背景
            Color[] pixels = new Color[width * height];
            Color bgColor = new Color(0.1f, 0.1f, 0.1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = bgColor;
            }
            
            // 获取重叠房间
            var overlaps = generator.GetOverlappingRooms();
            var overlappingRoomIds = new System.Collections.Generic.HashSet<int>();
            foreach (var (a, b) in overlaps)
            {
                overlappingRoomIds.Add(a);
                overlappingRoomIds.Add(b);
            }
            
            // 绘制房间
            foreach (var room in level.rooms)
            {
                Color roomColor = room.roomType switch
                {
                    RoomType.Entrance => new Color(0.2f, 0.8f, 0.2f, 0.7f),
                    RoomType.Combat => new Color(0.8f, 0.5f, 0.2f, 0.7f),
                    RoomType.Boss => new Color(0.8f, 0.2f, 0.2f, 0.7f),
                    _ => new Color(0.5f, 0.5f, 0.5f, 0.7f)
                };
                
                // 重叠房间用红色边框
                bool isOverlapping = overlappingRoomIds.Contains(room.id);
                
                DrawRoomToTexture(pixels, width, height, room, bounds, padding, roomColor, isOverlapping);
            }
            
            // 绘制走廊
            if (level.corridors != null)
            {
                foreach (var corridor in level.corridors)
                {
                    DrawCorridorToTexture(pixels, width, height, corridor, bounds, padding);
                }
            }
            
            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
            
            if (previewImage != null)
            {
                previewImage.image = previewTexture;
            }
        }
        
        private void DrawRoomToTexture(Color[] pixels, int texWidth, int texHeight, PlacedRoom room, RectInt levelBounds, int padding, Color color, bool isOverlapping)
        {
            int offsetX = -levelBounds.x + padding;
            int offsetY = -levelBounds.y + padding;
            
            int startX = (int)((room.worldPosition.x + offsetX) * previewScale);
            int startY = (int)((room.worldPosition.y + offsetY) * previewScale);
            int roomWidth = (int)(room.width * previewScale);
            int roomHeight = (int)(room.height * previewScale);
            
            // 绘制房间填充
            for (int x = startX; x < startX + roomWidth && x < texWidth; x++)
            {
                for (int y = startY; y < startY + roomHeight && y < texHeight; y++)
                {
                    if (x >= 0 && y >= 0)
                    {
                        pixels[y * texWidth + x] = color;
                    }
                }
            }
            
            // 绘制边框
            Color borderColor = isOverlapping ? Color.red : Color.white;
            int borderWidth = isOverlapping ? 3 : 1;
            
            for (int b = 0; b < borderWidth; b++)
            {
                // 上边
                for (int x = startX; x < startX + roomWidth && x < texWidth; x++)
                {
                    int y = startY + roomHeight - 1 - b;
                    if (x >= 0 && y >= 0 && y < texHeight)
                    {
                        pixels[y * texWidth + x] = borderColor;
                    }
                }
                // 下边
                for (int x = startX; x < startX + roomWidth && x < texWidth; x++)
                {
                    int y = startY + b;
                    if (x >= 0 && y >= 0 && y < texHeight)
                    {
                        pixels[y * texWidth + x] = borderColor;
                    }
                }
                // 左边
                for (int y = startY; y < startY + roomHeight && y < texHeight; y++)
                {
                    int x = startX + b;
                    if (x >= 0 && x < texWidth && y >= 0)
                    {
                        pixels[y * texWidth + x] = borderColor;
                    }
                }
                // 右边
                for (int y = startY; y < startY + roomHeight && y < texHeight; y++)
                {
                    int x = startX + roomWidth - 1 - b;
                    if (x >= 0 && x < texWidth && y >= 0)
                    {
                        pixels[y * texWidth + x] = borderColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制走廊到纹理
        /// </summary>
        private void DrawCorridorToTexture(Color[] pixels, int texWidth, int texHeight, CorridorData corridor, RectInt levelBounds, int padding)
        {
            if (corridor == null) return;
            
            int offsetX = -levelBounds.x + padding;
            int offsetY = -levelBounds.y + padding;
            
            Color corridorColor = new Color(0.4f, 0.4f, 0.8f, 0.7f);
            Color platformColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);
            
            int halfWidth = corridor.width / 2;
            int corridorThickness = (int)(corridor.width * previewScale);
            
            if (corridor.isStraight)
            {
                // 直线走廊
                int x1 = (int)((corridor.startPoint.x + offsetX) * previewScale);
                int y1 = (int)((corridor.startPoint.y + offsetY) * previewScale);
                int x2 = (int)((corridor.endPoint.x + offsetX) * previewScale);
                int y2 = (int)((corridor.endPoint.y + offsetY) * previewScale);
                
                DrawLine(pixels, texWidth, texHeight, x1, y1, x2, y2, corridorColor, corridorThickness / 2);
            }
            else
            {
                // L型走廊
                int startX = (int)((corridor.startPoint.x + offsetX) * previewScale);
                int startY = (int)((corridor.startPoint.y + offsetY) * previewScale);
                int cornerX = (int)((corridor.cornerPoint.x + offsetX) * previewScale);
                int cornerY1 = (int)((corridor.startPoint.y + offsetY) * previewScale);
                int cornerY2 = (int)((corridor.endPoint.y + offsetY) * previewScale);
                int endX = (int)((corridor.endPoint.x + offsetX) * previewScale);
                int endY = (int)((corridor.endPoint.y + offsetY) * previewScale);
                
                // 水平段1
                DrawLine(pixels, texWidth, texHeight, startX, startY, cornerX, cornerY1, corridorColor, corridorThickness / 2);
                // 垂直段
                DrawLine(pixels, texWidth, texHeight, cornerX, cornerY1, cornerX, cornerY2, corridorColor, corridorThickness / 2);
                // 水平段2
                DrawLine(pixels, texWidth, texHeight, cornerX, cornerY2, endX, endY, corridorColor, corridorThickness / 2);
            }
            
            // 绘制平台
            if (corridor.platforms != null)
            {
                foreach (var platform in corridor.platforms)
                {
                    int px = (int)((platform.x + offsetX) * previewScale);
                    int py = (int)((platform.y + offsetY) * previewScale);
                    
                    // 绘制平台标记（小方块）
                    int platformSize = Mathf.Max(2, corridorThickness / 2);
                    for (int dx = -platformSize; dx <= platformSize; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int drawX = px + dx;
                            int drawY = py + dy;
                            if (drawX >= 0 && drawX < texWidth && drawY >= 0 && drawY < texHeight)
                            {
                                pixels[drawY * texWidth + drawX] = platformColor;
                            }
                        }
                    }
                }
            }
        }
        
        private void DrawLine(Color[] pixels, int texWidth, int texHeight, int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            int dx = Mathf.Abs(x2 - x1);
            int dy = Mathf.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                // 绘制粗线
                for (int tx = -thickness; tx <= thickness; tx++)
                {
                    for (int ty = -thickness; ty <= thickness; ty++)
                    {
                        int px = x1 + tx;
                        int py = y1 + ty;
                        if (px >= 0 && px < texWidth && py >= 0 && py < texHeight)
                        {
                            pixels[py * texWidth + px] = color;
                        }
                    }
                }
                
                if (x1 == x2 && y1 == y2) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
        
        #endregion

        #region 辅助方法
        
        private void FindGenerator()
        {
            generator = FindObjectOfType<LevelGenerator>();
        }
        
        private void CreateGenerator()
        {
            GameObject go = new GameObject("LevelGenerator");
            generator = go.AddComponent<LevelGenerator>();
            Selection.activeGameObject = go;
            
            Debug.Log("[LevelGeneratorWindow] 已创建 LevelGenerator");
        }
        
        private void CreateNewLayoutSO()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建关卡布局配置",
                "NewLevelLayout",
                "asset",
                "选择保存位置"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                layoutSO = ScriptableObject.CreateInstance<LevelLayoutSO>();
                layoutSO.levelName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                AssetDatabase.CreateAsset(layoutSO, path);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[LevelGeneratorWindow] 已创建布局配置: {path}");
            }
        }
        
        #endregion
    }
}
