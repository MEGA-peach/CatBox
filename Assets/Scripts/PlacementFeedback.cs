using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlacementFeedback : MonoBehaviour
{
    [Header("Pop Animation")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float popScaleMultiplier = 1.1f;
    [SerializeField] private float popDuration = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placementClip;
    [SerializeField, Range(0f, 1f)] private float placementVolume = 1f;

    private Vector3 baseScale;
    private Coroutine popRoutine;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseScale = targetTransform.localScale;
    }

    public void PlayPlacementFeedback()
    {
        PlayPop();
        PlaySound();
    }

    public void PlayPop()
    {
        if (targetTransform == null)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine());
    }

    public void PlaySound()
    {
        if (audioSource == null || placementClip == null)
            return;

        audioSource.PlayOneShot(placementClip, placementVolume);
    }

    private IEnumerator PopRoutine()
    {
        Vector3 enlargedScale = baseScale * popScaleMultiplier;

        float halfDuration = popDuration * 0.5f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            targetTransform.localScale = Vector3.Lerp(baseScale, enlargedScale, t);
            yield return null;
        }

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            targetTransform.localScale = Vector3.Lerp(enlargedScale, baseScale, t);
            yield return null;
        }

        targetTransform.localScale = baseScale;
        popRoutine = null;
    }
}