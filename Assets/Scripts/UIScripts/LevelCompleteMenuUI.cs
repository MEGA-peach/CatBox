using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LevelCompleteMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuRoot;

    [Header("Scene Navigation")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private string nextLevelSceneName = "";
    [SerializeField] private bool useNextBuildIndexIfSceneNameEmpty = true;

    private void Awake()
    {
        if (menuRoot == null)
            menuRoot = gameObject;

        Hide();
    }

    public void Show()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);
    }

    public void Hide()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void ReplayLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void PlayNextLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }

        if (useNextBuildIndexIfSceneNameEmpty)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            int nextIndex = currentScene.buildIndex + 1;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
                return;
            }
        }

        Debug.LogWarning("[LevelCompleteMenuUI] No valid next level configured.");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}