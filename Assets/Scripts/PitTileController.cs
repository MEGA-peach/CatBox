using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class PitTileController : FloorButtonTarget
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap openPitTilemap;
    [SerializeField] private Tilemap sealedPitTilemap;

    [Header("State")]
    [SerializeField] private bool startSealed = false;

    [Header("Occupant Recheck")]
    [Tooltip("Objects on these layers can be re-checked and dropped when the pit opens.")]
    [SerializeField] private LayerMask fallableLayers;

    [Tooltip("Slightly smaller than a full cell to avoid edge false-positives.")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.8f, 0.8f);

    [Header("Grid")]
    [SerializeField] private Grid grid;

    private bool isSealed;

    public bool IsSealed => isSealed;

    private void Awake()
    {
        isSealed = startSealed;
        ApplyVisualState();
    }

    public override void OnButtonPressed(GridFloorButton button)
    {
        SetSealed(true);
    }

    public override void OnButtonReleased(GridFloorButton button)
    {
        SetSealed(false);
    }

    public void SetSealed(bool sealedState)
    {
        if (isSealed == sealedState)
            return;

        isSealed = sealedState;
        ApplyVisualState();

        // If the pit just became open again, re-check any objects standing on it.
        if (!isSealed)
            RecheckOccupantsOnOpenPits();
    }

    public bool IsOpenPitCell(Vector3Int cell)
    {
        return !isSealed && openPitTilemap != null && openPitTilemap.HasTile(cell);
    }

    public bool IsSealedPitCell(Vector3Int cell)
    {
        return isSealed && sealedPitTilemap != null && sealedPitTilemap.HasTile(cell);
    }

    private void ApplyVisualState()
    {
        if (openPitTilemap != null)
            openPitTilemap.gameObject.SetActive(!isSealed);

        if (sealedPitTilemap != null)
            sealedPitTilemap.gameObject.SetActive(isSealed);
    }

    private void RecheckOccupantsOnOpenPits()
    {
        if (grid == null || openPitTilemap == null)
            return;

        BoundsInt bounds = openPitTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!openPitTilemap.HasTile(cell))
                continue;

            Vector3 center = grid.GetCellCenterWorld(cell);

            Collider2D[] hits = Physics2D.OverlapBoxAll(
                center,
                overlapBoxSize,
                0f,
                fallableLayers
            );

            if (hits == null || hits.Length == 0)
                continue;

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                PitFallable fallable = hit.GetComponentInParent<PitFallable>();
                if (fallable == null || fallable.IsFalling)
                    continue;

                fallable.FallIntoPit(cell);
            }
        }
    }
}