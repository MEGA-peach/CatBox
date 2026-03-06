using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BoxSlide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSnapper snapper;

    [Header("Slide Settings")]
    [SerializeField] private float slideDuration = 0.15f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine slideRoutine;
    private bool isSliding;

    public bool IsSliding => isSliding;

    private void Awake()
    {
        if (snapper == null)
            snapper = GetComponent<GridSnapper>();
    }

    public bool SlideToWorld(Vector3 targetWorld)
    {
        if (isSliding)
            return false;

        slideRoutine = StartCoroutine(SlideRoutine(targetWorld));
        return true;
    }

    public void StopSlideAndSnap()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        isSliding = false;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow();
        }
    }

    private IEnumerator SlideRoutine(Vector3 targetWorld)
    {
        isSliding = true;

        if (snapper != null)
            snapper.SetSnappingEnabled(false);

        Vector3 startWorld = transform.position;
        float duration = Mathf.Max(0.0001f, slideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curvedTime = slideCurve.Evaluate(normalizedTime);

            transform.position = Vector3.LerpUnclamped(startWorld, targetWorld, curvedTime);
            yield return null;
        }

        transform.position = targetWorld;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow();
        }

        isSliding = false;
        slideRoutine = null;
    }
}