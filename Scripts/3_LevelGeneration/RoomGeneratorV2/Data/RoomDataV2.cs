using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoomV2
{
    /// <summary>
    /// 房间数据 v0.2
    /// 存储房间的网格数据、出入口位置、生成点等信息
    /// </summary>
    [Serializable]
    public class RoomDataV2
    {
        #region 字段
        
        /// <summary>
        /// 二维网格数据（存储 TileType 枚举值）
        /// </summary>
        public int[,] grid;
        
        /// <summary>
        /// 房间宽度
        /// </summary>
        public int width;
        
        /// <summary>
        /// 房间高度
        /// </summary>
        public int height;
        
        /// <summary>
        /// 入口位置（左侧）
        /// </summary>
        public Vector2Int startPos;
        
        /// <summary>
        /// 出口位置（右侧）
        /// </summary>
        public Vector2Int endPos;
        
        /// <summary>
        /// 所有地面格子的列表
        /// </summary>
        public List<Vector2Int> floorTiles;
        
        /// <summary>
        /// 所有敌人生成点
        /// </summary>
        public List<SpawnPointV2> potentialSpawns;
        
        /// <summary>
        /// BSP 树根节点
        /// </summary>
        public BSPNode bspRoot;
        
        /// <summary>
        /// 房间连接图
        /// </summary>
        public RoomGraph roomGraph;
        
        /// <summary>
        /// 生成时使用的随机种子
        /// </summary>
        public string seed;
        
        /// <summary>
        /// 是否需要在出口处放置门（Boss房间专用）
        /// </summary>
        public bool needsDoorAtExit;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 总格子数
        /// </summary>
        public int TotalTiles => width * height;
        
        /// <summary>
        /// 地面格子数
        /// </summary>
        public int FloorCount => floorTiles?.Count ?? 0;
        
        /// <summary>
        /// 开阔度（地面占比）
        /// </summary>
        public float Openness => TotalTiles > 0 ? (float)FloorCount / TotalTiles : 0f;
        
        /// <summary>
        /// 房间数量
        /// </summary>
        public int RoomCount => roomGraph?.RoomCount ?? 0;
        
        /// <summary>
        /// 平台数量
        /// </summary>
        public int PlatformCount
        {
            get
            {
                int count = 0;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (GetTile(x, y) == TileType.Platform)
                            count++;
                    }
                }
                return count;
            }
        }
        
        #endregion

        #region 构造函数
        
        public RoomDataV2(int w, int h)
        {
            width = w;
            height = h;
            grid = new int[w, h];
            floorTiles = new List<Vector2Int>();
            potentialSpawns = new List<SpawnPointV2>();
            roomGraph = new RoomGraph();
        }
        
        #endregion

        #region 网格操作
        
        /// <summary>
        /// 设置指定位置的瓦片类型
        /// </summary>
        public void SetTile(int x, int y, TileType type)
        {
            if (!IsValid(x, y)) return;
            
            grid[x, y] = (int)type;
        }
        
        /// <summary>
        /// 获取指定位置的瓦片类型
        /// </summary>
        public TileType GetTile(int x, int y)
        {
            if (!IsValid(x, y)) return TileType.Wall;
            return (TileType)grid[x, y];
        }
        
        /// <summary>
        /// 检查坐标是否有效
        /// </summary>
        public bool IsValid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        /// <summary>
        /// 检查指定位置是否可通行（地面或平台）
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            TileType type = GetTile(x, y);
            return type == TileType.Floor || type == TileType.Platform;
        }
        
        /// <summary>
        /// 检查指定位置是否是实心块（墙壁或平台）
        /// </summary>
        public bool IsSolid(int x, int y)
        {
            TileType type = GetTile(x, y);
            return type == TileType.Wall || type == TileType.Platform;
        }
        
        /// <summary>
        /// 填充整个网格为指定类型
        /// </summary>
        public void Fill(TileType type)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = (int)type;
                }
            }
        }
        
        /// <summary>
        /// 填充矩形区域
        /// </summary>
        public void FillRect(RectInt rect, TileType type)
        {
            for (int x = rect.x; x < rect.xMax; x++)
            {
                for (int y = rect.y; y < rect.yMax; y++)
                {
                    SetTile(x, y, type);
                }
            }
        }
        
        /// <summary>
        /// 挖掘矩形区域（设置为地面）
        /// </summary>
        public void DigRect(RectInt rect)
        {
            FillRect(rect, TileType.Floor);
        }
        
        /// <summary>
        /// 挖掘指定位置（使用刷子尺寸）
        /// </summary>
        public void DigBrush(int centerX, int centerY, int brushSize)
        {
            int half = brushSize / 2;
            
            for (int dx = -half; dx <= half; dx++)
            {
                for (int dy = -half; dy <= half; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (IsValid(x, y))
                    {
                        SetTile(x, y, TileType.Floor);
                    }
                }
            }
        }
        
        /// <summary>
        /// 挖掘指定位置（只挖墙壁，保留平台）
        /// </summary>
        public void DigBrushWallsOnly(int centerX, int centerY, int brushSize)
        {
            int half = brushSize / 2;
            
            for (int dx = -half; dx <= half; dx++)
            {
                for (int dy = -half; dy <= half; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (IsValid(x, y) && GetTile(x, y) == TileType.Wall)
                    {
                        SetTile(x, y, TileType.Floor);
                    }
                }
            }
        }
        
        #endregion

        #region 地面列表管理
        
        /// <summary>
        /// 重建地面格子列表
        /// </summary>
        public void RebuildFloorTiles()
        {
            floorTiles.Clear();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (GetTile(x, y) == TileType.Floor)
                    {
                        floorTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        #endregion

        #region 统计方法
        
        /// <summary>
        /// 计算开阔度
        /// </summary>
        public float CalculateOpenness()
        {
            int floorCount = 0;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (GetTile(x, y) != TileType.Wall)
                    {
                        floorCount++;
                    }
                }
            }
            
            return (float)floorCount / TotalTiles;
        }
        
        /// <summary>
        /// 统计各类型瓦片数量
        /// </summary>
        public Dictionary<TileType, int> GetTileStats()
        {
            Dictionary<TileType, int> stats = new Dictionary<TileType, int>
            {
                { TileType.Wall, 0 },
                { TileType.Floor, 0 },
                { TileType.Platform, 0 }
            };
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileType type = GetTile(x, y);
                    stats[type]++;
                }
            }
            
            return stats;
        }
        
        #endregion

        #region 调试
        
        public override string ToString()
        {
            return $"RoomDataV2({width}×{height}, Openness:{Openness:P1}, Rooms:{RoomCount}, Spawns:{potentialSpawns?.Count ?? 0})";
        }
        
        #endregion
    }
}
