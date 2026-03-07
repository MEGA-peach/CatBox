using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PitFallable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private GridSnapper snapper;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Fall Visuals")]
    [SerializeField] private float shrinkRate = 2.0f;
    [SerializeField] private float darkenRate = 2.0f;
    [SerializeField] private Color targetColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("End Behavior")]
    [SerializeField] private bool disableGameObjectAtEnd = true;
    [SerializeField] private UnityEvent onFinishedFalling;

    private bool isFalling;

    public bool IsFalling => isFalling;

    private void Awake()
    {
        if (snapper == null)
            snapper = GetComponent<GridSnapper>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public bool FallIntoPit(Vector3Int pitCell)
    {
        if (isFalling)
            return false;

        StartCoroutine(FallRoutine(pitCell));
        return true;
    }

    private IEnumerator FallRoutine(Vector3Int pitCell)
    {
        isFalling = true;

        DisableMovementComponents();

        if (grid != null)
        {
            if (snapper != null)
            {
                snapper.SetSnappingEnabled(true);
                snapper.SnapToCell(pitCell);
            }
            else
            {
                transform.position = grid.GetCellCenterWorld(pitCell);
            }
        }

        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float shrinkT = 0f;
        float darkenT = 0f;

        while (shrinkT < 1f || darkenT < 1f)
        {
            float dt = Time.deltaTime;

            shrinkT = Mathf.Clamp01(shrinkT + shrinkRate * dt);
            darkenT = Mathf.Clamp01(darkenT + darkenRate * dt);

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, shrinkT);

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(startColor, targetColor, darkenT);

            yield return null;
        }

        onFinishedFalling?.Invoke();

        if (disableGameObjectAtEnd)
            gameObject.SetActive(false);

        isFalling = false;
    }

    private void DisableMovementComponents()
    {
        GridSlideMover slideMover = GetComponent<GridSlideMover>();
        if (slideMover != null)
            slideMover.enabled = false;

        CardinalPushable pushable = GetComponent<CardinalPushable>();
        if (pushable != null)
            pushable.enabled = false;

        BoxWinSequence winSequence = GetComponent<BoxWinSequence>();
        if (winSequence != null)
            winSequence.enabled = false;
    }
}