#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif

public class SceneSequenceToolWindow : EditorWindow
{
    [MenuItem("自制工具/系统/场景系统/场景序列工具")] 
    public static void Open()
    {
        var wnd = GetWindow<SceneSequenceToolWindow>();
        wnd.titleContent = new GUIContent("场景序列工具");
        wnd.minSize = new Vector2(520, 420);
        wnd.Show();
    }

    ObjectField _assetField;
    ObjectField _mainSceneField;
    ListView _listView;
    VisualElement _dropZone;

    Toggle _useLoading;
    TextField _loadingName;
    FloatField _minShow;
    FloatField _activationDelay;
    Toggle _unloadUnused;
    Toggle _runGc;

    readonly List<SceneAsset> _additiveAssets = new List<SceneAsset>();

    void CreateGUI()
    {
        var root = rootVisualElement;
        #if ODIN_INSPECTOR
        var odinHeader = new IMGUIContainer(() =>
        {
            DrawOdinTitle("场景序列工具", "通过拖拽配置主/叠加场景及加载选项");
        });
        root.Add(odinHeader);
        #endif

        // Top bar: asset field + buttons
        var top = new VisualElement();
        top.style.flexDirection = FlexDirection.Row;
        top.style.marginBottom = 4;
        _assetField = new ObjectField("序列资产") { objectType = typeof(SceneLoadSequenceAsset) };
        top.Add(_assetField);
        var btnNew = new Button(CreateNewAsset) { text = "新建" };
        var btnLoad = new Button(LoadFromAsset) { text = "加载" };
        var btnSave = new Button(SaveToAsset) { text = "保存" };
        top.Add(btnNew); top.Add(btnLoad); top.Add(btnSave);
        root.Add(top);

        // Main scene selector
        _mainSceneField = new ObjectField("主场景") { objectType = typeof(SceneAsset) };
        root.Add(_mainSceneField);

        // Drop zone
        _dropZone = new VisualElement();
        _dropZone.style.height = 40;
        _dropZone.style.marginTop = 6;
        _dropZone.style.borderTopWidth = 1;
        _dropZone.style.borderBottomWidth = 1;
        _dropZone.style.borderLeftWidth = 1;
        _dropZone.style.borderRightWidth = 1;
        _dropZone.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f);
        _dropZone.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);
        _dropZone.style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f);
        _dropZone.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f);
        _dropZone.style.alignItems = Align.Center;
        _dropZone.style.justifyContent = Justify.Center;
        _dropZone.Add(new Label("将 SceneAsset 拖到此处添加"));
        _dropZone.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        _dropZone.RegisterCallback<DragPerformEvent>(OnDragPerform);
        root.Add(_dropZone);

        // ListView of additive scenes
        _listView = new ListView(_additiveAssets, itemHeight: 22, makeItem: () => new Label(), bindItem: (e, i) =>
        {
            var label = (Label)e;
            var sa = _additiveAssets[i];
            label.text = sa ? sa.name : "<Missing>";
        })
        {
            selectionType = SelectionType.Multiple,
            reorderable = true
        };
        root.Add(_listView);

        var listBtns = new VisualElement();
        listBtns.style.flexDirection = FlexDirection.Row;
        listBtns.style.marginTop = 4;
        listBtns.Add(new Button(AddScenePick) { text = "+ 添加" });
        listBtns.Add(new Button(RemoveSelected) { text = "- 移除" });
        listBtns.Add(new Button(ClearAll) { text = "清空" });
        root.Add(listBtns);

        // Options
        var optFold = new Foldout { text = "加载选项", value = true };
        _useLoading = new Toggle("使用加载场景") { value = true };
        _loadingName = new TextField("加载场景名") { value = "SP_LoadingScreen" };
        _minShow = new FloatField("最小显示时间(秒)") { value = 0.6f };
        _activationDelay = new FloatField("激活延迟(秒)") { value = 0f };
        _unloadUnused = new Toggle("清理未使用资源") { value = true };
        _runGc = new Toggle("切换后执行GC") { value = true };
        optFold.Add(_useLoading);
        optFold.Add(_loadingName);
        optFold.Add(_minShow);
        optFold.Add(_activationDelay);
        optFold.Add(_unloadUnused);
        optFold.Add(_runGc);
        root.Add(optFold);

        // Run buttons
        var runRow = new VisualElement();
        runRow.style.flexDirection = FlexDirection.Row;
        runRow.style.marginTop = 8;
        var btnRun = new Button(RunGoToWithLoading) { text = "执行 带加载界面切换" };
        var btnRunAdditives = new Button(RunLoadAdditives) { text = "执行 仅加载叠加" };
        runRow.Add(btnRun);
        runRow.Add(btnRunAdditives);
        root.Add(runRow);

        var hint = new HelpBox("仅在播放模式下可执行运行操作。", HelpBoxMessageType.Info);
        root.Add(hint);

        // Status updater
        root.schedule.Execute(() =>
        {
            btnRun.SetEnabled(EditorApplication.isPlaying);
            btnRunAdditives.SetEnabled(EditorApplication.isPlaying);
        }).Every(200);
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

    void OnDragUpdate(DragUpdatedEvent evt)
    {
        if (HasSceneAssets(DragAndDrop.objectReferences))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.StopPropagation();
        }
    }

    void OnDragPerform(DragPerformEvent evt)
    {
        if (!HasSceneAssets(DragAndDrop.objectReferences)) return;
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (obj is SceneAsset sa && !_additiveAssets.Contains(sa))
                _additiveAssets.Add(sa);
        }
        _listView.Rebuild();
        evt.StopPropagation();
    }

    bool HasSceneAssets(UnityEngine.Object[] objs)
    {
        foreach (var o in objs) if (o is SceneAsset) return true;
        return false;
    }

    void AddScenePick()
    {
        var path = EditorUtility.OpenFilePanel("Add Scene", Application.dataPath, "unity");
        if (!string.IsNullOrEmpty(path))
        {
            var rel = ToProjectRelativePath(path);
            var sa = AssetDatabase.LoadAssetAtPath<SceneAsset>(rel);
            if (sa && !_additiveAssets.Contains(sa)) { _additiveAssets.Add(sa); _listView.Rebuild(); }
        }
    }

    void RemoveSelected()
    {
        var indices = new List<int>(_listView.selectedIndices);
        indices.Sort();
        indices.Reverse();
        foreach (var i in indices)
        {
            if (i >= 0 && i < _additiveAssets.Count) _additiveAssets.RemoveAt(i);
        }
        _listView.Rebuild();
    }

    void ClearAll()
    {
        _additiveAssets.Clear();
        _listView.Rebuild();
    }

    void CreateNewAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject("Create Scene Load Sequence", "SceneLoadSequence", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        var asset = ScriptableObject.CreateInstance<SceneLoadSequenceAsset>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        _assetField.value = asset;
    }

    void LoadFromAsset()
    {
        var asset = _assetField.value as SceneLoadSequenceAsset;
        if (!asset) return;

        // main
        _mainSceneField.value = FindSceneAssetByName(asset.mainScene);
        // list
        _additiveAssets.Clear();
        if (asset.additiveScenes != null)
        {
            foreach (var n in asset.additiveScenes)
            {
                var sa = FindSceneAssetByName(n);
                if (sa) _additiveAssets.Add(sa);
            }
        }
        _listView.Rebuild();

        // options
        _useLoading.value = asset.useLoadingScreen;
        _loadingName.value = asset.loadingSceneName;
        _minShow.value = asset.minShowTime;
        _activationDelay.value = asset.activationDelay;
        _unloadUnused.value = asset.unloadUnusedAssets;
        _runGc.value = asset.runGC;
    }

    void SaveToAsset()
    {
        var asset = _assetField.value as SceneLoadSequenceAsset;
        if (!asset)
        {
            CreateNewAsset();
            asset = _assetField.value as SceneLoadSequenceAsset;
            if (!asset) return;
        }

        asset.mainScene = GetSceneName(_mainSceneField.value as SceneAsset);
        asset.additiveScenes = new List<string>();
        foreach (var sa in _additiveAssets) asset.additiveScenes.Add(GetSceneName(sa));
        asset.useLoadingScreen = _useLoading.value;
        asset.loadingSceneName = string.IsNullOrEmpty(_loadingName.value) ? "SP_LoadingScreen" : _loadingName.value;
        asset.minShowTime = Mathf.Max(0f, _minShow.value);
        asset.activationDelay = Mathf.Max(0f, _activationDelay.value);
        asset.unloadUnusedAssets = _unloadUnused.value;
        asset.runGC = _runGc.value;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    void RunGoToWithLoading()
    {
        if (!EditorApplication.isPlaying) { ShowPlayModeTip(); return; }
        var main = GetSceneName(_mainSceneField.value as SceneAsset);
        var adds = new List<string>();
        foreach (var sa in _additiveAssets) adds.Add(GetSceneName(sa));
        var opt = new SceneLoadOptions
        {
            useLoadingScreen = _useLoading.value,
            loadingSceneName = string.IsNullOrEmpty(_loadingName.value) ? "SP_LoadingScreen" : _loadingName.value,
            minShowTime = Mathf.Max(0f, _minShow.value),
            activationDelay = Mathf.Max(0f, _activationDelay.value),
            unloadUnusedAssets = _unloadUnused.value,
            runGC = _runGc.value,
            logVerbose = true
        };
        ScenesSystemAPI.GoToWithLoading(main, adds, opt);
    }

    void RunLoadAdditives()
    {
        if (!EditorApplication.isPlaying) { ShowPlayModeTip(); return; }
        var opt = new SceneLoadOptions { additive = true, allowSceneActivation = true };
        foreach (var sa in _additiveAssets)
        {
            var n = GetSceneName(sa);
            ScenesSystemAPI.LoadAdditive(n, opt);
        }
    }

    void ShowPlayModeTip()
    {
        EditorUtility.DisplayDialog("Scene System", "Please enter Play Mode to run scene operations.", "OK");
    }

    string GetSceneName(SceneAsset sa)
    {
        if (!sa) return string.Empty;
        var path = AssetDatabase.GetAssetPath(sa);
        return Path.GetFileNameWithoutExtension(path);
    }

    SceneAsset FindSceneAssetByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        var guids = AssetDatabase.FindAssets("t:Scene " + sceneName);
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == sceneName)
            {
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(p);
            }
        }
        return null;
    }

    string ToProjectRelativePath(string path)
    {
        if (path.StartsWith(Application.dataPath))
            return "Assets" + path.Substring(Application.dataPath.Length);
        return path;
    }
}
#endif
