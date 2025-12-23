using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 最小生成树算法
    /// 使用 Kruskal 算法从 Delaunay 边中选择必要的连接
    /// </summary>
    public class MinimumSpanningTree
    {
        #region 并查集
        
        /// <summary>
        /// 并查集（用于 Kruskal 算法）
        /// </summary>
        private class UnionFind
        {
            private int[] parent;
            private int[] rank;
            
            public UnionFind(int size)
            {
                parent = new int[size];
                rank = new int[size];
                
                for (int i = 0; i < size; i++)
                {
                    parent[i] = i;
                    rank[i] = 0;
                }
            }
            
            /// <summary>
            /// 查找根节点（带路径压缩）
            /// </summary>
            public int Find(int x)
            {
                if (parent[x] != x)
                {
                    parent[x] = Find(parent[x]);
                }
                return parent[x];
            }
            
            /// <summary>
            /// 合并两个集合（按秩合并）
            /// </summary>
            public void Union(int x, int y)
            {
                int rootX = Find(x);
                int rootY = Find(y);
                
                if (rootX == rootY) return;
                
                if (rank[rootX] < rank[rootY])
                {
                    parent[rootX] = rootY;
                }
                else if (rank[rootX] > rank[rootY])
                {
                    parent[rootY] = rootX;
                }
                else
                {
                    parent[rootY] = rootX;
                    rank[rootX]++;
                }
            }
            
            /// <summary>
            /// 检查两个元素是否在同一集合
            /// </summary>
            public bool Connected(int x, int y)
            {
                return Find(x) == Find(y);
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 使用 Kruskal 算法计算最小生成树
        /// </summary>
        /// <param name="rooms">房间列表</param>
        /// <param name="edges">所有边（通常来自 Delaunay 三角剖分）</param>
        /// <returns>MST 边列表</returns>
        public static List<RoomEdge> Kruskal(List<RoomRegion> rooms, List<RoomEdge> edges)
        {
            if (rooms == null || rooms.Count < 2 || edges == null || edges.Count == 0)
            {
                return new List<RoomEdge>();
            }
            
            // 创建房间 ID 到索引的映射
            Dictionary<int, int> idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < rooms.Count; i++)
            {
                idToIndex[rooms[i].id] = i;
            }
            
            // 按距离排序边
            List<RoomEdge> sortedEdges = edges.OrderBy(e => e.distance).ToList();
            
            // 初始化并查集
            UnionFind uf = new UnionFind(rooms.Count);
            
            List<RoomEdge> mst = new List<RoomEdge>();
            
            foreach (var edge in sortedEdges)
            {
                // 检查边的两个端点是否在映射中
                if (!idToIndex.ContainsKey(edge.roomA) || !idToIndex.ContainsKey(edge.roomB))
                {
                    continue;
                }
                
                int indexA = idToIndex[edge.roomA];
                int indexB = idToIndex[edge.roomB];
                
                // 如果不在同一集合，添加这条边
                if (!uf.Connected(indexA, indexB))
                {
                    mst.Add(new RoomEdge(edge.roomA, edge.roomB, edge.distance, true));
                    uf.Union(indexA, indexB);
                    
                    // MST 边数 = 节点数 - 1
                    if (mst.Count == rooms.Count - 1)
                    {
                        break;
                    }
                }
            }
            
            // MST生成完成
            
            return mst;
        }
        
        /// <summary>
        /// 从非 MST 边中选择额外的边（形成环路）
        /// </summary>
        /// <param name="allEdges">所有边</param>
        /// <param name="mstEdges">MST 边</param>
        /// <param name="ratio">额外边比例 (0~1)</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>额外边列表</returns>
        public static List<RoomEdge> SelectExtraEdges(
            List<RoomEdge> allEdges, 
            List<RoomEdge> mstEdges, 
            float ratio,
            System.Random random = null)
        {
            if (allEdges == null || mstEdges == null || ratio <= 0)
            {
                return new List<RoomEdge>();
            }
            
            random = random ?? new System.Random();
            
            // 找出非 MST 边
            HashSet<RoomEdge> mstSet = new HashSet<RoomEdge>(mstEdges);
            List<RoomEdge> nonMstEdges = allEdges
                .Where(e => !mstSet.Contains(e))
                .OrderBy(e => e.distance) // 优先选择短边
                .ToList();
            
            // 计算需要添加的额外边数量
            int extraCount = Mathf.RoundToInt(mstEdges.Count * ratio);
            extraCount = Mathf.Min(extraCount, nonMstEdges.Count);
            
            // 选择额外边（混合策略：一半按距离，一半随机）
            List<RoomEdge> extraEdges = new List<RoomEdge>();
            
            int halfCount = extraCount / 2;
            
            // 前半部分：选择最短的边
            for (int i = 0; i < halfCount && i < nonMstEdges.Count; i++)
            {
                extraEdges.Add(nonMstEdges[i]);
            }
            
            // 后半部分：随机选择
            List<RoomEdge> remaining = nonMstEdges.Skip(halfCount).ToList();
            for (int i = 0; i < extraCount - halfCount && remaining.Count > 0; i++)
            {
                int index = random.Next(remaining.Count);
                extraEdges.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            
            // 额外边选择完成
            
            return extraEdges;
        }
        
        /// <summary>
        /// 检查图是否连通
        /// </summary>
        public static bool IsConnected(List<RoomRegion> rooms, List<RoomEdge> edges)
        {
            if (rooms == null || rooms.Count == 0) return true;
            if (rooms.Count == 1) return true;
            if (edges == null || edges.Count == 0) return false;
            
            // 创建邻接表
            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
            foreach (var room in rooms)
            {
                adjacency[room.id] = new List<int>();
            }
            
            foreach (var edge in edges)
            {
                if (adjacency.ContainsKey(edge.roomA) && adjacency.ContainsKey(edge.roomB))
                {
                    adjacency[edge.roomA].Add(edge.roomB);
                    adjacency[edge.roomB].Add(edge.roomA);
                }
            }
            
            // BFS 检查连通性
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            
            queue.Enqueue(rooms[0].id);
            visited.Add(rooms[0].id);
            
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                
                foreach (int neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return visited.Count == rooms.Count;
        }
        
        #endregion
    }
}
