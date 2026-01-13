using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间连接边
    /// </summary>
    [Serializable]
    public struct RoomEdge : IEquatable<RoomEdge>
    {
        /// <summary>
        /// 房间 A 的 ID（较小的 ID）
        /// </summary>
        public int roomA;
        
        /// <summary>
        /// 房间 B 的 ID（较大的 ID）
        /// </summary>
        public int roomB;
        
        /// <summary>
        /// 两个房间中心点之间的距离
        /// </summary>
        public float distance;
        
        /// <summary>
        /// 是否是最小生成树的边（必要连接）
        /// </summary>
        public bool isMST;

        public RoomEdge(int a, int b, float dist, bool mst = false)
        {
            // 确保 roomA < roomB，避免重复边
            roomA = Mathf.Min(a, b);
            roomB = Mathf.Max(a, b);
            distance = dist;
            isMST = mst;
        }

        public bool Equals(RoomEdge other)
        {
            return roomA == other.roomA && roomB == other.roomB;
        }

        public override bool Equals(object obj)
        {
            return obj is RoomEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(roomA, roomB);
        }

        public override string ToString()
        {
            return $"Edge({roomA} <-> {roomB}, Dist:{distance:F1}, MST:{isMST})";
        }
    }

    /// <summary>
    /// 房间连接图
    /// 管理房间之间的连接关系
    /// </summary>
    [Serializable]
    public class RoomGraph
    {
        #region 字段
        
        /// <summary>
        /// 所有房间列表
        /// </summary>
        public List<RoomRegion> rooms;
        
        /// <summary>
        /// 所有潜在连接边（Delaunay 三角剖分结果）
        /// </summary>
        public List<RoomEdge> allEdges;
        
        /// <summary>
        /// 最小生成树边（必要连接）
        /// </summary>
        public List<RoomEdge> mstEdges;
        
        /// <summary>
        /// 额外边（形成环路）
        /// </summary>
        public List<RoomEdge> extraEdges;
        
        /// <summary>
        /// 最终使用的边（MST + 额外边）
        /// </summary>
        public List<RoomEdge> finalEdges;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 房间数量
        /// </summary>
        public int RoomCount => rooms?.Count ?? 0;
        
        /// <summary>
        /// 最终边数量
        /// </summary>
        public int EdgeCount => finalEdges?.Count ?? 0;
        
        #endregion

        #region 构造函数
        
        public RoomGraph()
        {
            rooms = new List<RoomRegion>();
            allEdges = new List<RoomEdge>();
            mstEdges = new List<RoomEdge>();
            extraEdges = new List<RoomEdge>();
            finalEdges = new List<RoomEdge>();
        }
        
        public RoomGraph(List<RoomRegion> rooms) : this()
        {
            this.rooms = rooms ?? new List<RoomRegion>();
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 根据 ID 获取房间
        /// </summary>
        public RoomRegion GetRoom(int id)
        {
            return rooms.FirstOrDefault(r => r.id == id);
        }
        
        /// <summary>
        /// 获取与指定房间相连的所有房间
        /// </summary>
        public List<RoomRegion> GetConnectedRooms(int roomId)
        {
            List<RoomRegion> connected = new List<RoomRegion>();
            
            foreach (var edge in finalEdges)
            {
                if (edge.roomA == roomId)
                {
                    var room = GetRoom(edge.roomB);
                    if (room != null) connected.Add(room);
                }
                else if (edge.roomB == roomId)
                {
                    var room = GetRoom(edge.roomA);
                    if (room != null) connected.Add(room);
                }
            }
            
            return connected;
        }
        
        /// <summary>
        /// 获取与指定房间相连的所有边
        /// </summary>
        public List<RoomEdge> GetEdgesForRoom(int roomId)
        {
            return finalEdges.Where(e => e.roomA == roomId || e.roomB == roomId).ToList();
        }
        
        /// <summary>
        /// 检查两个房间是否直接相连
        /// </summary>
        public bool AreConnected(int roomA, int roomB)
        {
            int a = Mathf.Min(roomA, roomB);
            int b = Mathf.Max(roomA, roomB);
            
            return finalEdges.Any(e => e.roomA == a && e.roomB == b);
        }
        
        /// <summary>
        /// 添加边（自动去重）
        /// </summary>
        public void AddEdge(RoomEdge edge)
        {
            if (!allEdges.Contains(edge))
            {
                allEdges.Add(edge);
            }
        }
        
        /// <summary>
        /// 构建最终边列表（MST + 额外边）
        /// </summary>
        public void BuildFinalEdges()
        {
            finalEdges.Clear();
            finalEdges.AddRange(mstEdges);
            finalEdges.AddRange(extraEdges);
        }
        
        /// <summary>
        /// 获取入口房间
        /// </summary>
        public RoomRegion GetEntranceRoom()
        {
            return rooms.FirstOrDefault(r => r.isEntrance);
        }
        
        /// <summary>
        /// 获取出口房间
        /// </summary>
        public RoomRegion GetExitRoom()
        {
            return rooms.FirstOrDefault(r => r.isExit);
        }
        
        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            rooms.Clear();
            allEdges.Clear();
            mstEdges.Clear();
            extraEdges.Clear();
            finalEdges.Clear();
        }
        
        public override string ToString()
        {
            return $"RoomGraph(Rooms:{RoomCount}, Edges:{EdgeCount}, MST:{mstEdges?.Count ?? 0}, Extra:{extraEdges?.Count ?? 0})";
        }
        
        #endregion
    }
}
