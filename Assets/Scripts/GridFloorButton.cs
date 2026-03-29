using UnityEngine;

[DisallowMultipleComponent]
public class GridFloorButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D ownCollider;

    [Header("Visuals")]
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Color unpressedColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Detection")]
    [SerializeField] private LayerMask occupantLayers;
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.8f, 0.8f);
    [SerializeField] private bool pollEveryFrame = true;
    [SerializeField] private bool useLateUpdateSafeguard = true;

    [Header("Targets")]
    [SerializeField] private FloorButtonTarget[] targets;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private bool isPressed;

    public bool IsPressed => isPressed;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (ownCollider == null)
            ownCollider = GetComponent<Collider2D>();

        SnapToGrid();
        ApplyVisualState(false);
    }

    private void OnEnable()
    {
        RefreshPressedState();
    }

    private void Update()
    {
        if (!pollEveryFrame)
            return;

        RefreshPressedState();
    }

    private void LateUpdate()
    {
        if (!pollEveryFrame || !useLateUpdateSafeguard)
            return;

        // Extra safety pass after movement/placement has finished for the frame.
        RefreshPressedState();
    }

    private void OnDisable()
    {
        ForceRelease();
    }

    private void OnDestroy()
    {
        ForceRelease();
    }

    public void RefreshPressedState()
    {
        bool pressedNow = CheckPressed();

        if (pressedNow == isPressed)
            return;

        SetPressedState(pressedNow);
    }

    public void ForceRelease()
    {
        if (!isPressed)
            return;

        SetPressedState(false);
    }

    private void SetPressedState(bool pressed)
    {
        isPressed = pressed;
        ApplyVisualState(isPressed);

        if (logStateChanges)
            Debug.Log($"[{nameof(GridFloorButton)}] {name} pressed = {isPressed}");

        if (isPressed)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].OnButtonPressed(this);
            }
        }
        else
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].OnButtonReleased(this);
            }
        }
    }

    private bool CheckPressed()
    {
        if (grid == null)
            return false;

        Vector3Int myCell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(myCell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, overlapBoxSize, 0f, occupantLayers);
        if (hits == null || hits.Length == 0)
            return false;

        Transform myRoot = transform.root;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || !hit.gameObject.activeInHierarchy)
                continue;

            if (hit.transform.root == myRoot)
                continue;

            if (ownCollider != null && hit == ownCollider)
                continue;

            // Ignore dragged cats so they do not press buttons while being moved.
            CatDragAndPlace cat = hit.GetComponentInParent<CatDragAndPlace>();
            if (cat != null && cat.IsDragging)
                continue;

            return true;
        }

        return false;
    }

    private void ApplyVisualState(bool pressed)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = pressed ? pressedSprite : unpressedSprite;
        spriteRenderer.color = pressed ? pressedColor : unpressedColor;
    }

    private void SnapToGrid()
    {
        if (grid == null)
            return;

        Vector3Int cell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(cell);
    }
}