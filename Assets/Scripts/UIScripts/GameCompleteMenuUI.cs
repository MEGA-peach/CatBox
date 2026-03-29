using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameCompleteMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text creditText;

    [Header("Scene Navigation")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    [Header("Text Content")]
    [TextArea(2, 4)]
    [SerializeField] private string winTitle = "You Beat the Final Level!";
    [TextArea(3, 8)]
    [SerializeField]
    private string winMessage =
        "Thank you so much for playing!\n\nYou completed the final level and finished the game.";
    [TextArea(1, 3)]
    [SerializeField] private string developerCredit = "Developed by MEGAPeach";

    private void Awake()
    {
        if (menuRoot == null)
            menuRoot = gameObject;

        RefreshText();
        Hide();
    }

    public void Show()
    {
        RefreshText();

        if (menuRoot != null)
            menuRoot.SetActive(true);
    }

    public void Hide()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    public void ReplayLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void RefreshText()
    {
        if (titleText != null)
            titleText.text = winTitle;

        if (bodyText != null)
            bodyText.text = winMessage;

        if (creditText != null)
            creditText.text = developerCredit;
    }
}