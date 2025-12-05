#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Systems.SaveSystem;

namespace Systems.SaveSystem.Editor
{
    /// <summary>
    /// 存档系统仪表盘
    /// <para>提供一个可视化的 Editor 窗口来管理、查看和调试游戏存档。</para>
    /// </summary>
    public class SaveSystemDashboard : OdinMenuEditorWindow
    {
        [MenuItem("自制工具/系统/保存系统/仪表盘")]
        private static void OpenWindow()
        {
            var window = GetWindow<SaveSystemDashboard>();
            window.titleContent = new GUIContent("存档仪表盘");
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Selection.SupportsMultiSelect = false;
            
            // 添加主仪表盘页面
            tree.Add("仪表盘概览", new DashboardViewModel());

            // 槽位列表
            int slotCount = 4;
            if (Application.isPlaying && SaveManager.Instance != null)
            {
                slotCount = SaveManager.Instance.MaxSlots;
            }

            for (int i = 0; i < slotCount; i++)
            {
                tree.Add($"存档槽位/槽位 {i}", new SaveSlotViewModel(i));
            }

            return tree;
        }
    }

    public class DashboardViewModel
    {
        [Title("存档系统概览")]
        [InfoBox("管理 Easy Save 3 (ES3) 的存档文件。\n注意：此面板直接操作文件系统，请谨慎操作。")]
        
        [ShowInInspector, DisplayAsString, LabelText("ES3 默认存储路径")]
        public string SavePath => ES3Settings.defaultSettings.path;

        [Button("删除所有存档", ButtonSizes.Large), GUIColor(1f, 0.4f, 0.4f)]
        public void DeleteAllSaves()
        {
            if (EditorUtility.DisplayDialog("严重警告", 
                "你确定要删除【所有】存档文件 (user_save_*.es3) 吗？\n此操作不可撤销！", 
                "确认删除", "取消"))
            {
                for (int i = 0; i < 20; i++)
                {
                    string fileName = $"user_save_{i}.es3";
                    if (ES3.FileExists(fileName))
                    {
                        ES3.DeleteFile(fileName);
                    }
                }
                Debug.Log("[SaveDashboard] 已删除所有已知的存档文件。");
            }
        }
        
        [Button("打开存档文件夹", ButtonSizes.Medium)]
        public void OpenSaveFolder()
        {
            string path = ES3Settings.defaultSettings.path;
            path = System.IO.Path.GetDirectoryName(path);
            EditorUtility.RevealInFinder(path);
        }
    }

    public class SaveSlotViewModel
    {
        private int _slotIndex;
        private string _fileName;

        // 用于编辑的缓存数据
        [SerializeField]
        private GameSaveData _cachedData;

        public SaveSlotViewModel(int index)
        {
            _slotIndex = index;
            _fileName = $"user_save_{index}.es3";
        }

        [Title("槽位状态")]
        [ShowInInspector, DisplayAsString, LabelText("文件名")]
        public string FileName => _fileName;

        [ShowInInspector, DisplayAsString, LabelText("状态")]
        public string Status => Exists ? "已占用" : "空闲";

        public bool Exists => ES3.FileExists(_fileName);

        // --- 数据编辑板块 ---

        [Title("数据编辑器")]
        [ShowIf("Exists")]
        [BoxGroup("数据详情")]
        [InfoBox("点击【加载/刷新数据】读取最新存档，修改任意属性后点击【保存修改】生效。")]
        
        [ShowInInspector, HideLabel]
        [InlineProperty(LabelWidth = 100)]
        public GameSaveData CachedData
        {
            get
            {
                // 自动加载：如果存在文件但缓存为空，尝试加载
                if (_cachedData == null && Exists) LoadData();
                return _cachedData;
            }
            set => _cachedData = value;
        }

        [BoxGroup("数据详情")]
        [ButtonGroup("数据详情/操作")]
        [Button("加载/刷新数据", ButtonSizes.Medium), ShowIf("Exists")]
        public void LoadData()
        {
            if (Exists && ES3.KeyExists("GameData", _fileName))
            {
                try 
                {
                    _cachedData = ES3.Load<GameSaveData>("GameData", _fileName);
                }
                catch
                {
                    _cachedData = null;
                }
            }
            else
            {
                _cachedData = null;
            }
        }

        [BoxGroup("数据详情")]
        [ButtonGroup("数据详情/操作")]
        [Button("保存修改", ButtonSizes.Medium), ShowIf("Exists"), GUIColor(0.6f, 1f, 0.6f)]
        public void SaveChanges()
        {
            if (_cachedData != null)
            {
                ES3.Save("GameData", _cachedData, _fileName);
                Debug.Log($"[SaveDashboard] 已将修改写入槽位 {_slotIndex}");
                
                // 如果游戏在运行，通知管理器刷新 UI
                if (Application.isPlaying && SaveManager.Instance != null)
                {
                    SaveManager.Instance.ForceRefreshUI();
                }
            }
        }

        // --- 快捷调试工具 ---

        [Title("快捷调试 (Quick Debug)")]
        [ShowIf("Exists")]
        [BoxGroup("快捷修改")]
        
        [Button("生命值: 100 (满)")]
        public void SetFullHealth()
        {
            if (_cachedData == null) LoadData();
            if (_cachedData != null)
            {
                _cachedData.CurrentHealth = 100;
                SaveChanges();
            }
        }

        [Button("生命值: 10 (残)")]
        public void SetLowHealth()
        {
            if (_cachedData == null) LoadData();
            if (_cachedData != null)
            {
                _cachedData.CurrentHealth = 10;
                SaveChanges();
            }
        }

        [Button("生命数 +1")]
        public void AddLife()
        {
            if (_cachedData == null) LoadData();
            if (_cachedData != null)
            {
                _cachedData.CurrentLives++;
                SaveChanges();
            }
        }

        [Button("生命数: 0")]
        public void SetZeroLives()
        {
            if (_cachedData == null) LoadData();
            if (_cachedData != null)
            {
                _cachedData.CurrentLives = 0;
                SaveChanges();
            }
        }

        // --- 基础操作 ---

        [Title("文件操作")]
        [Button("删除此存档"), ShowIf("Exists"), GUIColor(1f, 0.5f, 0.5f)]
        public void Delete()
        {
            if (EditorUtility.DisplayDialog("删除存档", $"确定要删除文件 {_fileName} 吗？", "是", "否"))
            {
                ES3.DeleteFile(_fileName);
                _cachedData = null;
            }
        }

        [Button("生成测试存档"), HideIf("Exists"), GUIColor(0.5f, 1f, 0.5f)]
        public void CreateTestSave()
        {
            var data = new GameSaveData
            {
                SaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                SceneName = "EditorDebugScene",
                PlayerPosition = Vector3.zero,
                PlayerRotation = Quaternion.identity,
                CurrentHealth = 100,
                CurrentLives = 3
            };
            ES3.Save("GameData", data, _fileName);
            LoadData();
        }
    }
}
#endif
