using UnityEngine;

[DisallowMultipleComponent]
public class GridAdjacentPushResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;

    [Header("Detection")]
    [SerializeField] private LayerMask pushableLayers;
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.8f, 0.8f);

    private static readonly Vector3Int[] CardinalDirections =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    public bool TryResolvePushFromCell(Vector3Int sourceCell)
    {
        if (!TryGetAdjacentPushable(sourceCell, out CardinalPushable pushable, out _, out _))
            return false;

        return pushable.TryPushFromSourceCell(sourceCell);
    }

    public bool TryGetAdjacentPushable(
        Vector3Int sourceCell,
        out CardinalPushable pushable,
        out Vector3Int directionFromSourceToPushable,
        out Transform pushableTransform)
    {
        pushable = null;
        directionFromSourceToPushable = Vector3Int.zero;
        pushableTransform = null;

        if (grid == null)
            return false;

        foreach (Vector3Int dir in CardinalDirections)
        {
            Vector3Int neighborCell = sourceCell + dir;
            Vector3 center = grid.GetCellCenterWorld(neighborCell);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, overlapBoxSize, 0f, pushableLayers);

            if (hits == null || hits.Length == 0)
                continue;

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;

                CardinalPushable foundPushable = hit.GetComponentInParent<CardinalPushable>();
                if (foundPushable == null) continue;

                pushable = foundPushable;
                directionFromSourceToPushable = dir;
                pushableTransform = foundPushable.transform;
                return true;
            }
        }

        return false;
    }
}