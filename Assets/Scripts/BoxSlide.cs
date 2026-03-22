using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BoxSlide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSnapper snapper;

    [Header("Slide Settings")]
    [Tooltip("Seconds per world unit moved.")]
    [SerializeField] private float secondsPerUnit = 0.08f;

    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Prevents extremely short or extremely long slide times.")]
    [SerializeField] private Vector2 durationClamp = new Vector2(0.08f, 0.5f);

    [Header("Landing Overshoot")]
    [SerializeField] private bool useOvershoot = true;
    [SerializeField] private float overshootDistance = 0.08f;
    [SerializeField] private float overshootReturnDuration = 0.05f;
    [SerializeField] private AnimationCurve overshootReturnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Blocked Bump")]
    [SerializeField] private float blockedBumpDistance = 0.08f;
    [SerializeField] private float blockedBumpOutDuration = 0.04f;
    [SerializeField] private float blockedBumpReturnDuration = 0.06f;
    [SerializeField] private AnimationCurve blockedBumpOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve blockedBumpReturnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio")]
    [SerializeField] private RandomizedAudioSet slideSounds;
    [SerializeField] private RandomizedAudioSet impactSounds;
    [SerializeField, Range(0f, 1f)] private float slideVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 1f;


    private int lastSlideIndex = -1;
    private int lastImpactIndex = -1;

    private Coroutine moveRoutine;
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

        moveRoutine = StartCoroutine(SlideRoutine(targetWorld));
        return true;
    }

    public bool PlayBlockedBump(Vector3 worldDirection)
    {
        if (isSliding)
            return false;

        if (worldDirection.sqrMagnitude <= 0.000001f)
            return false;

        moveRoutine = StartCoroutine(BlockedBumpRoutine(worldDirection.normalized));
        return true;
    }

    public void StopAndSnap()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isSliding = false;
        StopSlideSfx();

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow();
        }
    }

    private IEnumerator SlideRoutine(Vector3 targetWorld)
    {
        isSliding = true;
        PlaySlideSfx();

        if (snapper != null)
            snapper.SetSnappingEnabled(false);

        Vector3 startWorld = transform.position;
        Vector3 slideDelta = targetWorld - startWorld;

        float distance = slideDelta.magnitude;
        float mainDuration = Mathf.Clamp(
            distance * secondsPerUnit,
            durationClamp.x,
            durationClamp.y
        );

        Vector3 overshootTarget = targetWorld;

        if (useOvershoot && distance > 0.0001f)
        {
            Vector3 direction = slideDelta.normalized;
            overshootTarget = targetWorld + direction * overshootDistance;
        }

        float elapsed = 0f;

        while (elapsed < mainDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / mainDuration);
            float curvedTime = slideCurve.Evaluate(normalizedTime);

            transform.position = Vector3.LerpUnclamped(startWorld, overshootTarget, curvedTime);
            yield return null;
        }

        transform.position = overshootTarget;

        if (useOvershoot && overshootTarget != targetWorld)
        {
            elapsed = 0f;
            float returnDuration = Mathf.Max(0.0001f, overshootReturnDuration);
            Vector3 overshootStart = transform.position;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / returnDuration);
                float curvedTime = overshootReturnCurve.Evaluate(normalizedTime);

                transform.position = Vector3.LerpUnclamped(overshootStart, targetWorld, curvedTime);
                yield return null;
            }
        }

        transform.position = targetWorld;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow();
        }

        isSliding = false;
        moveRoutine = null;
        StopSlideSfx();
    }

    private IEnumerator BlockedBumpRoutine(Vector3 worldDirection)
    {
        isSliding = true;
        PlayImpactSfx();

        if (snapper != null)
            snapper.SetSnappingEnabled(false);

        Vector3 startWorld = transform.position;
        Vector3 bumpTarget = startWorld + (worldDirection * blockedBumpDistance);

        float elapsed = 0f;
        float outDuration = Mathf.Max(0.0001f, blockedBumpOutDuration);

        while (elapsed < outDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / outDuration);
            float curvedTime = blockedBumpOutCurve.Evaluate(normalizedTime);

            transform.position = Vector3.LerpUnclamped(startWorld, bumpTarget, curvedTime);
            yield return null;
        }

        transform.position = bumpTarget;

        elapsed = 0f;
        float returnDuration = Mathf.Max(0.0001f, blockedBumpReturnDuration);

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / returnDuration);
            float curvedTime = blockedBumpReturnCurve.Evaluate(normalizedTime);

            transform.position = Vector3.LerpUnclamped(bumpTarget, startWorld, curvedTime);
            yield return null;
        }

        transform.position = startWorld;

        if (snapper != null)
        {
            snapper.SetSnappingEnabled(true);
            snapper.SnapNow();
        }

        isSliding = false;
        moveRoutine = null;
    }

    public void PlayImpactSfx()
    {
        StopSlideSfx();

        if (impactSounds == null || !impactSounds.HasAnyClips)
            return;

        AudioManager.Instance?.PlaySfx(impactSounds, ref lastImpactIndex, impactVolume);
    }

    public void PlaySlideSfx()
    {
        if (slideSounds == null || !slideSounds.HasAnyClips)
            return;

        AudioManager.Instance?.PlaySfxForOwner(gameObject, slideSounds, ref lastSlideIndex, slideVolume);
    }

    public void StopSlideSfx()
    {
        AudioManager.Instance?.StopTaggedSfx(gameObject);
    }
}