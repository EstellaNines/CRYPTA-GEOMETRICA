using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 自定义点阵图案编辑器窗口
/// 提供可视化格子点击编辑功能
/// </summary>
public class DotMatrixPatternEditor : EditorWindow
{
    private DotMatrixPattern currentPattern;
    private Vector2 scrollPosition;
    private float cellSize = 20f;
    private bool isDragging = false;
    private bool dragState = false;

    // 临时编辑数据
    private int tempRows = 20;
    private int tempCols = 40;

    [MenuItem("自制工具/点阵效果/点阵图案编辑器")]
    public static void ShowWindow()
    {
        var window = GetWindow<DotMatrixPatternEditor>("点阵图案编辑器");
        window.minSize = new Vector2(600, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // 标题
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("点阵图案编辑器", titleStyle);

        EditorGUILayout.Space(10);
        DrawHorizontalLine();

        // 资源管理区
        DrawAssetManagement();

        if (currentPattern != null)
        {
            EditorGUILayout.Space(10);
            DrawHorizontalLine();

            // 编辑工具栏
            DrawToolbar();

            EditorGUILayout.Space(10);
            DrawHorizontalLine();

            // 绘制网格
            DrawGrid();
        }
        else
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("请创建或加载一个点阵图案资源", MessageType.Info);
        }
    }

    private void DrawAssetManagement()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("资源管理", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 当前资源
        EditorGUI.BeginChangeCheck();
        currentPattern = (DotMatrixPattern)EditorGUILayout.ObjectField(
            "当前图案", currentPattern, typeof(DotMatrixPattern), false);
        if (EditorGUI.EndChangeCheck() && currentPattern != null)
        {
            tempRows = currentPattern.rows;
            tempCols = currentPattern.cols;
        }

        // 创建新资源
        if (GUILayout.Button("新建图案", GUILayout.Width(80)))
        {
            CreateNewPattern();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("编辑工具", EditorStyles.boldLabel);

        // 图案名称
        currentPattern.patternName = EditorGUILayout.TextField("图案名称", currentPattern.patternName);

        EditorGUILayout.BeginHorizontal();

        // 网格尺寸
        EditorGUI.BeginChangeCheck();
        tempRows = EditorGUILayout.IntSlider("行数", tempRows, 5, 100);
        if (EditorGUI.EndChangeCheck())
        {
            if (tempRows != currentPattern.rows)
            {
                Undo.RecordObject(currentPattern, "调整图案行数");
                currentPattern.ResizePattern(tempRows, currentPattern.cols);
                EditorUtility.SetDirty(currentPattern);
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        tempCols = EditorGUILayout.IntSlider("列数", tempCols, 5, 200);
        if (EditorGUI.EndChangeCheck())
        {
            if (tempCols != currentPattern.cols)
            {
                Undo.RecordObject(currentPattern, "调整图案列数");
                currentPattern.ResizePattern(currentPattern.rows, tempCols);
                EditorUtility.SetDirty(currentPattern);
            }
        }

        EditorGUILayout.EndHorizontal();

        // 格子大小
        cellSize = EditorGUILayout.Slider("格子大小", cellSize, 10f, 40f);

        EditorGUILayout.BeginHorizontal();

        // 操作按钮
        if (GUILayout.Button("清空"))
        {
            Undo.RecordObject(currentPattern, "清空图案");
            currentPattern.Clear();
            EditorUtility.SetDirty(currentPattern);
        }

        if (GUILayout.Button("反转"))
        {
            Undo.RecordObject(currentPattern, "反转图案");
            currentPattern.Invert();
            EditorUtility.SetDirty(currentPattern);
        }

        if (GUILayout.Button("保存"))
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("保存成功", "图案数据已保存", "确定");
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawGrid()
    {
        if (currentPattern == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"图案预览 ({currentPattern.rows} x {currentPattern.cols})", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        Event e = Event.current;
        Rect gridRect = GUILayoutUtility.GetRect(
            currentPattern.cols * cellSize + 10,
            currentPattern.rows * cellSize + 10);

        // 绘制背景
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

        // 绘制网格
        for (int r = 0; r < currentPattern.rows; r++)
        {
            for (int c = 0; c < currentPattern.cols; c++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + 5 + c * cellSize,
                    gridRect.y + 5 + r * cellSize,
                    cellSize - 2,
                    cellSize - 2);

                // 绘制格子
                bool isOn = currentPattern.GetDot(r, c);
                Color cellColor = isOn ? new Color(0f, 0.83f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                EditorGUI.DrawRect(cellRect, cellColor);

                // 处理点击
                if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
                {
                    isDragging = true;
                    dragState = !isOn;
                    Undo.RecordObject(currentPattern, "编辑点阵");
                    currentPattern.SetDot(r, c, dragState);
                    EditorUtility.SetDirty(currentPattern);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDrag && isDragging && cellRect.Contains(e.mousePosition))
                {
                    if (currentPattern.GetDot(r, c) != dragState)
                    {
                        currentPattern.SetDot(r, c, dragState);
                        EditorUtility.SetDirty(currentPattern);
                        e.Use();
                        Repaint();
                    }
                }
            }
        }

        if (e.type == EventType.MouseUp)
        {
            isDragging = false;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewPattern()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建新点阵图案",
            "NewDotMatrixPattern",
            "asset",
            "请选择保存位置");

        if (!string.IsNullOrEmpty(path))
        {
            DotMatrixPattern newPattern = CreateInstance<DotMatrixPattern>();
            newPattern.rows = 20;
            newPattern.cols = 40;
            newPattern.patternName = Path.GetFileNameWithoutExtension(path);
            newPattern.InitializePattern();

            AssetDatabase.CreateAsset(newPattern, path);
            AssetDatabase.SaveAssets();

            currentPattern = newPattern;
            tempRows = currentPattern.rows;
            tempCols = currentPattern.cols;

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newPattern;
        }
    }

    private void DrawHorizontalLine()
    {
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 1));
    }
}
