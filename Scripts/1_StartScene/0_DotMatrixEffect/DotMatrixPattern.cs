using UnityEngine;

/// <summary>
/// 自定义点阵图案数据资源
/// 用于存储和序列化可编辑的点阵图案
/// </summary>
[CreateAssetMenu(fileName = "NewDotMatrixPattern", menuName = "自制工具/点阵效果/自定义点阵图案", order = 1)]
public class DotMatrixPattern : ScriptableObject
{
    [Header("图案设置")]
    [Tooltip("图案名称")]
    public string patternName = "新图案";

    [Tooltip("网格行数")]
    [Range(5, 100)]
    public int rows = 20;

    [Tooltip("网格列数")]
    [Range(5, 200)]
    public int cols = 40;

    [Header("图案数据")]
    [Tooltip("序列化的图案数据（行优先存储）")]
    [SerializeField]
    private bool[] patternData;

    /// <summary>
    /// 初始化或重置图案数据
    /// </summary>
    public void InitializePattern()
    {
        patternData = new bool[rows * cols];
    }

    /// <summary>
    /// 获取指定位置的点状态
    /// </summary>
    public bool GetDot(int row, int col)
    {
        if (patternData == null || patternData.Length != rows * cols)
            InitializePattern();

        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return false;

        return patternData[row * cols + col];
    }

    /// <summary>
    /// 设置指定位置的点状态
    /// </summary>
    public void SetDot(int row, int col, bool state)
    {
        if (patternData == null || patternData.Length != rows * cols)
            InitializePattern();

        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return;

        patternData[row * cols + col] = state;
    }

    /// <summary>
    /// 清空所有点
    /// </summary>
    public void Clear()
    {
        if (patternData == null || patternData.Length != rows * cols)
            InitializePattern();

        for (int i = 0; i < patternData.Length; i++)
            patternData[i] = false;
    }

    /// <summary>
    /// 反转所有点状态
    /// </summary>
    public void Invert()
    {
        if (patternData == null || patternData.Length != rows * cols)
            InitializePattern();

        for (int i = 0; i < patternData.Length; i++)
            patternData[i] = !patternData[i];
    }

    /// <summary>
    /// 调整网格尺寸（保留现有数据）
    /// </summary>
    public void ResizePattern(int newRows, int newCols)
    {
        if (newRows == rows && newCols == cols)
            return;

        bool[] oldData = patternData;
        int oldRows = rows;
        int oldCols = cols;

        rows = newRows;
        cols = newCols;
        patternData = new bool[rows * cols];

        // 复制旧数据到新数组
        if (oldData != null)
        {
            int minRows = Mathf.Min(oldRows, newRows);
            int minCols = Mathf.Min(oldCols, newCols);

            for (int r = 0; r < minRows; r++)
            {
                for (int c = 0; c < minCols; c++)
                {
                    patternData[r * cols + c] = oldData[r * oldCols + c];
                }
            }
        }
    }

    private void OnValidate()
    {
        if (patternData == null || patternData.Length != rows * cols)
        {
            InitializePattern();
        }
    }
}
