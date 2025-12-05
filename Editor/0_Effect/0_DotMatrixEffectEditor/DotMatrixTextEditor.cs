using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// DotMatrixText的自定义编辑器
/// 使用 UI Toolkit 实现，变量名显示为中文但脚本变量名保持英文
/// </summary>
[CustomEditor(typeof(DotMatrixText))]
public class DotMatrixTextEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        
        // 添加样式
        root.style.paddingTop = 5;
        root.style.paddingBottom = 5;
        
        // === 显示设置 ===
        var displayGroup = CreateGroup("显示设置");
        displayGroup.Add(CreatePropertyField("textLine1", "第一行文本"));
        displayGroup.Add(CreatePropertyField("textLine2", "第二行文本"));
        root.Add(displayGroup);
        
        root.Add(CreateSpace(10));
        
        // === 点阵设置 ===
        var dotGroup = CreateGroup("点阵设置");
        dotGroup.Add(CreatePropertyField("dotPrefab", "点的预制体"));
        dotGroup.Add(CreatePropertyField("dotSize", "点的大小"));
        dotGroup.Add(CreatePropertyField("dotSpacing", "点之间的间距"));
        dotGroup.Add(CreatePropertyField("letterSpacing", "字母之间的间距"));
        
        // 尺寸预设按钮
        var sizePresetRow = CreateButtonRow();
        sizePresetRow.Add(CreateButton("小尺寸 (15px)", () => SetSize(15f, 2f)));
        sizePresetRow.Add(CreateButton("中尺寸 (25px)", () => SetSize(25f, 3f)));
        sizePresetRow.Add(CreateButton("大尺寸 (35px)", () => SetSize(35f, 4f)));
        dotGroup.Add(sizePresetRow);
        
        root.Add(dotGroup);
        
        root.Add(CreateSpace(10));
        
        // === 颜色设置 ===
        var colorGroup = CreateGroup("颜色设置");
        colorGroup.Add(CreatePropertyField("dotOnColor", "点亮时的颜色"));
        colorGroup.Add(CreatePropertyField("dotOffColor", "熄灭时的颜色"));
        
        // 颜色预设按钮
        var colorPresetRow = CreateButtonRow();
        colorPresetRow.Add(CreateButton("霓虹蓝", () => SetColor(
            new Color(0f, 0.83f, 1f, 1f), 
            new Color(0f, 0.83f, 1f, 0.1f)
        )));
        colorPresetRow.Add(CreateButton("霓虹绿", () => SetColor(
            new Color(0.22f, 1f, 0.08f, 1f), 
            new Color(0.22f, 1f, 0.08f, 0.1f)
        )));
        colorPresetRow.Add(CreateButton("橙红", () => SetColor(
            new Color(1f, 0.42f, 0.21f, 1f), 
            new Color(1f, 0.42f, 0.21f, 0.1f)
        )));
        colorGroup.Add(colorPresetRow);
        
        root.Add(colorGroup);
        
        root.Add(CreateSpace(10));
        
        // === 动画设置 ===
        var animGroup = CreateGroup("动画设置");
        animGroup.Add(CreatePropertyField("enableTypewriterAnimation", "启用打字机动画"));
        animGroup.Add(CreatePropertyField("traverseSpeed", "字母遍历速度（秒）"));
        animGroup.Add(CreatePropertyField("letterDelay", "字母间延迟（秒）"));
        
        // 动画速度预设按钮
        var speedPresetRow = CreateButtonRow();
        speedPresetRow.Add(CreateButton("快速", () => SetSpeed(0.05f, 0.1f)));
        speedPresetRow.Add(CreateButton("正常", () => SetSpeed(0.1f, 0.2f)));
        speedPresetRow.Add(CreateButton("慢速", () => SetSpeed(0.2f, 0.3f)));
        animGroup.Add(speedPresetRow);
        
        root.Add(animGroup);
        
        root.Add(CreateSpace(10));
        
        // === 布局设置 ===
        var layoutGroup = CreateGroup("布局设置");
        layoutGroup.Add(CreatePropertyField("line1Container", "第一行容器"));
        layoutGroup.Add(CreatePropertyField("line2Container", "第二行容器"));
        layoutGroup.Add(CreatePropertyField("line1AnchorPosition", "第一行初始位置"));
        layoutGroup.Add(CreatePropertyField("line2AnchorPosition", "第二行初始位置"));
        root.Add(layoutGroup);
        
        root.Add(CreateSpace(15));
        
        // === 快速操作 ===
        var operationsGroup = CreateGroup("快速操作");
        
        var generateBtn = CreateActionButton("生成点阵文本", () => {
            var target = (DotMatrixText)this.target;
            target.GenerateText();
            EditorUtility.SetDirty(target);
        });
        generateBtn.style.backgroundColor = new Color(0.4f, 0.8f, 1f, 0.3f);
        operationsGroup.Add(generateBtn);
        
        var clearBtn = CreateActionButton("清除点阵文本", () => {
            var target = (DotMatrixText)this.target;
            target.ClearText();
            EditorUtility.SetDirty(target);
        });
        clearBtn.style.backgroundColor = new Color(1f, 0.6f, 0.4f, 0.3f);
        operationsGroup.Add(clearBtn);
        
        var resetBtn = CreateActionButton("重置容器位置", () => {
            var target = (DotMatrixText)this.target;
            target.ResetContainerPositions();
            EditorUtility.SetDirty(target);
        });
        resetBtn.style.backgroundColor = new Color(0.6f, 1f, 0.6f, 0.3f);
        operationsGroup.Add(resetBtn);
        
        var replayBtn = CreateActionButton("重新播放动画", () => {
            var target = (DotMatrixText)this.target;
            target.ReplayAnimation();
        });
        replayBtn.style.backgroundColor = new Color(1f, 1f, 0.4f, 0.3f);
        replayBtn.SetEnabled(Application.isPlaying);
        operationsGroup.Add(replayBtn);
        
        root.Add(operationsGroup);
        
        root.Add(CreateSpace(10));
        
        // === 状态信息 ===
        var statusGroup = CreateGroup("状态信息");
        var statusLabel = new Label();
        statusLabel.style.whiteSpace = WhiteSpace.Normal;
        statusLabel.style.paddingLeft = 5;
        statusLabel.style.paddingTop = 5;
        statusLabel.style.paddingBottom = 5;
        
        // 定时更新状态
        statusLabel.schedule.Execute(() => {
            var target = (DotMatrixText)this.target;
            if (target != null)
            {
                string line1Status = target.line1Container != null ? "✓ 已创建" : "✗ 未创建";
                string line2Status = target.line2Container != null ? "✓ 已创建" : "✗ 未创建";
                
                statusLabel.text = 
                    $"第一行: {target.textLine1}\n" +
                    $"第二行: {target.textLine2}\n" +
                    $"点大小: {target.dotSize}px\n" +
                    $"字母间距: {target.letterSpacing} 点阵单位\n" +
                    $"第一行容器: {line1Status}\n" +
                    $"第二行容器: {line2Status}";
            }
        }).Every(100);
        
        statusGroup.Add(statusLabel);
        root.Add(statusGroup);
        
        return root;
    }
    
    // === 辅助方法 ===
    
    private VisualElement CreateGroup(string title)
    {
        var group = new VisualElement();
        group.style.marginBottom = 5;
        group.style.paddingLeft = 5;
        group.style.paddingRight = 5;
        group.style.paddingTop = 5;
        group.style.paddingBottom = 5;
        group.style.backgroundColor = new Color(0, 0, 0, 0.1f);
        group.style.borderTopLeftRadius = 4;
        group.style.borderTopRightRadius = 4;
        group.style.borderBottomLeftRadius = 4;
        group.style.borderBottomRightRadius = 4;
        
        var titleLabel = new Label(title);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 12;
        titleLabel.style.marginBottom = 5;
        group.Add(titleLabel);
        
        return group;
    }
    
    private PropertyField CreatePropertyField(string propertyName, string chineseLabel)
    {
        var property = serializedObject.FindProperty(propertyName);
        var field = new PropertyField(property, chineseLabel);
        field.Bind(serializedObject);
        return field;
    }
    
    private VisualElement CreateSpace(int height)
    {
        var space = new VisualElement();
        space.style.height = height;
        return space;
    }
    
    private VisualElement CreateButtonRow()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 5;
        return row;
    }
    
    private Button CreateButton(string text, System.Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.style.flexGrow = 1;
        button.style.marginLeft = 2;
        button.style.marginRight = 2;
        return button;
    }
    
    private Button CreateActionButton(string text, System.Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.style.height = 30;
        button.style.marginTop = 3;
        button.style.marginBottom = 3;
        button.style.fontSize = 12;
        return button;
    }
    
    private void SetSize(float size, float spacing)
    {
        var target = (DotMatrixText)this.target;
        target.dotSize = size;
        target.dotSpacing = spacing;
        EditorUtility.SetDirty(target);
    }
    
    private void SetColor(Color onColor, Color offColor)
    {
        var target = (DotMatrixText)this.target;
        target.dotOnColor = onColor;
        target.dotOffColor = offColor;
        EditorUtility.SetDirty(target);
    }
    
    private void SetSpeed(float traverseSpeed, float letterDelay)
    {
        var target = (DotMatrixText)this.target;
        target.traverseSpeed = traverseSpeed;
        target.letterDelay = letterDelay;
        EditorUtility.SetDirty(target);
    }
}
