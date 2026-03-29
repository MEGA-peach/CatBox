using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class MovableObstacleTilemap : FloorButtonTarget
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tilemap visualTilemap;
    [SerializeField] private Tilemap logicalBlockingTilemap;
    [SerializeField] private TileBase logicalBlockTile;
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
    private bool isMoving;

    private readonly List<Vector3Int> localVisualCells = new List<Vector3Int>();
    private readonly HashSet<Vector3Int> currentOccupiedCells = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> previousWrittenCells = new HashSet<Vector3Int>();

    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (visualTilemap == null)
            visualTilemap = GetComponent<Tilemap>();

        homeWorldPosition = transform.position;

        CacheLocalVisualCells();
        RefreshCurrentOccupiedCells();
        RefreshLogicalBlocking();
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

    public bool OccupiesCell(Vector3Int cell)
    {
        RefreshCurrentOccupiedCells();
        return currentOccupiedCells.Contains(cell);
    }

    private IEnumerator MoveToStateRoutine(bool targetExtended)
    {
        Vector3 desiredWorld = GetWorldPositionForState(targetExtended);

        if (Vector3.Distance(transform.position, desiredWorld) <= 0.0001f)
        {
            isExtended = targetExtended;
            RefreshCurrentOccupiedCells();
            RefreshLogicalBlocking();
            moveRoutine = null;
            yield break;
        }

        HashSet<Vector3Int> targetCells = BuildTargetOccupiedCells(targetExtended);

        if (TryGetBlockingCell(targetCells, out Vector3Int blockingCell))
        {
            if (blockedCellIndicator != null)
                blockedCellIndicator.FlashCell(blockingCell, blockedIndicatorDuration);

            yield return StartCoroutine(BlockedBumpRoutine(desiredWorld));

            RefreshCurrentOccupiedCells();
            RefreshLogicalBlocking();
            moveRoutine = null;
            yield break;
        }

        isMoving = true;

        yield return StartCoroutine(SlideRoutine(desiredWorld));

        isMoving = false;
        isExtended = targetExtended;

        RefreshCurrentOccupiedCells();
        RefreshLogicalBlocking();

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

            // Update logical blocking continuously so the blocked cells match
            // the wall's current position while it moves.
            RefreshCurrentOccupiedCells();
            RefreshLogicalBlocking();

            yield return null;
        }

        transform.position = targetWorld;

        RefreshCurrentOccupiedCells();
        RefreshLogicalBlocking();
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

            RefreshCurrentOccupiedCells();
            RefreshLogicalBlocking();

            yield return null;
        }

        transform.position = bumpTarget;
        RefreshCurrentOccupiedCells();
        RefreshLogicalBlocking();

        elapsed = 0f;
        while (elapsed < blockedBumpReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blockedBumpReturnDuration);
            transform.position = Vector3.Lerp(bumpTarget, startWorld, t);

            RefreshCurrentOccupiedCells();
            RefreshLogicalBlocking();

            yield return null;
        }

        transform.position = startWorld;

        RefreshCurrentOccupiedCells();
        RefreshLogicalBlocking();
    }

    private bool TryGetBlockingCell(HashSet<Vector3Int> targetCells, out Vector3Int blockingCell)
    {
        blockingCell = Vector3Int.zero;

        if (grid == null)
            return false;

        Transform myRoot = transform.root;

        foreach (Vector3Int targetCell in targetCells)
        {
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
                if (hit == null || !hit.gameObject.activeInHierarchy)
                    continue;

                if (hit.transform.root == myRoot)
                    continue;

                CatDragAndPlace cat = hit.GetComponentInParent<CatDragAndPlace>();
                if (cat != null && cat.IsDragging)
                    continue;

                blockingCell = targetCell;
                return true;
            }
        }

        return false;
    }

    private void CacheLocalVisualCells()
    {
        localVisualCells.Clear();

        if (visualTilemap == null)
            return;

        BoundsInt bounds = visualTilemap.cellBounds;

        foreach (Vector3Int localCell in bounds.allPositionsWithin)
        {
            if (!visualTilemap.HasTile(localCell))
                continue;

            localVisualCells.Add(localCell);
        }
    }

    private void RefreshCurrentOccupiedCells()
    {
        currentOccupiedCells.Clear();

        if (visualTilemap == null || grid == null)
            return;

        foreach (Vector3Int localCell in localVisualCells)
        {
            Vector3 worldCenter = visualTilemap.GetCellCenterWorld(localCell);
            Vector3Int gridCell = grid.WorldToCell(worldCenter);
            currentOccupiedCells.Add(gridCell);
        }
    }

    private HashSet<Vector3Int> BuildTargetOccupiedCells(bool targetExtended)
    {
        HashSet<Vector3Int> result = new HashSet<Vector3Int>();

        RefreshCurrentOccupiedCells();

        Vector3Int offset = NormalizeToCardinal(moveDirection) * moveDistanceCells;

        if (!targetExtended)
            offset *= -1;

        foreach (Vector3Int currentCell in currentOccupiedCells)
            result.Add(currentCell + offset);

        return result;
    }

    private void RefreshLogicalBlocking()
    {
        if (logicalBlockingTilemap == null || logicalBlockTile == null)
            return;

        foreach (Vector3Int oldCell in previousWrittenCells)
            logicalBlockingTilemap.SetTile(oldCell, null);

        previousWrittenCells.Clear();

        foreach (Vector3Int cell in currentOccupiedCells)
        {
            logicalBlockingTilemap.SetTile(cell, logicalBlockTile);
            previousWrittenCells.Add(cell);
        }
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