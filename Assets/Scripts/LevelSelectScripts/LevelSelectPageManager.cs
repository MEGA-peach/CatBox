using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelSelectPageManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelSelectPage
    {
        [Header("Page Root")]
        public GameObject pageRoot;

        [Header("Level Range On Page")]
        public int firstLevelNumber = 1;
        public int lastLevelNumber = 1;

        [Header("Page Background")]
        public Color backgroundColor = Color.white;

        [Header("Navigation Arrows On This Page")]
        public Button leftArrow;
        public Button rightArrow;
    }

    [Header("Pages")]
    [SerializeField] private LevelSelectPage[] pages;

    [Header("Background")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;

    private int currentPageIndex = 0;

    private void Start()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("LevelSelectPageManager: No pages assigned.");
            return;
        }

        currentPageIndex = GetStartPageIndex();
        ShowPage(currentPageIndex);
    }

    public void GoToNextPage()
    {
        int nextIndex = Mathf.Clamp(currentPageIndex + 1, 0, pages.Length - 1);
        ShowPage(nextIndex);
    }

    public void GoToPreviousPage()
    {
        int prevIndex = Mathf.Clamp(currentPageIndex - 1, 0, pages.Length - 1);
        ShowPage(prevIndex);
    }

    private void ShowPage(int index)
    {
        currentPageIndex = index;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].pageRoot != null)
                pages[i].pageRoot.SetActive(i == currentPageIndex);
        }

        UpdateBackground();
        UpdateArrowVisibility();
    }

    private int GetStartPageIndex()
    {
        int highestUnlocked = 1;

        if (SaveManager.CurrentSave != null)
            highestUnlocked = Mathf.Max(1, SaveManager.CurrentSave.highestUnlockedLevel);

        for (int i = 0; i < pages.Length; i++)
        {
            if (highestUnlocked >= pages[i].firstLevelNumber &&
                highestUnlocked <= pages[i].lastLevelNumber)
            {
                return i;
            }
        }

        return 0;
    }

    private void UpdateBackground()
    {
        if (backgroundSpriteRenderer == null)
            return;

        backgroundSpriteRenderer.color = pages[currentPageIndex].backgroundColor;
    }

    private void UpdateArrowVisibility()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].leftArrow != null)
                pages[i].leftArrow.gameObject.SetActive(i > 0);

            if (pages[i].rightArrow != null)
                pages[i].rightArrow.gameObject.SetActive(i < pages.Length - 1);
        }
    }
}