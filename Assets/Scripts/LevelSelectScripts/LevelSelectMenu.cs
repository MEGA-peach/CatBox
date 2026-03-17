using UnityEngine;

public class LevelSelectMenu : MonoBehaviour
{
    [Header("Level Setup")]
    [SerializeField] private int totalLevels = 10;
    [SerializeField] private LevelSelectButton[] levelButtons;

    private void Awake()
    {
        SaveManager.LoadOrCreateSave(totalLevels);
    }

    private void Start()
    {
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        if (levelButtons == null)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null)
            {
                levelButtons[i].Refresh();
            }
        }
    }

    public void ResetProgress()
    {
        SaveManager.ResetSave(totalLevels);
        RefreshButtons();
    }
}