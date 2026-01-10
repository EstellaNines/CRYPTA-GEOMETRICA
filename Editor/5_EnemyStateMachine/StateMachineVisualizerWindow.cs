using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using CryptaGeometrica.EnemyStateMachine;

namespace CryptaGeometrica.EnemyStateMachine.Editor
{
    /// <summary>
    /// 状态机可视化窗口 / State Machine Visualizer Window
    /// 类似动画器的Canvas界面，用于可视化状态机结构 / Canvas-style interface similar to Animator for visualizing state machine structure
    /// 支持状态：Idle, Patrol, Chase, Attack, Hurt, Death / Supported states: Idle, Patrol, Chase, Attack, Hurt, Death
    /// </summary>
    public class StateMachineVisualizerWindow : EditorWindow
    {
        #region 字段
        
        private GenericEnemyController selectedController;
        private Vector2 canvasOffset = Vector2.zero;
        private float zoomLevel = 1f;
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private string selectedStateName = null;
        
        // 状态节点位置
        private Dictionary<string, Vector2> statePositions = new Dictionary<string, Vector2>();
        
        // 状态颜色配置
        private static readonly Dictionary<string, Color> stateColors = new Dictionary<string, Color>
        {
            { "Idle", new Color(0.3f, 0.8f, 0.8f) },      // 青色
            { "Patrol", new Color(0.3f, 0.8f, 0.3f) },    // 绿色
            { "Chase", new Color(0.3f, 0.5f, 1f) },       // 蓝色
            { "Attack", new Color(0.4f, 0.6f, 1f) },      // 浅蓝色
            { "Hurt", new Color(1f, 0.3f, 0.3f) },        // 红色
            { "Death", new Color(0.2f, 0.2f, 0.2f) }      // 黑色
        };
        
        // 状态图标
        private static readonly Dictionary<string, string> stateIcons = new Dictionary<string, string>
        {
            { "Idle", "🧍" },
            { "Patrol", "🚶" },
            { "Chase", "🏃" },
            { "Attack", "⚔️" },
            { "Hurt", "💥" },
            { "Death", "💀" }
        };

        // 状态转换关系定义
        private static readonly Dictionary<string, string[]> stateTransitions = new Dictionary<string, string[]>
        {
            { "Idle", new[] { "Patrol", "Chase" } },
            { "Patrol", new[] { "Idle", "Chase" } },
            { "Chase", new[] { "Attack", "Patrol" } },
            { "Attack", new[] { "Chase", "Patrol" } },
            { "Hurt", new[] { "Chase", "Patrol" } },
            { "Death", new string[0] }  // 死亡状态无转出
        };
        
        // 转换标签
        private static readonly Dictionary<string, Dictionary<string, string>> transitionLabels = new Dictionary<string, Dictionary<string, string>>
        {
            { "Idle", new Dictionary<string, string> { { "Patrol", "超时" }, { "Chase", "发现玩家" } } },
            { "Patrol", new Dictionary<string, string> { { "Idle", "巡逻结束" }, { "Chase", "发现玩家" } } },
            { "Chase", new Dictionary<string, string> { { "Attack", "进入攻击范围" }, { "Patrol", "目标丢失" } } },
            { "Attack", new Dictionary<string, string> { { "Chase", "离开攻击范围" }, { "Patrol", "离开追击范围" } } },
            { "Hurt", new Dictionary<string, string> { { "Chase", "玩家在范围内" }, { "Patrol", "玩家不在范围" } } }
        };
        
        // 节点样式常量
        private const float NODE_WIDTH = 140f;
        private const float NODE_HEIGHT = 70f;
        private const float GRID_SIZE = 20f;
        
        #endregion
        
        #region 菜单入口
        
        [MenuItem("Window/敌人状态机/状态机可视化器 (State Machine Visualizer) %#V")]
        public static void ShowWindow()
        {
            var window = GetWindow<StateMachineVisualizerWindow>("状态机可视化器 (State Machine Visualizer)");
            window.minSize = new Vector2(900, 700);
            window.Show();
            window.Focus();
        }
        
        #endregion
        
        #region 生命周期
        
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            OnSelectionChanged();
        }
        
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
        
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            Repaint();
        }
        
        private void OnSelectionChanged()
        {
            var activeObject = Selection.activeGameObject;
            if (activeObject != null)
            {
                var controller = activeObject.GetComponent<GenericEnemyController>();
                if (controller != null)
                {
                    selectedController = controller;
                    InitializeStatePositions();
                    Repaint();
                }
            }
        }

        private void Update()
        {
            if (Application.isPlaying && selectedController != null)
            {
                Repaint();
            }
        }
        
        #endregion
        
        #region 状态位置初始化
        
        private void InitializeStatePositions()
        {
            if (selectedController?.enabledStates == null) return;
            
            statePositions.Clear();
            
            // 使用预定义的布局位置（状态机流程图布局）
            var defaultPositions = new Dictionary<string, Vector2>
            {
                { "Idle", new Vector2(100, 200) },
                { "Patrol", new Vector2(300, 200) },
                { "Chase", new Vector2(500, 200) },
                { "Attack", new Vector2(700, 200) },
                { "Hurt", new Vector2(400, 50) },
                { "Death", new Vector2(400, 350) }
            };
            
            var enabledStates = selectedController.enabledStates.Where(s => s.enabled).ToList();
            
            foreach (var state in enabledStates)
            {
                if (defaultPositions.ContainsKey(state.stateName))
                {
                    statePositions[state.stateName] = defaultPositions[state.stateName];
                }
                else
                {
                    // 未知状态放在右侧
                    int index = enabledStates.IndexOf(state);
                    statePositions[state.stateName] = new Vector2(800 + (index % 2) * 150, 100 + (index / 2) * 100);
                }
            }
        }
        
        #endregion
        
        #region GUI绘制
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawLegend();
            
            if (selectedController == null)
            {
                DrawNoSelectionMessage();
                return;
            }
            
            DrawCanvas();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 控制器信息
            string controllerName = selectedController != null ? selectedController.name : "未选中";
            string enemyType = selectedController != null ? selectedController.GetType().Name : "";
            EditorGUILayout.LabelField($"🎯 {controllerName}", EditorStyles.toolbarButton, GUILayout.Width(150));
            
            if (Application.isPlaying && selectedController?.StateMachine != null)
            {
                string currentState = selectedController.StateMachine.CurrentStateName ?? "无";
                string icon = stateIcons.ContainsKey(currentState) ? stateIcons[currentState] : "❓";
                EditorGUILayout.LabelField($"{icon} 当前: {currentState}", EditorStyles.toolbarButton, GUILayout.Width(120));
            }
            
            GUILayout.FlexibleSpace();
            
            // 缩放控制
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            zoomLevel = EditorGUILayout.Slider(zoomLevel, 0.5f, 2f, GUILayout.Width(100));
            
            // 重置按钮
            if (GUILayout.Button("🔄 重置", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                canvasOffset = Vector2.zero;
                zoomLevel = 1f;
                InitializeStatePositions();
            }
            
            // 自动布局按钮
            if (GUILayout.Button("📐 自动布局", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                InitializeStatePositions();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("图例 (Legend):", GUILayout.Width(90));
            
            foreach (var kvp in stateColors)
            {
                string icon = stateIcons.ContainsKey(kvp.Key) ? stateIcons[kvp.Key] : "";
                
                // 绘制颜色方块
                Rect colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
                EditorGUI.DrawRect(colorRect, kvp.Value);
                
                EditorGUILayout.LabelField($"{icon}{kvp.Key}", GUILayout.Width(70));
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("💡 Right-drag canvas | Scroll to zoom | Left-click to switch state (runtime)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoSelectionMessage()
        {
            var rect = new Rect(0, 50, position.width, position.height - 50);
            
            GUIStyle messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 16,
                normal = { textColor = Color.gray }
            };
            
            GUI.Label(rect, "Please select a GameObject with GenericEnemyController in Hierarchy\n请在Hierarchy中选择一个带有GenericEnemyController的GameObject\n\nTip: Press Ctrl+Shift+V to open this window\n提示: 可以使用快捷键 Ctrl+Shift+V 打开此窗口", messageStyle);
        }
        
        private void DrawCanvas()
        {
            var canvasRect = new Rect(0, 40, position.width, position.height - 40);
            
            HandleCanvasEvents(canvasRect);
            
            GUI.BeginGroup(canvasRect);
            
            // 绘制网格
            DrawGrid(canvasRect);
            
            // 应用变换
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(canvasOffset, Quaternion.identity, Vector3.one * zoomLevel);
            
            // 绘制连线
            DrawStateConnections();
            
            // 绘制特殊连线（Hurt/Death）
            DrawSpecialConnections();
            
            // 绘制节点
            DrawStateNodes();
            
            // 绘制信息面板
            DrawRuntimeInfoPanel();
            
            GUI.matrix = oldMatrix;
            GUI.EndGroup();
        }
        
        #endregion
        
        #region 画布事件处理
        
        private void HandleCanvasEvents(Rect canvasRect)
        {
            Event e = Event.current;
            
            if (!canvasRect.Contains(e.mousePosition)) return;
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 1)
                    {
                        isDragging = true;
                        dragStartPos = e.mousePosition;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (isDragging && e.button == 1)
                    {
                        canvasOffset += e.mousePosition - dragStartPos;
                        dragStartPos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (e.button == 1)
                    {
                        isDragging = false;
                        e.Use();
                    }
                    break;
                    
                case EventType.ScrollWheel:
                    zoomLevel = Mathf.Clamp(zoomLevel - e.delta.y * 0.05f, 0.5f, 2f);
                    Repaint();
                    e.Use();
                    break;
            }
        }
        
        #endregion

        #region 绘制方法
        
        private void DrawGrid(Rect canvasRect)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
            
            float gridStep = GRID_SIZE * zoomLevel;
            
            for (float x = canvasOffset.x % gridStep; x < canvasRect.width; x += gridStep)
            {
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvasRect.height));
            }
            
            for (float y = canvasOffset.y % gridStep; y < canvasRect.height; y += gridStep)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(canvasRect.width, y));
            }
            
            Handles.EndGUI();
        }
        
        private void DrawStateConnections()
        {
            if (selectedController?.enabledStates == null) return;
            
            Handles.BeginGUI();
            
            var enabledStateNames = selectedController.enabledStates
                .Where(s => s.enabled)
                .Select(s => s.stateName)
                .ToHashSet();
            
            foreach (var fromState in stateTransitions.Keys)
            {
                if (!enabledStateNames.Contains(fromState)) continue;
                if (!statePositions.ContainsKey(fromState)) continue;
                
                foreach (var toState in stateTransitions[fromState])
                {
                    if (!enabledStateNames.Contains(toState)) continue;
                    if (!statePositions.ContainsKey(toState)) continue;
                    
                    DrawConnection(fromState, toState, Color.gray);
                }
            }
            
            Handles.EndGUI();
        }
        
        private void DrawSpecialConnections()
        {
            if (selectedController?.enabledStates == null) return;
            
            Handles.BeginGUI();
            
            var enabledStateNames = selectedController.enabledStates
                .Where(s => s.enabled)
                .Select(s => s.stateName)
                .ToHashSet();
            
            // Hurt状态的特殊连线（从所有状态可进入）
            if (enabledStateNames.Contains("Hurt") && statePositions.ContainsKey("Hurt"))
            {
                foreach (var state in new[] { "Idle", "Patrol", "Chase", "Attack" })
                {
                    if (enabledStateNames.Contains(state) && statePositions.ContainsKey(state))
                    {
                        DrawDashedConnection(state, "Hurt", new Color(1f, 0.5f, 0.5f, 0.5f), "受伤");
                    }
                }
            }
            
            // Death状态的特殊连线（从所有状态可进入）
            if (enabledStateNames.Contains("Death") && statePositions.ContainsKey("Death"))
            {
                foreach (var state in new[] { "Idle", "Patrol", "Chase", "Attack", "Hurt" })
                {
                    if (enabledStateNames.Contains(state) && statePositions.ContainsKey(state))
                    {
                        DrawDashedConnection(state, "Death", new Color(0.3f, 0.3f, 0.3f, 0.5f), "死亡");
                    }
                }
            }
            
            Handles.EndGUI();
        }
        
        private void DrawConnection(string fromState, string toState, Color color)
        {
            Vector2 fromPos = statePositions[fromState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
            Vector2 toPos = statePositions[toState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
            
            // 计算连线端点（避免穿过节点）
            Vector2 direction = (toPos - fromPos).normalized;
            fromPos += direction * (NODE_WIDTH / 2 + 5);
            toPos -= direction * (NODE_WIDTH / 2 + 5);
            
            Handles.color = color;
            Handles.DrawLine(fromPos, toPos);
            
            // 绘制箭头
            DrawArrowHead(toPos, direction, color);
            
            // 绘制标签
            if (transitionLabels.ContainsKey(fromState) && transitionLabels[fromState].ContainsKey(toState))
            {
                string label = transitionLabels[fromState][toState];
                Vector2 midPoint = (fromPos + toPos) / 2;
                DrawConnectionLabel(midPoint, label);
            }
        }

        private void DrawDashedConnection(string fromState, string toState, Color color, string label)
        {
            Vector2 fromPos = statePositions[fromState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
            Vector2 toPos = statePositions[toState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
            
            Vector2 direction = (toPos - fromPos).normalized;
            fromPos += direction * (NODE_WIDTH / 2 + 5);
            toPos -= direction * (NODE_WIDTH / 2 + 5);
            
            // 绘制虚线
            Handles.color = color;
            float dashLength = 8f;
            float gapLength = 4f;
            float totalLength = Vector2.Distance(fromPos, toPos);
            float currentLength = 0f;
            
            while (currentLength < totalLength)
            {
                Vector2 start = fromPos + direction * currentLength;
                float endLength = Mathf.Min(currentLength + dashLength, totalLength);
                Vector2 end = fromPos + direction * endLength;
                
                Handles.DrawLine(start, end);
                currentLength += dashLength + gapLength;
            }
            
            // 绘制箭头
            DrawArrowHead(toPos, direction, color);
        }
        
        private void DrawArrowHead(Vector2 tip, Vector2 direction, Color color)
        {
            float arrowSize = 10f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            Vector2 arrowLeft = tip - direction * arrowSize + perpendicular * (arrowSize / 2);
            Vector2 arrowRight = tip - direction * arrowSize - perpendicular * (arrowSize / 2);
            
            Handles.color = color;
            Handles.DrawLine(tip, arrowLeft);
            Handles.DrawLine(tip, arrowRight);
        }
        
        private void DrawConnectionLabel(Vector2 position, string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            
            Vector2 size = style.CalcSize(new GUIContent(label));
            Rect labelRect = new Rect(position.x - size.x / 2, position.y - size.y / 2 - 8, size.x + 4, size.y);
            
            EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, 0.6f));
            GUI.Label(labelRect, label, style);
        }
        
        private void DrawStateNodes()
        {
            if (selectedController?.enabledStates == null) return;
            
            var enabledStates = selectedController.enabledStates.Where(s => s.enabled).ToList();
            string currentState = Application.isPlaying && selectedController.StateMachine != null
                ? selectedController.StateMachine.CurrentStateName
                : null;
            
            foreach (var state in enabledStates)
            {
                if (!statePositions.ContainsKey(state.stateName)) continue;
                
                Vector2 pos = statePositions[state.stateName];
                Rect nodeRect = new Rect(pos.x, pos.y, NODE_WIDTH, NODE_HEIGHT);
                
                bool isCurrentState = state.stateName == currentState;
                bool isSelected = state.stateName == selectedStateName;
                
                // 获取颜色
                Color nodeColor = stateColors.ContainsKey(state.stateName)
                    ? stateColors[state.stateName]
                    : Color.gray;
                
                // 绘制阴影
                Rect shadowRect = new Rect(nodeRect.x + 3, nodeRect.y + 3, nodeRect.width, nodeRect.height);
                EditorGUI.DrawRect(shadowRect, new Color(0, 0, 0, 0.3f));
                
                // 绘制节点背景
                Color bgColor = isCurrentState ? nodeColor : nodeColor * 0.6f;
                EditorGUI.DrawRect(nodeRect, bgColor);
                
                // 绘制边框
                Color borderColor = isCurrentState ? Color.white : (isSelected ? Color.yellow : new Color(0.3f, 0.3f, 0.3f));
                DrawNodeBorder(nodeRect, borderColor, isCurrentState ? 3f : 1f);
                
                // 绘制图标和名称
                string icon = stateIcons.ContainsKey(state.stateName) ? stateIcons[state.stateName] : "❓";
                string displayText = $"{icon} {state.stateName}";
                
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    normal = { textColor = isCurrentState ? Color.black : Color.white }
                };
                
                Rect nameRect = new Rect(nodeRect.x, nodeRect.y + 5, nodeRect.width, 25);
                GUI.Label(nameRect, displayText, nameStyle);
                
                // 绘制状态指示器
                if (isCurrentState)
                {
                    Rect indicatorRect = new Rect(nodeRect.x + 5, nodeRect.y + 5, 10, 10);
                    EditorGUI.DrawRect(indicatorRect, Color.green);
                }
                
                // 绘制描述
                if (!string.IsNullOrEmpty(state.description))
                {
                    GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        wordWrap = true,
                        normal = { textColor = isCurrentState ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.8f, 0.8f, 0.8f) }
                    };
                    
                    Rect descRect = new Rect(nodeRect.x + 5, nodeRect.y + 30, nodeRect.width - 10, 35);
                    GUI.Label(descRect, state.description, descStyle);
                }
                
                // 处理点击
                HandleNodeClick(nodeRect, state.stateName);
            }
        }

        private void DrawNodeBorder(Rect rect, Color color, float thickness)
        {
            // 上边
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            // 下边
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            // 左边
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            // 右边
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        
        private void HandleNodeClick(Rect nodeRect, string stateName)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                selectedStateName = stateName;
                
                if (e.button == 0 && Application.isPlaying && selectedController?.StateMachine != null)
                {
                    // 左键点击：运行时切换状态
                    if (selectedController.StateMachine.HasState(stateName))
                    {
                        selectedController.StateMachine.ForceTransitionTo(stateName);
                        Debug.Log($"[状态机可视化器] 强制切换到状态: {stateName}");
                    }
                }
                
                e.Use();
                Repaint();
            }
        }
        
        private void DrawRuntimeInfoPanel()
        {
            if (!Application.isPlaying || selectedController?.StateMachine == null) return;
            
            Rect panelRect = new Rect(10, 10, 220, 160);
            
            // 背景
            EditorGUI.DrawRect(panelRect, new Color(0, 0, 0, 0.85f));
            DrawNodeBorder(panelRect, new Color(0.3f, 0.6f, 1f), 2f);
            
            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 20, panelRect.height - 20));
            
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.3f, 0.8f, 1f) }
            };
            GUILayout.Label("📊 Runtime Info", titleStyle);
            
            GUILayout.Space(8);
            
            // 信息样式
            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            
            GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.cyan }
            };
            
            // 当前状态 / Current State
            string currentState = selectedController.StateMachine.CurrentStateName ?? "None";
            string stateIcon = stateIcons.ContainsKey(currentState) ? stateIcons[currentState] : "❓";
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("State:", infoStyle, GUILayout.Width(70));
            GUILayout.Label($"{stateIcon} {currentState}", valueStyle);
            EditorGUILayout.EndHorizontal();
            
            // 状态数量 / State Count
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Count:", infoStyle, GUILayout.Width(70));
            GUILayout.Label($"{selectedController.StateMachine.StateCount}", valueStyle);
            EditorGUILayout.EndHorizontal();
            
            // 生命值 / Health
            float healthPercent = selectedController.HealthPercentage * 100f;
            Color healthColor = healthPercent > 50 ? Color.green : (healthPercent > 25 ? Color.yellow : Color.red);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Health:", infoStyle, GUILayout.Width(70));
            GUIStyle healthStyle = new GUIStyle(valueStyle) { normal = { textColor = healthColor } };
            GUILayout.Label($"{selectedController.CurrentHealth:F0} ({healthPercent:F0}%)", healthStyle);
            EditorGUILayout.EndHorizontal();
            
            // 存活状态 / Alive
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Alive:", infoStyle, GUILayout.Width(70));
            string aliveText = selectedController.IsAlive ? "✅ Yes" : "❌ No";
            Color aliveColor = selectedController.IsAlive ? Color.green : Color.red;
            GUIStyle aliveStyle = new GUIStyle(valueStyle) { normal = { textColor = aliveColor } };
            GUILayout.Label(aliveText, aliveStyle);
            EditorGUILayout.EndHorizontal();
            
            // 可行动 / Can Act
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Can Act:", infoStyle, GUILayout.Width(70));
            string canActText = selectedController.CanAct ? "✅ Yes" : "❌ No";
            GUILayout.Label(canActText, valueStyle);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
