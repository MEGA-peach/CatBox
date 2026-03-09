using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BoxWinSequence : MonoBehaviour
{
    [Header("Box")]
    [SerializeField] private SpriteRenderer boxSpriteRenderer;
    [SerializeField] private Sprite closedBoxSprite;
    [SerializeField] private Sprite openBoxSprite;

    [Header("Poof")]
    [SerializeField] private SpriteRenderer poofPrefab;
    [SerializeField] private Vector3 poofOffset = Vector3.zero;
    [SerializeField] private Vector3 poofStartScale = Vector3.one;
    [SerializeField] private float poofShrinkRate = 2.0f;
    [SerializeField] private float poofFadeRate = 2.0f;

    [Header("State")]
    [SerializeField] private bool disableFurtherBoxMovementAfterGoal = true;

    private bool goalReached;
    private bool catEnteredBox;
    private bool boxOpenForCat;

    public bool GoalReached => goalReached;
    public bool CatEnteredBox => catEnteredBox;
    public bool IsOpenForCat => boxOpenForCat;

    private void Awake()
    {
        if (boxSpriteRenderer == null)
            boxSpriteRenderer = GetComponent<SpriteRenderer>();

        if (boxSpriteRenderer != null && closedBoxSprite != null)
            boxSpriteRenderer.sprite = closedBoxSprite;

        boxOpenForCat = false;
    }

    public void PlayGoalReachedSequence()
    {
        if (goalReached)
            return;

        goalReached = true;
        StartCoroutine(GoalReachedRoutine());
    }

    public bool TryPlaceCatInBox(
     Transform catTransform,
     CatAnimatorDriver catAnimator,
     CatDragAndPlace catDragAndPlace = null)
    {
        Debug.Log($"[BoxWinSequence] TryPlaceCatInBox called on {name}. boxOpenForCat={boxOpenForCat}, catEnteredBox={catEnteredBox}");

        if (!boxOpenForCat || catEnteredBox)
            return false;

        catEnteredBox = true;

        if (catTransform != null)
            catTransform.position = transform.position;

        if (catAnimator != null)
        {
            Debug.Log("[BoxWinSequence] Setting cat to InBox state.");
            catAnimator.SetDragged(false);
            catAnimator.FaceDefault();
            catAnimator.SetInBox(true);
        }
        else
        {
            Debug.LogWarning("[BoxWinSequence] catAnimator is NULL.");
        }

        if (catDragAndPlace != null)
            catDragAndPlace.LockInteraction();

        OnCatEnteredBox();
        return true;
    }

    private IEnumerator GoalReachedRoutine()
    {
        Vector3 boxPosition = transform.position;

        SpriteRenderer poofInstance = null;

        if (poofPrefab != null)
        {
            poofInstance = Instantiate(poofPrefab, boxPosition + poofOffset, Quaternion.identity);
            poofInstance.transform.localScale = poofStartScale;
        }

        // While the poof is covering the box, open the box.
        if (boxSpriteRenderer != null && openBoxSprite != null)
            boxSpriteRenderer.sprite = openBoxSprite;

        if (poofInstance != null)
            yield return StartCoroutine(FadeAndShrinkPoof(poofInstance));

        // After the poof dissipates, the box can accept the cat.
        boxOpenForCat = true;

        if (disableFurtherBoxMovementAfterGoal)
        {
            CardinalPushable pushable = GetComponent<CardinalPushable>();
            if (pushable != null)
                pushable.enabled = false;

            GridSlideMover slideMover = GetComponent<GridSlideMover>();
            if (slideMover != null)
                slideMover.enabled = false;
        }
    }

    private IEnumerator FadeAndShrinkPoof(SpriteRenderer poofInstance)
    {
        Color color = poofInstance.color;
        Vector3 scale = poofInstance.transform.localScale;

        while (color.a > 0f || scale.x > 0f || scale.y > 0f)
        {
            float dt = Time.deltaTime;

            float shrinkAmount = poofShrinkRate * dt;
            scale.x = Mathf.Max(0f, scale.x - shrinkAmount);
            scale.y = Mathf.Max(0f, scale.y - shrinkAmount);
            scale.z = Mathf.Max(0f, scale.z - shrinkAmount);
            poofInstance.transform.localScale = scale;

            color.a = Mathf.Max(0f, color.a - (poofFadeRate * dt));
            poofInstance.color = color;

            if (color.a <= 0f && scale.x <= 0f && scale.y <= 0f)
                break;

            yield return null;
        }

        Destroy(poofInstance.gameObject);
    }

    private void OnCatEnteredBox()
    {
        Debug.Log($"[{nameof(BoxWinSequence)}] Cat entered the box.");
    }
}