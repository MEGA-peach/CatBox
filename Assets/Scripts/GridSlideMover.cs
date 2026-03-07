using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GridSlideMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GridSnapper snapper;
    [SerializeField] private BoxSlide slideAnimator;
    [SerializeField] private GridCellBlockChecker blockChecker;
    [SerializeField] private GridBoxSpecialTileChecker specialTileChecker;

    [Header("Rules")]
    [SerializeField] private bool allowOnlyCardinalDirections = true;
    [SerializeField] private int maxSlideCells = 50;

    [Header("Arrow Behavior")]
    [Tooltip("Delay after landing on an arrow tile before the arrow pushes the box.")]
    [SerializeField] private float arrowTriggerDelay = 0.10f;

    [Header("Debug")]
    [SerializeField] private bool logSlideDebug = false;

    private Coroutine chainedMoveRoutine;

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
        {
            Debug.LogWarning($"[{nameof(GridSlideMover)}] Missing Grid or GridCellBlockChecker on {name}.");
            return false;
        }

        if (allowOnlyCardinalDirections && !IsCardinalDirection(direction))
            return false;

        if (IsMoving)
            return false;

        Vector3Int currentCell = grid.WorldToCell(transform.position);
        Vector3Int slideDirection = NormalizeToCardinal(direction);

        Vector3Int firstNextCell = currentCell + slideDirection;
        bool blockedImmediately = blockChecker.IsCellBlocked(firstNextCell, gameObject);

        if (blockedImmediately)
        {
            if (logSlideDebug)
                Debug.Log($"[{nameof(GridSlideMover)}] {name} blocked immediately at {firstNextCell}");

            if (slideAnimator != null)
            {
                Vector3 bumpDirection = new Vector3(slideDirection.x, slideDirection.y, 0f);
                slideAnimator.PlayBlockedBump(bumpDirection);
            }

            return false;
        }

        Vector3Int finalCell = currentCell;
        int cellsMoved = 0;

        while (cellsMoved < maxSlideCells)
        {
            Vector3Int nextCell = finalCell + slideDirection;
            bool blocked = blockChecker.IsCellBlocked(nextCell, gameObject);

            if (logSlideDebug)
                Debug.Log($"[{nameof(GridSlideMover)}] {name} checking {nextCell} | blocked = {blocked}");

            if (blocked)
                break;

            finalCell = nextCell;
            cellsMoved++;

            // If we encounter an arrow while moving, stop on that arrow tile.
            if (specialTileChecker != null &&
                specialTileChecker.TryGetArrowDirection(finalCell, out Vector3Int foundArrowDirection))
            {
                if (logSlideDebug)
                    Debug.Log($"[{nameof(GridSlideMover)}] {name} stopping on arrow at {finalCell} with direction {foundArrowDirection}");

                break;
            }
        }

        if (cellsMoved >= maxSlideCells)
        {
            Debug.LogWarning($"[{nameof(GridSlideMover)}] {name} hit maxSlideCells before finding a blocker.");
        }

        if (finalCell == currentCell)
            return false;

        Vector3 targetWorld = snapper != null
            ? snapper.GetSnappedWorldPosition(finalCell)
            : grid.GetCellCenterWorld(finalCell);

        if (slideAnimator != null)
        {
            bool started = slideAnimator.SlideToWorld(targetWorld);

            if (!started)
                return false;
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

        if (chainedMoveRoutine != null)
            StopCoroutine(chainedMoveRoutine);

        chainedMoveRoutine = StartCoroutine(HandlePostSlide(finalCell));

        return true;
    }

    private IEnumerator HandlePostSlide(Vector3Int landedCell)
    {
        while (slideAnimator != null && slideAnimator.IsSliding)
            yield return null;

        if (logSlideDebug)
            Debug.Log($"[{nameof(GridSlideMover)}] {name} landed on {landedCell}");

        // Check win first.
        if (specialTileChecker != null && specialTileChecker.IsGoalCell(landedCell))
        {
            OnReachedGoalTile(landedCell);
            chainedMoveRoutine = null;
            yield break;
        }

        // Then check arrow directly on the landed cell.
        if (specialTileChecker != null &&
            specialTileChecker.TryGetArrowDirection(landedCell, out Vector3Int arrowDirection))
        {
            if (logSlideDebug)
                Debug.Log($"[{nameof(GridSlideMover)}] {name} triggering arrow from {landedCell} toward {arrowDirection}");

            if (arrowTriggerDelay > 0f)
                yield return new WaitForSeconds(arrowTriggerDelay);

            TrySlideInDirection(arrowDirection);
        }

        chainedMoveRoutine = null;
    }

    private void OnReachedGoalTile(Vector3Int goalCell)
    {
        Debug.Log($"[{nameof(GridSlideMover)}] {name} reached goal tile at {goalCell}");

        BoxWinSequence winSequence = GetComponent<BoxWinSequence>();
        if (winSequence != null)
        {
            winSequence.PlayGoalReachedSequence();
            return;
        }

        Debug.LogWarning($"[{nameof(GridSlideMover)}] No {nameof(BoxWinSequence)} found on {name}.");
    }

    private Vector3Int NormalizeToCardinal(Vector3Int direction)
    {
        if (Mathf.Abs(direction.x) > 0)
            return new Vector3Int(direction.x > 0 ? 1 : -1, 0, 0);

        if (Mathf.Abs(direction.y) > 0)
            return new Vector3Int(0, direction.y > 0 ? 1 : -1, 0);

        return Vector3Int.zero;
    }

    private bool IsCardinalDirection(Vector3Int direction)
    {
        int dx = Mathf.Abs(direction.x);
        int dy = Mathf.Abs(direction.y);
        return (dx + dy) == 1;
    }
}