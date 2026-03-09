using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class RestartLevelButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image targetImage;
    [SerializeField] private PauseMenuController pauseMenuController;

    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;

    [Header("Opacity")]
    [SerializeField, Range(0f, 1f)] private float defaultOpacity = 0.7f;
    [SerializeField, Range(0f, 1f)] private float hoverOpacity = 1f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        button.onClick.AddListener(OnClicked);
        ApplyDefaultVisual();
    }

    private void OnEnable()
    {
        ApplyDefaultVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyHoverVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyDefaultVisual();
    }

    private void OnClicked()
    {
        if (pauseMenuController != null)
            pauseMenuController.OpenRestartConfirm();
    }

    private void ApplyDefaultVisual()
    {
        if (targetImage == null)
            return;

        if (defaultSprite != null)
            targetImage.sprite = defaultSprite;

        Color color = targetImage.color;
        color.a = defaultOpacity;
        targetImage.color = color;
    }

    private void ApplyHoverVisual()
    {
        if (targetImage == null)
            return;

        if (hoverSprite != null)
            targetImage.sprite = hoverSprite;

        Color color = targetImage.color;
        color.a = hoverOpacity;
        targetImage.color = color;
    }
}