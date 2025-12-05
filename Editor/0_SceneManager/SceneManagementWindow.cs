using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.IO;
using System.Linq;

/// <summary>
/// 场景管理系统统一管理界面
/// 左：场景排序 | 中：消息预览 | 右：操作工具
/// </summary>
public class SceneManagementWindow : OdinEditorWindow
{
    [MenuItem("自制工具/系统/场景系统/场景管理/统一管理面板")]
    private static void OpenWindow()
    {
        var window = GetWindow<SceneManagementWindow>();
        window.titleContent = new GUIContent("场景管理中心");
        // window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1000, 600);
        // 手动居中
        var main = EditorGUIUtility.GetMainWindowPosition();
        var pos = window.position;
        float w = 1000, h = 600;
        float x = main.x + (main.width - w) * 0.5f;
        float y = main.y + (main.height - h) * 0.5f;
        window.position = new Rect(x, y, w, h);
        window.Show();
    }

    [PropertyOrder(-10)]
    [OnInspectorInit]
    private void CreateData()
    {
        // 尝试加载配置文件，如果不存在则提示创建
        if (data == null)
        {
            data = AssetDatabase.LoadAssetAtPath<SceneManagerSO>(DataPath);
            if (data == null)
            {
                // 自动创建数据文件
                string dir = Path.GetDirectoryName(DataPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                data = ScriptableObject.CreateInstance<SceneManagerSO>();
                AssetDatabase.CreateAsset(data, DataPath);
                AssetDatabase.SaveAssets();
            }
        }
    }

    // 数据文件路径
    private const string DataPath = "Assets/Resources/0_ScriptableObjects/SceneManagerSO/SceneManagerData.asset";

    [HideInInspector]
    public SceneManagerSO data;

    // ==================== 布局定义 ====================

    [HorizontalGroup("Split", Width = 0.35f, LabelWidth = 100)]
    [BoxGroup("Split/Left", LabelText = "📚 场景清单", ShowLabel = true)]
    [GUIColor(0.95f, 0.95f, 1f)] // 微淡蓝背景
    [InfoBox("拖拽列表项可调整 Build Index 顺序", InfoMessageType.None)]
    [PropertySpace(10)]
    [ListDrawerSettings(
        ShowIndexLabels = true, 
        DraggableItems = true, 
        OnTitleBarGUI = "DrawRefreshButton", 
        CustomRemoveIndexFunction = "RemoveScene", 
        CustomAddFunction = "AddEmptyScene",
        ElementColor = "GetElementColor",
        ListElementLabelName = "sceneName"
    )]
    [ShowInInspector]
    [LabelText(" ")]
    private System.Collections.Generic.List<SceneConfigData> SceneList
    {
        get => data ? data.scenes : null;
        set { if (data) data.scenes = value; }
    }

    [HorizontalGroup("Split", Width = 0.4f)]
    [BoxGroup("Split/Middle", LabelText = "💻 代码预览", ShowLabel = true)]
    [GUIColor(1f, 1f, 1f)]
    [PropertySpace(10)]
    [ShowInInspector]
    [HideLabel]
    [HideReferenceObjectPicker]
    private MessageCodePreview codePreview;

    [HorizontalGroup("Split", Width = 0.25f)]
    [BoxGroup("Split/Right", LabelText = "🔧 工具箱", ShowLabel = true)]
    [GUIColor(1f, 1f, 1f)]
    [PropertySpace(10)]
    [ShowInInspector]
    [HideLabel]
    private SceneOperations operations;

    // ==================== 初始化 ====================

    protected override void OnEnable()
    {
        base.OnEnable();
        CreateData();
        
        if (codePreview == null) codePreview = new MessageCodePreview(this);
        if (operations == null) operations = new SceneOperations(this);
    }

    protected override void OnImGUI()
    {
        base.OnImGUI();
    }

    // ==================== 左侧列表方法 ====================

    private Color GetElementColor(int index, Color defaultColor)
    {
        // 深色主题下的交替色：深灰蓝 vs 深灰
        return index % 2 == 0 
            ? new Color(0.22f, 0.22f, 0.24f, 1f) 
            : new Color(0.18f, 0.18f, 0.20f, 1f);
    }

    private void DrawRefreshButton()
    {
        if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
        {
            if (data != null)
            {
                data.SyncFromBuildSettings();
                EditorUtility.SetDirty(data);
            }
        }
    }
    
    // ... (RemoveScene and AddEmptyScene remain the same)

    private void RemoveScene(int index)
    {
        if (data != null && index >= 0 && index < data.scenes.Count)
        {
            data.scenes.RemoveAt(index);
            EditorUtility.SetDirty(data);
        }
    }

    private void AddEmptyScene()
    {
        if (data != null)
        {
            data.scenes.Add(new SceneConfigData { sceneName = "New Scene", scenePath = "" });
            EditorUtility.SetDirty(data);
        }
    }

    // ==================== 内部类：代码预览 ====================
    [System.Serializable]
    public class MessageCodePreview
    {
        private SceneManagementWindow _window;

        [TitleGroup("配置区")]
        [LabelText("选中场景")]
        [ValueDropdown("GetSceneNames")]
        [OnValueChanged("UpdateCode")]
        [GUIColor(0.6f, 0.8f, 1f)] // 淡蓝色高亮
        public string selectedScene;

        [TitleGroup("预览区")]
        [TextArea(18, 20)]
        [HideLabel]
        [ReadOnly]
        [GUIColor(0.15f, 0.15f, 0.15f)] // 深黑背景，模拟代码编辑器
        public string codeOutput;

        public MessageCodePreview(SceneManagementWindow window)
        {
            _window = window;
            UpdateCode();
        }
        
        // ... (GetSceneNames and UpdateCode remain the same)

        private System.Collections.Generic.IEnumerable<string> GetSceneNames()
        {
            if (_window.data == null) return null;
            return _window.data.scenes.Select(s => s.sceneName);
        }

        private void UpdateCode()
        {
            if (_window.data == null || string.IsNullOrEmpty(selectedScene))
            {
                codeOutput = "// 请在上方选择一个场景...";
                return;
            }

            var config = _window.data.scenes.FirstOrDefault(s => s.sceneName == selectedScene);
            if (config == null) return;

            string boolStr = config.useLoadingScreen.ToString().ToLower();
            
            codeOutput = $@"// 场景切换调用示例：
// 目标场景: {config.sceneName}

// 1. 构建请求数据
var request = new SceneChangeRequest(
    targetScene: ""{config.sceneName}"",
    useLoading: {boolStr},
    loadingScene: ""{config.loadingSceneName}"",
    minLoadingTime: {config.minLoadingTime}f
);

// 2. 发送切换消息
MessageManager.Instance.Send(MessageDefine.SCENE_CHANGE_REQUEST, request);";
        }

        [Button("复制代码", ButtonSizes.Large, Icon = SdfIconType.Clipboard)]
        [GUIColor(0.4f, 0.9f, 0.4f)] // 鲜艳的绿色
        [PropertySpace(15)]
        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = codeOutput;
            Debug.Log($"<color=#00FF00><b>[SceneManager]</b></color> 代码已复制！");
        }
    }

    // ==================== 内部类：操作工具 ====================
    [System.Serializable]
    public class SceneOperations
    {
        private SceneManagementWindow _window;

        [Title("新建场景", "快速创建一个新场景并注册")]
        [BoxGroup("Create", ShowLabel = false)]
        [LabelText("名称")]
        public string newSceneName = "NewScene";

        public SceneOperations(SceneManagementWindow window)
        {
            _window = window;
        }

        [BoxGroup("Create")]
        [Button("创建场景", ButtonSizes.Medium, Icon = SdfIconType.Plus)]
        [GUIColor(0.2f, 0.8f, 0.6f)] // 青绿色 (Teal)
        [PropertySpace(5)]
        private void CreateNewScene()
        {
            if (string.IsNullOrEmpty(newSceneName)) return;

            string path = $"Assets/Scenes/{newSceneName}.unity";
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("错误", $"场景 {path} 已存在！", "确定");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, path);
            
            Debug.Log($"[SceneManager] 创建新场景: {path}");
            AddSceneToBuildSettings(path);
            if (_window.data) _window.data.SyncFromBuildSettings();
        }

        [Title("同步设置", "管理 Unity Build Settings")]
        [BoxGroup("Sync", ShowLabel = false)]
        [Button("应用排序", ButtonSizes.Medium, Icon = SdfIconType.Recycle)]
        [GUIColor(1f, 0.6f, 0.2f)] // 橙色
        [Tooltip("将左侧列表的顺序应用到 Unity Build Settings")]
        private void ApplySortToBuildSettings()
        {
            if (_window.data == null) return;

            var newSettings = new EditorBuildSettingsScene[_window.data.scenes.Count];
            for (int i = 0; i < _window.data.scenes.Count; i++)
            {
                var config = _window.data.scenes[i];
                newSettings[i] = new EditorBuildSettingsScene(config.scenePath, true);
            }

            EditorBuildSettings.scenes = newSettings;
            Debug.Log("[SceneManager] 已更新 EditorBuildSettings 场景列表");
        }

        [BoxGroup("Sync")]
        [Button("清空配置", ButtonSizes.Small, Icon = SdfIconType.Trash)]
        [GUIColor(1f, 0.3f, 0.3f)] // 红色
        private void ClearBuildSettings()
        {
            if (EditorUtility.DisplayDialog("警告", "确定要清空 Build Settings 中的所有场景吗？", "确定清空", "取消"))
            {
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
                if (_window.data) _window.data.SyncFromBuildSettings();
            }
        }
        
        // ... (AddSceneToBuildSettings remains the same)

        private void AddSceneToBuildSettings(string path)
        {
            var original = EditorBuildSettings.scenes;
            var newSettings = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, newSettings, original.Length);
            newSettings[newSettings.Length - 1] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = newSettings;
        }
    }
}
