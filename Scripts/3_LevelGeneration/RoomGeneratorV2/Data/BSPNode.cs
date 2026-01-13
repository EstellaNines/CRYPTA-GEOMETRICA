using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// BSP 树节点
    /// 用于空间分割，将房间区域递归划分为更小的子区域
    /// </summary>
    [Serializable]
    public class BSPNode
    {
        #region 字段
        
        /// <summary>
        /// 节点边界（在网格坐标系中）
        /// </summary>
        public RectInt bounds;
        
        /// <summary>
        /// 左子节点（水平分割时为下方，垂直分割时为左方）
        /// </summary>
        public BSPNode left;
        
        /// <summary>
        /// 右子节点（水平分割时为上方，垂直分割时为右方）
        /// </summary>
        public BSPNode right;
        
        /// <summary>
        /// 该节点对应的房间区域（仅叶节点有效）
        /// </summary>
        public RoomRegion room;
        
        /// <summary>
        /// 节点在树中的深度（根节点为 0）
        /// </summary>
        public int depth;
        
        /// <summary>
        /// 分割方向（仅非叶节点有效）
        /// </summary>
        public SplitDirection splitDirection;
        
        /// <summary>
        /// 分割位置（仅非叶节点有效）
        /// </summary>
        public int splitPosition;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 是否为叶节点（没有子节点）
        /// </summary>
        public bool IsLeaf => left == null && right == null;
        
        /// <summary>
        /// 节点中心点
        /// </summary>
        public Vector2Int Center => new Vector2Int(
            bounds.x + bounds.width / 2,
            bounds.y + bounds.height / 2
        );
        
        /// <summary>
        /// 节点宽度
        /// </summary>
        public int Width => bounds.width;
        
        /// <summary>
        /// 节点高度
        /// </summary>
        public int Height => bounds.height;
        
        #endregion

        #region 构造函数
        
        public BSPNode()
        {
        }
        
        public BSPNode(RectInt bounds, int depth = 0)
        {
            this.bounds = bounds;
            this.depth = depth;
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 收集所有叶节点
        /// </summary>
        public List<BSPNode> GetLeaves()
        {
            List<BSPNode> leaves = new List<BSPNode>();
            CollectLeaves(this, leaves);
            return leaves;
        }
        
        private void CollectLeaves(BSPNode node, List<BSPNode> leaves)
        {
            if (node == null) return;
            
            if (node.IsLeaf)
            {
                leaves.Add(node);
            }
            else
            {
                CollectLeaves(node.left, leaves);
                CollectLeaves(node.right, leaves);
            }
        }
        
        /// <summary>
        /// 获取该节点（或其子节点）中的所有房间
        /// </summary>
        public List<RoomRegion> GetRooms()
        {
            List<RoomRegion> rooms = new List<RoomRegion>();
            CollectRooms(this, rooms);
            return rooms;
        }
        
        private void CollectRooms(BSPNode node, List<RoomRegion> rooms)
        {
            if (node == null) return;
            
            if (node.IsLeaf && node.room != null)
            {
                rooms.Add(node.room);
            }
            else
            {
                CollectRooms(node.left, rooms);
                CollectRooms(node.right, rooms);
            }
        }
        
        /// <summary>
        /// 获取兄弟节点的房间（用于走廊连接）
        /// </summary>
        public RoomRegion GetRoomFromSubtree()
        {
            if (IsLeaf)
            {
                return room;
            }
            
            // 优先返回左子树的房间
            RoomRegion leftRoom = left?.GetRoomFromSubtree();
            if (leftRoom != null) return leftRoom;
            
            return right?.GetRoomFromSubtree();
        }
        
        /// <summary>
        /// 检查点是否在节点边界内
        /// </summary>
        public bool Contains(Vector2Int point)
        {
            return bounds.Contains(point);
        }
        
        /// <summary>
        /// 检查另一个矩形是否与节点边界相交
        /// </summary>
        public bool Overlaps(RectInt other)
        {
            return bounds.Overlaps(other);
        }
        
        public override string ToString()
        {
            return $"BSPNode(Bounds:{bounds}, Depth:{depth}, IsLeaf:{IsLeaf})";
        }
        
        #endregion
    }
}
