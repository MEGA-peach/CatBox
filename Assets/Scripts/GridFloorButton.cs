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

    [Header("Targets")]
    [SerializeField] private FloorButtonTarget[] targets;

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

    private void Update()
    {
        if (!pollEveryFrame)
            return;

        RefreshPressedState();
    }

    public void RefreshPressedState()
    {
        bool pressedNow = CheckPressed();

        if (pressedNow == isPressed)
            return;

        isPressed = pressedNow;
        ApplyVisualState(isPressed);

        if (isPressed)
        {
            foreach (FloorButtonTarget target in targets)
            {
                if (target != null)
                    target.OnButtonPressed(this);
            }
        }
        else
        {
            foreach (FloorButtonTarget target in targets)
            {
                if (target != null)
                    target.OnButtonReleased(this);
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

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.transform.root == myRoot)
                continue;

            if (ownCollider != null && hit == ownCollider)
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