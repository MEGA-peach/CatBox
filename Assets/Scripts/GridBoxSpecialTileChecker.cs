using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class GridBoxSpecialTileChecker : MonoBehaviour
{
    [Header("Arrow Tilemaps")]
    [SerializeField] private Tilemap leftArrowTilemap;
    [SerializeField] private Tilemap rightArrowTilemap;
    [SerializeField] private Tilemap upArrowTilemap;
    [SerializeField] private Tilemap downArrowTilemap;

    [Header("Goal Tilemap")]
    [SerializeField] private Tilemap goalTilemap;

    public bool IsGoalCell(Vector3Int cell)
    {
        return goalTilemap != null && goalTilemap.HasTile(cell);
    }

    public bool TryGetArrowDirection(Vector3Int cell, out Vector3Int direction)
    {
        direction = Vector3Int.zero;

        if (leftArrowTilemap != null && leftArrowTilemap.HasTile(cell))
        {
            direction = Vector3Int.left;
            return true;
        }

        if (rightArrowTilemap != null && rightArrowTilemap.HasTile(cell))
        {
            direction = Vector3Int.right;
            return true;
        }

        if (upArrowTilemap != null && upArrowTilemap.HasTile(cell))
        {
            direction = Vector3Int.up;
            return true;
        }

        if (downArrowTilemap != null && downArrowTilemap.HasTile(cell))
        {
            direction = Vector3Int.down;
            return true;
        }

        return false;
    }

    public bool TryGetSpecialTileData(Vector3Int cell, out bool isGoal, out Vector3Int arrowDirection)
    {
        isGoal = IsGoalCell(cell);
        return TryGetArrowDirection(cell, out arrowDirection);
    }
}