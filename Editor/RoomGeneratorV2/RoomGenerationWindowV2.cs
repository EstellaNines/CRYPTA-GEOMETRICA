#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间生成编辑器窗口 v0.2
    /// 使用 Odin Inspector 增强的现代化 UI
    /// </summary>
    public class RoomGenerationWindowV2 : OdinEditorWindow
    {
        #region 菜单入口
        
        [MenuItem("自制工具/程序化关卡/程序化房间生成V2/Room Generator V2", false, 100)]
        private static void OpenWindow()
        {
            var window = GetWindow<RoomGenerationWindowV2>();
            window.titleContent = new GUIContent("房间生成器 V2", EditorGUIUtility.IconContent("d_Grid.Default").image);
            window.minSize = new Vector2(900, 650);
            window.Show();
        }
        
        #endregion

        #region 配置字段
        
        [TitleGroup("配置", "Configuration", TitleAlignments.Centered)]
        [LabelText("配置文件"), InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed)]
        [InfoBox("选择或创建一个配置文件来保存生成参数", InfoMessageType.Info, VisibleIf = "@settings == null")]
        public RoomGenerationSettingsV2 settings;
        
        [TitleGroup("配置")]
        [LabelText("目标 Tilemap（墙壁层）"), Required]
        public Tilemap targetTilemap;
        
        [TitleGroup("配置")]
        [LabelText("目标 Tilemap（平台层）")]
        public Tilemap platformTilemap;
        
        #endregion

        #region 生成参数
        
        [TitleGroup("生成参数", "Generation Parameters", TitleAlignments.Centered)]
        [ShowIf("@settings == null")]
        [HideLabel, InlineProperty]
        public RoomGenParamsV2 inlineParams = new RoomGenParamsV2();
        
        #endregion

        #region 主题选择
        
        [TitleGroup("视觉主题", "Visual Theme", TitleAlignments.Centered)]
        [LabelText("主题配置文件"), Required]
        [InfoBox("请创建或选择一个主题配置文件", InfoMessageType.Warning, VisibleIf = "@themeConfig == null")]
        public RoomThemeConfigSO themeConfig;
        
        [TitleGroup("视觉主题")]
        [LabelText("当前主题"), ValueDropdown("GetThemeNames")]
        [ShowIf("@themeConfig != null && themeConfig.ThemeCount > 0")]
        public int selectedThemeIndex = 0;
        
        private IEnumerable<ValueDropdownItem<int>> GetThemeNames()
        {
            if (themeConfig == null || themeConfig.themes == null) yield break;
            
            for (int i = 0; i < themeConfig.themes.Count; i++)
            {
                string name = string.IsNullOrEmpty(themeConfig.themes[i].themeName) ? $"主题 {i}" : themeConfig.themes[i].themeName;
                yield return new ValueDropdownItem<int>(name, i);
            }
        }
        
        // 兼容旧代码的主题列表属性
        private List<RoomThemeV2> themes => themeConfig?.themes ?? new List<RoomThemeV2>();
        
        #endregion

        #region 预览数据
        
        [TitleGroup("预览", "Preview", TitleAlignments.Centered)]
        [ShowInInspector, ReadOnly]
        [LabelText("当前种子")]
        private string currentSeed => lastGeneratedRoom?.seed ?? "未生成";
        
        [TitleGroup("预览")]
        [ShowInInspector, ReadOnly]
        [LabelText("房间尺寸")]
        private string roomSize => lastGeneratedRoom != null 
            ? $"{lastGeneratedRoom.width} x {lastGeneratedRoom.height}" 
            : "未生成";
        
        [TitleGroup("预览")]
        [ShowInInspector, ReadOnly]
        [LabelText("房间数量")]
        private int roomCount => lastGeneratedRoom?.roomGraph?.RoomCount ?? 0;
        
        [TitleGroup("预览")]
        [ShowInInspector, ReadOnly]
        [LabelText("走廊数量")]
        private int corridorCount => lastGeneratedRoom?.roomGraph?.finalEdges?.Count ?? 0;
        
        [TitleGroup("预览")]
        [ShowInInspector, ReadOnly]
        [LabelText("地面瓦片")]
        private int floorTileCount => lastGeneratedRoom?.FloorCount ?? 0;
        
        [TitleGroup("预览")]
        [ShowInInspector, ReadOnly]
        [LabelText("生成点数量")]
        private int spawnPointCount => lastGeneratedRoom?.potentialSpawns?.Count ?? 0;
        
        #endregion

        #region 统计信息
        
        [TitleGroup("统计", "Statistics", TitleAlignments.Centered)]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1, ColorGetter = "GetOpennessColor")]
        [LabelText("开放度")]
        private float openness => lastGeneratedRoom != null 
            ? (float)lastGeneratedRoom.FloorCount / (lastGeneratedRoom.width * lastGeneratedRoom.height) 
            : 0;
        
        private Color GetOpennessColor(float value)
        {
            return Color.Lerp(Color.red, Color.green, value * 2);
        }
        
        [TitleGroup("统计")]
        [ShowInInspector, ReadOnly]
        [LabelText("生成耗时")]
        private string generationTime => lastGenerationTime > 0 
            ? $"{lastGenerationTime:F2} ms" 
            : "未计时";
        
        #endregion

        #region 内部状态
        
        private RoomDataV2 lastGeneratedRoom;
        private RoomGeneratorV2 generator;
        private float lastGenerationTime;
        
        // Gizmo 绘制
        private bool showBSPGizmos = true;
        private bool showRoomGizmos = true;
        private bool showGraphGizmos = true;
        private bool showSpawnGizmos = true;
        
        // 小地图预览
        private Texture2D previewTexture;
        private Vector2 previewScrollPos;
        private float previewScale = 1f;
        
        // 小地图显示选项
        private bool previewShowBSP = false;
        private bool previewShowRooms = true;
        private bool previewShowGraph = true;
        private bool previewShowSpawns = true;
        private bool previewShowPlatforms = true;
        
        #endregion

        #region 操作按钮
        
        [TitleGroup("操作", "Actions", TitleAlignments.Centered)]
        [HorizontalGroup("操作/Buttons", Width = 0.5f)]
        [Button("生成房间", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        private void GenerateRoom()
        {
            if (!ValidateSetup()) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                EnsureGenerator();
                
                // 设置参数
                generator.parameters = GetCurrentParams();
                generator.targetTilemap = targetTilemap;
                generator.platformTilemap = platformTilemap;
                generator.themes = themes;
                
                // 生成
                generator.GenerateRoom();
                lastGeneratedRoom = generator.CurrentRoom;
                
                stopwatch.Stop();
                lastGenerationTime = stopwatch.ElapsedMilliseconds;
                
                Debug.Log($"[RoomGenerationWindowV2] 房间生成完成，耗时 {lastGenerationTime:F2} ms");
                
                // 刷新 Scene 视图
                SceneView.RepaintAll();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomGenerationWindowV2] 生成失败: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [HorizontalGroup("操作/Buttons")]
        [Button("烘焙到 Tilemap", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 0.9f)]
        private void BakeToTilemap()
        {
            if (lastGeneratedRoom == null)
            {
                EditorUtility.DisplayDialog("错误", "请先生成房间", "确定");
                return;
            }
            
            if (targetTilemap == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定目标 Tilemap", "确定");
                return;
            }
            
            try
            {
                EnsureGenerator();
                generator.targetTilemap = targetTilemap;
                generator.platformTilemap = platformTilemap;
                generator.themes = themes;
                generator.SetRoomData(lastGeneratedRoom);
                generator.ForcePickTheme();
                generator.BakeToTilemap();
                
                // 标记场景已修改
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );
                
                Debug.Log("[RoomGenerationWindowV2] 烘焙完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomGenerationWindowV2] 烘焙失败: {e.Message}");
            }
        }
        
        [TitleGroup("操作")]
        [HorizontalGroup("操作/Buttons2", Width = 0.33f)]
        [Button("清空 Tilemap"), GUIColor(0.9f, 0.6f, 0.4f)]
        private void ClearTilemap()
        {
            if (targetTilemap != null)
            {
                Undo.RecordObject(targetTilemap, "Clear Tilemap");
                targetTilemap.ClearAllTiles();
            }
            
            if (platformTilemap != null)
            {
                Undo.RecordObject(platformTilemap, "Clear Platform Tilemap");
                platformTilemap.ClearAllTiles();
            }
            
            Debug.Log("[RoomGenerationWindowV2] Tilemap 已清空");
        }
        
        [HorizontalGroup("操作/Buttons2")]
        [Button("复制种子")]
        private void CopySeed()
        {
            if (lastGeneratedRoom != null && !string.IsNullOrEmpty(lastGeneratedRoom.seed))
            {
                GUIUtility.systemCopyBuffer = lastGeneratedRoom.seed;
                Debug.Log($"[RoomGenerationWindowV2] 种子已复制: {lastGeneratedRoom.seed}");
            }
        }
        
        [HorizontalGroup("操作/Buttons2")]
        [Button("重置参数")]
        private void ResetParams()
        {
            if (settings != null)
            {
                settings.parameters = new RoomGenParamsV2();
                EditorUtility.SetDirty(settings);
            }
            else
            {
                inlineParams = new RoomGenParamsV2();
            }
            
            Debug.Log("[RoomGenerationWindowV2] 参数已重置");
        }
        
        #endregion

        #region Gizmo 控制
        
        [TitleGroup("调试显示", "Debug Visualization", TitleAlignments.Centered)]
        [HorizontalGroup("调试显示/Toggles")]
        [ToggleLeft, LabelText("BSP 分割")]
        [OnValueChanged("RepaintSceneView")]
        public bool ShowBSP { get => showBSPGizmos; set => showBSPGizmos = value; }
        
        [HorizontalGroup("调试显示/Toggles")]
        [ToggleLeft, LabelText("房间区域")]
        [OnValueChanged("RepaintSceneView")]
        public bool ShowRooms { get => showRoomGizmos; set => showRoomGizmos = value; }
        
        [HorizontalGroup("调试显示/Toggles")]
        [ToggleLeft, LabelText("连接图")]
        [OnValueChanged("RepaintSceneView")]
        public bool ShowGraph { get => showGraphGizmos; set => showGraphGizmos = value; }
        
        [HorizontalGroup("调试显示/Toggles")]
        [ToggleLeft, LabelText("生成点")]
        [OnValueChanged("RepaintSceneView")]
        public bool ShowSpawns { get => showSpawnGizmos; set => showSpawnGizmos = value; }
        
        private void RepaintSceneView()
        {
            SceneView.RepaintAll();
        }
        
        #endregion

        #region 配置文件操作
        
        [TitleGroup("配置文件", "Configuration File", TitleAlignments.Centered)]
        [HorizontalGroup("配置文件/FileOps")]
        [Button("新建参数配置")]
        private void CreateNewSettings()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存参数配置文件",
                "RoomGenerationSettingsV2",
                "asset",
                "选择保存位置"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            var newSettings = CreateInstance<RoomGenerationSettingsV2>();
            newSettings.parameters = inlineParams != null ? inlineParams : new RoomGenParamsV2();
            
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            
            settings = newSettings;
            
            Debug.Log($"[RoomGenerationWindowV2] 参数配置文件已创建: {path}");
        }
        
        [HorizontalGroup("配置文件/FileOps")]
        [Button("新建主题配置")]
        private void CreateNewThemeConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存主题配置文件",
                "RoomThemeConfig",
                "asset",
                "选择保存位置"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            var newConfig = CreateInstance<RoomThemeConfigSO>();
            
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            
            themeConfig = newConfig;
            
            Debug.Log($"[RoomGenerationWindowV2] 主题配置文件已创建: {path}");
        }
        
        [HorizontalGroup("配置文件/FileOps")]
        [Button("保存参数")]
        [EnableIf("@settings != null")]
        private void SaveSettings()
        {
            if (settings == null) return;
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[RoomGenerationWindowV2] 参数配置已保存");
        }
        
        #endregion

        #region 辅助方法
        
        private bool ValidateSetup()
        {
            if (targetTilemap == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定目标 Tilemap（墙壁层）", "确定");
                return false;
            }
            
            if (themeConfig == null || themeConfig.ThemeCount == 0)
            {
                EditorUtility.DisplayDialog("警告", "未设置主题配置，将使用默认瓦片", "继续");
            }
            
            return true;
        }
        
        private void EnsureGenerator()
        {
            if (generator == null)
            {
                // 查找场景中的生成器
                generator = FindObjectOfType<RoomGeneratorV2>();
                
                if (generator == null)
                {
                    // 创建临时 GameObject
                    var go = new GameObject("_TempRoomGenerator");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    generator = go.AddComponent<RoomGeneratorV2>();
                }
            }
        }
        
        private RoomGenParamsV2 GetCurrentParams()
        {
            if (settings != null && settings.parameters != null)
            {
                return settings.parameters;
            }
            return inlineParams ?? new RoomGenParamsV2();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        #endregion

        #region Scene GUI 绘制
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (lastGeneratedRoom == null) return;
            
            Vector3 offset = targetTilemap != null 
                ? targetTilemap.transform.position 
                : Vector3.zero;
            
            Handles.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
            
            // 绘制 BSP 分割线
            if (showBSPGizmos && lastGeneratedRoom.bspRoot != null)
            {
                DrawBSPGizmos(lastGeneratedRoom.bspRoot);
            }
            
            // 绘制房间区域
            if (showRoomGizmos && lastGeneratedRoom.roomGraph != null)
            {
                DrawRoomGizmos(lastGeneratedRoom.roomGraph.rooms);
            }
            
            // 绘制连接图
            if (showGraphGizmos && lastGeneratedRoom.roomGraph != null)
            {
                DrawGraphGizmos(lastGeneratedRoom.roomGraph);
            }
            
            // 绘制生成点
            if (showSpawnGizmos && lastGeneratedRoom.potentialSpawns != null)
            {
                DrawSpawnGizmos(lastGeneratedRoom.potentialSpawns);
            }
            
            // 绘制入口/出口
            DrawEntranceExitGizmos();
            
            Handles.matrix = Matrix4x4.identity;
        }
        
        private void DrawBSPGizmos(BSPNode node)
        {
            if (node == null) return;
            
            // 绘制节点边界
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            DrawRect(node.bounds);
            
            // 绘制分割线
            if (node.left != null && node.right != null)
            {
                Handles.color = Color.yellow;
                
                if (node.splitDirection == SplitDirection.Horizontal)
                {
                    Vector3 start = new Vector3(node.bounds.x, node.splitPosition, 0);
                    Vector3 end = new Vector3(node.bounds.xMax, node.splitPosition, 0);
                    Handles.DrawLine(start, end);
                }
                else
                {
                    Vector3 start = new Vector3(node.splitPosition, node.bounds.y, 0);
                    Vector3 end = new Vector3(node.splitPosition, node.bounds.yMax, 0);
                    Handles.DrawLine(start, end);
                }
                
                DrawBSPGizmos(node.left);
                DrawBSPGizmos(node.right);
            }
        }
        
        private void DrawRoomGizmos(List<RoomRegion> rooms)
        {
            if (rooms == null) return;
            
            foreach (var room in rooms)
            {
                // 房间边界
                if (room.isEntrance)
                    Handles.color = new Color(0, 1, 0, 0.4f);
                else if (room.isExit)
                    Handles.color = new Color(1, 0, 0, 0.4f);
                else
                    Handles.color = new Color(0, 0.5f, 1, 0.3f);
                
                DrawFilledRect(room.bounds);
                
                // 房间 ID
                Handles.color = Color.white;
                Handles.Label(
                    new Vector3(room.center.x, room.center.y, 0),
                    $"R{room.id}",
                    EditorStyles.boldLabel
                );
            }
        }
        
        private void DrawGraphGizmos(RoomGraph graph)
        {
            if (graph == null || graph.rooms == null) return;
            
            // 绘制 MST 边（绿色）
            Handles.color = Color.green;
            foreach (var edge in graph.mstEdges)
            {
                var roomA = graph.GetRoom(edge.roomA);
                var roomB = graph.GetRoom(edge.roomB);
                if (roomA != null && roomB != null)
                {
                    Vector3 a = new Vector3(roomA.center.x, roomA.center.y, 0);
                    Vector3 b = new Vector3(roomB.center.x, roomB.center.y, 0);
                    Handles.DrawLine(a, b);
                }
            }
            
            // 绘制额外边（蓝色虚线）
            Handles.color = new Color(0.3f, 0.6f, 1f, 0.7f);
            foreach (var edge in graph.extraEdges)
            {
                var roomA = graph.GetRoom(edge.roomA);
                var roomB = graph.GetRoom(edge.roomB);
                if (roomA != null && roomB != null)
                {
                    Vector3 a = new Vector3(roomA.center.x, roomA.center.y, 0);
                    Vector3 b = new Vector3(roomB.center.x, roomB.center.y, 0);
                    Handles.DrawDottedLine(a, b, 4f);
                }
            }
        }
        
        private void DrawSpawnGizmos(List<SpawnPointV2> spawns)
        {
            if (spawns == null) return;
            
            foreach (var spawn in spawns)
            {
                Vector3 pos = new Vector3(spawn.position.x, spawn.position.y, 0);
                
                if (spawn.type == SpawnType.Ground)
                {
                    Handles.color = Color.red;
                    Handles.DrawSolidDisc(pos, Vector3.forward, 0.5f);
                }
                else
                {
                    Handles.color = Color.magenta;
                    Handles.DrawWireDisc(pos, Vector3.forward, 0.5f);
                }
            }
        }
        
        private void DrawEntranceExitGizmos()
        {
            if (lastGeneratedRoom == null) return;
            
            // 入口（绿色箭头）
            Handles.color = Color.green;
            Vector3 entrance = new Vector3(lastGeneratedRoom.startPos.x, lastGeneratedRoom.startPos.y, 0);
            Handles.DrawSolidDisc(entrance, Vector3.forward, 1f);
            Handles.ArrowHandleCap(0, entrance, Quaternion.LookRotation(Vector3.right), 2f, EventType.Repaint);
            
            // 出口（红色箭头）
            Handles.color = Color.red;
            Vector3 exit = new Vector3(lastGeneratedRoom.endPos.x, lastGeneratedRoom.endPos.y, 0);
            Handles.DrawSolidDisc(exit, Vector3.forward, 1f);
            Handles.ArrowHandleCap(0, exit, Quaternion.LookRotation(Vector3.right), 2f, EventType.Repaint);
        }
        
        private void DrawRect(RectInt rect)
        {
            Vector3[] points = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0),
                new Vector3(rect.x, rect.y, 0)
            };
            Handles.DrawPolyLine(points);
        }
        
        private void DrawFilledRect(RectInt rect)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(rect.x, rect.y, 0),
                new Vector3(rect.xMax, rect.y, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.x, rect.yMax, 0)
            };
            Handles.DrawSolidRectangleWithOutline(verts, Handles.color, Color.white);
        }
        
        #endregion

        #region 小地图预览
        
        /// <summary>
        /// 重写 OnImGUI 添加右侧小地图预览
        /// </summary>
        protected override void OnImGUI()
        {
            // 左右分割布局
            EditorGUILayout.BeginHorizontal();
            
            // 左侧参数面板（固定宽度）
            EditorGUILayout.BeginVertical(GUILayout.Width(420));
            base.OnImGUI();
            EditorGUILayout.EndVertical();
            
            // 右侧小地图预览
            EditorGUILayout.BeginVertical("box");
            DrawMinimapPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制小地图面板
        /// </summary>
        private void DrawMinimapPanel()
        {
            // 标题
            GUILayout.Label("房间预览", EditorStyles.boldLabel);
            
            // 显示选项
            EditorGUILayout.BeginHorizontal();
            previewShowRooms = GUILayout.Toggle(previewShowRooms, "房间", EditorStyles.miniButtonLeft);
            previewShowGraph = GUILayout.Toggle(previewShowGraph, "连接", EditorStyles.miniButtonMid);
            previewShowPlatforms = GUILayout.Toggle(previewShowPlatforms, "平台", EditorStyles.miniButtonMid);
            previewShowSpawns = GUILayout.Toggle(previewShowSpawns, "生成点", EditorStyles.miniButtonMid);
            previewShowBSP = GUILayout.Toggle(previewShowBSP, "BSP", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
            
            // 缩放控制
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("缩放:", GUILayout.Width(35));
            previewScale = GUILayout.HorizontalSlider(previewScale, 0.5f, 3f);
            GUILayout.Label($"{previewScale:F1}x", GUILayout.Width(35));
            if (GUILayout.Button("重置", GUILayout.Width(40)))
            {
                previewScale = 1f;
            }
            EditorGUILayout.EndHorizontal();
            
            // 小地图绘制区域
            Rect previewRect = GUILayoutUtility.GetRect(400, 500, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            if (lastGeneratedRoom == null)
            {
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));
                GUI.Label(previewRect, "请先生成房间", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
                return;
            }
            
            // 绘制背景
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f));
            
            // 计算缩放和偏移
            float roomWidth = lastGeneratedRoom.width;
            float roomHeight = lastGeneratedRoom.height;
            
            float scaleX = (previewRect.width - 20) / roomWidth * previewScale;
            float scaleY = (previewRect.height - 20) / roomHeight * previewScale;
            float scale = Mathf.Min(scaleX, scaleY);
            
            float offsetX = previewRect.x + (previewRect.width - roomWidth * scale) / 2;
            float offsetY = previewRect.y + (previewRect.height - roomHeight * scale) / 2;
            
            // 开始裁剪
            GUI.BeginClip(previewRect);
            
            // 调整偏移以适应裁剪区域
            float clipOffsetX = (previewRect.width - roomWidth * scale) / 2;
            float clipOffsetY = (previewRect.height - roomHeight * scale) / 2;
            
            // 绘制网格
            DrawMinimapGrid(clipOffsetX, clipOffsetY, scale);
            
            // 绘制 BSP 分割线
            if (previewShowBSP && lastGeneratedRoom.bspRoot != null)
            {
                DrawMinimapBSP(lastGeneratedRoom.bspRoot, clipOffsetX, clipOffsetY, scale);
            }
            
            // 绘制房间区域
            if (previewShowRooms && lastGeneratedRoom.roomGraph != null)
            {
                DrawMinimapRooms(clipOffsetX, clipOffsetY, scale);
            }
            
            // 绘制连接图
            if (previewShowGraph && lastGeneratedRoom.roomGraph != null)
            {
                DrawMinimapGraph(clipOffsetX, clipOffsetY, scale);
            }
            
            // 绘制生成点
            if (previewShowSpawns && lastGeneratedRoom.potentialSpawns != null)
            {
                DrawMinimapSpawns(clipOffsetX, clipOffsetY, scale);
            }
            
            // 绘制入口/出口
            DrawMinimapEntranceExit(clipOffsetX, clipOffsetY, scale);
            
            GUI.EndClip();
            
            // 绘制图例
            DrawMinimapLegend(previewRect);
        }
        
        /// <summary>
        /// 绘制小地图网格
        /// </summary>
        private void DrawMinimapGrid(float offsetX, float offsetY, float scale)
        {
            if (lastGeneratedRoom == null || lastGeneratedRoom.grid == null) return;
            
            int width = lastGeneratedRoom.width;
            int height = lastGeneratedRoom.height;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int tileType = lastGeneratedRoom.grid[x, y];
                    Color color;
                    
                    switch ((TileType)tileType)
                    {
                        case TileType.Wall:
                            color = new Color(0.3f, 0.3f, 0.3f); // 深灰色墙壁
                            break;
                        case TileType.Floor:
                            color = new Color(0.6f, 0.5f, 0.4f); // 棕色地板
                            break;
                        case TileType.Platform:
                            if (!previewShowPlatforms) continue;
                            color = new Color(0.9f, 0.7f, 0.2f); // 金色平台
                            break;
                        case TileType.Entrance:
                            color = new Color(0.2f, 0.8f, 0.2f); // 绿色入口
                            break;
                        case TileType.Exit:
                            color = new Color(0.8f, 0.2f, 0.2f); // 红色出口
                            break;
                        default:
                            continue;
                    }
                    
                    // Y 轴翻转（Unity 坐标系 Y 向上，GUI Y 向下）
                    float drawY = (height - 1 - y) * scale + offsetY;
                    Rect tileRect = new Rect(x * scale + offsetX, drawY, scale, scale);
                    EditorGUI.DrawRect(tileRect, color);
                }
            }
        }
        
        /// <summary>
        /// 绘制小地图 BSP 分割线
        /// </summary>
        private void DrawMinimapBSP(BSPNode node, float offsetX, float offsetY, float scale)
        {
            if (node == null) return;
            
            int height = lastGeneratedRoom.height;
            
            if (node.left != null && node.right != null)
            {
                // 绘制分割线
                if (node.splitDirection == SplitDirection.Horizontal)
                {
                    float y = (height - node.splitPosition) * scale + offsetY;
                    float x1 = node.bounds.x * scale + offsetX;
                    float x2 = node.bounds.xMax * scale + offsetX;
                    DrawLine(new Vector2(x1, y), new Vector2(x2, y), Color.yellow, 1);
                }
                else
                {
                    float x = node.splitPosition * scale + offsetX;
                    float y1 = (height - node.bounds.y) * scale + offsetY;
                    float y2 = (height - node.bounds.yMax) * scale + offsetY;
                    DrawLine(new Vector2(x, y1), new Vector2(x, y2), Color.yellow, 1);
                }
                
                DrawMinimapBSP(node.left, offsetX, offsetY, scale);
                DrawMinimapBSP(node.right, offsetX, offsetY, scale);
            }
        }
        
        /// <summary>
        /// 绘制小地图房间区域
        /// </summary>
        private void DrawMinimapRooms(float offsetX, float offsetY, float scale)
        {
            if (lastGeneratedRoom.roomGraph?.rooms == null) return;
            
            int height = lastGeneratedRoom.height;
            
            foreach (var room in lastGeneratedRoom.roomGraph.rooms)
            {
                Color color;
                if (room.isEntrance)
                    color = new Color(0, 1, 0, 0.3f);
                else if (room.isExit)
                    color = new Color(1, 0, 0, 0.3f);
                else
                    color = new Color(0, 0.5f, 1, 0.2f);
                
                float x = room.bounds.x * scale + offsetX;
                float y = (height - room.bounds.yMax) * scale + offsetY;
                float w = room.bounds.width * scale;
                float h = room.bounds.height * scale;
                
                Rect roomRect = new Rect(x, y, w, h);
                EditorGUI.DrawRect(roomRect, color);
                
                // 绘制边框
                DrawRectOutline(roomRect, Color.white, 1);
                
                // 房间 ID
                GUI.Label(roomRect, $"R{room.id}", new GUIStyle(EditorStyles.miniLabel) 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                });
            }
        }
        
        /// <summary>
        /// 绘制小地图连接图
        /// </summary>
        private void DrawMinimapGraph(float offsetX, float offsetY, float scale)
        {
            if (lastGeneratedRoom.roomGraph == null) return;
            
            int height = lastGeneratedRoom.height;
            var graph = lastGeneratedRoom.roomGraph;
            
            // 绘制 MST 边（绿色）
            if (graph.mstEdges != null)
            {
                foreach (var edge in graph.mstEdges)
                {
                    var roomA = graph.GetRoom(edge.roomA);
                    var roomB = graph.GetRoom(edge.roomB);
                    if (roomA != null && roomB != null)
                    {
                        Vector2 a = new Vector2(roomA.center.x * scale + offsetX, (height - roomA.center.y) * scale + offsetY);
                        Vector2 b = new Vector2(roomB.center.x * scale + offsetX, (height - roomB.center.y) * scale + offsetY);
                        DrawLine(a, b, Color.green, 2);
                    }
                }
            }
            
            // 绘制额外边（蓝色虚线）
            if (graph.extraEdges != null)
            {
                foreach (var edge in graph.extraEdges)
                {
                    var roomA = graph.GetRoom(edge.roomA);
                    var roomB = graph.GetRoom(edge.roomB);
                    if (roomA != null && roomB != null)
                    {
                        Vector2 a = new Vector2(roomA.center.x * scale + offsetX, (height - roomA.center.y) * scale + offsetY);
                        Vector2 b = new Vector2(roomB.center.x * scale + offsetX, (height - roomB.center.y) * scale + offsetY);
                        DrawDottedLine(a, b, new Color(0.3f, 0.6f, 1f), 2);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制小地图生成点
        /// </summary>
        private void DrawMinimapSpawns(float offsetX, float offsetY, float scale)
        {
            if (lastGeneratedRoom.potentialSpawns == null) return;
            
            int height = lastGeneratedRoom.height;
            
            foreach (var spawn in lastGeneratedRoom.potentialSpawns)
            {
                float x = spawn.position.x * scale + offsetX;
                float y = (height - spawn.position.y) * scale + offsetY;
                float size = Mathf.Max(4, scale * 0.8f);
                
                Color color = spawn.type == SpawnType.Ground ? Color.red : Color.magenta;
                Rect spawnRect = new Rect(x - size/2, y - size/2, size, size);
                
                if (spawn.type == SpawnType.Ground)
                {
                    EditorGUI.DrawRect(spawnRect, color);
                }
                else
                {
                    DrawRectOutline(spawnRect, color, 2);
                }
            }
        }
        
        /// <summary>
        /// 绘制小地图入口/出口
        /// </summary>
        private void DrawMinimapEntranceExit(float offsetX, float offsetY, float scale)
        {
            if (lastGeneratedRoom == null) return;
            
            int height = lastGeneratedRoom.height;
            float markerSize = Mathf.Max(8, scale * 1.5f);
            
            // 入口（绿色三角）
            float entranceX = lastGeneratedRoom.startPos.x * scale + offsetX;
            float entranceY = (height - lastGeneratedRoom.startPos.y) * scale + offsetY;
            DrawTriangle(new Vector2(entranceX, entranceY), markerSize, Color.green, true);
            
            // 出口（红色三角）
            float exitX = lastGeneratedRoom.endPos.x * scale + offsetX;
            float exitY = (height - lastGeneratedRoom.endPos.y) * scale + offsetY;
            DrawTriangle(new Vector2(exitX, exitY), markerSize, Color.red, true);
        }
        
        /// <summary>
        /// 绘制图例
        /// </summary>
        private void DrawMinimapLegend(Rect previewRect)
        {
            float legendX = previewRect.x + 5;
            float legendY = previewRect.y + 5;
            float itemHeight = 14;
            float boxSize = 10;
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            
            // 图例项
            var legends = new (Color color, string label)[]
            {
                (new Color(0.6f, 0.5f, 0.4f), "地板"),
                (new Color(0.3f, 0.3f, 0.3f), "墙壁"),
                (new Color(0.9f, 0.7f, 0.2f), "平台"),
                (Color.green, "入口"),
                (Color.red, "出口"),
                (Color.red, "地面生成点"),
                (Color.magenta, "空中生成点"),
            };
            
            for (int i = 0; i < legends.Length; i++)
            {
                Rect boxRect = new Rect(legendX, legendY + i * itemHeight, boxSize, boxSize);
                EditorGUI.DrawRect(boxRect, legends[i].color);
                GUI.Label(new Rect(legendX + boxSize + 3, legendY + i * itemHeight - 2, 80, itemHeight), legends[i].label, labelStyle);
            }
        }
        
        #region 绘制辅助方法
        
        private void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0));
            Handles.color = oldColor;
            Handles.EndGUI();
        }
        
        private void DrawDottedLine(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawDottedLine(new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0), 3f);
            Handles.color = oldColor;
            Handles.EndGUI();
        }
        
        private void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), color, thickness);
            DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax), color, thickness);
            DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.x, rect.yMax), color, thickness);
            DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.x, rect.y), color, thickness);
        }
        
        private void DrawTriangle(Vector2 center, float size, Color color, bool pointRight)
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = color;
            
            Vector3[] points;
            if (pointRight)
            {
                points = new Vector3[]
                {
                    new Vector3(center.x - size/2, center.y - size/2, 0),
                    new Vector3(center.x + size/2, center.y, 0),
                    new Vector3(center.x - size/2, center.y + size/2, 0)
                };
            }
            else
            {
                points = new Vector3[]
                {
                    new Vector3(center.x + size/2, center.y - size/2, 0),
                    new Vector3(center.x - size/2, center.y, 0),
                    new Vector3(center.x + size/2, center.y + size/2, 0)
                };
            }
            
            Handles.DrawAAConvexPolygon(points);
            Handles.color = oldColor;
            Handles.EndGUI();
        }
        
        #endregion
        
        #endregion
    }
}
#endif
