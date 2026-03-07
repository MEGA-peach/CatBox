using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class GridCellBlockChecker : MonoBehaviour
{
    [Header("Tilemap Blocking")]
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    [Header("Object Blocking")]
    [Tooltip("Objects on these layers will make a cell invalid for placement.")]
    [SerializeField] private LayerMask blockingObjectLayers;

    [Tooltip("Slightly smaller than a full cell to avoid edge false-positives.")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.8f, 0.8f);

    [Header("Grid")]
    [SerializeField] private Grid grid;

    public bool IsCellBlocked(Vector3Int cell)
    {
        return IsCellBlocked(cell, null);
    }

    public bool IsCellBlocked(Vector3Int cell, GameObject ignoredObject)
    {
        bool blockedByWall = wallTilemap != null && wallTilemap.HasTile(cell);
        bool blockedByObstacle = obstacleTilemap != null && obstacleTilemap.HasTile(cell);

        if (blockedByWall || blockedByObstacle)
            return true;

        if (grid == null)
            return false;

        Vector3 cellCenter = grid.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            cellCenter,
            overlapBoxSize,
            0f,
            blockingObjectLayers
        );

        if (hits == null || hits.Length == 0)
            return false;

        Transform ignoredRoot = ignoredObject != null ? ignoredObject.transform.root : null;
        bool ignoredObjectIsCat = ignoredObject != null && ignoredObject.GetComponent<CatDragAndPlace>() != null;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (ignoredRoot != null && hit.transform.root == ignoredRoot)
                continue;

            // Special case:
            // If the moving object is the cat, allow placement onto an open goal box.
            if (ignoredObjectIsCat)
            {
                BoxWinSequence openBox = hit.GetComponentInParent<BoxWinSequence>();
                if (openBox != null && openBox.IsOpenForCat)
                    continue;
            }

            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (grid == null) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Gizmos.DrawWireCube(origin, overlapBoxSize);
    }
#endif
}