using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene Names")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    private void Start()
    {
        ShowMainMenu();
    }

    public void OnPlayPressed()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void OnHowToPlayPressed()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void OnCreditsPressed()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void OnSettingsPressed()
    {

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void OnBackToMenuPressed()
    {
        ShowMainMenu();
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quit button pressed.");

        Application.Quit();
    }

    private void ShowMainMenu()
    {

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }
}