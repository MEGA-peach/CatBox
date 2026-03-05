// CatDragToGrid.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CatDragToGrid : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GridSnapper snapper;

    private bool dragging;
    private Vector3 dragOffset;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (snapper == null) snapper = GetComponent<GridSnapper>();
    }

    private void OnMouseDown()
    {
        dragging = true;
        if (snapper != null) snapper.SetSnappingEnabled(false);

        Vector3 mouseWorld = GetMouseWorld();
        dragOffset = transform.position - mouseWorld;
    }

    private void OnMouseDrag()
    {
        if (!dragging) return;

        Vector3 mouseWorld = GetMouseWorld();
        transform.position = mouseWorld + dragOffset; // free movement, no snapping
    }

    private void OnMouseUp()
    {
        dragging = false;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow(); // snap once when stationary
        }
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 m = Input.mousePosition;
        m.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(m);
    }
}