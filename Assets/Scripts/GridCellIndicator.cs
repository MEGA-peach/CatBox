using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GridCellIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visuals")]
    [SerializeField] private Color defaultColor = new Color(1f, 0f, 0f, 0.55f);

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Hide();
    }

    public void FlashCell(Vector3Int cell, float duration)
    {
        FlashCell(cell, defaultColor, duration);
    }

    public void FlashCell(Vector3Int cell, Color color, float duration)
    {
        if (grid == null || spriteRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(cell, color, duration));
    }

    private IEnumerator FlashRoutine(Vector3Int cell, Color color, float duration)
    {
        transform.position = grid.GetCellCenterWorld(cell);
        spriteRenderer.color = color;
        spriteRenderer.enabled = true;

        yield return new WaitForSeconds(duration);

        Hide();
        flashRoutine = null;
    }

    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}