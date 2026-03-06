using UnityEngine;

[DisallowMultipleComponent]
public class GridPlacementPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GridCellBlockChecker blockChecker;

    [Header("Colors")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.55f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.55f);

    public Vector3Int CurrentCell { get; private set; }
    public bool IsCurrentCellValid { get; private set; }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Hide();
    }

    public void Show()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    public void UpdatePreview(Vector3 worldPosition, GameObject ignoredObject = null)
    {
        if (grid == null) return;

        CurrentCell = grid.WorldToCell(worldPosition);
        transform.position = grid.GetCellCenterWorld(CurrentCell);

        bool isBlocked = blockChecker != null && blockChecker.IsCellBlocked(CurrentCell, ignoredObject);
        IsCurrentCellValid = !isBlocked;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = IsCurrentCellValid ? validColor : invalidColor;

            if (!spriteRenderer.enabled)
                spriteRenderer.enabled = true;
        }
    }
}