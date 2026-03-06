using UnityEngine;

[DisallowMultipleComponent]
public class GridSnapper : MonoBehaviour
{
    [SerializeField] private GridSnapSettings settings;
    [SerializeField] private Grid grid;
    [SerializeField] private bool requireEnabledToSnap = true;

    private bool snappingEnabled = true;

    public void SetSnappingEnabled(bool enabled)
    {
        snappingEnabled = enabled;
    }

    public void SnapNow()
    {
        if (requireEnabledToSnap && !snappingEnabled) return;
        if (grid == null) return;

        Vector3Int cell = grid.WorldToCell(transform.position);
        SnapToCell(cell);
    }

    public void SnapToCell(Vector3Int cell)
    {
        if (requireEnabledToSnap && !snappingEnabled) return;
        if (grid == null) return;

        Vector3 current = transform.position;
        Vector3 snapped = GetSnappedWorldPosition(cell);

        if (settings != null)
        {
            if (!settings.snapX) snapped.x = current.x;
            if (!settings.snapY) snapped.y = current.y;
            if (settings.preserveZ) snapped.z = current.z;
        }
        else
        {
            snapped.z = current.z;
        }

        transform.position = snapped;
    }

    public Vector3Int GetCurrentCell()
    {
        if (grid == null) return Vector3Int.zero;
        return grid.WorldToCell(transform.position);
    }

    public Vector3 GetSnappedWorldPosition(Vector3Int cell)
    {
        if (grid == null) return transform.position;

        Vector3 basePos = grid.GetCellCenterWorld(cell);

        if (settings == null)
            return basePos;

        switch (settings.anchor)
        {
            case GridSnapSettings.CellAnchor.Center:
                return basePos;

            case GridSnapSettings.CellAnchor.BottomCenter:
                return new Vector3(
                    basePos.x,
                    basePos.y - (grid.cellSize.y * 0.5f),
                    basePos.z
                );

            case GridSnapSettings.CellAnchor.Custom:
                return basePos + (Vector3)settings.customWorldOffset;

            default:
                return basePos;
        }
    }
}