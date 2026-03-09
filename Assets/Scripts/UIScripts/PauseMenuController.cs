using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject restartConfirmPopupRoot;
    [SerializeField] private GameObject settingsMenuRoot;

    [Header("Scenes")]
    [Tooltip("Scene name for the Level Select screen")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    [Header("Behavior")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool pauseTimeScale = true;

    private bool isPaused;

    public bool IsPaused => isPaused;
    public bool IsSettingsOpen => settingsMenuRoot != null && settingsMenuRoot.activeSelf;
    public bool IsRestartPopupOpen => restartConfirmPopupRoot != null && restartConfirmPopupRoot.activeSelf;

    private void Start()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (restartConfirmPopupRoot != null)
            restartConfirmPopupRoot.SetActive(false);

        if (settingsMenuRoot != null)
            settingsMenuRoot.SetActive(false);

        ResumeGameTime();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            HandlePauseKey();
        }
    }

    private void HandlePauseKey()
    {
        // If either pause or settings is open, ESC should close both and resume.
        if (isPaused || IsSettingsOpen || IsRestartPopupOpen)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (settingsMenuRoot != null)
            settingsMenuRoot.SetActive(false);

        if (restartConfirmPopupRoot != null)
            restartConfirmPopupRoot.SetActive(false);

        if (pauseTimeScale)
            Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (settingsMenuRoot != null)
            settingsMenuRoot.SetActive(true);

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    public void CloseSettingsToPauseMenu()
    {
        if (settingsMenuRoot != null)
            settingsMenuRoot.SetActive(false);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    public void OpenRestartConfirm()
    {
        if (restartConfirmPopupRoot != null)
            restartConfirmPopupRoot.SetActive(true);

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    public void CloseRestartConfirm()
    {
        if (restartConfirmPopupRoot != null)
            restartConfirmPopupRoot.SetActive(false);

        if (pauseTimeScale)
            Time.timeScale = isPaused ? 0f : 1f;
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void QuitToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void QuitToDesktop()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void ResumeGameTime()
    {
        if (pauseTimeScale)
            Time.timeScale = 1f;
    }
}