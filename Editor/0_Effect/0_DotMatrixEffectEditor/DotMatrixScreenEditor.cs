using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(DotMatrixScreen))]
public class DotMatrixScreenEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        var matrix = target as DotMatrixScreen;

        // 状态
        var status = CreateGroup("状态");
        var hasCanvas = matrix.GetComponentInParent<Canvas>() != null;
        var canvasLabel = new Label(hasCanvas ? "✓ 已在 Canvas 下" : "✗ 未在 Canvas 下（必须）");
        canvasLabel.style.color = hasCanvas ? new StyleColor(new Color(0.3f, 1f, 0.3f)) : new StyleColor(new Color(1f, 0.3f, 0.3f));
        status.Add(canvasLabel);

        var so = serializedObject;
        var prefabProp = so.FindProperty("dotPrefab");
        var hasPrefab = prefabProp != null && prefabProp.objectReferenceValue != null;
        var prefabLabel = new Label(hasPrefab ? "✓ 点预制体已设置" : "✗ 点预制体未设置");
        prefabLabel.style.color = hasPrefab ? new StyleColor(new Color(0.3f, 1f, 0.3f)) : new StyleColor(new Color(1f, 0.8f, 0.3f));
        status.Add(prefabLabel);

        // 检查 CustomPattern 相关状态
        var effectTypeProp = so.FindProperty("effectType");
        var customPatternProp = so.FindProperty("customPattern");
        if (effectTypeProp != null && effectTypeProp.enumValueIndex == (int)DotMatrixScreen.EffectType.CustomPattern)
        {
            var hasCustomPattern = customPatternProp != null && customPatternProp.objectReferenceValue != null;
            var patternLabel = new Label(hasCustomPattern ? "✓ 自定义图案已设置" : "✗ 自定义图案未设置(必须)");
            patternLabel.style.color = hasCustomPattern ? new StyleColor(new Color(0.3f, 1f, 0.3f)) : new StyleColor(new Color(1f, 0.3f, 0.3f));
            status.Add(patternLabel);
        }

        root.Add(status);

        // 点阵设置
        var dotGroup = CreateGroup("点阵设置");
        dotGroup.Add(CreatePropertyField("dotPrefab", "点预制体"));
        dotGroup.Add(CreatePropertyField("dotSize", "点大小"));
        dotGroup.Add(CreatePropertyField("dotSpacing", "点间距(参考)"));
        root.Add(dotGroup);

        // 颜色
        var colorGroup = CreateGroup("颜色");
        colorGroup.Add(CreatePropertyField("dotOnColor", "点亮颜色"));
        colorGroup.Add(CreatePropertyField("dotOffColor", "熄灭颜色"));
        root.Add(colorGroup);

        // 播放设置
        var playGroup = CreateGroup("播放设置");
        playGroup.Add(CreatePropertyField("autoPlay", "自动播放"));
        
        var effectTypeField = CreatePropertyField("effectType", "特效类型");
        playGroup.Add(effectTypeField);

        // 自定义图案字段（条件显示）
        var customPatternField = CreatePropertyField("customPattern", "自定义图案");
        customPatternField.style.backgroundColor = new StyleColor(new Color(0.1f, 0.3f, 0.4f, 0.3f));
        customPatternField.style.paddingTop = 4;
        customPatternField.style.paddingBottom = 4;
        customPatternField.style.paddingLeft = 4;
        customPatternField.style.paddingRight = 4;
        customPatternField.style.borderTopLeftRadius = 3;
        customPatternField.style.borderTopRightRadius = 3;
        customPatternField.style.borderBottomLeftRadius = 3;
        customPatternField.style.borderBottomRightRadius = 3;
        customPatternField.style.marginTop = 4;
        customPatternField.style.marginBottom = 4;

        // 根据 effectType 显示/隐藏 customPattern
        void UpdateCustomPatternVisibility()
        {
            var currentType = (DotMatrixScreen.EffectType)effectTypeProp.enumValueIndex;
            customPatternField.style.display = currentType == DotMatrixScreen.EffectType.CustomPattern 
                ? DisplayStyle.Flex 
                : DisplayStyle.None;
        }

        UpdateCustomPatternVisibility();
        effectTypeField.RegisterValueChangeCallback(evt => UpdateCustomPatternVisibility());
        
        playGroup.Add(customPatternField);
        playGroup.Add(CreatePropertyField("effectSpeed", "特效速度"));
        playGroup.Add(CreatePropertyField("playTimes", "播放次数"));
        root.Add(playGroup);

        // 控制按钮
        var controls = CreateGroup("控制");
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        var buildBtn = new Button(() =>
        {
            if (Application.isPlaying)
            {
                Debug.Log("[Editor] 运行时无需生成点阵，进入场景时会自动构建");
                return;
            }
            if (!hasCanvas)
            {
                Debug.LogError("[Editor] 需要将对象置于 Canvas 下");
                return;
            }
            if (!hasPrefab)
            {
                Debug.LogError("[Editor] 请先设置点预制体");
                return;
            }
            var method = typeof(DotMatrixScreen).GetMethod("BuildGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(matrix, null);
            Debug.Log("[Editor] 点阵已生成/重建");
        }) { text = "生成/重建 点阵(编辑器)" };
        buildBtn.style.flexGrow = 1;
        row.Add(buildBtn);

        var clearBtn = new Button(() =>
        {
            if (Application.isPlaying)
            {
                var clear = typeof(DotMatrixScreen).GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                clear?.Invoke(matrix, null);
                return;
            }
            var t = matrix.transform.Find("DotScreenContainer");
            if (t != null) DestroyImmediate(t.gameObject);
            Debug.Log("[Editor] 已清除点阵");
        }) { text = "清除点阵" };
        clearBtn.style.flexGrow = 1;
        clearBtn.style.marginLeft = 6;
        row.Add(clearBtn);

        controls.Add(row);
        root.Add(controls);

        // 特效测试
        var effects = CreateGroup("特效测试(运行时)");
        var names = System.Enum.GetNames(typeof(DotMatrixScreen.EffectType));
        for (int i = 0; i < names.Length; i += 2)
        {
            var r = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            AddEffectButton(r, names[i]);
            if (i + 1 < names.Length) AddEffectButton(r, names[i + 1]);
            effects.Add(r);
        }
        root.Add(effects);

        return root;
    }

    void AddEffectButton(VisualElement row, string enumName)
    {
        var btn = new Button(() =>
        {
            var matrix = target as DotMatrixScreen;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Editor] 运行时才可播放特效");
                return;
            }
            var type = (DotMatrixScreen.EffectType)System.Enum.Parse(typeof(DotMatrixScreen.EffectType), enumName);
            matrix.PlayEffect(type);
        }) { text = GetDisplayName(enumName) };
        btn.style.flexGrow = 1;
        btn.style.height = 28;
        btn.style.marginRight = 6;
        btn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.8f));
        row.Add(btn);
    }

    string GetDisplayName(string name)
    {
        switch (name)
        {
            case "ScanLine": return "扫描线";
            case "Ripple": return "波纹";
            case "RandomBlink": return "随机闪烁";
            case "Pulse": return "脉冲";
            case "Wave": return "波浪";
            case "MatrixRain": return "矩阵雨";
            case "Glitch": return "故障";
            case "IsometricCube": return "立方体";
            case "CustomPattern": return "自定义图案";
            case "CountdownBar": return "倒计进度条";
            default: return name;
        }
    }

    VisualElement CreateGroup(string title)
    {
        var g = new VisualElement();
        g.style.marginBottom = 8;
        g.style.paddingLeft = 8;
        g.style.paddingRight = 8;
        g.style.paddingTop = 6;
        g.style.paddingBottom = 6;
        g.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.15f));
        g.style.borderTopLeftRadius = 4;
        g.style.borderTopRightRadius = 4;
        g.style.borderBottomLeftRadius = 4;
        g.style.borderBottomRightRadius = 4;
        var label = new Label(title);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 4;
        label.style.color = new StyleColor(new Color(0.85f, 0.95f, 1f));
        g.Add(label);
        return g;
    }

    PropertyField CreatePropertyField(string name, string label)
    {
        var p = serializedObject.FindProperty(name);
        var f = new PropertyField(p, label);
        f.Bind(serializedObject);
        return f;
    }
}
