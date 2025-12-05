using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>
/// ButtonFadeInController 的自定义 Inspector
/// </summary>
[CustomEditor(typeof(ButtonFadeInController))]
public class ButtonFadeInControllerEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        
        // 创建淡入设置分组
        var fadeInGroup = CreateGroup("淡入设置");
        fadeInGroup.Add(CreatePropertyField("fadeInDuration", "淡入持续时间"));
        fadeInGroup.Add(CreatePropertyField("buttonDelay", "按钮间隔延迟"));
        fadeInGroup.Add(CreatePropertyField("fadeInEase", "淡入曲线"));
        root.Add(fadeInGroup);
        
        // 创建调试选项分组
        var debugGroup = CreateGroup("调试选项");
        debugGroup.Add(CreatePropertyField("showDebugLogs", "显示调试日志"));
        root.Add(debugGroup);
        
        // 创建测试按钮区域
        var testGroup = CreateGroup("测试功能");
        
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.marginTop = 5;
        
        var testFadeInButton = new Button(() => 
        {
            var controller = target as ButtonFadeInController;
            controller?.TestFadeIn();
        })
        {
            text = "测试淡入效果"
        };
        testFadeInButton.style.flexGrow = 1;
        testFadeInButton.style.height = 30;
        testFadeInButton.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.2f, 0.6f, 0.8f));
        
        var resetButton = new Button(() => 
        {
            var controller = target as ButtonFadeInController;
            controller?.ResetButtons();
        })
        {
            text = "重置按钮"
        };
        resetButton.style.flexGrow = 1;
        resetButton.style.height = 30;
        resetButton.style.marginLeft = 5;
        resetButton.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.6f, 0.4f, 0.2f));
        
        buttonContainer.Add(testFadeInButton);
        buttonContainer.Add(resetButton);
        testGroup.Add(buttonContainer);
        
        root.Add(testGroup);
        
        // 创建状态信息区域
        var infoGroup = CreateGroup("状态信息");
        var infoLabel = new Label();
        infoLabel.style.whiteSpace = WhiteSpace.Normal;
        infoLabel.style.color = new StyleColor(new UnityEngine.Color(0.7f, 0.7f, 0.7f));
        
        // 定时更新状态信息
        infoLabel.schedule.Execute(() => 
        {
            var controller = target as ButtonFadeInController;
            if (controller != null)
            {
                int childCount = controller.transform.childCount;
                string status = childCount >= 3 
                    ? $"✓ 检测到 {childCount} 个子对象（前 3 个将被淡入）" 
                    : $"⚠ 子对象不足！当前 {childCount} 个，需要至少 3 个";
                
                infoLabel.text = $"【组件状态】\n{status}\n\n" +
                                $"【工作流程】\n" +
                                $"1. 初始隐藏所有按钮\n" +
                                $"2. 监听点阵效果结束消息\n" +
                                $"3. 发送淡入开始消息\n" +
                                $"4. 依次淡入按钮（间隔 {controller.fadeInDuration:F2}s）\n" +
                                $"5. 发送淡入结束消息";
            }
        }).Every(100);
        
        infoGroup.Add(infoLabel);
        root.Add(infoGroup);
        
        // 创建使用说明区域
        var helpGroup = CreateGroup("使用说明");
        var helpLabel = new Label(
            "【设置步骤】\n" +
            "1. 将此脚本添加到父对象\n" +
            "2. 确保有 3 个子对象（Button）\n" +
            "3. 运行游戏，按钮将在点阵效果结束后自动淡入\n\n" +
            "【注意事项】\n" +
            "• 脚本会自动为按钮添加 CanvasGroup 组件\n" +
            "• 按钮初始状态为完全透明且不可交互\n" +
            "• 使用 DOTween 实现平滑淡入动画\n" +
            "• 可通过右键菜单或 Inspector 按钮测试效果"
        );
        helpLabel.style.whiteSpace = WhiteSpace.Normal;
        helpLabel.style.color = new StyleColor(new UnityEngine.Color(0.8f, 0.8f, 0.6f));
        helpLabel.style.fontSize = 11;
        helpGroup.Add(helpLabel);
        root.Add(helpGroup);
        
        return root;
    }
    
    /// <summary>
    /// 创建分组容器
    /// </summary>
    private VisualElement CreateGroup(string title)
    {
        var group = new VisualElement();
        group.style.marginBottom = 10;
        group.style.paddingTop = 5;
        group.style.paddingBottom = 5;
        group.style.paddingLeft = 10;
        group.style.paddingRight = 10;
        group.style.backgroundColor = new StyleColor(new UnityEngine.Color(0f, 0f, 0f, 0.2f));
        group.style.borderTopLeftRadius = 5;
        group.style.borderTopRightRadius = 5;
        group.style.borderBottomLeftRadius = 5;
        group.style.borderBottomRightRadius = 5;
        
        var titleLabel = new Label(title);
        titleLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
        titleLabel.style.fontSize = 12;
        titleLabel.style.marginBottom = 5;
        titleLabel.style.color = new StyleColor(new UnityEngine.Color(0.8f, 0.9f, 1f));
        group.Add(titleLabel);
        
        return group;
    }
    
    /// <summary>
    /// 创建属性字段
    /// </summary>
    private PropertyField CreatePropertyField(string propertyName, string label)
    {
        var property = serializedObject.FindProperty(propertyName);
        var field = new PropertyField(property, label);
        field.Bind(serializedObject);
        return field;
    }
}
