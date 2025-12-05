using System;
using UnityEngine;

namespace Systems.SaveSystem
{
    /// <summary>
    /// 游戏存档数据类
    /// <para>这是一个纯数据类（POCO），用于通过 ES3 进行序列化和反序列化。</para>
    /// <para>所有需要保存的游戏状态都应该在此类中定义。</para>
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        // --- 元数据 (Metadata) ---
        
        /// <summary>
        /// 存档创建的具体时间 (格式: yyyy-MM-dd HH:mm:ss)
        /// <para>用于在加载界面显示存档时间。</para>
        /// </summary>
        public string SaveTime;

        /// <summary>
        /// 保存时所在的场景名称
        /// <para>加载存档时，通常需要先加载此场景。</para>
        /// </summary>
        public string SceneName;

        // --- 玩家数据 (Player Data) ---

        /// <summary>
        /// 玩家在世界空间中的位置坐标
        /// </summary>
        public Vector3 PlayerPosition;

        /// <summary>
        /// 玩家的旋转角度
        /// </summary>
        public Quaternion PlayerRotation;

        // --- 游戏进程数据 (Gameplay Data - 占位符) ---

        /// <summary>
        /// 玩家当前生命值
        /// <para>TODO: 这是一个占位符，后续需要对接真实的 Health 组件。</para>
        /// </summary>
        public int CurrentHealth;

        /// <summary>
        /// 玩家剩余生命数量 (残机数)
        /// <para>TODO: 这是一个占位符，后续对接游戏循环系统。</para>
        /// </summary>
        public int CurrentLives;

        /// <summary>
        /// 构造函数：初始化默认值
        /// <para>防止反序列化空对象时出现异常。</para>
        /// </summary>
        public GameSaveData()
        {
            SaveTime = "";
            SceneName = "";
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            CurrentHealth = 100;
            CurrentLives = 3;
        }
    }
}
