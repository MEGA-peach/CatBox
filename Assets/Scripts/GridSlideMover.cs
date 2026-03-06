using UnityEngine;

[DisallowMultipleComponent]
public class GridSlideMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GridSnapper snapper;
    [SerializeField] private BoxSlide slideAnimator;
    [SerializeField] private GridCellBlockChecker blockChecker;

    [Header("Rules")]
    [SerializeField] private bool allowOnlyCardinalDirections = true;

    public bool IsMoving => slideAnimator != null && slideAnimator.IsSliding;

    private void Awake()
    {
        if (snapper == null)
            snapper = GetComponent<GridSnapper>();

        if (slideAnimator == null)
            slideAnimator = GetComponent<BoxSlide>();
    }

    public bool TrySlideInDirection(Vector3Int direction)
    {
        if (grid == null || blockChecker == null)
            return false;

        if (allowOnlyCardinalDirections && !IsCardinalDirection(direction))
            return false;

        if (IsMoving)
            return false;

        Vector3Int currentCell = grid.WorldToCell(transform.position);
        Vector3Int normalizedDirection = new Vector3Int(
            Mathf.Clamp(direction.x, -1, 1),
            Mathf.Clamp(direction.y, -1, 1),
            0
        );

        Vector3Int finalCell = currentCell;

        while (true)
        {
            Vector3Int nextCell = finalCell + normalizedDirection;

            // Ignore this object itself while checking path.
            if (blockChecker.IsCellBlocked(nextCell, gameObject))
                break;

            finalCell = nextCell;
        }

        if (finalCell == currentCell)
            return false;

        Vector3 targetWorld = grid.GetCellCenterWorld(finalCell);

        if (slideAnimator != null)
        {
            slideAnimator.SlideToWorld(targetWorld);
        }
        else if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapToCell(finalCell);
        }
        else
        {
            transform.position = targetWorld;
        }

        return true;
    }

    private bool IsCardinalDirection(Vector3Int direction)
    {
        int dx = Mathf.Abs(direction.x);
        int dy = Mathf.Abs(direction.y);
        return (dx + dy) == 1;
    }
}