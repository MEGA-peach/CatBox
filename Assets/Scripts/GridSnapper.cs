// GridSnapper.cs
using UnityEngine;

[DisallowMultipleComponent]
public class GridSnapper : MonoBehaviour
{
    [SerializeField] private GridSnapSettings settings;
    [SerializeField] private Grid grid; // assign your scene Grid here

    [SerializeField] private bool requireEnabledToSnap = true;
    private bool snappingEnabled = true;

    public void SetSnappingEnabled(bool enabled) => snappingEnabled = enabled;

    public void SnapNow()
    {
        if (requireEnabledToSnap && !snappingEnabled) return;
        if (grid == null)
        {
            Debug.LogWarning($"[{nameof(GridSnapper)}] No Grid assigned for {name}.");
            return;
        }

        Vector3 current = transform.position;
        Vector3Int cell = grid.WorldToCell(current);

        Vector3 snapped = GetAnchoredCellWorld(cell);

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

    private Vector3 GetAnchoredCellWorld(Vector3Int cell)
    {
        Vector3 basePos = grid.GetCellCenterWorld(cell);

        if (settings == null) return basePos;

        return settings.anchor switch
        {
            GridSnapSettings.CellAnchor.Center => basePos,
            GridSnapSettings.CellAnchor.BottomCenter => new Vector3(
                basePos.x,
                basePos.y - (grid.cellSize.y * 0.5f),
                basePos.z
            ),
            GridSnapSettings.CellAnchor.Custom => basePos + (Vector3)settings.customWorldOffset,
            _ => basePos
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
        }
    }
#endif
}