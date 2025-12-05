#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

[CustomEditor(typeof(SceneSystemManager))]
public class SceneSystemManagerEditor : Editor
{
    TextField _targetField;
    TextField _additiveField;
    TextField _setActiveField;

    Toggle _useLoading;
    TextField _loadingName;
    FloatField _minShowTime;
    FloatField _activationDelay;
    Toggle _unloadUnused;
    Toggle _runGc;

    Label _busyLabel;
    Label _queueLabel;
    Label _activeSceneLabel;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        #if ODIN_INSPECTOR
        // Odin 风格标题（IMGUI 嵌入，通过反射适配不同版本）
        var odinHeader = new IMGUIContainer(() =>
        {
            DrawOdinTitle("场景系统管理器", "统一管理场景切换与异步加载、并行加载与队列调度");
        });
        root.Add(odinHeader);
        #endif
        var so = serializedObject;
        var modeProp = so.FindProperty("mode");
        var priorityProp = so.FindProperty("priority");

        var settingsFold = new Foldout { text = "设置", value = true };
        settingsFold.Add(new PropertyField(modeProp, "实例模式"));
        settingsFold.Add(new PropertyField(priorityProp, "优先级"));
        root.Add(settingsFold);

        var statusFold = new Foldout { text = "运行状态", value = false };
        _busyLabel = new Label();
        _queueLabel = new Label();
        _activeSceneLabel = new Label();
        statusFold.Add(_busyLabel);
        statusFold.Add(_queueLabel);
        statusFold.Add(_activeSceneLabel);
        root.Add(statusFold);

        var actionFold = new Foldout { text = "快捷操作", value = false };
        _targetField = new TextField("目标主场景");
        _additiveField = new TextField("叠加场景");
        _setActiveField = new TextField("设为激活的场景");
        actionFold.Add(_targetField);
        actionFold.Add(_additiveField);
        actionFold.Add(_setActiveField);

        var optFold = new Foldout { text = "加载选项", value = false };
        _useLoading = new Toggle("使用加载场景") { value = true };
        _loadingName = new TextField("加载场景名") { value = "SP_LoadingScreen" };
        _minShowTime = new FloatField("最小显示时间(秒)") { value = 0.6f };
        _activationDelay = new FloatField("激活延迟(秒)") { value = 0f };
        _unloadUnused = new Toggle("清理未使用资源") { value = true };
        _runGc = new Toggle("切换后执行GC") { value = true };
        optFold.Add(_useLoading);
        optFold.Add(_loadingName);
        optFold.Add(_minShowTime);
        optFold.Add(_activationDelay);
        optFold.Add(_unloadUnused);
        optFold.Add(_runGc);
        actionFold.Add(optFold);

        var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var btnGoTo = new Button(() => DoGoTo()) { text = "切换" };
        var btnGoToWithLoading = new Button(() => DoGoToWithLoading()) { text = "加载界面切换" };
        var btnReload = new Button(() => DoReload()) { text = "重载当前" };
        row1.Add(btnGoTo);
        row1.Add(btnGoToWithLoading);
        row1.Add(btnReload);
        actionFold.Add(row1);

        var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var btnLoadAdd = new Button(() => DoLoadAdditive()) { text = "加载叠加" };
        var btnUnloadAdd = new Button(() => DoUnloadAdditive()) { text = "卸载叠加" };
        var btnSetActive = new Button(() => DoSetActive()) { text = "设为激活" };
        row2.Add(btnLoadAdd);
        row2.Add(btnUnloadAdd);
        row2.Add(btnSetActive);
        actionFold.Add(row2);

        var row3 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var btnPreload = new Button(() => DoPreload()) { text = "预加载(0.9)" };
        var btnActivatePreloaded = new Button(() => DoActivatePreloaded()) { text = "激活预加载" };
        row3.Add(btnPreload);
        row3.Add(btnActivatePreloaded);
        actionFold.Add(row3);

        root.Add(actionFold);

        actionFold.SetEnabled(EditorApplication.isPlaying);
        root.schedule.Execute(() =>
        {
            var mgr = target as SceneSystemManager;
            if (mgr == null) return;
            _busyLabel.text = $"正忙: {mgr.IsBusy}";
            _queueLabel.text = $"队列长度: {mgr.QueueLength}";
            _activeSceneLabel.text = $"当前激活场景: {mgr.CurrentSceneName}";
            actionFold.SetEnabled(EditorApplication.isPlaying);
        }).Every(200);

        return root;
    }

    SceneLoadOptions BuildOptions()
    {
        return new SceneLoadOptions
        {
            useLoadingScreen = _useLoading.value,
            loadingSceneName = string.IsNullOrEmpty(_loadingName.value) ? "SP_LoadingScreen" : _loadingName.value,
            minShowTime = Mathf.Max(0f, _minShowTime.value),
            activationDelay = Mathf.Max(0f, _activationDelay.value),
            unloadUnusedAssets = _unloadUnused.value,
            runGC = _runGc.value,
            logVerbose = true
        };
    }

    void DoGoTo()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        var opt = BuildOptions();
        mgr.EnqueueGoTo(_targetField.value, opt);
    }

    void DoGoToWithLoading()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        var opt = BuildOptions();
        mgr.EnqueueGoToWithLoading(_targetField.value, string.IsNullOrEmpty(_additiveField.value) ? null : new[] { _additiveField.value }, opt);
    }

    void DoReload()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        mgr.EnqueueReload(BuildOptions());
    }

    void DoLoadAdditive()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        var opt = BuildOptions();
        opt.additive = true;
        mgr.EnqueueLoadAdditive(_additiveField.value, opt);
    }

    void DoUnloadAdditive()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        mgr.EnqueueUnloadAdditive(_additiveField.value);
    }

    void DoSetActive()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        mgr.EnqueueSetActive(string.IsNullOrEmpty(_setActiveField.value) ? _targetField.value : _setActiveField.value);
    }

    void DoPreload()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        mgr.EnqueuePreload(_additiveField.value, null);
    }

    void DoActivatePreloaded()
    {
        var mgr = (SceneSystemManager)target;
        if (mgr == null) return;
        mgr.EnqueueActivatePreloaded(_additiveField.value);
    }

#if ODIN_INSPECTOR
    static void DrawOdinTitle(string title, string subtitle)
    {
        var type = Type.GetType("Sirenix.Utilities.Editor.SirenixEditorGUI, Sirenix.OdinInspector.Editor");
        if (type != null)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            // Prefer 5-arg overload: (string, string, <enum>, bool, bool)
            foreach (var m in methods)
            {
                if (m.Name != "Title") continue;
                var ps = m.GetParameters();
                if (ps.Length == 5 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string) && ps[3].ParameterType == typeof(bool) && ps[4].ParameterType == typeof(bool))
                {
                    var alignType = ps[2].ParameterType; // enum type (unknown location/version)
                    object left = alignType.IsEnum ? Enum.ToObject(alignType, 0) : null; // assume 0 = Left
                    try { m.Invoke(null, new object[] { title, subtitle, left, true, true }); return; } catch { }
                }
            }
            // Fallback 3-arg: (string, string, bool)
            foreach (var m in methods)
            {
                if (m.Name != "Title") continue;
                var ps = m.GetParameters();
                if (ps.Length == 3 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string) && ps[2].ParameterType == typeof(bool))
                {
                    try { m.Invoke(null, new object[] { title, subtitle, true }); return; } catch { }
                }
            }
            // Fallback 2-arg: (string, string)
            foreach (var m in methods)
            {
                if (m.Name != "Title") continue;
                var ps = m.GetParameters();
                if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string))
                {
                    try { m.Invoke(null, new object[] { title, subtitle }); return; } catch { }
                }
            }
        }
        // Ultimate fallback (no Odin or unexpected version)
        GUILayout.Label(title, EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(subtitle)) EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
        EditorGUILayout.Space(4);
    }
#endif
}
#endif
