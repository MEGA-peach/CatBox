using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FloatingSpriteEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite[] possibleSprites;

    [Header("Timing")]
    [SerializeField] private float totalDuration = 2.0f;
    [SerializeField] private float popInDuration = 0.18f;

    [Header("Motion")]
    [SerializeField] private float riseDistance = 0.45f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private Vector3 peakScale = Vector3.one;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine playRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        if (possibleSprites != null && possibleSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, possibleSprites.Length);
            spriteRenderer.sprite = possibleSprites[randomIndex];
        }

        Vector3 basePosition = transform.position;
        Color baseColor = spriteRenderer.color;

        transform.localScale = startScale;

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / totalDuration);

            // Rise
            float riseT = riseCurve.Evaluate(normalizedTime);
            Vector3 pos = basePosition;
            pos.y += riseDistance * riseT;
            transform.position = pos;

            // Pop in, then stay at peak scale
            if (elapsed <= popInDuration)
            {
                float popT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, popInDuration));
                float curvedPopT = popCurve.Evaluate(popT);
                transform.localScale = Vector3.LerpUnclamped(startScale, peakScale, curvedPopT);
            }
            else
            {
                transform.localScale = peakScale;
            }

            // Fade
            Color color = baseColor;
            color.a = fadeCurve.Evaluate(normalizedTime);
            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}