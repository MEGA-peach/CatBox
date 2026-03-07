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
}