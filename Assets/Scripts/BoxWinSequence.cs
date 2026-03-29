using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Final Celebration")]
    [SerializeField] private FloatingSpriteEffect heartsEffectPrefab;
    [SerializeField] private Vector3 heartsOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float heartsEffectDuration = 2.0f;
    [SerializeField] private LevelCompleteMenuUI levelCompleteMenuUI;
    [SerializeField] private GameCompleteMenuUI gameCompleteMenuUI;

    [Header("State")]
    [SerializeField] private bool disableFurtherBoxMovementAfterGoal = true;

    [Header("Level Progress")]
    [SerializeField] private int levelNumber = 0; // 0 = auto-detect from scene name
    [SerializeField] private int finalLevelNumber = 0; // 0 = auto-detect from save length

    private bool goalReached;
    private bool catEnteredBox;
    private bool boxOpenForCat;
    private bool finalSequencePlaying;

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

        if (levelCompleteMenuUI != null)
            levelCompleteMenuUI.Hide();

        if (gameCompleteMenuUI != null)
            gameCompleteMenuUI.Hide();

        ResolveLevelNumber();
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

        if (boxSpriteRenderer != null && openBoxSprite != null)
            boxSpriteRenderer.sprite = openBoxSprite;

        if (poofInstance != null)
            yield return StartCoroutine(FadeAndShrinkPoof(poofInstance));

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

        if (finalSequencePlaying)
            return;

        StartCoroutine(FinalCelebrationRoutine());
    }

    private IEnumerator FinalCelebrationRoutine()
    {
        finalSequencePlaying = true;

        if (heartsEffectPrefab != null)
        {
            FloatingSpriteEffect effectInstance = Instantiate(
                heartsEffectPrefab,
                transform.position + heartsOffset,
                Quaternion.identity
            );

            effectInstance.Play();
        }

        yield return new WaitForSeconds(heartsEffectDuration);

        Debug.Log($"[BoxWinSequence] Completing level {levelNumber}");
        SaveManager.CompleteLevel(levelNumber);

        if (IsFinalLevel())
        {
            if (gameCompleteMenuUI != null)
                gameCompleteMenuUI.Show();
            else if (levelCompleteMenuUI != null)
                levelCompleteMenuUI.Show();
        }
        else
        {
            if (levelCompleteMenuUI != null)
                levelCompleteMenuUI.Show();
        }

        finalSequencePlaying = false;
    }

    private bool IsFinalLevel()
    {
        if (finalLevelNumber > 0)
            return levelNumber == finalLevelNumber;

        if (SaveManager.CurrentSave != null && SaveManager.CurrentSave.completedLevels != null)
            return levelNumber == SaveManager.CurrentSave.completedLevels.Length;

        return false;
    }

    private void ResolveLevelNumber()
    {
        if (levelNumber > 0)
        {
            Debug.Log($"[BoxWinSequence] Using manually assigned level number: {levelNumber}");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        Match match = Regex.Match(sceneName, @"\d+");

        if (match.Success && int.TryParse(match.Value, out int parsedLevel))
        {
            levelNumber = parsedLevel;
            Debug.Log($"[BoxWinSequence] Auto-detected level number {levelNumber} from scene '{sceneName}'");
        }
        else
        {
            Debug.LogWarning($"[BoxWinSequence] Could not auto-detect level number from scene name '{sceneName}'. Defaulting to 1.");
            levelNumber = 1;
        }
    }
}