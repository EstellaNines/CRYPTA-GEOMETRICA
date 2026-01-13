using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 连通性保障器
    /// 使用双向随机游走确保入口到出口的连通性
    /// </summary>
    public class ConnectivityGuarantor
    {
        #region 字段
        
        private readonly RoomGenParamsV2 parameters;
        private readonly System.Random random;
        
        // 统计信息
        private int forwardSteps;
        private int backwardSteps;
        private int totalTilesDug;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 正向游走步数
        /// </summary>
        public int ForwardSteps => forwardSteps;
        
        /// <summary>
        /// 反向游走步数
        /// </summary>
        public int BackwardSteps => backwardSteps;
        
        /// <summary>
        /// 挖掘的总瓦片数
        /// </summary>
        public int TotalTilesDug => totalTilesDug;
        
        #endregion

        #region 构造函数
        
        public ConnectivityGuarantor(RoomGenParamsV2 parameters, System.Random random)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.random = random ?? new System.Random();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 执行双向随机游走确保连通性
        /// </summary>
        /// <param name="roomData">房间数据</param>
        public void EnsureConnectivity(RoomDataV2 roomData)
        {
            if (roomData == null)
            {
                // 房间数据为空
                return;
            }
            
            if (!parameters.enableBidirectionalWalk)
            {
                // 双向游走已禁用
                return;
            }
            
            forwardSteps = 0;
            backwardSteps = 0;
            totalTilesDug = 0;
            
            int brushSize = parameters.walkBrushSize;
            
            // 第一遍：入口 → 出口
            forwardSteps = RandomWalkPath(
                roomData.startPos, 
                roomData.endPos, 
                brushSize, 
                roomData,
                preferRight: true
            );
            
            // 第二遍：出口 → 入口
            backwardSteps = RandomWalkPath(
                roomData.endPos, 
                roomData.startPos, 
                brushSize, 
                roomData,
                preferRight: false
            );
            
            // 双向游走完成
        }
        
        /// <summary>
        /// 验证入口到出口是否连通
        /// </summary>
        public static bool VerifyConnectivity(RoomDataV2 roomData)
        {
            if (roomData == null) return false;
            
            return IsPathExists(roomData, roomData.startPos, roomData.endPos);
        }
        
        #endregion

        #region 随机游走算法
        
        /// <summary>
        /// 执行随机游走路径
        /// </summary>
        /// <returns>实际步数</returns>
        private int RandomWalkPath(
            Vector2Int start, 
            Vector2Int target, 
            int brushSize, 
            RoomDataV2 roomData,
            bool preferRight)
        {
            Vector2Int current = start;
            int halfBrush = brushSize / 2;
            int maxSteps = (roomData.width + roomData.height) * 3;
            int steps = 0;
            
            for (int step = 0; step < maxSteps; step++)
            {
                steps++;
                
                // 挖掘当前位置（只挖墙壁，保留平台）
                int dugCount = DigBrushWallsOnly(current.x, current.y, brushSize, roomData);
                totalTilesDug += dugCount;
                
                // 检查是否到达目标
                if (Mathf.Abs(current.x - target.x) <= halfBrush &&
                    Mathf.Abs(current.y - target.y) <= halfBrush)
                {
                    // 确保目标点被挖掘
                    DigBrushWallsOnly(target.x, target.y, brushSize, roomData);
                    break;
                }
                
                // 计算下一步方向
                Vector2Int move = CalculateNextMove(current, target, preferRight);
                
                // 应用移动（带边界检查）
                Vector2Int next = current + move;
                next.x = Mathf.Clamp(next.x, halfBrush + 1, roomData.width - halfBrush - 2);
                next.y = Mathf.Clamp(next.y, halfBrush + 1, roomData.height - halfBrush - 2);
                
                current = next;
            }
            
            return steps;
        }
        
        /// <summary>
        /// 计算下一步移动方向
        /// </summary>
        private Vector2Int CalculateNextMove(Vector2Int current, Vector2Int target, bool preferRight)
        {
            int dx = target.x - current.x;
            int dy = target.y - current.y;
            
            // 水平移动偏好
            if (random.NextDouble() < parameters.horizontalBias && dx != 0)
            {
                return new Vector2Int(Math.Sign(dx), 0);
            }
            
            // 垂直移动
            if (dy != 0)
            {
                return new Vector2Int(0, Math.Sign(dy));
            }
            
            // 如果已经在目标 Y 轴上，强制水平移动
            if (dx != 0)
            {
                return new Vector2Int(Math.Sign(dx), 0);
            }
            
            // 默认移动
            return preferRight ? Vector2Int.right : Vector2Int.left;
        }
        
        /// <summary>
        /// 挖掘刷子区域（只挖墙壁）
        /// </summary>
        private int DigBrushWallsOnly(int centerX, int centerY, int brushSize, RoomDataV2 roomData)
        {
            int half = brushSize / 2;
            int dugCount = 0;
            
            for (int dx = -half; dx <= half; dx++)
            {
                for (int dy = -half; dy <= half; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (roomData.IsValid(x, y) && roomData.GetTile(x, y) == TileType.Wall)
                    {
                        roomData.SetTile(x, y, TileType.Floor);
                        dugCount++;
                    }
                }
            }
            
            return dugCount;
        }
        
        #endregion

        #region 连通性验证
        
        /// <summary>
        /// 使用 BFS 检查两点之间是否存在路径
        /// </summary>
        private static bool IsPathExists(RoomDataV2 roomData, Vector2Int start, Vector2Int end)
        {
            if (roomData.GetTile(start.x, start.y) == TileType.Wall ||
                roomData.GetTile(end.x, end.y) == TileType.Wall)
            {
                return false;
            }
            
            bool[,] visited = new bool[roomData.width, roomData.height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                // 检查是否到达目标（允许一定误差）
                if (Mathf.Abs(current.x - end.x) <= 1 && Mathf.Abs(current.y - end.y) <= 1)
                {
                    return true;
                }
                
                foreach (var dir in dirs)
                {
                    Vector2Int next = current + dir;
                    
                    if (roomData.IsValid(next.x, next.y) &&
                        !visited[next.x, next.y] &&
                        roomData.GetTile(next.x, next.y) != TileType.Wall)
                    {
                        visited[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取从入口可达的所有格子数量
        /// </summary>
        public static int CountReachableTiles(RoomDataV2 roomData)
        {
            if (roomData == null) return 0;
            
            bool[,] visited = new bool[roomData.width, roomData.height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            
            queue.Enqueue(roomData.startPos);
            visited[roomData.startPos.x, roomData.startPos.y] = true;
            int count = 1;
            
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                foreach (var dir in dirs)
                {
                    Vector2Int next = current + dir;
                    
                    if (roomData.IsValid(next.x, next.y) &&
                        !visited[next.x, next.y] &&
                        roomData.GetTile(next.x, next.y) != TileType.Wall)
                    {
                        visited[next.x, next.y] = true;
                        queue.Enqueue(next);
                        count++;
                    }
                }
            }
            
            return count;
        }
        
        #endregion
    }
}
