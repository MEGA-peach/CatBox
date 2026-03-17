using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelSelectButton : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string sceneName = "";

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject completionIcon;

    [Header("Newest Uncompleted Highlight")]
    [SerializeField] private GameObject newestUnlockedHighlightObject;
    [SerializeField] private Image newestUnlockedHighlightImage;
    [SerializeField] private Color newestUnlockedHighlightColor = Color.white;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (newestUnlockedHighlightImage == null && newestUnlockedHighlightObject != null)
            newestUnlockedHighlightImage = newestUnlockedHighlightObject.GetComponent<Image>();
    }

    public void Refresh()
    {
        bool isUnlocked = SaveManager.IsLevelUnlocked(levelNumber);
        bool isCompleted = SaveManager.IsLevelCompleted(levelNumber);

        if (levelNumberText != null)
            levelNumberText.text = levelNumber.ToString();

        if (button != null)
            button.interactable = isUnlocked;

        if (lockIcon != null)
            lockIcon.SetActive(!isUnlocked);

        if (completionIcon != null)
            completionIcon.SetActive(isCompleted);

        RefreshNewestUnlockedHighlight(isUnlocked, isCompleted);
    }

    public void OnPressed()
    {
        if (!SaveManager.IsLevelUnlocked(levelNumber))
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"No scene name assigned for level {levelNumber} on {name}.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void RefreshNewestUnlockedHighlight(bool isUnlocked, bool isCompleted)
    {
        if (newestUnlockedHighlightObject == null)
            return;

        bool shouldShowHighlight = false;

        if (SaveManager.CurrentSave != null)
        {
            int highestUnlockedLevel = Mathf.Max(1, SaveManager.CurrentSave.highestUnlockedLevel);

            shouldShowHighlight =
                isUnlocked &&
                !isCompleted &&
                levelNumber == highestUnlockedLevel;
        }

        newestUnlockedHighlightObject.SetActive(shouldShowHighlight);

        if (shouldShowHighlight && newestUnlockedHighlightImage != null)
        {
            newestUnlockedHighlightImage.color = newestUnlockedHighlightColor;
        }
    }
}