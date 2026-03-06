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

    private bool dragging;
    private Vector3 dragOffset;
    private Vector3Int startCell;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (snapper == null)
            snapper = GetComponent<GridSnapper>();

        if (placementFeedback == null)
            placementFeedback = GetComponent<PlacementFeedback>();
    }

    private void OnMouseDown()
    {
        dragging = true;

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
        if (!dragging) return;

        Vector3 mouseWorld = GetMouseWorld();
        Vector3 intendedPosition = mouseWorld + dragOffset;

        transform.position = intendedPosition;

        if (preview != null)
            preview.UpdatePreview(intendedPosition, gameObject);
    }

    private void OnMouseUp()
    {
        dragging = false;

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
        {
            placementFeedback?.PlayPlacementFeedback();
        }

        if (placedOnValidCell && grid != null && pushResolver != null)
        {
            Vector3Int catCell = grid.WorldToCell(transform.position);
            pushResolver.TryResolvePushFromCell(catCell);
        }
    }

    private void GetOutOfDragState()
    {
        dragging = false;

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