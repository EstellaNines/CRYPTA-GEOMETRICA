using UnityEngine;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.SmallRoom
{
    [CreateAssetMenu(fileName = "NewRoomGenSettings", menuName = "自制工具/程序化关卡/房间生成配置文件")]
    public class RoomGenerationSettings : ScriptableObject
    {
        public RoomGenParams parameters = new RoomGenParams();
        public List<RoomTheme> themes = new List<RoomTheme>();
    }
}
