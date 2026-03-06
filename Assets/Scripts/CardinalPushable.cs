using UnityEngine;

[DisallowMultipleComponent]
public class CardinalPushable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GridSlideMover slideMover;

    private void Awake()
    {
        if (slideMover == null)
            slideMover = GetComponent<GridSlideMover>();
    }

    public bool TryPushFromSourceCell(Vector3Int sourceCell)
    {
        if (grid == null || slideMover == null)
            return false;

        Vector3Int myCell = grid.WorldToCell(transform.position);
        Vector3Int delta = myCell - sourceCell;

        if (!IsCardinalAdjacent(delta))
            return false;

        // Push away from the source.
        return slideMover.TrySlideInDirection(delta);
    }

    private bool IsCardinalAdjacent(Vector3Int delta)
    {
        int dx = Mathf.Abs(delta.x);
        int dy = Mathf.Abs(delta.y);
        return (dx + dy) == 1;
    }
}