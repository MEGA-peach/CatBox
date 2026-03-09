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

    [Header("Blocking Object Check")]
    [SerializeField] private LayerMask blockingObjectLayers;

    [Header("Open Box Detection")]
    [SerializeField] private LayerMask openBoxLayers;
    [SerializeField] private Vector2 openBoxCheckSize = new Vector2(0.8f, 0.8f);

    [Header("Push Timing")]
    [SerializeField] private float preHitDelay = 0.06f;
    [SerializeField] private float hitImpactDelay = 0.10f;

    private bool dragging;
    private bool actionLocked;
    private bool permanentlyLocked;
    private bool currentDropBlockedByCollider;
    private bool currentDropBlockedByClosedBox;

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
        catAnimator?.SetDragged(true);

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

        currentDropBlockedByCollider = IsPlacementBlockedByObjectCollider();

        Vector3Int targetCell = grid != null ? grid.WorldToCell(intendedPosition) : Vector3Int.zero;
        currentDropBlockedByClosedBox = IsTargetCellBlockedByClosedBox(targetCell);

        bool forceBlocked = currentDropBlockedByCollider || currentDropBlockedByClosedBox;

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
            bool finalValid = preview.IsCurrentCellValid && !currentDropBlockedByCollider && !currentDropBlockedByClosedBox;

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
            bool finalValid = !currentDropBlockedByCollider && !currentDropBlockedByClosedBox;

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

        currentDropBlockedByCollider = false;
        currentDropBlockedByClosedBox = false;

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

    private bool IsPlacementBlockedByObjectCollider()
    {
        if (catCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = blockingObjectLayers;
        filter.useTriggers = true;

        int hitCount = catCollider.OverlapCollider(filter, overlapResults);

        if (hitCount <= 0)
            return false;

        Transform myRoot = transform.root;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];
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