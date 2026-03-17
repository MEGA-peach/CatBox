using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompletionHandler : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private int earnedStars = 0;

    private float levelStartTime;
    private bool levelCompleted;

    private void Start()
    {
        levelStartTime = Time.time;
    }

    public void CompleteLevel()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;

        float completionTime = Time.time - levelStartTime;

        SaveManager.CompleteLevel(levelNumber, completionTime, earnedStars);

        Debug.Log("Level " + levelNumber + " completed in " + completionTime + " seconds.");

        SceneManager.LoadScene("LevelSelect");
    }
}