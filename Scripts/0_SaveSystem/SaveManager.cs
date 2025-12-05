using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.SaveSystem
{
    /// <summary>
    /// 存档管理器 (Save Manager)
    /// <para>单例模式。负责管理存档槽位、执行实际的保存和加载操作。</para>
    /// <para>它是系统与 Easy Save 3 插件之间的主要桥梁。</para>
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        /// <summary>
        /// 全局单例访问点
        /// </summary>
        public static SaveManager Instance { get; private set; }

        [Header("配置 (Configuration)")]
        [Tooltip("游戏允许的最大存档槽位数量。")]
        public int MaxSlots = 4;

        [Header("运行时状态 (Runtime State)")]
        [Tooltip("当前激活的存档槽位索引。-1 表示未选择任何槽位。")]
        [SerializeField] private int _currentSlotIndex = -1;

        /// <summary>
        /// 获取当前选中的槽位索引
        /// </summary>
        public int CurrentSlotIndex => _currentSlotIndex;

        // --- 事件系统 (Events) ---
        
        /// <summary>
        /// 当游戏保存成功时触发
        /// </summary>
        public event Action OnGameSaved;

        /// <summary>
        /// 当游戏加载成功并数据就绪时触发
        /// <para>参数: 加载到的存档数据对象</para>
        /// </summary>
        public event Action<GameSaveData> OnGameLoaded;

        /// <summary>
        /// 当存档被删除时触发
        /// <para>参数: 被删除的槽位索引</para>
        /// </summary>
        public event Action<int> OnSaveDeleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 设置当前激活的存档槽位。
        /// <para>应该在存档选择界面（Save Slot Selection UI）中调用此方法。</para>
        /// </summary>
        /// <param name="index">槽位索引 (0 到 MaxSlots-1)</param>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= MaxSlots)
            {
                Debug.LogError($"[SaveManager] 无效的槽位索引: {index}");
                return;
            }
            _currentSlotIndex = index;
            Debug.Log($"[SaveManager] 已选择槽位 {index} 作为当前活动存档槽。");
        }

        /// <summary>
        /// 在指定槽位创建一个全新的默认存档
        /// </summary>
        public void CreateNewSave(int index)
        {
            string fileName = GetSaveFileName(index);
            GameSaveData defaultData = new GameSaveData();
            
            // 设置一些初始元数据
            defaultData.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            defaultData.SceneName = "3_Teaching level"; // 默认进入教学关
            
            ES3.Save("GameData", defaultData, fileName);
            Debug.Log($"[SaveManager] 在槽位 {index} 创建了新存档。");
            
            // 触发保存事件，通知 UI 刷新
            OnGameSaved?.Invoke();
        }

        /// <summary>
        /// 将当前游戏状态保存到选定的槽位中。
        /// </summary>
        public void SaveGame()
        {
            if (_currentSlotIndex == -1)
            {
                Debug.LogWarning("[SaveManager] 未选择槽位！无法保存。请先选择一个槽位。");
                return;
            }

            string fileName = GetSaveFileName(_currentSlotIndex);
            GameSaveData data = CollectGameData();

            // 使用 ES3 保存整个数据对象
            ES3.Save("GameData", data, fileName);
            
            // 我们选择保存整个对象而不是分散的 Key，以便于后续的数据迁移和管理
            Debug.Log($"[SaveManager] 游戏已保存至 {fileName}");
            
            // 触发保存完成事件
            OnGameSaved?.Invoke();
        }

        /// <summary>
        /// 从当前选定的槽位加载游戏。
        /// </summary>
        public void LoadGame()
        {
            if (_currentSlotIndex == -1)
            {
                Debug.LogWarning("[SaveManager] 未选择槽位！无法加载。");
                return;
            }

            string fileName = GetSaveFileName(_currentSlotIndex);
            if (!ES3.FileExists(fileName))
            {
                Debug.LogWarning($"[SaveManager] 存档文件未找到: {fileName}");
                return;
            }

            try
            {
                if (ES3.KeyExists("GameData", fileName))
                {
                    GameSaveData data = ES3.Load<GameSaveData>("GameData", fileName);
                    ApplyGameData(data);
                    
                    // 触发加载完成事件，传递数据给监听者
                    OnGameLoaded?.Invoke(data);
                    Debug.Log($"[SaveManager] 游戏已从 {fileName} 加载。");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 加载游戏失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取指定槽位的存档数据（仅读取，不应用到游戏）。
        /// <para>用于在 UI 上显示存档详情（如时间、场景等）。</para>
        /// </summary>
        public GameSaveData GetSlotData(int index)
        {
            string fileName = GetSaveFileName(index);
            if (ES3.FileExists(fileName) && ES3.KeyExists("GameData", fileName))
            {
                return ES3.Load<GameSaveData>("GameData", fileName);
            }
            return null;
        }

        /// <summary>
        /// 检查指定槽位是否存在存档文件。
        /// </summary>
        public bool HasSave(int index)
        {
            return ES3.FileExists(GetSaveFileName(index));
        }

        /// <summary>
        /// 删除指定槽位的存档文件。
        /// </summary>
        public void DeleteSave(int index)
        {
            string fileName = GetSaveFileName(index);
            if (ES3.FileExists(fileName))
            {
                ES3.DeleteFile(fileName);
                Debug.Log($"[SaveManager] 已删除存档文件: {fileName}");
                OnSaveDeleted?.Invoke(index);
            }
        }

        /// <summary>
        /// 获取存档文件的标准命名格式
        /// </summary>
        private string GetSaveFileName(int index)
        {
            return $"user_save_{index}.es3";
        }

        /// <summary>
        /// 收集当前游戏状态并封装为 GameSaveData 对象
        /// </summary>
        private GameSaveData CollectGameData()
        {
            GameSaveData data = new GameSaveData();
            
            // 收集元数据
            data.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            data.SceneName = SceneManager.GetActiveScene().name;

            // 收集玩家数据
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                data.PlayerPosition = player.transform.position;
                data.PlayerRotation = player.transform.rotation;
                
                // 占位符：后续在这里获取真实的生命值组件
                // var healthComp = player.GetComponent<HealthComponent>();
                // if (healthComp) data.CurrentHealth = healthComp.Value;
            }
            else
            {
                Debug.LogWarning("[SaveManager] 未找到 'Player' 标签的对象。将保存默认坐标。");
            }

            return data;
        }

        /// <summary>
        /// 将读取到的存档数据应用到游戏世界中
        /// </summary>
        private void ApplyGameData(GameSaveData data)
        {
            // 这里负责将数据还原到游戏对象上
            // 注意：如果是跨场景加载，应该先加载场景，再调用此方法应用数据。
            // 目前假设我们在正确的场景中，或者场景加载由外部逻辑控制。
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = data.PlayerPosition;
                player.transform.rotation = data.PlayerRotation;
                Debug.Log($"[SaveManager] 已恢复玩家位置: {data.PlayerPosition}");
            }
        }
        /// <summary>
        /// 强制通知 UI 刷新
        /// <para>主要用于外部工具（如编辑器仪表盘）修改存档文件后，通知 UI 重新读取显示。</para>
        /// </summary>
        public void ForceRefreshUI()
        {
            OnGameSaved?.Invoke();
        }
    }
}
