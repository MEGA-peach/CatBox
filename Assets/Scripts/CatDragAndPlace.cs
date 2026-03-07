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

    [Header("Open Box Detection")]
    [SerializeField] private LayerMask openBoxLayers;
    [SerializeField] private Vector2 openBoxCheckSize = new Vector2(0.8f, 0.8f);

    [Header("Push Timing")]
    [SerializeField] private float preHitDelay = 0.06f;
    [SerializeField] private float hitImpactDelay = 0.10f;

    private bool dragging;
    private bool actionLocked;
    private Vector3 dragOffset;
    private Vector3Int startCell;
    private Coroutine actionRoutine;
    private bool permanentlyLocked;

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
    }

    private void OnMouseDown()
    {
        if (permanentlyLocked || actionLocked)
            return;

        dragging = true;
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
            preview.UpdatePreview(transform.position, gameObject);
        }
    }

    private void OnMouseDrag()
    {
        if (permanentlyLocked || !dragging || actionLocked)
            return;

        Vector3 mouseWorld = GetMouseWorld();
        Vector3 intendedPosition = mouseWorld + dragOffset;

        transform.position = intendedPosition;

        if (preview != null)
            preview.UpdatePreview(intendedPosition, gameObject);
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
            if (preview.IsCurrentCellValid)
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
            snapper.SnapNow();
            placedOnValidCell = true;
            landedThisDrop = true;
        }

        if (landedThisDrop)
            placementFeedback?.PlayPlacementFeedback();

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

    private bool TryPlaceCatIntoOpenBox(Vector3Int catCell)
    {
        Vector3 center = grid.GetCellCenterWorld(catCell);

        // Check everything in the cell instead of relying on a specific layer mask.
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, openBoxCheckSize, 0f);
        if (hits == null || hits.Length == 0)
        {
            Debug.Log("[CatDragAndPlace] No colliders found for open box check.");
            return false;
        }

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BoxWinSequence openBox = hit.GetComponentInParent<BoxWinSequence>();
            if (openBox == null)
                continue;

            Debug.Log($"[CatDragAndPlace] Found BoxWinSequence on {openBox.name}. IsOpenForCat = {openBox.IsOpenForCat}");

            if (!openBox.IsOpenForCat)
                continue;

            bool accepted = openBox.TryPlaceCatInBox(transform, catAnimator, this);

            Debug.Log($"[CatDragAndPlace] TryPlaceCatInBox result = {accepted}");

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