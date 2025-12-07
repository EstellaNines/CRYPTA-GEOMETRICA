using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// Delaunay 三角剖分
    /// 使用 Bowyer-Watson 算法生成房间之间的潜在连接边
    /// </summary>
    public class DelaunayTriangulation
    {
        #region 内部数据结构
        
        /// <summary>
        /// 三角形
        /// </summary>
        private struct Triangle
        {
            public Vector2 a, b, c;
            
            public Triangle(Vector2 a, Vector2 b, Vector2 c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
            
            /// <summary>
            /// 检查点是否在外接圆内
            /// </summary>
            public bool CircumcircleContains(Vector2 point)
            {
                // 计算外接圆圆心和半径
                float ax = a.x, ay = a.y;
                float bx = b.x, by = b.y;
                float cx = c.x, cy = c.y;
                
                float d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
                
                if (Mathf.Abs(d) < 0.0001f) return false;
                
                float ux = ((ax * ax + ay * ay) * (by - cy) + 
                           (bx * bx + by * by) * (cy - ay) + 
                           (cx * cx + cy * cy) * (ay - by)) / d;
                
                float uy = ((ax * ax + ay * ay) * (cx - bx) + 
                           (bx * bx + by * by) * (ax - cx) + 
                           (cx * cx + cy * cy) * (bx - ax)) / d;
                
                Vector2 center = new Vector2(ux, uy);
                float radius = Vector2.Distance(center, a);
                
                return Vector2.Distance(center, point) <= radius;
            }
            
            /// <summary>
            /// 获取三角形的三条边
            /// </summary>
            public Edge[] GetEdges()
            {
                return new Edge[]
                {
                    new Edge(a, b),
                    new Edge(b, c),
                    new Edge(c, a)
                };
            }
            
            /// <summary>
            /// 检查是否包含指定顶点
            /// </summary>
            public bool ContainsVertex(Vector2 v)
            {
                return Vector2.Distance(a, v) < 0.001f || 
                       Vector2.Distance(b, v) < 0.001f || 
                       Vector2.Distance(c, v) < 0.001f;
            }
            
            /// <summary>
            /// 检查是否有指定边
            /// </summary>
            public bool HasEdge(Edge edge)
            {
                Edge[] edges = GetEdges();
                return edges.Any(e => e.Equals(edge));
            }
        }
        
        /// <summary>
        /// 边
        /// </summary>
        private struct Edge : IEquatable<Edge>
        {
            public Vector2 a, b;
            
            public Edge(Vector2 a, Vector2 b)
            {
                // 确保边的方向一致（用于比较）
                if (a.x < b.x || (Mathf.Approximately(a.x, b.x) && a.y < b.y))
                {
                    this.a = a;
                    this.b = b;
                }
                else
                {
                    this.a = b;
                    this.b = a;
                }
            }
            
            public bool Equals(Edge other)
            {
                return Vector2.Distance(a, other.a) < 0.001f && 
                       Vector2.Distance(b, other.b) < 0.001f;
            }
            
            public override bool Equals(object obj)
            {
                return obj is Edge other && Equals(other);
            }
            
            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Mathf.RoundToInt(a.x * 100), 
                    Mathf.RoundToInt(a.y * 100),
                    Mathf.RoundToInt(b.x * 100), 
                    Mathf.RoundToInt(b.y * 100)
                );
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 对房间列表执行 Delaunay 三角剖分
        /// </summary>
        /// <param name="rooms">房间列表</param>
        /// <returns>房间连接边列表</returns>
        public static List<RoomEdge> Triangulate(List<RoomRegion> rooms)
        {
            if (rooms == null || rooms.Count < 2)
            {
                return new List<RoomEdge>();
            }
            
            // 特殊情况：只有两个房间
            if (rooms.Count == 2)
            {
                float dist = rooms[0].DistanceTo(rooms[1]);
                return new List<RoomEdge>
                {
                    new RoomEdge(rooms[0].id, rooms[1].id, dist)
                };
            }
            
            // 提取房间中心点
            List<Vector2> points = rooms.Select(r => (Vector2)r.center).ToList();
            
            // 执行三角剖分
            List<Triangle> triangles = BowyerWatson(points);
            
            // 提取边并转换为 RoomEdge
            HashSet<RoomEdge> edges = new HashSet<RoomEdge>();
            
            foreach (var tri in triangles)
            {
                foreach (var edge in tri.GetEdges())
                {
                    int roomA = FindRoomByCenter(rooms, edge.a);
                    int roomB = FindRoomByCenter(rooms, edge.b);
                    
                    if (roomA != -1 && roomB != -1 && roomA != roomB)
                    {
                        float dist = Vector2.Distance(edge.a, edge.b);
                        edges.Add(new RoomEdge(roomA, roomB, dist));
                    }
                }
            }
            
            Debug.Log($"[DelaunayTriangulation] 生成了 {edges.Count} 条边");
            
            return edges.ToList();
        }
        
        #endregion

        #region Bowyer-Watson 算法
        
        /// <summary>
        /// Bowyer-Watson 算法实现
        /// </summary>
        private static List<Triangle> BowyerWatson(List<Vector2> points)
        {
            List<Triangle> triangles = new List<Triangle>();
            
            // 计算边界
            float minX = points.Min(p => p.x) - 10;
            float maxX = points.Max(p => p.x) + 10;
            float minY = points.Min(p => p.y) - 10;
            float maxY = points.Max(p => p.y) + 10;
            
            float width = maxX - minX;
            float height = maxY - minY;
            
            // 创建超级三角形（包含所有点）
            Vector2 superA = new Vector2(minX - width, minY - 1);
            Vector2 superB = new Vector2(maxX + width, minY - 1);
            Vector2 superC = new Vector2((minX + maxX) / 2, maxY + height + 1);
            
            triangles.Add(new Triangle(superA, superB, superC));
            
            // 逐点插入
            foreach (var point in points)
            {
                // 找到所有外接圆包含该点的三角形
                List<Triangle> badTriangles = new List<Triangle>();
                
                foreach (var tri in triangles)
                {
                    if (tri.CircumcircleContains(point))
                    {
                        badTriangles.Add(tri);
                    }
                }
                
                // 找到多边形边界（非共享边）
                List<Edge> polygon = new List<Edge>();
                
                foreach (var tri in badTriangles)
                {
                    foreach (var edge in tri.GetEdges())
                    {
                        // 检查这条边是否被其他坏三角形共享
                        bool isShared = false;
                        foreach (var otherTri in badTriangles)
                        {
                            if (!tri.Equals(otherTri) && otherTri.HasEdge(edge))
                            {
                                isShared = true;
                                break;
                            }
                        }
                        
                        if (!isShared)
                        {
                            polygon.Add(edge);
                        }
                    }
                }
                
                // 移除坏三角形
                foreach (var bad in badTriangles)
                {
                    triangles.Remove(bad);
                }
                
                // 用新点创建新三角形
                foreach (var edge in polygon)
                {
                    triangles.Add(new Triangle(edge.a, edge.b, point));
                }
            }
            
            // 移除包含超级三角形顶点的三角形
            triangles.RemoveAll(t => 
                t.ContainsVertex(superA) || 
                t.ContainsVertex(superB) || 
                t.ContainsVertex(superC)
            );
            
            return triangles;
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 根据中心点查找房间 ID
        /// </summary>
        private static int FindRoomByCenter(List<RoomRegion> rooms, Vector2 center)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (Vector2.Distance(rooms[i].center, center) < 0.5f)
                {
                    return rooms[i].id;
                }
            }
            return -1;
        }
        
        #endregion
    }
}
