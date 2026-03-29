using UnityEngine;

[DisallowMultipleComponent]
public class FirstTimeLevel1HowToPlayPopup : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject howToPlayPopupRoot;

    [Header("Level")]
    [Tooltip("Only show automatically on this level number.")]
    [SerializeField] private int levelNumber = 1;

    [Tooltip("The current level number for this scene.")]
    [SerializeField] private int currentSceneLevelNumber = 1;

    [Header("Behavior")]
    [SerializeField] private bool pauseGameWhenShown = true;
    [SerializeField] private bool showOnStart = true;

    private bool popupShownThisSession;

    private void Start()
    {
        if (howToPlayPopupRoot != null)
            howToPlayPopupRoot.SetActive(false);

        if (showOnStart)
            TryShowPopup();
    }

    public void TryShowPopup()
    {
        if (popupShownThisSession)
            return;

        if (currentSceneLevelNumber != levelNumber)
            return;

        if (SaveManager.CurrentSave == null)
        {
            Debug.LogWarning("[FirstTimeLevel1HowToPlayPopup] No save loaded yet.");
            return;
        }

        if (SaveManager.HasShownLevel1HowToPlay())
            return;

        ShowPopup();

        SaveManager.SetShownLevel1HowToPlay(true);
        popupShownThisSession = true;
    }

    public void ShowPopup()
    {
        if (howToPlayPopupRoot != null)
            howToPlayPopupRoot.SetActive(true);

        if (pauseGameWhenShown)
            Time.timeScale = 0f;
    }

    public void HidePopup()
    {
        if (howToPlayPopupRoot != null)
            howToPlayPopupRoot.SetActive(false);

        if (pauseGameWhenShown)
            Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        if (pauseGameWhenShown && Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}