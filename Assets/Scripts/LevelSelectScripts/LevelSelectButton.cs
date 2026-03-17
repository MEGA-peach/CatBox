using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string sceneName = "";

    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject completedIcon;
    [SerializeField] private TMP_Text starText;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Refresh()
    {
        bool isUnlocked = SaveManager.IsLevelUnlocked(levelNumber);
        bool isCompleted = SaveManager.IsLevelCompleted(levelNumber);

        if (levelNumberText != null)
        {
            levelNumberText.text = levelNumber.ToString();
        }

        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        if (completedIcon != null)
        {
            completedIcon.SetActive(isCompleted);
        }

        if (starText != null)
        {
            int index = levelNumber - 1;
            int stars = 0;

            if (SaveManager.CurrentSave != null &&
                index >= 0 &&
                index < SaveManager.CurrentSave.starRatings.Length)
            {
                stars = SaveManager.CurrentSave.starRatings[index];
            }

            starText.text = stars > 0 ? stars.ToString() + "★" : "";
        }
    }

    public void OnPressed()
    {
        if (!SaveManager.IsLevelUnlocked(levelNumber))
            return;

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("LevelSelectButton: Scene name is empty for level " + levelNumber);
        }
    }
}