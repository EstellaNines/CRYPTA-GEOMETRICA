using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 点阵文本显示控制器
/// 支持打字机遍历动画效果
/// </summary>
public class DotMatrixText : MonoBehaviour
{
    [Header("显示设置")]
    [Tooltip("要显示的文本（第一行）")]
    public string textLine1 = "CRYPTA";
    
    [Tooltip("要显示的文本（第二行）")]
    public string textLine2 = "GEOMETRICA";
    
    [Header("点阵设置")]
    [Tooltip("点的预制体（Image组件）")]
    public GameObject dotPrefab;
    
    [Tooltip("点的大小")]
    public float dotSize = 25f;
    
    [Tooltip("点之间的间距")]
    public float dotSpacing = 2f;
    
    [Tooltip("字母之间的间距（点阵单位）")]
    [Range(1, 5)]
    public int letterSpacing = 2;
    
    [Header("颜色设置")]
    [Tooltip("点亮时的颜色")]
    public Color dotOnColor = new Color(0f, 0.83f, 1f, 1f); // #00d4ff
    
    [Tooltip("熄灭时的颜色")]
    public Color dotOffColor = new Color(0f, 0.83f, 1f, 0.1f); // 半透明
    
    [Header("动画设置")]
    [Tooltip("是否启用打字机遍历动画")]
    public bool enableTypewriterAnimation = true;
    
    [Tooltip("每个字母遍历的速度（秒/字母）")]
    public float traverseSpeed = 0.1f;
    
    [Tooltip("字母固定后的延迟（秒）")]
    public float letterDelay = 0.2f;
    
    [Header("下划线设置")]
    [Tooltip("是否显示左右下划线")]
    public bool showUnderscores = true;
    
    [Tooltip("下划线闪烁速度（秒）")]
    public float underscoreBlinkSpeed = 0.5f;
    
    [Header("布局设置")]
    [Tooltip("第一行文本容器（可在场景中拖拽调整位置）")]
    public Transform line1Container;
    
    [Tooltip("第二行文本容器（可在场景中拖拽调整位置）")]
    public Transform line2Container;
    
    [Tooltip("第一行的初始锚点位置（左上角）")]
    public Vector2 line1AnchorPosition = new Vector2(-400f, 200f);
    
    [Tooltip("第二行的初始锚点位置（右下角）")]
    public Vector2 line2AnchorPosition = new Vector2(100f, -100f);

    // 内部数据
    private RectTransform canvasRect;
    private List<DotMatrixCharacter> line1Characters = new List<DotMatrixCharacter>();
    private List<DotMatrixCharacter> line2Characters = new List<DotMatrixCharacter>();
    
    // 下划线字符
    private DotMatrixCharacter line1LeftUnderscore;
    private DotMatrixCharacter line1RightUnderscore;
    private DotMatrixCharacter line2LeftUnderscore;
    private DotMatrixCharacter line2RightUnderscore;
    
    void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            canvasRect = gameObject.AddComponent<RectTransform>();
        }
        
        GenerateText();
        
        // 使用 PlayAnimationCoroutine 确保消息系统被触发
        StartCoroutine(PlayAnimationCoroutine());
    }

    /// <summary>
    /// 生成点阵文本
    /// </summary>
    public void GenerateText()
    {
        // 停止所有正在播放的协程，防止集合修改异常
        StopAllCoroutines();
        
        // 清除旧的
        ClearText();
        
        // 创建或获取第一行容器
        if (line1Container == null)
        {
            GameObject line1Obj = new GameObject("Line1Container");
            line1Obj.transform.SetParent(transform, false);
            RectTransform line1Rect = line1Obj.AddComponent<RectTransform>();
            line1Rect.anchorMin = new Vector2(0.5f, 0.5f);
            line1Rect.anchorMax = new Vector2(0.5f, 0.5f);
            line1Rect.pivot = new Vector2(0f, 1f);
            line1Rect.anchoredPosition = line1AnchorPosition;
            line1Container = line1Obj.transform;
        }
        
        // 创建或获取第二行容器
        if (line2Container == null)
        {
            GameObject line2Obj = new GameObject("Line2Container");
            line2Obj.transform.SetParent(transform, false);
            RectTransform line2Rect = line2Obj.AddComponent<RectTransform>();
            line2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            line2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            line2Rect.pivot = new Vector2(0f, 1f);
            line2Rect.anchoredPosition = line2AnchorPosition;
            line2Container = line2Obj.transform;
        }
        
        // 生成第一行
        GenerateLine(textLine1, line1Container, line1Characters);
        
        // 生成第二行
        GenerateLine(textLine2, line2Container, line2Characters);
        
        // 生成下划线
        if (showUnderscores)
        {
            GenerateUnderscores();
        }
    }

    /// <summary>
    /// 生成一行文本
    /// </summary>
    void GenerateLine(string text, Transform container, List<DotMatrixCharacter> characterList)
    {
        float currentX = 0f; // 相对于容器的位置，从0开始
        
        foreach (char c in text)
        {
            if (c == ' ')
            {
                currentX += (5 + letterSpacing) * (dotSize + dotSpacing);
                continue;
            }
            
            // 创建字符容器
            GameObject charObj = new GameObject($"Char_{c}");
            charObj.transform.SetParent(container, false);
            
            RectTransform charRect = charObj.AddComponent<RectTransform>();
            charRect.anchorMin = new Vector2(0f, 1f);
            charRect.anchorMax = new Vector2(0f, 1f);
            charRect.pivot = new Vector2(0f, 1f);
            charRect.anchoredPosition = new Vector2(currentX, 0f);
            
            // 创建点阵字符
            DotMatrixCharacter dotChar = charObj.AddComponent<DotMatrixCharacter>();
            dotChar.Initialize(c, dotPrefab, dotSize, dotSpacing, dotOnColor, dotOffColor);
            
            characterList.Add(dotChar);
            
            // 移动到下一个字符位置
            currentX += (5 + letterSpacing) * (dotSize + dotSpacing);
        }
    }

    /// <summary>
    /// 清除所有文本
    /// </summary>
    public void ClearText()
    {
        // 停止所有协程，防止在清除时仍在遍历
        StopAllCoroutines();
        
        line1Characters.Clear();
        line2Characters.Clear();
        
        // 清除第一行容器内的字符
        if (line1Container != null)
        {
            for (int i = line1Container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(line1Container.GetChild(i).gameObject);
            }
        }
        
        // 清除第二行容器内的字符
        if (line2Container != null)
        {
            for (int i = line2Container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(line2Container.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// 播放打字机遍历动画（两行同时播放）
    /// </summary>
    IEnumerator PlayTypewriterAnimation()
    {
        // 创建列表副本，防止遍历时列表被修改
        var line1Copy = new List<DotMatrixCharacter>(line1Characters);
        var line2Copy = new List<DotMatrixCharacter>(line2Characters);
        
        // 先隐藏所有字符
        foreach (var ch in line1Copy)
        {
            if (ch != null)
            {
                ch.SetAllDotsOff();
            }
        }
        foreach (var ch in line2Copy)
        {
            if (ch != null)
            {
                ch.SetAllDotsOff();
            }
        }
        
        // 同时播放两行动画
        int maxLength = Mathf.Max(line1Copy.Count, line2Copy.Count);
        
        for (int i = 0; i < maxLength; i++)
        {
            // 同时启动两行的字符动画
            if (i < line1Copy.Count && line1Copy[i] != null)
            {
                StartCoroutine(line1Copy[i].PlayTraverseAnimation(traverseSpeed));
            }
            
            if (i < line2Copy.Count && line2Copy[i] != null)
            {
                StartCoroutine(line2Copy[i].PlayTraverseAnimation(traverseSpeed));
            }
            
            // 等待遍历动画完成 + 字母延迟
            yield return new WaitForSeconds(traverseSpeed + letterDelay);
        }
        
        // 打字机效果结束，停止下划线闪烁
        if (showUnderscores)
        {
            StopUnderscoreBlink();
        }
    }

    /// <summary>
    /// 播放动画协程（供外部调用，可等待完成）
    /// </summary>
    public IEnumerator PlayAnimationCoroutine()
    {
        // 发送效果开始播放消息
        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_START, $"点阵文字效果 [{gameObject.name}]");
        
        if (enableTypewriterAnimation)
        {
            yield return StartCoroutine(PlayTypewriterAnimation());
        }
        else
        {
            // 如果不启用动画，直接显示所有字符
            foreach (var ch in line1Characters)
            {
                ch.ShowTargetCharacter();
            }
            foreach (var ch in line2Characters)
            {
                ch.ShowTargetCharacter();
            }
        }
        
        // 发送效果播放结束消息
        MessageManager.Instance.Send(MessageDefine.EFFECT_PLAY_END, $"点阵文字效果 [{gameObject.name}]");
    }
    
    /// <summary>
    /// 重新播放动画
    /// </summary>
    public void ReplayAnimation()
    {
        StopAllCoroutines();
        if (enableTypewriterAnimation)
        {
            StartCoroutine(PlayTypewriterAnimation());
        }
    }

    /// <summary>
    /// 重置容器位置到初始锚点位置
    /// </summary>
    public void ResetContainerPositions()
    {
        if (line1Container != null)
        {
            RectTransform rect = line1Container.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = line1AnchorPosition;
            }
        }
        
        if (line2Container != null)
        {
            RectTransform rect = line2Container.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = line2AnchorPosition;
            }
        }
    }
    
    /// <summary>
    /// 生成左右下划线
    /// </summary>
    void GenerateUnderscores()
    {
        // 计算单个字符的宽度（5个点 + 字母间距）
        float charWidth = (5 + letterSpacing) * (dotSize + dotSpacing);
        
        // 第一行：左下划线在第一个字符左侧，右下划线在最后一个字符右侧
        float line1LeftX = -charWidth;
        float line1RightX = line1Characters.Count * charWidth;
        
        // 第二行：左下划线在第一个字符左侧，右下划线在最后一个字符右侧
        float line2LeftX = -charWidth;
        float line2RightX = line2Characters.Count * charWidth;
        
        // 创建第一行左下划线
        line1LeftUnderscore = CreateUnderscore("Line1_LeftUnderscore", line1Container, line1LeftX);
        
        // 创建第一行右下划线
        line1RightUnderscore = CreateUnderscore("Line1_RightUnderscore", line1Container, line1RightX);
        
        // 创建第二行左下划线
        line2LeftUnderscore = CreateUnderscore("Line2_LeftUnderscore", line2Container, line2LeftX);
        
        // 创建第二行右下划线
        line2RightUnderscore = CreateUnderscore("Line2_RightUnderscore", line2Container, line2RightX);
        
        // 启动闪烁动画
        StartCoroutine(BlinkUnderscores());
    }
    
    /// <summary>
    /// 创建单个下划线字符
    /// </summary>
    DotMatrixCharacter CreateUnderscore(string name, Transform container, float posX)
    {
        GameObject underscoreObj = new GameObject(name);
        underscoreObj.transform.SetParent(container, false);
        
        RectTransform underscoreRect = underscoreObj.AddComponent<RectTransform>();
        underscoreRect.anchorMin = new Vector2(0f, 1f);
        underscoreRect.anchorMax = new Vector2(0f, 1f);
        underscoreRect.pivot = new Vector2(0f, 1f);
        underscoreRect.anchoredPosition = new Vector2(posX, 0f);
        
        DotMatrixCharacter underscore = underscoreObj.AddComponent<DotMatrixCharacter>();
        underscore.Initialize('_', dotPrefab, dotSize, dotSpacing, dotOnColor, dotOffColor);
        underscore.ShowTargetCharacter(); // 立即显示
        
        return underscore;
    }
    
    /// <summary>
    /// 下划线闪烁动画（在打字机效果期间闪烁）
    /// </summary>
    IEnumerator BlinkUnderscores()
    {
        bool isOn = true;
        
        while (true)
        {
            // 切换所有下划线的显示状态
            if (line1LeftUnderscore != null)
            {
                if (isOn) line1LeftUnderscore.ShowTargetCharacter();
                else line1LeftUnderscore.SetAllDotsOff();
            }
            
            if (line1RightUnderscore != null)
            {
                if (isOn) line1RightUnderscore.ShowTargetCharacter();
                else line1RightUnderscore.SetAllDotsOff();
            }
            
            if (line2LeftUnderscore != null)
            {
                if (isOn) line2LeftUnderscore.ShowTargetCharacter();
                else line2LeftUnderscore.SetAllDotsOff();
            }
            
            if (line2RightUnderscore != null)
            {
                if (isOn) line2RightUnderscore.ShowTargetCharacter();
                else line2RightUnderscore.SetAllDotsOff();
            }
            
            isOn = !isOn;
            yield return new WaitForSeconds(underscoreBlinkSpeed);
        }
    }
    
    /// <summary>
    /// 停止下划线闪烁并保持显示
    /// </summary>
    void StopUnderscoreBlink()
    {
        // 停止闪烁协程
        StopCoroutine(BlinkUnderscores());
        
        // 确保所有下划线都显示
        if (line1LeftUnderscore != null)
        {
            line1LeftUnderscore.ShowTargetCharacter();
        }
        
        if (line1RightUnderscore != null)
        {
            line1RightUnderscore.ShowTargetCharacter();
        }
        
        if (line2LeftUnderscore != null)
        {
            line2LeftUnderscore.ShowTargetCharacter();
        }
        
        if (line2RightUnderscore != null)
        {
            line2RightUnderscore.ShowTargetCharacter();
        }
    }
}

/// <summary>
/// 单个点阵字符
/// </summary>
public class DotMatrixCharacter : MonoBehaviour
{
    private char character;
    private Image[,] dotImages; // 7行5列
    private bool[,] targetMatrix;
    private Color onColor;
    private Color offColor;
    
    public void Initialize(char c, GameObject dotPrefab, float dotSize, float dotSpacing, Color onCol, Color offCol)
    {
        character = c;
        onColor = onCol;
        offColor = offCol;
        targetMatrix = DotMatrixFont.GetCharacterMatrix(c);
        
        // 创建7x5的点阵
        dotImages = new Image[7, 5];
        
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                GameObject dotObj = Instantiate(dotPrefab, transform);
                dotObj.name = $"Dot_{row}_{col}";
                
                RectTransform dotRect = dotObj.GetComponent<RectTransform>();
                if (dotRect == null)
                {
                    dotRect = dotObj.AddComponent<RectTransform>();
                }
                
                dotRect.anchorMin = new Vector2(0f, 1f);
                dotRect.anchorMax = new Vector2(0f, 1f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = new Vector2(dotSize, dotSize);
                
                float x = col * (dotSize + dotSpacing) + dotSize / 2f;
                float y = -row * (dotSize + dotSpacing) - dotSize / 2f;
                dotRect.anchoredPosition = new Vector2(x, y);
                
                Image dotImage = dotObj.GetComponent<Image>();
                if (dotImage == null)
                {
                    dotImage = dotObj.AddComponent<Image>();
                }
                
                dotImages[row, col] = dotImage;
                
                // 初始设置为关闭状态
                dotImage.color = offColor;
            }
        }
    }
    
    /// <summary>
    /// 设置所有点为关闭状态（隐藏）
    /// </summary>
    public void SetAllDotsOff()
    {
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (dotImages[row, col] != null)
                {
                    dotImages[row, col].gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 显示目标字符
    /// </summary>
    public void ShowTargetCharacter()
    {
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (dotImages[row, col] != null)
                {
                    bool isOn = targetMatrix[row, col];
                    dotImages[row, col].gameObject.SetActive(isOn);
                    if (isOn)
                    {
                        dotImages[row, col].color = onColor;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 显示指定字符
    /// </summary>
    public void ShowCharacter(char c)
    {
        bool[,] matrix = DotMatrixFont.GetCharacterMatrix(c);
        
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (dotImages[row, col] != null)
                {
                    bool isOn = matrix[row, col];
                    dotImages[row, col].gameObject.SetActive(isOn);
                    if (isOn)
                    {
                        dotImages[row, col].color = onColor;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 播放从A遍历到目标字符的动画
    /// </summary>
    public IEnumerator PlayTraverseAnimation(float speed)
    {
        char targetChar = char.ToUpper(character);
        
        // 从A遍历到目标字符
        for (char c = 'A'; c <= targetChar; c++)
        {
            ShowCharacter(c);
            yield return new WaitForSeconds(speed);
        }
        
        // 确保最终显示目标字符
        ShowTargetCharacter();
    }
}
