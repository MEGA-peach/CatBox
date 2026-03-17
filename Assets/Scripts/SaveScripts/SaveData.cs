using System;

[Serializable]
public class SaveData
{
    public int highestUnlockedLevel = 1;
    public bool[] completedLevels;
    public float[] bestTimes;
    public int[] starRatings;

    public SaveData(int totalLevels)
    {
        highestUnlockedLevel = 1;
        completedLevels = new bool[totalLevels];
        bestTimes = new float[totalLevels];
        starRatings = new int[totalLevels];

        for (int i = 0; i < totalLevels; i++)
        {
            completedLevels[i] = false;
            bestTimes[i] = -1f;
            starRatings[i] = 0;
        }
    }
}