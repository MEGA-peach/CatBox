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

    [Header("Push Timing")]
    [SerializeField] private float preHitDelay = 0.06f;
    [SerializeField] private float hitImpactDelay = 0.10f;

    private bool dragging;
    private bool actionLocked;
    private Vector3 dragOffset;
    private Vector3Int startCell;
    private Coroutine actionRoutine;

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
        if (actionLocked)
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
        if (!dragging || actionLocked)
            return;

        Vector3 mouseWorld = GetMouseWorld();
        Vector3 intendedPosition = mouseWorld + dragOffset;

        transform.position = intendedPosition;

        if (preview != null)
            preview.UpdatePreview(intendedPosition, gameObject);
    }

    private void OnMouseUp()
    {
        if (actionLocked)
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

        if (placedOnValidCell && grid != null && pushResolver != null)
        {
            Vector3Int catCell = grid.WorldToCell(transform.position);

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