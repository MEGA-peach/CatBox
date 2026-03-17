using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "saveData.json");

    public static SaveData CurrentSave { get; private set; }

    public static void LoadOrCreateSave(int totalLevels)
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            CurrentSave = JsonUtility.FromJson<SaveData>(json);

            if (CurrentSave == null)
            {
                CurrentSave = new SaveData(totalLevels);
                SaveGame();
            }

            EnsureSaveMatchesLevelCount(totalLevels);
        }
        else
        {
            CurrentSave = new SaveData(totalLevels);
            SaveGame();
        }
    }

    public static void SaveGame()
    {
        if (CurrentSave == null)
        {
            Debug.LogWarning("SaveManager: No save data exists to save.");
            return;
        }

        string json = JsonUtility.ToJson(CurrentSave, true);
        File.WriteAllText(SavePath, json);
    }

    public static void ResetSave(int totalLevels)
    {
        CurrentSave = new SaveData(totalLevels);
        SaveGame();
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (CurrentSave == null)
            return false;

        return levelNumber <= CurrentSave.highestUnlockedLevel;
    }

    public static bool IsLevelCompleted(int levelNumber)
    {
        if (CurrentSave == null)
            return false;

        int index = levelNumber - 1;
        if (index < 0 || index >= CurrentSave.completedLevels.Length)
            return false;

        return CurrentSave.completedLevels[index];
    }

    public static void CompleteLevel(int levelNumber, float completionTime = -1f, int stars = 0)
    {
        if (CurrentSave == null)
        {
            Debug.LogWarning("SaveManager: No current save loaded.");
            return;
        }

        if (CurrentSave.completedLevels == null)
        {
            Debug.LogWarning("SaveManager: completedLevels array is null.");
            return;
        }

        int index = levelNumber - 1;
        if (index < 0 || index >= CurrentSave.completedLevels.Length)
        {
            Debug.LogWarning("SaveManager: Tried to complete invalid level number: " + levelNumber);
            return;
        }

        Debug.Log($"[SaveManager] Before CompleteLevel: levelNumber={levelNumber}, highestUnlockedLevel={CurrentSave.highestUnlockedLevel}");

        CurrentSave.completedLevels[index] = true;

        if (completionTime >= 0f)
        {
            float oldBest = CurrentSave.bestTimes[index];
            if (oldBest < 0f || completionTime < oldBest)
            {
                CurrentSave.bestTimes[index] = completionTime;
            }
        }

        if (stars > CurrentSave.starRatings[index])
        {
            CurrentSave.starRatings[index] = stars;
        }

        int nextLevel = levelNumber + 1;
        if (nextLevel > CurrentSave.highestUnlockedLevel)
        {
            CurrentSave.highestUnlockedLevel = nextLevel;
        }

        Debug.Log($"[SaveManager] After CompleteLevel: completed level {levelNumber}, new highestUnlockedLevel={CurrentSave.highestUnlockedLevel}");

        SaveGame();
    }

    private static void EnsureSaveMatchesLevelCount(int totalLevels)
    {
        bool needsResize = false;

        if (CurrentSave.completedLevels == null || CurrentSave.completedLevels.Length != totalLevels)
            needsResize = true;

        if (CurrentSave.bestTimes == null || CurrentSave.bestTimes.Length != totalLevels)
            needsResize = true;

        if (CurrentSave.starRatings == null || CurrentSave.starRatings.Length != totalLevels)
            needsResize = true;

        if (!needsResize)
            return;

        SaveData newSave = new SaveData(totalLevels);

        int copyCount = 0;
        if (CurrentSave.completedLevels != null)
        {
            copyCount = Mathf.Min(CurrentSave.completedLevels.Length, totalLevels);
            for (int i = 0; i < copyCount; i++)
            {
                newSave.completedLevels[i] = CurrentSave.completedLevels[i];
            }
        }

        if (CurrentSave.bestTimes != null)
        {
            copyCount = Mathf.Min(CurrentSave.bestTimes.Length, totalLevels);
            for (int i = 0; i < copyCount; i++)
            {
                newSave.bestTimes[i] = CurrentSave.bestTimes[i];
            }
        }

        if (CurrentSave.starRatings != null)
        {
            copyCount = Mathf.Min(CurrentSave.starRatings.Length, totalLevels);
            for (int i = 0; i < copyCount; i++)
            {
                newSave.starRatings[i] = CurrentSave.starRatings[i];
            }
        }

        newSave.highestUnlockedLevel = Mathf.Max(1, CurrentSave.highestUnlockedLevel);
        CurrentSave = newSave;
        SaveGame();
    }
}