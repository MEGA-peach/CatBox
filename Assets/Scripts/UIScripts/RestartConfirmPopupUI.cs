using UnityEngine;

[DisallowMultipleComponent]
public class RestartConfirmPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private PauseMenuController pauseMenuController;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;
    }

    public void Show()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void OnYesPressed()
    {
        if (pauseMenuController != null)
            pauseMenuController.RestartCurrentLevel();
    }

    public void OnNoPressed()
    {
        if (pauseMenuController != null)
            pauseMenuController.CloseRestartConfirm();
        else
            Hide();
    }
}