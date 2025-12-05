using UnityEngine;
using UnityEngine.UI;

namespace Systems.SaveSystem.UI
{
    /// <summary>
    /// 存档槽位控制器 (Controller)
    /// <para>挂载在槽位根节点（Button）上，负责逻辑控制和交互。</para>
    /// <para>协调 SaveManager 和 SaveSlotView。</para>
    /// </summary>
    public class SaveSlotController : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("此槽位对应的索引 (0, 1, 2...)")]
        public int slotIndex = 0;

        [Header("引用")]
        [Tooltip("负责显示的 View 组件（可位于子节点）")]
        public SaveSlotView view;

        [Tooltip("删除按钮（可选）")]
        public Button deleteButton;

        [Header("调试")]
        public bool refreshOnEnable = true;

        private void Start()
        {
            // 自动查找 View
            if (view == null) view = GetComponentInChildren<SaveSlotView>();

            // 绑定主按钮点击
            Button selfButton = GetComponent<Button>();
            if (selfButton != null)
            {
                selfButton.onClick.RemoveAllListeners();
                selfButton.onClick.AddListener(OnSlotClicked);
            }

            // 绑定删除按钮
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }

            // 监听全局事件
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameSaved += OnGlobalSaveChanged;
                SaveManager.Instance.OnSaveDeleted += OnGlobalSaveDeleted;
            }
            
            // 再次尝试刷新，以防 OnEnable 执行时 SaveManager 尚未初始化
            RefreshState();
        }

        private void OnDestroy()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameSaved -= OnGlobalSaveChanged;
                SaveManager.Instance.OnSaveDeleted -= OnGlobalSaveDeleted;
            }
        }

        private void OnEnable()
        {
            if (refreshOnEnable) RefreshState();
        }

        // 事件回调
        private void OnGlobalSaveChanged() => RefreshState();
        private void OnGlobalSaveDeleted(int index) { if (index == slotIndex) RefreshState(); }

        /// <summary>
        /// 刷新当前槽位状态
        /// </summary>
        public void RefreshState()
        {
            if (view == null) return;

            // 如果 SaveManager 不存在（比如直接在场景中测试），默认为空状态
            if (SaveManager.Instance == null)
            {
                // Debug.LogWarning($"[SaveSlotController] SaveManager not found. Defaulting to empty state for Slot {slotIndex}.");
                view.ShowEmpty();
                if (deleteButton != null) deleteButton.gameObject.SetActive(false);
                return;
            }

            GameSaveData data = SaveManager.Instance.GetSlotData(slotIndex);
            if (data != null)
            {
                view.ShowSaveContent(data);
                if (deleteButton != null) deleteButton.gameObject.SetActive(true);
            }
            else
            {
                view.ShowEmpty();
                if (deleteButton != null) deleteButton.gameObject.SetActive(false); // 空档隐藏删除按钮
            }
        }

        /// <summary>
        /// 槽位点击逻辑
        /// </summary>
        public void OnSlotClicked()
        {
            if (SaveManager.Instance == null) return;

            if (!SaveManager.Instance.HasSave(slotIndex))
            {
                Debug.Log($"[SaveSlot] 槽位 {slotIndex} 无存档，创建新存档并进入游戏...");
                SaveManager.Instance.CreateNewSave(slotIndex);
                
                // 创建完存档后，立即加载
                // 注意：这里假设 CreateNewSave 已经生成了数据，我们可以直接 SelectSlot 然后加载
                SaveManager.Instance.SelectSlot(slotIndex);
                EnterGame(slotIndex);
            }
            else
            {
                Debug.Log($"[SaveSlot] 选中槽位 {slotIndex}，加载游戏...");
                SaveManager.Instance.SelectSlot(slotIndex);
                EnterGame(slotIndex);
            }
        }

        /// <summary>
        /// 进入游戏逻辑
        /// </summary>
        private void EnterGame(int index)
        {
            GameSaveData data = SaveManager.Instance.GetSlotData(index);
            if (data != null && !string.IsNullOrEmpty(data.SceneName))
            {
                // 使用 Loading 界面跳转
                // 需要确保 ScenesSystemAPI 可用
                var opt = new SceneLoadOptions 
                { 
                    useLoadingScreen = true,
                    loadingSceneName = "SP_LoadingScreen", // 确保这里与你的 Loading 场景名一致
                    minShowTime = 3.0f // 配合倒计时
                };
                
                // 如果你需要保留 2_Save 场景作为 Additive，或者卸载它，通常是跳转到一个新的全屏场景
                // 这里假设是全屏跳转
                ScenesSystemAPI.GoToWithLoading(data.SceneName, null, opt);
            }
            else
            {
                Debug.LogError($"[SaveSlot] 槽位 {index} 数据异常或场景名为空！");
            }
        }

        /// <summary>
        /// 删除点击逻辑
        /// </summary>
        public void OnDeleteClicked()
        {
            if (SaveManager.Instance == null) return;

            if (SaveManager.Instance.HasSave(slotIndex))
            {
                // TODO: 建议添加确认弹窗
                SaveManager.Instance.DeleteSave(slotIndex);
            }
        }
    }
}
