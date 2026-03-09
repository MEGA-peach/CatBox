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

    [Header("Grid")]
    [SerializeField] private Grid grid;

    [Header("Special Tiles")]
    [SerializeField] private GridBoxSpecialTileChecker specialTileChecker;

    [Header("General Object Overlap")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(1.0f, 1.0f);

    public bool IsCellBlocked(Vector3Int cell)
    {
        return IsCellBlocked(cell, null);
    }

    public bool IsCellBlocked(Vector3Int cell, GameObject ignoredObject)
    {
        bool ignoredObjectIsCat = ignoredObject != null && ignoredObject.GetComponent<CatDragAndPlace>() != null;

        if (grid == null)
            return false;

        // Tilemap blocking
        if (wallTilemap != null && wallTilemap.HasTile(cell))
            return true;

        if (obstacleTilemap != null && obstacleTilemap.HasTile(cell))
            return true;

        // Open pits are invalid for the cat
        if (ignoredObjectIsCat && specialTileChecker != null && specialTileChecker.IsOpenPitCell(cell))
            return true;

        Transform ignoredRoot = ignoredObject != null ? ignoredObject.transform.root : null;

        // ------------------------------------------------------------
        // BOX RULE USING TAG + LOGICAL CELL
        // If this cell contains a box, it is blocked unless the box is open.
        // ------------------------------------------------------------
        GameObject[] boxes = GameObject.FindGameObjectsWithTag("Box");

        foreach (GameObject boxObject in boxes)
        {
            if (boxObject == null || !boxObject.activeInHierarchy)
                continue;

            if (ignoredRoot != null && boxObject.transform.root == ignoredRoot)
                continue;

            Vector3Int boxCell = GetObjectCell(boxObject);

            if (boxCell != cell)
                continue;

            BoxWinSequence boxWinSequence = boxObject.GetComponent<BoxWinSequence>();
            if (boxWinSequence == null)
                boxWinSequence = boxObject.GetComponentInParent<BoxWinSequence>();

            if (ignoredObjectIsCat && boxWinSequence != null && boxWinSequence.IsOpenForCat)
                return false;

            return true;
        }

        // ------------------------------------------------------------
        // General object blocking
        // ------------------------------------------------------------
        Vector3 cellCenter = grid.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            cellCenter,
            overlapBoxSize,
            0f,
            blockingObjectLayers
        );

        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy)
                continue;

            if (ignoredRoot != null && hit.transform.root == ignoredRoot)
                continue;

            // Skip boxes here because they were already handled above.
            if (hit.CompareTag("Box") || hit.GetComponentInParent<BoxWinSequence>() != null)
                continue;

            return true;
        }

        return false;
    }

    private Vector3Int GetObjectCell(GameObject obj)
    {
        if (obj == null || grid == null)
            return Vector3Int.zero;

        GridSnapper snapper = obj.GetComponent<GridSnapper>();
        if (snapper != null)
            return snapper.GetCurrentCell();

        return grid.WorldToCell(obj.transform.position);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (grid == null)
            return;

        Vector3Int cell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(cell);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, overlapBoxSize);
    }
#endif
}