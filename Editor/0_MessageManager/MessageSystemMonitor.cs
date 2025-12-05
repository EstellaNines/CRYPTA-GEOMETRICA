using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 消息系统监控工具
/// 实时监控和记录消息系统的消息传递
/// </summary>
public class MessageSystemMonitor : EditorWindow
{
    #region 静态成员
    
    private static MessageSystemMonitor instance;
    private static List<MessageRecord> messageHistory = new List<MessageRecord>();
    private static MessageStatistics statistics = new MessageStatistics();
    private static bool isMonitoring = true;
    private static int maxHistoryCount = 1000;
    
    #endregion
    
    #region UI 元素
    
    private ScrollView historyScrollView;
    private Label totalMessagesLabel;
    private Label monitoringStatusLabel;
    private VisualElement statisticsContainer;
    private Button clearButton;
    private Button pauseButton;
    private TextField filterTextField;
    private DropdownField maxHistoryDropdown;
    
    #endregion
    
    #region 过滤和显示
    
    private string currentFilter = "";
    private bool autoScroll = true;
    
    #endregion
    
    #region 窗口创建
    
    [MenuItem("自制工具/消息系统/消息系统监控器", false, 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<MessageSystemMonitor>("消息系统监控");
        window.minSize = new Vector2(800, 600);
        instance = window;
    }
    
    #endregion
    
    #region Unity 生命周期
    
    void OnEnable()
    {
        // 订阅消息代理事件
        MessageMonitorProxy.OnMessageSent = RecordMessageFromProxy;
    }
    
    public void CreateGUI()
    {
        // 创建根容器
        var root = rootVisualElement;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        
        // 创建标题
        CreateHeader(root);
        
        // 创建工具栏
        CreateToolbar(root);
        
        // 创建统计信息区域
        CreateStatisticsPanel(root);
        
        // 创建消息历史区域
        CreateHistoryPanel(root);
        
        // 启动定时刷新
        root.schedule.Execute(RefreshUI).Every(100);
    }
    
    void OnDisable()
    {
        // 取消订阅消息代理事件
        if (MessageMonitorProxy.OnMessageSent == RecordMessageFromProxy)
        {
            MessageMonitorProxy.OnMessageSent = null;
        }
    }
    
    void OnDestroy()
    {
        instance = null;
    }
    
    #endregion
    
    #region UI 创建方法
    
    private void CreateHeader(VisualElement root)
    {
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 10;
        header.style.paddingBottom = 10;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        
        var titleLabel = new Label("消息系统监控器");
        titleLabel.style.fontSize = 18;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        monitoringStatusLabel = new Label(isMonitoring ? "● 监控中" : "○ 已暂停");
        monitoringStatusLabel.style.color = isMonitoring ? Color.green : Color.gray;
        monitoringStatusLabel.style.fontSize = 14;
        
        header.Add(titleLabel);
        header.Add(monitoringStatusLabel);
        
        root.Add(header);
    }
    
    private void CreateToolbar(VisualElement root)
    {
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.marginBottom = 10;
        toolbar.style.paddingBottom = 10;
        toolbar.style.borderBottomWidth = 1;
        toolbar.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        
        // 暂停/继续按钮
        pauseButton = new Button(ToggleMonitoring);
        pauseButton.text = isMonitoring ? "暂停监控" : "继续监控";
        pauseButton.style.width = 100;
        pauseButton.style.marginRight = 5;
        
        // 清除按钮
        clearButton = new Button(ClearHistory);
        clearButton.text = "清除历史";
        clearButton.style.width = 100;
        clearButton.style.marginRight = 10;
        
        // 过滤输入框
        filterTextField = new TextField("过滤消息");
        filterTextField.style.flexGrow = 1;
        filterTextField.style.marginRight = 10;
        filterTextField.RegisterValueChangedCallback(evt => {
            currentFilter = evt.newValue;
            RefreshHistoryDisplay();
        });
        
        // 历史记录数量下拉框
        var historyOptions = new List<string> { "100", "500", "1000", "5000", "10000" };
        maxHistoryDropdown = new DropdownField("最大记录数", historyOptions, maxHistoryCount.ToString());
        maxHistoryDropdown.style.width = 150;
        maxHistoryDropdown.RegisterValueChangedCallback(evt => {
            maxHistoryCount = int.Parse(evt.newValue);
            TrimHistory();
        });
        
        // 自动滚动开关
        var autoScrollToggle = new Toggle("自动滚动");
        autoScrollToggle.value = autoScroll;
        autoScrollToggle.style.marginLeft = 10;
        autoScrollToggle.RegisterValueChangedCallback(evt => autoScroll = evt.newValue);
        
        toolbar.Add(pauseButton);
        toolbar.Add(clearButton);
        toolbar.Add(filterTextField);
        toolbar.Add(maxHistoryDropdown);
        toolbar.Add(autoScrollToggle);
        
        root.Add(toolbar);
    }
    
    private void CreateStatisticsPanel(VisualElement root)
    {
        var panel = new VisualElement();
        panel.style.marginBottom = 10;
        panel.style.paddingTop = 10;
        panel.style.paddingBottom = 10;
        panel.style.paddingLeft = 10;
        panel.style.paddingRight = 10;
        panel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        panel.style.borderTopLeftRadius = 5;
        panel.style.borderTopRightRadius = 5;
        panel.style.borderBottomLeftRadius = 5;
        panel.style.borderBottomRightRadius = 5;
        
        var titleLabel = new Label("统计信息");
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 5;
        
        totalMessagesLabel = new Label($"总消息数: {statistics.TotalMessagesSent}");
        totalMessagesLabel.style.marginBottom = 5;
        
        statisticsContainer = new VisualElement();
        statisticsContainer.style.maxHeight = 150;
        
        var statsScrollView = new ScrollView(ScrollViewMode.Vertical);
        statsScrollView.Add(statisticsContainer);
        statsScrollView.style.maxHeight = 150;
        
        panel.Add(titleLabel);
        panel.Add(totalMessagesLabel);
        panel.Add(statsScrollView);
        
        root.Add(panel);
    }
    
    private void CreateHistoryPanel(VisualElement root)
    {
        var panel = new VisualElement();
        panel.style.flexGrow = 1;
        panel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        panel.style.borderTopLeftRadius = 5;
        panel.style.borderTopRightRadius = 5;
        panel.style.borderBottomLeftRadius = 5;
        panel.style.borderBottomRightRadius = 5;
        panel.style.paddingTop = 10;
        panel.style.paddingLeft = 10;
        panel.style.paddingRight = 10;
        panel.style.paddingBottom = 10;
        
        var titleLabel = new Label("消息历史");
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 5;
        
        // 表头
        var headerRow = CreateHeaderRow();
        
        historyScrollView = new ScrollView(ScrollViewMode.Vertical);
        historyScrollView.style.flexGrow = 1;
        
        panel.Add(titleLabel);
        panel.Add(headerRow);
        panel.Add(historyScrollView);
        
        root.Add(panel);
    }
    
    private VisualElement CreateHeaderRow()
    {
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        header.style.paddingTop = 5;
        header.style.paddingBottom = 5;
        header.style.paddingLeft = 5;
        header.style.paddingRight = 5;
        header.style.marginBottom = 5;
        header.style.borderTopLeftRadius = 3;
        header.style.borderTopRightRadius = 3;
        
        var timeLabel = new Label("时间");
        timeLabel.style.width = 100;
        timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var keyLabel = new Label("消息键");
        keyLabel.style.width = 200;
        keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var typeLabel = new Label("数据类型");
        typeLabel.style.width = 150;
        typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var valueLabel = new Label("数据值");
        valueLabel.style.flexGrow = 1;
        valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var listenerLabel = new Label("监听者");
        listenerLabel.style.width = 80;
        listenerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        header.Add(timeLabel);
        header.Add(keyLabel);
        header.Add(typeLabel);
        header.Add(valueLabel);
        header.Add(listenerLabel);
        
        return header;
    }
    
    private VisualElement CreateMessageRow(MessageRecord record)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.paddingTop = 3;
        row.style.paddingBottom = 3;
        row.style.paddingLeft = 5;
        row.style.paddingRight = 5;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        
        var timeLabel = new Label(record.Timestamp);
        timeLabel.style.width = 100;
        timeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        
        var keyLabel = new Label(record.MessageKey);
        keyLabel.style.width = 200;
        keyLabel.style.color = new Color(0.5f, 0.8f, 1f);
        
        var typeLabel = new Label(record.DataType);
        typeLabel.style.width = 150;
        typeLabel.style.color = new Color(1f, 0.8f, 0.5f);
        
        var valueLabel = new Label(record.DataValue);
        valueLabel.style.flexGrow = 1;
        valueLabel.style.whiteSpace = WhiteSpace.Normal;
        
        var listenerLabel = new Label(record.ListenerCount.ToString());
        listenerLabel.style.width = 80;
        listenerLabel.style.color = record.ListenerCount > 0 ? Color.green : Color.gray;
        
        row.Add(timeLabel);
        row.Add(keyLabel);
        row.Add(typeLabel);
        row.Add(valueLabel);
        row.Add(listenerLabel);
        
        return row;
    }
    
    #endregion
    
    #region 消息记录
    
    /// <summary>
    /// 从代理接收消息记录
    /// </summary>
    private static void RecordMessageFromProxy(string key, string dataType, string dataValue, int listenerCount)
    {
        if (!isMonitoring) return;
        
        var record = new MessageRecord(key, dataType, dataValue, listenerCount);
        
        messageHistory.Add(record);
        statistics.RecordMessage(key);
        
        // 限制历史记录数量
        TrimHistory();
    }
    
    #endregion
    
    #region 私有方法
    
    private void ToggleMonitoring()
    {
        isMonitoring = !isMonitoring;
        pauseButton.text = isMonitoring ? "暂停监控" : "继续监控";
        monitoringStatusLabel.text = isMonitoring ? "● 监控中" : "○ 已暂停";
        monitoringStatusLabel.style.color = isMonitoring ? Color.green : Color.gray;
    }
    
    private void ClearHistory()
    {
        messageHistory.Clear();
        statistics.Clear();
        RefreshHistoryDisplay();
        RefreshStatistics();
    }
    
    private static void TrimHistory()
    {
        if (messageHistory.Count > maxHistoryCount)
        {
            int removeCount = messageHistory.Count - maxHistoryCount;
            messageHistory.RemoveRange(0, removeCount);
        }
    }
    
    private void RefreshUI()
    {
        if (historyScrollView == null) return;
        
        RefreshHistoryDisplay();
        RefreshStatistics();
    }
    
    private void RefreshHistoryDisplay()
    {
        if (historyScrollView == null) return;
        
        historyScrollView.Clear();
        
        var filteredRecords = string.IsNullOrEmpty(currentFilter)
            ? messageHistory
            : messageHistory.Where(r => r.MessageKey.Contains(currentFilter)).ToList();
        
        foreach (var record in filteredRecords)
        {
            historyScrollView.Add(CreateMessageRow(record));
        }
        
        // 自动滚动到底部
        if (autoScroll && filteredRecords.Count > 0)
        {
            historyScrollView.schedule.Execute(() => {
                historyScrollView.scrollOffset = new Vector2(0, historyScrollView.contentContainer.layout.height);
            }).ExecuteLater(10);
        }
    }
    
    private void RefreshStatistics()
    {
        if (totalMessagesLabel == null || statisticsContainer == null) return;
        
        totalMessagesLabel.text = $"总消息数: {statistics.TotalMessagesSent}";
        
        statisticsContainer.Clear();
        
        var sortedStats = statistics.MessageCounts.OrderByDescending(kvp => kvp.Value).Take(10);
        
        foreach (var stat in sortedStats)
        {
            var statRow = new VisualElement();
            statRow.style.flexDirection = FlexDirection.Row;
            statRow.style.justifyContent = Justify.SpaceBetween;
            statRow.style.marginBottom = 2;
            
            var keyLabel = new Label(stat.Key);
            keyLabel.style.color = new Color(0.5f, 0.8f, 1f);
            
            var countLabel = new Label($"{stat.Value} 次");
            countLabel.style.color = Color.green;
            
            statRow.Add(keyLabel);
            statRow.Add(countLabel);
            
            statisticsContainer.Add(statRow);
        }
    }
    
    #endregion
}
