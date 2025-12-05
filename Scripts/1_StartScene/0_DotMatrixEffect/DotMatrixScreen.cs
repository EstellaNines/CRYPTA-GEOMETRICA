using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏点阵（1920x1080 区域）生成与特效播放
/// 仿照 DotMatrixText 的风格与消息机制
/// </summary>
public class DotMatrixScreen : MonoBehaviour
{
    [Header("点阵设置")]
    [Tooltip("点的预制体（需要包含 Image 组件的 UI 物体）")]
    public GameObject dotPrefab;

    [Tooltip("点的大小（像素）")]
    public float dotSize = 10f;

    [Tooltip("点之间的参考间距（像素，实际会按 1920x1080 自动分配）")]
    public float dotSpacing = 5f;

    [Header("颜色设置")]
    [Tooltip("点亮时的颜色")]
    public Color dotOnColor = new Color(0f, 0.83f, 1f, 1f);

    [Tooltip("熄灭时的颜色")]
    public Color dotOffColor = new Color(0f, 0.83f, 1f, 0.08f);

    [Header("播放设置")]
    [Tooltip("进入场景后自动播放特效")]
    public bool autoPlay = true;

    public EffectType effectType = EffectType.ScanLine;

    [Header("自定义图案")]
    [Tooltip("自定义点阵图案资源（仅当 effectType 为 CustomPattern 时使用）")]
    public DotMatrixPattern customPattern;

    [Tooltip("特效速度缩放（越大越快）")]
    [Range(0.1f, 5f)]
    public float effectSpeed = 1f;
    
    [Tooltip("持续性特效（如 Glitch/IsometricCube）的一次播放时长")]
    public float defaultDuration = 3.0f;

    // 固定目标区域（像素）
    private const float BaseWidth = 1920f;
    private const float BaseHeight = 1080f;

    // 内部
    private RectTransform container; // 1920x1080 区域容器
    private Image[,] dotGrid;
    private int rows;
    private int cols;
    private float actualSpacingX;
    private float actualSpacingY;
    private Coroutine runningEffect;

    public enum EffectType
    {
        ScanLine,
        Ripple,
        RandomBlink,
        Pulse,
        Wave,
        MatrixRain,
        Glitch,
        IsometricCube,
        CustomPattern,
        CountdownBar,
    }

    void Start()
    {
        // 必须在 Canvas 下
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DotMatrixScreen] 需要挂在 Canvas 下使用。");
            return;
        }

        if (dotPrefab == null)
        {
            Debug.LogError("[DotMatrixScreen] 点预制体未设置。");
            return;
        }

        BuildGrid();

        if (autoPlay)
        {
            PlayEffect(effectType);
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    // 生成 1920x1080 区域容器 + 自动计算行列并生成点
    void BuildGrid()
    {
        // 清理旧容器
        var old = transform.Find("DotScreenContainer");
        if (old != null) DestroyImmediate(old.gameObject);

        var obj = new GameObject("DotScreenContainer");
        obj.transform.SetParent(transform, false);
        container = obj.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.sizeDelta = new Vector2(BaseWidth, BaseHeight);
        container.anchoredPosition = Vector2.zero;

        // 计算行列（尽可能填满）
        cols = Mathf.Max(1, Mathf.FloorToInt((BaseWidth + dotSpacing) / (dotSize + dotSpacing)));
        rows = Mathf.Max(1, Mathf.FloorToInt((BaseHeight + dotSpacing) / (dotSize + dotSpacing)));

        // 让点均匀填满：重新分配实际间距
        actualSpacingX = (BaseWidth - cols * dotSize) / (cols + 1);
        actualSpacingY = (BaseHeight - rows * dotSize) / (rows + 1);
        if (actualSpacingX < 0f) actualSpacingX = 0f;
        if (actualSpacingY < 0f) actualSpacingY = 0f;

        dotGrid = new Image[rows, cols];

        // 左上角第一个点中心位置
        float startX = -BaseWidth / 2f + actualSpacingX + dotSize * 0.5f;
        float startY = BaseHeight / 2f - actualSpacingY - dotSize * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var dot = Instantiate(dotPrefab, container);
                dot.name = $"Dot_{r}_{c}";

                var rect = dot.GetComponent<RectTransform>();
                if (rect == null) rect = dot.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(dotSize, dotSize);

                float x = startX + c * (dotSize + actualSpacingX);
                float y = startY - r * (dotSize + actualSpacingY);
                rect.anchoredPosition = new Vector2(x, y);

                var img = dot.GetComponent<Image>();
                if (img == null) img = dot.AddComponent<Image>();
                img.color = dotOffColor;

                dotGrid[r, c] = img;
            }
        }
    }

    public void PlayEffect(EffectType type)
    {
        if (runningEffect != null) StopCoroutine(runningEffect);
        runningEffect = StartCoroutine(PlayEffectCoroutine(type));
    }

    IEnumerator PlayEffectCoroutine(EffectType type)
    {
        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_START, $"全屏点阵效果 [{type}] [{gameObject.name}]");

        // 只要不是倒计时，就只播放一次
        // 倒计时本身包含完整的流程控制，不受 defaultDuration 限制
        var routine = GetEffectRoutine(type);
        if (routine != null)
        {
            yield return StartCoroutine(routine);
        }

        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_END, $"全屏点阵效果 [{type}] [{gameObject.name}]");
    }

    IEnumerator GetEffectRoutine(EffectType type)
    {
        switch (type)
        {
            case EffectType.ScanLine:
                return Effect_ScanLine();
            case EffectType.Ripple:
                return Effect_Ripple();
            case EffectType.RandomBlink:
                return Effect_RandomBlink();
            case EffectType.Pulse:
                return Effect_Pulse();
            case EffectType.Wave:
                return Effect_Wave();
            case EffectType.MatrixRain:
                return Effect_MatrixRain();
            case EffectType.Glitch:
                return Effect_Glitch();
            case EffectType.IsometricCube:
                return Effect_IsometricCube();
            case EffectType.CustomPattern:
                return Effect_CustomPattern();
            case EffectType.CountdownBar:
                return Effect_CountdownBar();
            default:
                return null;
        }
    }

    // ============= 特效实现 =============

    #region 特效：ScanLine
    IEnumerator Effect_ScanLine()
    {
        SetAll(dotOffColor);
        float delay = 0.02f / effectSpeed;
        int tail = 2; // 尾迹长度

        for (int r = 0; r < rows + tail; r++)
        {
            // 点亮当前行
            if (r < rows)
            {
                for (int c = 0; c < cols; c++)
                    dotGrid[r, c].color = dotOnColor;
            }

            // 熄灭 r-tail 行
            int offRow = r - tail;
            if (offRow >= 0)
            {
                for (int c = 0; c < cols; c++)
                    dotGrid[offRow, c].color = dotOffColor;
            }

            yield return new WaitForSeconds(delay);
        }
    }
    #endregion

    #region 特效：Ripple
    IEnumerator Effect_Ripple()
    {
        SetAll(dotOffColor);
        float delay = 0.03f / effectSpeed;
        int centerR = rows / 2;
        int centerC = cols / 2;
        int maxRadius = Mathf.CeilToInt(Mathf.Sqrt(centerR * centerR + centerC * centerC));

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int dr = r - centerR;
                    int dc = c - centerC;
                    int d = Mathf.RoundToInt(Mathf.Sqrt(dr * dr + dc * dc));
                    if (d == radius)
                        dotGrid[r, c].color = dotOnColor;
                    else if (d < radius - 2)
                        dotGrid[r, c].color = dotOffColor;
                }
            }
            yield return new WaitForSeconds(delay);
        }

        // 收尾清理
        yield return new WaitForSeconds(0.3f / effectSpeed);
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：IsometricCube
    IEnumerator Effect_IsometricCube()
    {
        SetAll(dotOffColor);

        int centerR = rows / 2;
        int centerC = cols / 2;
        int s = Mathf.Max(3, Mathf.Min(rows, cols) / 8);

        s = Mathf.Min(s, centerR);
        s = Mathf.Min(s, rows - 1 - centerR);
        s = Mathf.Min(s, centerC / 2);
        s = Mathf.Min(s, (cols - 1 - centerC) / 2);
        s = Mathf.Max(s, 2);

        System.Action<int, int, Color, int> Plot = (r, c, col, thick) =>
        {
            for (int dr = -thick; dr <= thick; dr++)
            {
                for (int dc = -thick; dc <= thick; dc++)
                {
                    int rr = r + dr;
                    int cc = c + dc;
                    if (rr >= 0 && rr < rows && cc >= 0 && cc < cols)
                        dotGrid[rr, cc].color = col;
                }
            }
        };

        System.Action<int, int, int, int, Color, int> DrawLine = (r0, c0, r1, c1, col, thick) =>
        {
            int steps = Mathf.Max(Mathf.Abs(r1 - r0), Mathf.Abs(c1 - c0));
            if (steps == 0)
            {
                Plot(r0, c0, col, thick);
                return;
            }
            float fr = r0;
            float fc = c0;
            float sr = (r1 - r0) / (float)steps;
            float sc_ = (c1 - c0) / (float)steps;
            for (int i = 0; i <= steps; i++)
            {
                int rr = Mathf.RoundToInt(fr);
                int cc = Mathf.RoundToInt(fc);
                Plot(rr, cc, col, thick);
                fr += sr;
                fc += sc_;
            }
        };

        Color col = dotOnColor;
        int thick = 1;

        int tR = centerR - s;
        int tC = centerC;
        int rR = centerR;
        int rC = centerC + s;
        int bR = centerR + s;
        int bC = centerC;
        int lR = centerR;
        int lC = centerC - s;

        DrawLine(tR, tC, rR, rC, col, thick);
        DrawLine(rR, rC, bR, bC, col, thick);
        DrawLine(bR, bC, lR, lC, col, thick);
        DrawLine(lR, lC, tR, tC, col, thick);

        int t2R = tR + s;
        int t2C = tC - s;
        int r2R = rR + s;
        int r2C = rC + s;
        int l2R = lR + s;
        int l2C = lC - s;

        DrawLine(tR, tC, t2R, t2C, col, thick);
        DrawLine(rR, rC, r2R, r2C, col, thick);
        DrawLine(lR, lC, l2R, l2C, col, thick);

        DrawLine(l2R, l2C, t2R, t2C, col, thick);
        DrawLine(t2R, t2C, r2R, r2C, col, thick);
        DrawLine(l2R, l2C, r2R, r2C, col, thick);

        // 持续显示一段时间
        yield return new WaitForSeconds(defaultDuration);
        
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：RandomBlink
    IEnumerator Effect_RandomBlink()
    {
        SetAll(dotOffColor);
        int frames = 40; // 每个循环帧数
        float step = 0.05f / effectSpeed;

        int batch = Mathf.Max(1, (rows * cols) / 12);

        for (int f = 0; f < frames; f++)
        {
            // 点亮一些
            for (int i = 0; i < batch; i++)
            {
                int r = Random.Range(0, rows);
                int c = Random.Range(0, cols);
                dotGrid[r, c].color = dotOnColor;
            }

            yield return new WaitForSeconds(step);

            // 熄灭一些
            for (int i = 0; i < batch; i++)
            {
                int r = Random.Range(0, rows);
                int c = Random.Range(0, cols);
                dotGrid[r, c].color = dotOffColor;
            }
        }

        // 收尾
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：Pulse
    IEnumerator Effect_Pulse()
    {
        int pulses = 3;
        float half = 0.25f / effectSpeed;
        for (int p = 0; p < pulses; p++)
        {
            float t = 0f;
            while (t < half)
            {
                float k = t / half;
                Color col = Color.Lerp(dotOffColor, dotOnColor, k);
                SetAll(col);
                t += Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                float k = t / half;
                Color col = Color.Lerp(dotOnColor, dotOffColor, k);
                SetAll(col);
                t += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
        }
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：Wave
    IEnumerator Effect_Wave()
    {
        SetAll(dotOffColor);
        int frames = 60; // 每个循环帧数
        float step = 0.05f / effectSpeed;
        for (int f = 0; f < frames; f++)
        {
            float elapsed = f * step;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float wave = Mathf.Sin((c * 0.2f) + (elapsed * 5f));
                    int waveRow = Mathf.RoundToInt((wave + 1f) * (rows - 1) * 0.5f);
                    if (Mathf.Abs(r - waveRow) <= 1)
                        dotGrid[r, c].color = dotOnColor;
                    else
                        dotGrid[r, c].color = dotOffColor;
                }
            }
            yield return new WaitForSeconds(step);
        }
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：MatrixRain
    IEnumerator Effect_MatrixRain()
    {
        SetAll(dotOffColor);
        int frames = 60; // 每个循环帧数
        float step = 0.05f / effectSpeed;
        int[] pos = new int[cols];
        for (int i = 0; i < cols; i++) pos[i] = Random.Range(-10, 0);
        for (int f = 0; f < frames; f++)
        {
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    Color cur = dotGrid[r, c].color;
                    dotGrid[r, c].color = Color.Lerp(cur, dotOffColor, 0.2f);
                }
                int p = pos[c];
                if (p >= 0 && p < rows)
                {
                    dotGrid[p, c].color = dotOnColor;
                    for (int t = 1; t <= 4 && p - t >= 0; t++)
                    {
                        float a = 1f - (t / 4f);
                        dotGrid[p - t, c].color = Color.Lerp(dotOffColor, dotOnColor, a);
                    }
                }
                pos[c]++;
                if (pos[c] > rows + 4) pos[c] = Random.Range(-8, -3);
            }
            yield return new WaitForSeconds(step);
        }
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：Glitch
    IEnumerator Effect_Glitch()
    {
        SetAll(dotOnColor);
        
        float elapsed = 0f;
        while (elapsed < defaultDuration)
        {
            int gr = Random.Range(0, Mathf.Max(1, rows - 6));
            int gh = Random.Range(3, Mathf.Min(8, rows - gr));
            int shift = Random.Range(-4, 5);
            for (int r = gr; r < gr + gh; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int nc = c + shift;
                    if (nc >= 0 && nc < cols)
                        dotGrid[r, c].color = Random.value > 0.5f ? dotOnColor : dotOffColor;
                }
            }
            
            float step1 = 0.06f / effectSpeed;
            yield return new WaitForSeconds(step1);
            elapsed += step1;

            for (int r = gr; r < gr + gh; r++)
                for (int c = 0; c < cols; c++)
                    dotGrid[r, c].color = dotOnColor;
            
            float step2 = 0.1f / effectSpeed;
            yield return new WaitForSeconds(step2);
            elapsed += step2;
        }
        
        SetAll(dotOffColor);
    }
    #endregion

    #region 特效：CustomPattern
    IEnumerator Effect_CustomPattern()
    {
        if (customPattern == null)
        {
            Debug.LogWarning("[DotMatrixScreen] 自定义图案资源未设置");
            SetAll(dotOffColor);
            yield break;
        }

        SetAll(dotOffColor);

        // 计算缩放以适配实际网格
        float scaleR = (float)rows / customPattern.rows;
        float scaleC = (float)cols / customPattern.cols;
        float scale = Mathf.Min(scaleR, scaleC);

        int displayRows = Mathf.RoundToInt(customPattern.rows * scale);
        int displayCols = Mathf.RoundToInt(customPattern.cols * scale);

        int startR = (rows - displayRows) / 2;
        int startC = (cols - displayCols) / 2;

        // 淡入
        float fadeInDur = 0.4f / effectSpeed;
        float t = 0f;
        while (t < fadeInDur)
        {
            float intensity = t / fadeInDur;
            DrawCustomPattern(startR, startC, displayRows, displayCols, intensity);
            t += Time.deltaTime;
            yield return null;
        }

        // 完全显示
        DrawCustomPattern(startR, startC, displayRows, displayCols, 1f);
        yield return new WaitForSeconds(2f / effectSpeed);

        // 淡出
        float fadeOutDur = 0.4f / effectSpeed;
        t = fadeOutDur;
        while (t > 0f)
        {
            float intensity = t / fadeOutDur;
            DrawCustomPattern(startR, startC, displayRows, displayCols, intensity);
            t -= Time.deltaTime;
            yield return null;
        }

        SetAll(dotOffColor);
    }

    void DrawCustomPattern(int startR, int startC, int displayRows, int displayCols, float intensity)
    {
        SetAll(dotOffColor);
        Color col = Color.Lerp(dotOffColor, dotOnColor, Mathf.Clamp01(intensity));

        for (int dr = 0; dr < displayRows; dr++)
        {
            for (int dc = 0; dc < displayCols; dc++)
            {
                int targetR = startR + dr;
                int targetC = startC + dc;

                if (targetR < 0 || targetR >= rows || targetC < 0 || targetC >= cols)
                    continue;

                // 从自定义图案采样
                float srcR = dr / (float)displayRows * customPattern.rows;
                float srcC = dc / (float)displayCols * customPattern.cols;

                int patternR = Mathf.Clamp(Mathf.RoundToInt(srcR), 0, customPattern.rows - 1);
                int patternC = Mathf.Clamp(Mathf.RoundToInt(srcC), 0, customPattern.cols - 1);

                if (customPattern.GetDot(patternR, patternC))
                {
                    dotGrid[targetR, targetC].color = col;
                }
            }
        }
    }
    #endregion

    #region 特效：CountdownBar
    IEnumerator Effect_CountdownBar()
    {
        SetAll(dotOffColor);

        int charSpacing = 1;
        Color glyphColor = dotOnColor;

        void DrawPercentageValue(int value)
        {
            SetAll(dotOffColor);
            string text = $"{value:000}%";
            var matrices = new List<bool[,]>(text.Length);
            int textPixelWidth = 0;
            int charRows = 0;

            for (int i = 0; i < text.Length; i++)
            {
                bool[,] matrix = DotMatrixFont.GetCharacterMatrix(text[i]);
                matrices.Add(matrix);
                int mCols = matrix.GetLength(1);
                textPixelWidth += mCols;
                if (i < text.Length - 1) textPixelWidth += charSpacing;
                if (charRows == 0) charRows = matrix.GetLength(0);
            }

            if (charRows == 0 || textPixelWidth == 0)
                return;

            float targetRowCoverage = 0.6f; // 覆盖约60%的高度
            float targetColCoverage = 0.8f; // 覆盖约80%的宽度

            int scaleR = Mathf.Max(1, Mathf.FloorToInt((rows * targetRowCoverage) / charRows));
            int scaleC = Mathf.Max(1, Mathf.FloorToInt((cols * targetColCoverage) / textPixelWidth));
            
            // 使用统一缩放以保持纵横比
            int scale = Mathf.Min(scaleR, scaleC);
            scaleR = scale;
            scaleC = scale;

            int totalRows = charRows * scaleR;
            int totalCols = textPixelWidth * scaleC;
            int startRow = Mathf.Max(0, (rows - totalRows) / 2);
            int startCol = Mathf.Max(0, (cols - totalCols) / 2);

            int pixelColOffset = 0;
            for (int idx = 0; idx < matrices.Count; idx++)
            {
                var matrix = matrices[idx];
                int mRows = matrix.GetLength(0);
                int mCols = matrix.GetLength(1);
                for (int mr = 0; mr < mRows; mr++)
                {
                    for (int mc = 0; mc < mCols; mc++)
                    {
                        if (!matrix[mr, mc]) continue;
                        for (int sr = 0; sr < scaleR; sr++)
                        {
                            int targetR = startRow + mr * scaleR + sr;
                            if (targetR < 0 || targetR >= rows) continue;
                            for (int sc = 0; sc < scaleC; sc++)
                            {
                                int targetC = startCol + (pixelColOffset + mc) * scaleC + sc;
                                if (targetC < 0 || targetC >= cols) continue;
                                dotGrid[targetR, targetC].color = glyphColor;
                            }
                        }
                    }
                }

                pixelColOffset += mCols;
                if (idx < matrices.Count - 1)
                    pixelColOffset += charSpacing;
            }
        }

        float stepDuration = Mathf.Max(0.02f, 3.0f / 100f); // 确保总时长约为3秒
        for (int value = 100; value >= 0; value--)
        {
            DrawPercentageValue(value);
            yield return new WaitForSeconds(stepDuration);
        }

        while (true)
        {
            yield return null;
        }
    }
    #endregion

    // ============= 工具方法 =============

    void SetAll(Color color)
    {
        if (dotGrid == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                dotGrid[r, c].color = color;
    }

    public void Clear()
    {
        SetAll(dotOffColor);
    }
}
