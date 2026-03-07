using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class MovableObstacleTilemap : FloorButtonTarget
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GridCellIndicator blockedCellIndicator;

    [Header("Movement")]
    [SerializeField] private Vector3Int moveDirection = Vector3Int.right;
    [SerializeField] private int moveDistanceCells = 1;
    [SerializeField] private float secondsPerUnit = 0.08f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector2 durationClamp = new Vector2(0.08f, 0.4f);

    [Header("Blocking Check")]
    [Tooltip("Only objects on these layers will block wall movement (box, cat, bottles, yarn, etc).")]
    [SerializeField] private LayerMask blockingOccupantLayers;

    [Tooltip("Slightly smaller than a full cell to avoid edge false-positives.")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.8f, 0.8f);

    [Header("Blocked Jiggle")]
    [SerializeField] private float blockedBumpDistance = 0.08f;
    [SerializeField] private float blockedBumpOutDuration = 0.04f;
    [SerializeField] private float blockedBumpReturnDuration = 0.06f;
    [SerializeField] private float blockedIndicatorDuration = 0.2f;

    private Vector3 homeWorldPosition;
    private Coroutine moveRoutine;
    private bool isExtended;

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        // Keep the exact authored scene position.
        homeWorldPosition = transform.position;
    }

    public override void OnButtonPressed(GridFloorButton button)
    {
        SetExtended(true);
    }

    public override void OnButtonReleased(GridFloorButton button)
    {
        SetExtended(false);
    }

    public void SetExtended(bool extended)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToStateRoutine(extended));
    }

    private IEnumerator MoveToStateRoutine(bool targetExtended)
    {
        Vector3 desiredWorld = GetWorldPositionForState(targetExtended);

        if (Vector3.Distance(transform.position, desiredWorld) <= 0.0001f)
        {
            isExtended = targetExtended;
            moveRoutine = null;
            yield break;
        }

        if (TryGetBlockingCell(desiredWorld, out Vector3Int blockingCell))
        {
            if (blockedCellIndicator != null)
                blockedCellIndicator.FlashCell(blockingCell, blockedIndicatorDuration);

            yield return StartCoroutine(BlockedBumpRoutine(desiredWorld));
            moveRoutine = null;
            yield break;
        }

        yield return StartCoroutine(SlideRoutine(desiredWorld));

        isExtended = targetExtended;
        moveRoutine = null;
    }

    private IEnumerator SlideRoutine(Vector3 targetWorld)
    {
        Vector3 startWorld = transform.position;
        Vector3 delta = targetWorld - startWorld;
        float distance = delta.magnitude;

        float duration = Mathf.Clamp(distance * secondsPerUnit, durationClamp.x, durationClamp.y);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = slideCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startWorld, targetWorld, curved);
            yield return null;
        }

        transform.position = targetWorld;
    }

    private IEnumerator BlockedBumpRoutine(Vector3 desiredWorld)
    {
        Vector3 startWorld = transform.position;
        Vector3 direction = (desiredWorld - startWorld).normalized;
        Vector3 bumpTarget = startWorld + direction * blockedBumpDistance;

        float elapsed = 0f;
        while (elapsed < blockedBumpOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blockedBumpOutDuration);
            transform.position = Vector3.Lerp(startWorld, bumpTarget, t);
            yield return null;
        }

        transform.position = bumpTarget;

        elapsed = 0f;
        while (elapsed < blockedBumpReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blockedBumpReturnDuration);
            transform.position = Vector3.Lerp(bumpTarget, startWorld, t);
            yield return null;
        }

        transform.position = startWorld;
    }

    private bool TryGetBlockingCell(Vector3 desiredWorld, out Vector3Int blockingCell)
    {
        blockingCell = Vector3Int.zero;

        if (grid == null || tilemap == null)
            return false;

        Vector3 worldDelta = desiredWorld - transform.position;
        BoundsInt bounds = tilemap.cellBounds;
        Transform myRoot = transform.root;

        foreach (Vector3Int localCell in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(localCell))
                continue;

            Vector3 currentTileWorld = tilemap.GetCellCenterWorld(localCell);
            Vector3 targetTileWorld = currentTileWorld + worldDelta;
            Vector3Int targetCell = grid.WorldToCell(targetTileWorld);
            Vector3 targetCellCenter = grid.GetCellCenterWorld(targetCell);

            Collider2D[] hits = Physics2D.OverlapBoxAll(
                targetCellCenter,
                overlapBoxSize,
                0f,
                blockingOccupantLayers
            );

            if (hits == null || hits.Length == 0)
                continue;

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                if (hit.transform.root == myRoot)
                    continue;

                blockingCell = targetCell;
                return true;
            }
        }

        return false;
    }

    private Vector3 GetWorldPositionForState(bool extended)
    {
        if (!extended || grid == null)
            return homeWorldPosition;

        Vector3Int normalizedDirection = NormalizeToCardinal(moveDirection);

        Vector3 worldOffset = new Vector3(
            normalizedDirection.x * grid.cellSize.x * moveDistanceCells,
            normalizedDirection.y * grid.cellSize.y * moveDistanceCells,
            0f
        );

        Vector3 targetWorld = homeWorldPosition + worldOffset;
        targetWorld.z = homeWorldPosition.z;
        return targetWorld;
    }

    private Vector3Int NormalizeToCardinal(Vector3Int direction)
    {
        if (Mathf.Abs(direction.x) > 0)
            return new Vector3Int(direction.x > 0 ? 1 : -1, 0, 0);

        if (Mathf.Abs(direction.y) > 0)
            return new Vector3Int(0, direction.y > 0 ? 1 : -1, 0);

        return Vector3Int.zero;
    }
}