using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler
{
    [Header("Audio")]
    [SerializeField] private RandomizedAudioSet hoverSounds;
    [SerializeField] private RandomizedAudioSet clickSounds;

    private Button button;
    private int lastHoverIndex = -1;
    private int lastClickIndex = -1;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        AudioManager.Instance?.PlaySfx(hoverSounds, ref lastHoverIndex);
    }

    private void HandleClick()
    {
        if (button != null && !button.interactable)
            return;

        AudioManager.Instance?.PlaySfx(clickSounds, ref lastClickIndex);
    }
}