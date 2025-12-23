using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using CryptaGeometrica.EnemyStateMachine;

namespace CryptaGeometrica.EnemyStateMachine.Editor
{
    /// <summary>
    /// 状态机可视化窗口
    /// 类似动画器的Canvas界面，用于可视化状态机结构
    /// </summary>
    public class StateMachineVisualizerWindow : EditorWindow
    {
        private GenericEnemyController selectedController;
        private Vector2 canvasOffset = Vector2.zero;
        private float zoomLevel = 1f;
        private bool isDragging = false;
        private Vector2 dragStartPos;
        
        // 状态节点相关
        private Dictionary<string, Vector2> statePositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Color> stateColors = new Dictionary<string, Color>
        {
            { "Idle", new Color(0.3f, 0.8f, 0.8f) },
            { "Patrol", new Color(0.3f, 0.8f, 0.3f) },
            { "Chase", new Color(0.8f, 0.3f, 0.3f) },
            { "Attack", new Color(0.8f, 0.8f, 0.3f) },
            { "Hurt", new Color(0.8f, 0.5f, 0.2f) },
            { "Death", new Color(0.4f, 0.4f, 0.4f) }
        };
        
        // 节点样式
        private const float NODE_WIDTH = 120f;
        private const float NODE_HEIGHT = 60f;
        private const float GRID_SIZE = 20f;
        
        [MenuItem("Window/状态机可视化器")]
        public static void ShowWindow()
        {
            var window = GetWindow<StateMachineVisualizerWindow>("状态机可视化器");
            window.minSize = new Vector2(800, 600);
            window.Show();
            window.Focus();
            Debug.Log("[StateMachineVisualizerWindow] 窗口已打开");
        }
        
        private void OnEnable()
        {
            // 监听选择变化
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }
        
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }
        
        private void OnSelectionChanged()
        {
            // 获取当前选中的GenericEnemyController
            var activeObject = Selection.activeGameObject;
            if (activeObject != null)
            {
                selectedController = activeObject.GetComponent<GenericEnemyController>();
                if (selectedController != null)
                {
                    InitializeStatePositions();
                    Repaint();
                }
            }
        }
        
        private void InitializeStatePositions()
        {
            if (selectedController?.enabledStates == null) return;
            
            statePositions.Clear();
            
            // 自动布局状态节点
            var enabledStates = selectedController.enabledStates.Where(s => s.enabled).ToList();
            int columns = Mathf.CeilToInt(Mathf.Sqrt(enabledStates.Count));
            
            for (int i = 0; i < enabledStates.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                
                Vector2 position = new Vector2(
                    200 + col * (NODE_WIDTH + 50),
                    150 + row * (NODE_HEIGHT + 50)
                );
                
                statePositions[enabledStates[i].stateName] = position;
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
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
            
            // 显示当前选中的控制器
            string controllerName = selectedController != null ? selectedController.name : "未选中";
            EditorGUILayout.LabelField($"🎯 当前控制器: {controllerName}", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            // 缩放控制
            EditorGUILayout.LabelField("缩放:", GUILayout.Width(40));
            float newZoom = EditorGUILayout.Slider(zoomLevel, 0.5f, 2f, GUILayout.Width(100));
            if (newZoom != zoomLevel)
            {
                zoomLevel = newZoom;
                Repaint();
            }
            
            // 重置视图按钮
            if (GUILayout.Button("🔄 重置视图", EditorStyles.toolbarButton))
            {
                canvasOffset = Vector2.zero;
                zoomLevel = 1f;
                InitializeStatePositions();
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoSelectionMessage()
        {
            var rect = new Rect(0, EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);
            
            GUIStyle messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 16,
                normal = { textColor = Color.gray }
            };
            
            GUI.Label(rect, "请在Hierarchy中选择一个带有GenericEnemyController的GameObject", messageStyle);
        }
        
        private void DrawCanvas()
        {
            var canvasRect = new Rect(0, EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);
            
            // 处理画布事件
            HandleCanvasEvents(canvasRect);
            
            // 开始画布绘制
            GUI.BeginGroup(canvasRect);
            
            // 绘制网格背景
            DrawGrid(canvasRect);
            
            // 应用缩放和偏移
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(canvasOffset, Quaternion.identity, Vector3.one * zoomLevel);
            
            // 绘制状态连线
            DrawStateConnections();
            
            // 绘制状态节点
            DrawStateNodes();
            
            // 绘制状态信息面板
            DrawStateInfoPanel();
            
            // 恢复矩阵
            GUI.matrix = oldMatrix;
            
            GUI.EndGroup();
        }
        
        private void HandleCanvasEvents(Rect canvasRect)
        {
            Event e = Event.current;
            
            if (canvasRect.Contains(e.mousePosition))
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 1) // 右键拖拽
                        {
                            isDragging = true;
                            dragStartPos = e.mousePosition;
                            e.Use();
                        }
                        break;
                        
                    case EventType.MouseDrag:
                        if (isDragging && e.button == 1)
                        {
                            Vector2 delta = e.mousePosition - dragStartPos;
                            canvasOffset += delta;
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
                        float zoomDelta = -e.delta.y * 0.1f;
                        zoomLevel = Mathf.Clamp(zoomLevel + zoomDelta, 0.5f, 2f);
                        Repaint();
                        e.Use();
                        break;
                }
            }
        }
        
        private void DrawGrid(Rect canvasRect)
        {
            // 绘制网格背景
            Handles.BeginGUI();
            
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            
            // 垂直线
            for (float x = canvasOffset.x % GRID_SIZE; x < canvasRect.width; x += GRID_SIZE)
            {
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvasRect.height));
            }
            
            // 水平线
            for (float y = canvasOffset.y % GRID_SIZE; y < canvasRect.height; y += GRID_SIZE)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(canvasRect.width, y));
            }
            
            Handles.EndGUI();
        }
        
        private void DrawStateConnections()
        {
            if (selectedController?.enabledStates == null) return;
            
            Handles.BeginGUI();
            
            var enabledStates = selectedController.enabledStates.Where(s => s.enabled).ToList();
            
            // 绘制状态之间的连线（简化版本）
            for (int i = 0; i < enabledStates.Count - 1; i++)
            {
                string fromState = enabledStates[i].stateName;
                string toState = enabledStates[i + 1].stateName;
                
                if (statePositions.ContainsKey(fromState) && statePositions.ContainsKey(toState))
                {
                    Vector2 fromPos = statePositions[fromState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
                    Vector2 toPos = statePositions[toState] + new Vector2(NODE_WIDTH / 2, NODE_HEIGHT / 2);
                    
                    Handles.color = Color.gray;
                    Handles.DrawLine(fromPos, toPos);
                    
                    // 绘制箭头
                    Vector2 direction = (toPos - fromPos).normalized;
                    Vector2 arrowHead = toPos - direction * 20f;
                    Vector2 arrowLeft = arrowHead + new Vector2(-direction.y, direction.x) * 8f;
                    Vector2 arrowRight = arrowHead + new Vector2(direction.y, -direction.x) * 8f;
                    
                    Handles.DrawLine(toPos, arrowLeft);
                    Handles.DrawLine(toPos, arrowRight);
                }
            }
            
            Handles.EndGUI();
        }
        
        private void DrawStateNodes()
        {
            if (selectedController?.enabledStates == null) return;
            
            var enabledStates = selectedController.enabledStates.Where(s => s.enabled).ToList();
            string currentState = Application.isPlaying && selectedController.StateMachine != null ? 
                selectedController.StateMachine.CurrentStateName : null;
            
            foreach (var state in enabledStates)
            {
                if (!statePositions.ContainsKey(state.stateName)) continue;
                
                Vector2 position = statePositions[state.stateName];
                Rect nodeRect = new Rect(position.x, position.y, NODE_WIDTH, NODE_HEIGHT);
                
                // 确定节点颜色
                Color nodeColor = stateColors.ContainsKey(state.stateName) ? 
                    stateColors[state.stateName] : Color.gray;
                
                bool isCurrentState = state.stateName == currentState;
                
                // 绘制节点背景
                Color bgColor = isCurrentState ? nodeColor : nodeColor * 0.7f;
                EditorGUI.DrawRect(nodeRect, bgColor);
                
                // 绘制节点边框
                Color borderColor = isCurrentState ? Color.white : Color.black;
                GUI.Box(nodeRect, "", EditorStyles.helpBox);
                
                // 绘制状态名称
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    normal = { textColor = isCurrentState ? Color.black : Color.white }
                };
                
                string displayText = isCurrentState ? $"● {state.stateName}" : state.stateName;
                GUI.Label(nodeRect, displayText, labelStyle);
                
                // 绘制状态描述
                if (!string.IsNullOrEmpty(state.description))
                {
                    Rect descRect = new Rect(nodeRect.x, nodeRect.yMax + 2, NODE_WIDTH, 20);
                    GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        normal = { textColor = Color.gray }
                    };
                    GUI.Label(descRect, state.description, descStyle);
                }
                
                // 处理节点点击
                if (Event.current.type == EventType.MouseDown && nodeRect.Contains(Event.current.mousePosition))
                {
                    if (Application.isPlaying && selectedController.StateMachine != null)
                    {
                        selectedController.StateMachine.TransitionTo(state.stateName);
                        Debug.Log($"[状态机可视化器] 切换到状态: {state.stateName}");
                    }
                    Event.current.Use();
                }
            }
        }
        
        private void DrawStateInfoPanel()
        {
            if (!Application.isPlaying || selectedController?.StateMachine == null) return;
            
            // 绘制状态信息面板
            Rect panelRect = new Rect(10, 10, 200, 120);
            EditorGUI.DrawRect(panelRect, new Color(0, 0, 0, 0.8f));
            
            GUILayout.BeginArea(panelRect);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 14
            };
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.cyan },
                fontSize = 11
            };
            
            GUILayout.Label("📊 运行时信息", titleStyle);
            GUILayout.Space(5);
            
            string currentState = selectedController.StateMachine.CurrentStateName ?? "未知";
            GUILayout.Label($"当前状态: {currentState}", infoStyle);
            GUILayout.Label($"状态数量: {selectedController.StateMachine.StateCount}", infoStyle);
            GUILayout.Label($"生命值: {selectedController.CurrentHealth:F0}", infoStyle);
            GUILayout.Label($"存活: {selectedController.IsAlive}", infoStyle);
            
            GUILayout.EndArea();
        }
        
        private void Update()
        {
            // 运行时自动刷新
            if (Application.isPlaying && selectedController != null)
            {
                Repaint();
            }
        }
    }
}
