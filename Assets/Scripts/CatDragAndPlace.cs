using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CatDragAndPlace : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Grid grid;
    [SerializeField] private GridSnapper snapper;
    [SerializeField] private GridPlacementPreview preview;
    [SerializeField] private PlacementFeedback placementFeedback;
    [SerializeField] private GridAdjacentPushResolver pushResolver;
    [SerializeField] private CatAnimatorDriver catAnimator;
    [SerializeField] private Collider2D catCollider;

    [Header("Movable Wall")]
    [SerializeField] private MovableObstacleTilemap movableWall;

    [Header("Blocking Object Check")]
    [SerializeField] private LayerMask blockingObjectLayers;

    [Header("Open Box Detection")]
    [SerializeField] private LayerMask openBoxLayers;
    [SerializeField] private Vector2 openBoxCheckSize = new Vector2(0.8f, 0.8f);

    [Header("Push Timing")]
    [SerializeField] private float preHitDelay = 0.06f;
    [SerializeField] private float hitImpactDelay = 0.10f;

    [Header("Cat Audio")]
    [SerializeField] private RandomizedAudioSet pickupSounds;
    [SerializeField] private RandomizedAudioSet placeSounds;

    private int lastPickupIndex = -1;
    private int lastPlaceIndex = -1;

    private bool dragging;
    private bool actionLocked;
    private bool permanentlyLocked;
    private bool currentDropBlockedByCollider;
    private bool currentDropBlockedByClosedBox;
    private bool currentDropBlockedByMovableWall;

    public bool IsDragging => dragging;

    private Vector3 dragOffset;
    private Vector3Int startCell;
    private Coroutine actionRoutine;

    private readonly Collider2D[] overlapResults = new Collider2D[16];

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (snapper == null)
            snapper = GetComponent<GridSnapper>();

        if (placementFeedback == null)
            placementFeedback = GetComponent<PlacementFeedback>();

        if (catAnimator == null)
            catAnimator = GetComponent<CatAnimatorDriver>();

        if (catCollider == null)
            catCollider = GetComponent<Collider2D>();
    }

    private void OnMouseDown()
    {
        if (permanentlyLocked || actionLocked)
            return;

        dragging = true;
        currentDropBlockedByCollider = false;
        currentDropBlockedByClosedBox = false;
        currentDropBlockedByMovableWall = false;

        catAnimator?.SetDragged(true);
        AudioManager.Instance?.PlaySfx(pickupSounds, ref lastPickupIndex);

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(false);
            startCell = snapper.GetCurrentCell();
        }

        Vector3 mouseWorld = GetMouseWorld();
        dragOffset = transform.position - mouseWorld;

        if (preview != null)
        {
            preview.Show();
            preview.UpdatePreview(transform.position, gameObject, false);
        }
    }

    private void OnMouseDrag()
    {
        if (permanentlyLocked || !dragging || actionLocked)
            return;

        Vector3 mouseWorld = GetMouseWorld();
        Vector3 intendedPosition = mouseWorld + dragOffset;
        transform.position = intendedPosition;

        Vector3Int targetCell = grid != null ? grid.WorldToCell(intendedPosition) : Vector3Int.zero;

        currentDropBlockedByCollider = IsTargetCellBlockedByObjectCollider(targetCell);
        currentDropBlockedByClosedBox = IsTargetCellBlockedByClosedBox(targetCell);
        currentDropBlockedByMovableWall = IsTargetCellBlockedByMovableWall(targetCell);

        bool forceBlocked =
            currentDropBlockedByCollider ||
            currentDropBlockedByClosedBox ||
            currentDropBlockedByMovableWall;

        if (preview != null)
            preview.UpdatePreview(intendedPosition, gameObject, forceBlocked);
    }

    private void OnMouseUp()
    {
        if (permanentlyLocked || actionLocked)
            return;

        dragging = false;
        catAnimator?.SetDragged(false);

        if (snapper != null)
            snapper.SetSnappingEnabled(true);

        bool landedThisDrop = false;
        bool placedOnValidCell = false;

        if (preview != null && snapper != null)
        {
            bool finalValid =
                preview.IsCurrentCellValid &&
                !currentDropBlockedByCollider &&
                !currentDropBlockedByClosedBox &&
                !currentDropBlockedByMovableWall;

            if (finalValid)
            {
                snapper.SnapToCell(preview.CurrentCell);
                placedOnValidCell = true;
            }
            else
            {
                snapper.SnapToCell(startCell);
            }

            preview.Hide();
            landedThisDrop = true;
        }
        else if (snapper != null)
        {
            bool finalValid =
                !currentDropBlockedByCollider &&
                !currentDropBlockedByClosedBox &&
                !currentDropBlockedByMovableWall;

            if (finalValid)
            {
                snapper.SnapNow();
                placedOnValidCell = true;
            }
            else
            {
                snapper.SnapToCell(startCell);
            }

            landedThisDrop = true;
        }

        if (landedThisDrop)
            placementFeedback?.PlayPlacementFeedback();

        if (landedThisDrop)
            AudioManager.Instance?.PlaySfx(placeSounds, ref lastPlaceIndex);

        currentDropBlockedByCollider = false;
        currentDropBlockedByClosedBox = false;
        currentDropBlockedByMovableWall = false;

        if (!placedOnValidCell || grid == null)
        {
            catAnimator?.FaceDefault();
            return;
        }

        Vector3Int catCell = grid.WorldToCell(transform.position);

        if (TryPlaceCatIntoOpenBox(catCell))
            return;

        if (pushResolver != null)
        {
            bool foundPushable = pushResolver.TryGetAdjacentPushable(
                catCell,
                out CardinalPushable pushable,
                out Vector3Int directionToPushable,
                out Transform pushableTransform
            );

            if (foundPushable)
            {
                catAnimator?.FaceDirection(directionToPushable);

                if (actionRoutine != null)
                    StopCoroutine(actionRoutine);

                actionRoutine = StartCoroutine(PlayPushSequence(catCell, pushable));
            }
            else
            {
                catAnimator?.FaceDefault();
            }
        }
        else
        {
            catAnimator?.FaceDefault();
        }
    }

    public void LockInteraction()
    {
        permanentlyLocked = true;
        dragging = false;
        actionLocked = true;

        if (preview != null)
            preview.Hide();

        if (snapper != null)
            snapper.SetSnappingEnabled(true);
    }

    private bool IsTargetCellBlockedByObjectCollider(Vector3Int targetCell)
    {
        if (grid == null)
            return false;

        Vector3 cellCenter = grid.GetCellCenterWorld(targetCell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            cellCenter,
            openBoxCheckSize,
            0f,
            blockingObjectLayers
        );

        if (hits == null || hits.Length == 0)
            return false;

        Transform myRoot = transform.root;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.transform.root == myRoot)
                continue;

            BoxWinSequence box = hit.GetComponentInParent<BoxWinSequence>();
            if (box != null)
            {
                if (box.IsOpenForCat)
                    continue;

                return true;
            }

            // Movable wall is handled by IsTargetCellBlockedByMovableWall()
            if (hit.GetComponentInParent<MovableObstacleTilemap>() != null)
                continue;

            return true;
        }

        return false;
    }

    private bool IsTargetCellBlockedByClosedBox(Vector3Int targetCell)
    {
        if (grid == null)
            return false;

        GameObject[] boxes = GameObject.FindGameObjectsWithTag("Box");

        foreach (GameObject boxObject in boxes)
        {
            if (boxObject == null || !boxObject.activeInHierarchy)
                continue;

            GridSnapper boxSnapper = boxObject.GetComponent<GridSnapper>();
            Vector3Int boxCell = boxSnapper != null
                ? boxSnapper.GetCurrentCell()
                : grid.WorldToCell(boxObject.transform.position);

            if (boxCell != targetCell)
                continue;

            BoxWinSequence boxWinSequence = boxObject.GetComponent<BoxWinSequence>();
            if (boxWinSequence == null)
                boxWinSequence = boxObject.GetComponentInParent<BoxWinSequence>();

            if (boxWinSequence != null && boxWinSequence.IsOpenForCat)
                return false;

            return true;
        }

        return false;
    }

    private bool IsTargetCellBlockedByMovableWall(Vector3Int targetCell)
    {
        if (movableWall == null || !movableWall.gameObject.activeInHierarchy)
            return false;

        return movableWall.OccupiesCell(targetCell);
    }

    private bool TryPlaceCatIntoOpenBox(Vector3Int catCell)
    {
        Vector3 center = grid.GetCellCenterWorld(catCell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, openBoxCheckSize, 0f);
        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BoxWinSequence openBox = hit.GetComponentInParent<BoxWinSequence>();
            if (openBox == null)
                continue;

            if (!openBox.IsOpenForCat)
                continue;

            bool accepted = openBox.TryPlaceCatInBox(transform, catAnimator, this);
            if (accepted)
                return true;
        }

        return false;
    }

    private IEnumerator PlayPushSequence(Vector3Int catCell, CardinalPushable pushable)
    {
        actionLocked = true;

        if (preHitDelay > 0f)
            yield return new WaitForSeconds(preHitDelay);

        catAnimator?.PlayHit();

        if (hitImpactDelay > 0f)
            yield return new WaitForSeconds(hitImpactDelay);

        if (pushable != null)
            pushable.TryPushFromSourceCell(catCell);

        actionLocked = false;
        actionRoutine = null;
    }

    private void GetOutOfDragState()
    {
        dragging = false;
        actionLocked = false;
        currentDropBlockedByCollider = false;
        currentDropBlockedByClosedBox = false;
        currentDropBlockedByMovableWall = false;

        catAnimator?.SetDragged(false);
        catAnimator?.FaceDefault();

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
            actionRoutine = null;
        }

        if (preview != null)
            preview.Hide();

        if (snapper != null)
            snapper.SetSnappingEnabled(true);
    }

    private void OnDisable()
    {
        GetOutOfDragState();
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z);

        Vector3 world = cam.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        return world;
    }
}