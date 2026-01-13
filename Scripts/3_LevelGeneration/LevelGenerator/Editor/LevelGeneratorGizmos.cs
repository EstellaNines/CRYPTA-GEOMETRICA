using UnityEngine;
using UnityEditor;
using CryptaGeometrica.LevelGeneration.MultiRoom;
using CryptaGeometrica.LevelGeneration.SmallRoomV2;
using System.Collections.Generic;
using RoomType = CryptaGeometrica.LevelGeneration.MultiRoom.RoomType;

namespace CryptaGeometrica.Tools.LevelGeneration
{
    /// <summary>
    /// 关卡生成器 Gizmos 绘制和场景编辑
    /// 提供实时可视化和房间位置调整功能
    /// </summary>
    [CustomEditor(typeof(LevelGenerator))]
    public class LevelGeneratorGizmos : Editor
    {
        #region 字段
        
        private LevelGenerator generator;
        
        // 颜色定义
        private readonly Color entranceColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        private readonly Color combatColor = new Color(0.8f, 0.5f, 0.2f, 0.5f);
        private readonly Color bossColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        private readonly Color corridorColor = new Color(0.4f, 0.4f, 0.8f, 0.8f);
        private readonly Color overlapColor = new Color(1f, 0f, 0f, 0.8f);
        private readonly Color doorColor = new Color(0.2f, 0.8f, 0.8f, 0.8f);
        
        // 拖拽状态（预留，后续可用于更复杂的拖拽逻辑）
        #pragma warning disable CS0414
        private int draggingRoomId = -1;
        private Vector2Int dragStartPosition;
        #pragma warning restore CS0414
        
        // 重叠房间缓存
        private HashSet<int> overlappingRoomIds = new HashSet<int>();
        
        #endregion

        #region Unity 生命周期
        
        private void OnEnable()
        {
            generator = (LevelGenerator)target;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        #endregion

        #region Inspector GUI
        
        public override void OnInspectorGUI()
        {
            // 绘制默认 Inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // 快捷操作按钮
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开编辑器窗口"))
            {
                EditorApplication.ExecuteMenuItem("自制工具/程序化关卡/多房间关卡生成");
            }
            if (GUILayout.Button("刷新 Scene 视图"))
            {
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示重叠警告
            if (generator.CurrentLevel != null)
            {
                var overlaps = generator.GetOverlappingRooms();
                if (overlaps.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox($"检测到 {overlaps.Count} 对房间重叠！在 Scene 视图中拖拽房间调整位置。", MessageType.Error);
                }
            }
        }
        
        #endregion

        #region Scene GUI
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (generator == null || generator.CurrentLevel == null) return;
            
            var level = generator.CurrentLevel;
            if (level.RoomCount == 0) return;
            
            // 更新重叠检测
            UpdateOverlapDetection();
            
            // 绘制房间
            foreach (var room in level.rooms)
            {
                DrawRoom(room);
                DrawRoomHandle(room);
            }
            
            // 绘制走廊
            if (level.corridors != null)
            {
                foreach (var corridor in level.corridors)
                {
                    DrawCorridor(corridor);
                }
            }
            
            // 绘制关卡边界
            DrawLevelBounds(level);
            
            // 绘制怪物类型图例
            DrawSpawnTypeLegend(sceneView);
            
            // 处理输入
            HandleInput();
        }
        
        #endregion

        #region 绘制方法
        
        /// <summary>
        /// 绘制房间
        /// </summary>
        private void DrawRoom(PlacedRoom room)
        {
            if (room == null) return;
            
            var bounds = room.WorldBounds;
            Vector3 center = new Vector3(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f, 0);
            Vector3 size = new Vector3(bounds.width, bounds.height, 0);
            
            // 选择颜色
            Color fillColor = room.roomType switch
            {
                RoomType.Entrance => entranceColor,
                RoomType.Combat => combatColor,
                RoomType.Boss => bossColor,
                _ => Color.gray
            };
            
            // 重叠房间使用红色
            bool isOverlapping = overlappingRoomIds.Contains(room.id);
            Color borderColor = isOverlapping ? overlapColor : fillColor;
            
            // 绘制填充
            Handles.color = fillColor;
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[]
                {
                    new Vector3(bounds.x, bounds.y, 0),
                    new Vector3(bounds.x + bounds.width, bounds.y, 0),
                    new Vector3(bounds.x + bounds.width, bounds.y + bounds.height, 0),
                    new Vector3(bounds.x, bounds.y + bounds.height, 0)
                },
                fillColor,
                borderColor
            );
            
            // 重叠时绘制更粗的边框
            if (isOverlapping)
            {
                Handles.color = overlapColor;
                Handles.DrawWireCube(center, size);
                Handles.DrawWireCube(center, size * 1.02f);
            }
            
            // 绘制房间标签
            DrawRoomLabel(room, center);
            
            // 绘制出入口标记
            DrawDoorMarkers(room);
            
            // 绘制刷怪点
            DrawSpawnPoints(room);
        }
        
        /// <summary>
        /// 绘制房间标签
        /// </summary>
        private void DrawRoomLabel(PlacedRoom room, Vector3 center)
        {
            string typeStr = room.roomType switch
            {
                RoomType.Entrance => "入口",
                RoomType.Combat => "战斗",
                RoomType.Boss => "Boss",
                _ => "未知"
            };
            
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            style.normal.textColor = Color.white;
            
            Handles.Label(center + Vector3.up * 2, $"#{room.id} {typeStr}\n{room.width}x{room.height}", style);
        }
        
        /// <summary>
        /// 绘制出入口标记
        /// </summary>
        private void DrawDoorMarkers(PlacedRoom room)
        {
            // 入口标记（左侧，绿色）
            Vector3 entrancePos = new Vector3(room.WorldEntrance.x, room.WorldEntrance.y + 1, 0);
            Handles.color = new Color(0f, 1f, 0f, 0.8f);
            Handles.DrawSolidDisc(entrancePos, Vector3.forward, 0.8f);
            Handles.Label(entrancePos + Vector3.up, "入", EditorStyles.miniLabel);
            
            // 出口标记（右侧，蓝色）
            Vector3 exitPos = new Vector3(room.WorldExit.x, room.WorldExit.y + 1, 0);
            Handles.color = doorColor;
            Handles.DrawSolidDisc(exitPos, Vector3.forward, 0.8f);
            Handles.Label(exitPos + Vector3.up, "出", EditorStyles.miniLabel);
        }
        
        /// <summary>
        /// 绘制刷怪点
        /// </summary>
        private void DrawSpawnPoints(PlacedRoom room)
        {
            if (room?.roomData?.potentialSpawns == null) return;
            
            // 入口房间不显示刷怪点
            if (room.roomType == RoomType.Entrance) return;
            
            var worldSpawns = room.GetWorldSpawnPoints();
            
            foreach (var spawn in worldSpawns)
            {
                Vector3 pos = new Vector3(spawn.position.x + 0.5f, spawn.position.y + 0.5f, 0);
                
                // 根据敌人类型设置颜色
                Color spawnColor = GetEnemyTypeColor(spawn.enemyType, spawn.type);
                Handles.color = spawnColor;
                Handles.DrawSolidDisc(pos, Vector3.forward, 0.4f);
                
                // 绘制敌人类型标签
                string label = GetEnemyTypeLabel(spawn.enemyType);
                if (!string.IsNullOrEmpty(label))
                {
                    GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = Color.white;
                    style.fontSize = 10;
                    style.alignment = TextAnchor.MiddleCenter;
                    Handles.Label(pos + Vector3.up * 0.6f, label, style);
                }
            }
        }
        
        /// <summary>
        /// 根据敌人类型获取颜色
        /// </summary>
        private Color GetEnemyTypeColor(EnemyType enemyType, SpawnType spawnType)
        {
            switch (enemyType)
            {
                case EnemyType.TriangleSharpshooter:
                    return new Color(1f, 0.5f, 0f, 0.8f); // 锐枪手：橙色
                case EnemyType.TriangleShieldbearer:
                    return new Color(1f, 0.2f, 0.2f, 0.8f); // 盾卫：红色
                case EnemyType.TriangleMoth:
                    return new Color(0.2f, 1f, 0.2f, 0.8f); // 飞蛾：绿色
                case EnemyType.CompositeGuardian:
                    return new Color(1f, 1f, 0f, 0.8f); // Boss：黄色
                default:
                    // 未分配敌人类型时，根据位置类型显示
                    return spawnType == SpawnType.Air 
                        ? new Color(0.8f, 0.2f, 1f, 0.8f)  // 空中：紫色
                        : new Color(0.2f, 0.8f, 0.8f, 0.8f); // 地面：青色
            }
        }
        
        /// <summary>
        /// 获取敌人类型标签
        /// </summary>
        private string GetEnemyTypeLabel(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.TriangleSharpshooter:
                    return "枪";
                case EnemyType.TriangleShieldbearer:
                    return "盾";
                case EnemyType.TriangleMoth:
                    return "蛾";
                case EnemyType.CompositeGuardian:
                    return "Boss";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 绘制怪物类型图例
        /// </summary>
        private void DrawSpawnTypeLegend(SceneView sceneView)
        {
            Handles.BeginGUI();
            
            // 图例背景
            GUILayout.BeginArea(new Rect(10, 10, 200, 200));
            GUI.Box(new Rect(0, 0, 200, 200), "", EditorStyles.helpBox);
            
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            GUILayout.Label("怪物生成点图例", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // 战斗房间怪物（每房间随机1-3种）
            DrawLegendItem("锐枪手 (2-3个)", new Color(1f, 0.5f, 0f, 1f), "枪");
            DrawLegendItem("盾卫 (1-2个)", new Color(1f, 0.2f, 0.2f, 1f), "盾");
            DrawLegendItem("飞蛾 (1-2个)", new Color(0.2f, 1f, 0.2f, 1f), "蛾");
            
            // Boss房间
            DrawLegendItem("复合守卫者", new Color(1f, 1f, 0f, 1f), "Boss");
            
            GUILayout.Space(5);
            GUILayout.Label("未分配:", EditorStyles.miniLabel);
            DrawLegendItem("地面生成点", new Color(0.2f, 0.8f, 0.8f, 1f), "");
            DrawLegendItem("空中生成点", new Color(0.8f, 0.2f, 1f, 1f), "");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制单个图例项
        /// </summary>
        private void DrawLegendItem(string name, Color color, string label)
        {
            GUILayout.BeginHorizontal();
            
            // 颜色方块
            Rect colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            EditorGUI.DrawRect(colorRect, color);
            
            // 标签
            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.Label($"[{label}]", GUILayout.Width(35));
            }
            else
            {
                GUILayout.Space(35);
            }
            
            // 名称
            GUILayout.Label(name, EditorStyles.miniLabel);
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制房间拖拽手柄
        /// </summary>
        private void DrawRoomHandle(PlacedRoom room)
        {
            if (room == null) return;
            
            var bounds = room.WorldBounds;
            Vector3 handlePos = new Vector3(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f, 0);
            
            // 使用 FreeMoveHandle 允许拖拽
            EditorGUI.BeginChangeCheck();
            
            float handleSize = HandleUtility.GetHandleSize(handlePos) * 0.15f;
            Handles.color = Color.white;
            
            Vector3 newPos = Handles.FreeMoveHandle(
                handlePos,
                handleSize,
                Vector3.one,
                Handles.CircleHandleCap
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                // 计算新位置（取整）
                Vector2Int newWorldPos = new Vector2Int(
                    Mathf.RoundToInt(newPos.x - bounds.width / 2f),
                    Mathf.RoundToInt(newPos.y - bounds.height / 2f)
                );
                
                // 记录撤销
                Undo.RecordObject(generator, "Move Room");
                
                // 更新房间位置
                generator.UpdateRoomPosition(room.id, newWorldPos);
                
                // 标记为已修改
                EditorUtility.SetDirty(generator);
                
                // 刷新视图
                SceneView.RepaintAll();
            }
        }
        
        /// <summary>
        /// 绘制L型走廊（3格宽3格高，无重叠）
        /// </summary>
        private void DrawCorridor(CorridorData corridor)
        {
            if (corridor == null) return;
            
            int width = corridor.width;   // 3
            int height = corridor.height; // 3
            
            Handles.color = corridorColor;
            
            if (corridor.isStraight)
            {
                // 直线走廊：从起点-1到终点+1（延伸到房间内部）
                DrawHorizontalSegment(corridor.startPoint.x - 1, corridor.endPoint.x + 1, corridor.startPoint.y, width, height);
            }
            else
            {
                // L型走廊：先水平段，再垂直段，避免重叠
                int cornerX = corridor.cornerPoint.x;
                int startY = corridor.startPoint.y;
                int endY = corridor.endPoint.y;
                bool goingUp = endY > startY;
                
                // 水平段1：起点-1 → 拐角X+width（起点向外延伸1列）
                DrawHorizontalSegment(corridor.startPoint.x - 1, cornerX + width, startY, width, height);
                
                // 水平段2：拐角X → 终点+1（终点向外延伸1列）
                DrawHorizontalSegment(cornerX, corridor.endPoint.x + 1, endY, width, height);
                
                // 垂直段：从水平段1的顶部/底部到水平段2的底部/顶部
                // 避免与水平段重叠
                int verticalStartY, verticalEndY;
                if (goingUp)
                {
                    // 向上：从水平段1顶部开始，到水平段2底部结束
                    verticalStartY = startY + height; // 水平段1顶部
                    verticalEndY = endY;              // 水平段2底部
                }
                else
                {
                    // 向下：从水平段1底部开始，到水平段2顶部结束
                    verticalStartY = endY + height;   // 水平段2顶部
                    verticalEndY = startY;            // 水平段1底部
                }
                
                // 只有当垂直段有实际高度时才绘制
                if (verticalStartY != verticalEndY)
                {
                    DrawVerticalSegment(cornerX, verticalStartY, verticalEndY, width);
                }
            }
            
            // 绘制平台
            if (corridor.platforms != null && corridor.platforms.Count > 0)
            {
                Handles.color = new Color(0.8f, 0.8f, 0.2f, 0.9f); // 黄色表示平台
                foreach (var platformPos in corridor.platforms)
                {
                    DrawPlatform(platformPos, width);
                }
            }
            
            // 绘制走廊标签
            Vector3 labelPos = new Vector3(
                (corridor.startPoint.x + corridor.endPoint.x) / 2f,
                Mathf.Max(corridor.startPoint.y, corridor.endPoint.y) + corridor.height + 2,
                0
            );
            
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = corridorColor;
            
            string platformInfo = corridor.platforms?.Count > 0 ? $" (平台:{corridor.platforms.Count})" : "";
            Handles.Label(labelPos, $"走廊#{corridor.id}{platformInfo}", style);
        }
        
        /// <summary>
        /// 绘制水平段（3格宽3格高）
        /// </summary>
        private void DrawHorizontalSegment(int fromX, int toX, int baseY, int width, int height)
        {
            int minX = Mathf.Min(fromX, toX);
            int maxX = Mathf.Max(fromX, toX);
            
            // 绘制填充区域：宽度从 minX 到 maxX，高度从 baseY 到 baseY + height
            Vector3[] verts = new Vector3[]
            {
                new Vector3(minX, baseY, 0),
                new Vector3(maxX, baseY, 0),
                new Vector3(maxX, baseY + height, 0),
                new Vector3(minX, baseY + height, 0)
            };
            
            Handles.color = corridorColor;
            Handles.DrawSolidRectangleWithOutline(
                verts, 
                new Color(corridorColor.r, corridorColor.g, corridorColor.b, 0.2f), 
                corridorColor
            );
        }
        
        /// <summary>
        /// 绘制垂直段（3格宽）
        /// </summary>
        private void DrawVerticalSegment(int centerX, int fromY, int toY, int width)
        {
            int minY = Mathf.Min(fromY, toY);
            int maxY = Mathf.Max(fromY, toY);
            
            // 绘制填充区域：宽度从 centerX 到 centerX + width，高度从 minY 到 maxY
            Vector3[] verts = new Vector3[]
            {
                new Vector3(centerX, minY, 0),
                new Vector3(centerX + width, minY, 0),
                new Vector3(centerX + width, maxY, 0),
                new Vector3(centerX, maxY, 0)
            };
            
            Handles.color = corridorColor;
            Handles.DrawSolidRectangleWithOutline(
                verts, 
                new Color(corridorColor.r, corridorColor.g, corridorColor.b, 0.2f), 
                corridorColor
            );
        }
        
        /// <summary>
        /// 绘制平台
        /// </summary>
        private void DrawPlatform(Vector2Int pos, int width)
        {
            // 绘制平台线（3格宽）
            Handles.DrawLine(
                new Vector3(pos.x, pos.y, 0),
                new Vector3(pos.x + width, pos.y, 0)
            );
            
            // 绘制平台标记
            Handles.DrawSolidDisc(new Vector3(pos.x + width / 2f, pos.y, 0), Vector3.forward, 0.3f);
        }
        
        /// <summary>
        /// 绘制关卡边界
        /// </summary>
        private void DrawLevelBounds(LevelData level)
        {
            var bounds = level.TotalBounds;
            
            // 扩展边界
            int padding = 5;
            Vector3 min = new Vector3(bounds.x - padding, bounds.y - padding, 0);
            Vector3 max = new Vector3(bounds.xMax + padding, bounds.yMax + padding, 0);
            
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.DrawDottedLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0), 4f);
            Handles.DrawDottedLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0), 4f);
            Handles.DrawDottedLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0), 4f);
            Handles.DrawDottedLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0), 4f);
            
            // 显示关卡尺寸
            Handles.Label(
                new Vector3(bounds.x + bounds.width / 2f, bounds.yMax + padding + 2, 0),
                $"关卡尺寸: {bounds.width} x {bounds.height}",
                EditorStyles.boldLabel
            );
        }
        
        #endregion

        #region 输入处理
        
        private void HandleInput()
        {
            Event e = Event.current;
            
            // 按 C 键重新生成走廊
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && e.control)
            {
                generator.GenerateCorridors();
                SceneView.RepaintAll();
                e.Use();
            }
            
            // 按 G 键生成关卡
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G && e.control)
            {
                generator.GenerateLevel();
                SceneView.RepaintAll();
                e.Use();
            }
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 更新重叠检测
        /// </summary>
        private void UpdateOverlapDetection()
        {
            overlappingRoomIds.Clear();
            
            if (generator?.CurrentLevel == null) return;
            
            var overlaps = generator.GetOverlappingRooms();
            foreach (var (roomA, roomB) in overlaps)
            {
                overlappingRoomIds.Add(roomA);
                overlappingRoomIds.Add(roomB);
            }
        }
        
        #endregion

        #region 静态 Gizmos 绘制
        
        /// <summary>
        /// 在 Scene 视图中绘制 Gizmos（即使未选中对象）
        /// </summary>
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawGizmos(LevelGenerator generator, GizmoType gizmoType)
        {
            if (generator == null || generator.CurrentLevel == null) return;
            
            var level = generator.CurrentLevel;
            if (level.RoomCount == 0) return;
            
            // 简化的 Gizmos 绘制（非选中状态）
            bool isSelected = (gizmoType & GizmoType.Selected) != 0;
            
            if (!isSelected)
            {
                // 非选中状态只绘制简单轮廓
                foreach (var room in level.rooms)
                {
                    var bounds = room.WorldBounds;
                    Vector3 center = new Vector3(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f, 0);
                    Vector3 size = new Vector3(bounds.width, bounds.height, 1);
                    
                    Color color = room.roomType switch
                    {
                        RoomType.Entrance => new Color(0.2f, 0.8f, 0.2f, 0.3f),
                        RoomType.Combat => new Color(0.8f, 0.5f, 0.2f, 0.3f),
                        RoomType.Boss => new Color(0.8f, 0.2f, 0.2f, 0.3f),
                        _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)
                    };
                    
                    Gizmos.color = color;
                    Gizmos.DrawWireCube(center, size);
                }
                
                // 绘制走廊简化轮廓
                if (level.corridors != null)
                {
                    foreach (var corridor in level.corridors)
                    {
                        DrawCorridorGizmo(corridor);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制走廊 Gizmo（简化版）
        /// </summary>
        private static void DrawCorridorGizmo(CorridorData corridor)
        {
            if (corridor == null) return;
            
            Color color = new Color(0.4f, 0.4f, 0.8f, 0.5f);
            Gizmos.color = color;
            
            // 绘制L型走廊路径
            Vector3 start = new Vector3(corridor.startPoint.x, corridor.startPoint.y + 1, 0);
            Vector3 corner = new Vector3(corridor.cornerPoint.x, corridor.startPoint.y + 1, 0);
            Vector3 corner2 = new Vector3(corridor.cornerPoint.x, corridor.endPoint.y + 1, 0);
            Vector3 end = new Vector3(corridor.endPoint.x, corridor.endPoint.y + 1, 0);
            
            if (corridor.isStraight)
            {
                Gizmos.DrawLine(start, end);
            }
            else
            {
                Gizmos.DrawLine(start, corner);
                Gizmos.DrawLine(corner, corner2);
                Gizmos.DrawLine(corner2, end);
            }
            
            // 绘制平台标记
            if (corridor.platforms != null)
            {
                Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.6f);
                foreach (var platform in corridor.platforms)
                {
                    Gizmos.DrawWireSphere(new Vector3(platform.x, platform.y, 0), 0.5f);
                }
            }
        }
        
        #endregion
    }
}
