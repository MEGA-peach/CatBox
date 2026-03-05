// BoxSlide.cs
using System.Collections;
using UnityEngine;

public class BoxSlide : MonoBehaviour
{
    [SerializeField] private GridSnapper snapper;
    [SerializeField] private float slideDuration = 0.15f;

    private bool sliding;

    private void Awake()
    {
        if (snapper == null) snapper = GetComponent<GridSnapper>();
    }

    public bool IsSliding => sliding;

    public void SlideToWorld(Vector3 targetWorld)
    {
        if (sliding) return;
        StartCoroutine(SlideRoutine(targetWorld));
    }

    private IEnumerator SlideRoutine(Vector3 targetWorld)
    {
        sliding = true;
        if (snapper != null) snapper.SetSnappingEnabled(false);

        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, slideDuration);
            transform.position = Vector3.Lerp(start, targetWorld, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = targetWorld;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow(); // snap once when stationary
        }

        sliding = false;
    }
}