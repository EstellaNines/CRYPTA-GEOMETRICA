using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间区域数据
    /// 表示 BSP 叶节点内生成的一个房间
    /// </summary>
    [Serializable]
    public class RoomRegion
    {
        #region 字段
        
        /// <summary>
        /// 房间唯一 ID
        /// </summary>
        public int id;
        
        /// <summary>
        /// 房间边界（在网格坐标系中）
        /// </summary>
        public RectInt bounds;
        
        /// <summary>
        /// 房间中心点
        /// </summary>
        public Vector2Int center;
        
        /// <summary>
        /// 房间内所有地面格子的列表
        /// </summary>
        public List<Vector2Int> floorTiles;
        
        /// <summary>
        /// 是否包含入口
        /// </summary>
        public bool isEntrance;
        
        /// <summary>
        /// 是否包含出口
        /// </summary>
        public bool isExit;
        
        /// <summary>
        /// 房间所属的 BSP 节点边界
        /// </summary>
        public RectInt bspBounds;
        
        /// <summary>
        /// 房间类型（休息房/战斗房/连接房）
        /// </summary>
        public RoomType roomType;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 房间宽度
        /// </summary>
        public int Width => bounds.width;
        
        /// <summary>
        /// 房间高度
        /// </summary>
        public int Height => bounds.height;
        
        /// <summary>
        /// 房间面积
        /// </summary>
        public int Area => bounds.width * bounds.height;
        
        /// <summary>
        /// 房间左边界 X
        /// </summary>
        public int Left => bounds.x;
        
        /// <summary>
        /// 房间右边界 X
        /// </summary>
        public int Right => bounds.xMax;
        
        /// <summary>
        /// 房间下边界 Y
        /// </summary>
        public int Bottom => bounds.y;
        
        /// <summary>
        /// 房间上边界 Y
        /// </summary>
        public int Top => bounds.yMax;
        
        #endregion

        #region 构造函数
        
        public RoomRegion()
        {
            floorTiles = new List<Vector2Int>();
        }
        
        public RoomRegion(int id, RectInt bounds)
        {
            this.id = id;
            this.bounds = bounds;
            this.center = new Vector2Int(
                bounds.x + bounds.width / 2,
                bounds.y + bounds.height / 2
            );
            this.floorTiles = new List<Vector2Int>();
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 检查点是否在房间内
        /// </summary>
        public bool Contains(Vector2Int point)
        {
            return bounds.Contains(point);
        }
        
        /// <summary>
        /// 检查另一个矩形是否与房间相交
        /// </summary>
        public bool Overlaps(RectInt other)
        {
            return bounds.Overlaps(other);
        }
        
        /// <summary>
        /// 计算到另一个房间的距离（中心点距离）
        /// </summary>
        public float DistanceTo(RoomRegion other)
        {
            return Vector2Int.Distance(center, other.center);
        }
        
        /// <summary>
        /// 获取房间边界上的随机点（用于走廊连接）
        /// </summary>
        public Vector2Int GetRandomEdgePoint(System.Random random, int padding = 1)
        {
            // 随机选择一条边
            int edge = random.Next(4);
            
            switch (edge)
            {
                case 0: // 上边
                    return new Vector2Int(
                        random.Next(bounds.x + padding, bounds.xMax - padding),
                        bounds.yMax - 1
                    );
                case 1: // 下边
                    return new Vector2Int(
                        random.Next(bounds.x + padding, bounds.xMax - padding),
                        bounds.y
                    );
                case 2: // 左边
                    return new Vector2Int(
                        bounds.x,
                        random.Next(bounds.y + padding, bounds.yMax - padding)
                    );
                case 3: // 右边
                default:
                    return new Vector2Int(
                        bounds.xMax - 1,
                        random.Next(bounds.y + padding, bounds.yMax - padding)
                    );
            }
        }
        
        /// <summary>
        /// 获取最接近目标点的边界点
        /// </summary>
        public Vector2Int GetClosestPointTo(Vector2Int target)
        {
            int x = Mathf.Clamp(target.x, bounds.x, bounds.xMax - 1);
            int y = Mathf.Clamp(target.y, bounds.y, bounds.yMax - 1);
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// 重新计算中心点
        /// </summary>
        public void RecalculateCenter()
        {
            center = new Vector2Int(
                bounds.x + bounds.width / 2,
                bounds.y + bounds.height / 2
            );
        }
        
        public override string ToString()
        {
            string typeStr = roomType.ToString();
            if (isEntrance) typeStr = "入口";
            if (isExit) typeStr = "出口";
            return $"Room#{id}[{typeStr}](Bounds:{bounds}, Center:{center})";
        }
        
        #endregion
    }
}
