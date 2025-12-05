using UnityEngine;

public struct RoomAnchorsData
{
    public Vector2Int startGridPos;
    public Vector2Int endGridPos;
    public Vector3 startWorldPos;
    public Vector3 endWorldPos;
    
    // 出入口方位: (-1,0)=Left, (1,0)=Right, (0,1)=Up, (0,-1)=Down
    public Vector2Int startDirection; 
    public Vector2Int endDirection;
}
