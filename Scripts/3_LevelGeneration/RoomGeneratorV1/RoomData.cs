using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.SmallRoom
{
    public class RoomData
    {
        public int[,] grid;
        public int width;
        public int height;
        
        public Vector2Int startPos;
        public Vector2Int endPos;
        
        public List<Vector2Int> floorTiles = new List<Vector2Int>();
        public List<SpawnPoint> potentialSpawns = new List<SpawnPoint>();

        public RoomData(int w, int h)
        {
            width = w;
            height = h;
            grid = new int[w, h];
        }

        public void SetTile(int x, int y, TileType type)
        {
            if (IsValid(x, y))
            {
                grid[x, y] = (int)type;
                if (type == TileType.Floor)
                {
                    floorTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        public TileType GetTile(int x, int y)
        {
            if (!IsValid(x, y)) return TileType.Wall;
            return (TileType)grid[x, y];
        }

        public bool IsValid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
