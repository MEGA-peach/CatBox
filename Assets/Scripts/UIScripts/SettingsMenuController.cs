using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsMenuController : MonoBehaviour
{
    private const string MasterVolumeKey = "settings_master_volume";
    private const string MusicVolumeKey = "settings_music_volume";
    private const string SfxVolumeKey = "settings_sfx_volume";
    private const string FullscreenKey = "settings_fullscreen";

    [Header("References")]
    [SerializeField] private PauseMenuController pauseMenuController;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Text resolutionLabel;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;
    [SerializeField] private bool defaultFullscreen = true;

    [Header("Windowed Mode")]
    [Tooltip("If enabled, windowed mode will use a slightly smaller size than the desktop so it fits cleanly on screen.")]
    [SerializeField] private bool useReducedWindowedSize = true;

    [Tooltip("Amount to subtract from desktop width/height in windowed mode.")]
    [SerializeField] private Vector2Int windowedPadding = new Vector2Int(160, 120);

    private bool suppressCallbacks;

    private void Awake()
    {
        HookUpUIEvents();
        LoadSavedSettings();
    }

    private void HookUpUIEvents()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void LoadSavedSettings()
    {
        suppressCallbacks = true;

        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, defaultFullscreen ? 1 : 0) == 1;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen;

        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;

        ApplyFullscreen(fullscreen);
        ApplyMasterVolume(masterVolume);
        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);
        RefreshResolutionLabel();

        suppressCallbacks = false;
    }

    public void OnBackPressed()
    {
        pauseMenuController?.CloseSettingsToPauseMenu();
    }

    public void OnRevertDefaultsPressed()
    {
        suppressCallbacks = true;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = defaultFullscreen;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = defaultMasterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = defaultMusicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = defaultSfxVolume;

        suppressCallbacks = false;

        ApplyFullscreen(defaultFullscreen);
        ApplyMasterVolume(defaultMasterVolume);
        ApplyMusicVolume(defaultMusicVolume);
        ApplySfxVolume(defaultSfxVolume);

        PlayerPrefs.SetInt(FullscreenKey, defaultFullscreen ? 1 : 0);
        PlayerPrefs.SetFloat(MasterVolumeKey, defaultMasterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, defaultMusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, defaultSfxVolume);
        PlayerPrefs.Save();

        RefreshResolutionLabel();
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        if (suppressCallbacks)
            return;

        ApplyFullscreen(isFullscreen);
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        RefreshResolutionLabel();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (suppressCallbacks)
            return;

        ApplyMasterVolume(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (suppressCallbacks)
            return;

        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (suppressCallbacks)
            return;

        ApplySfxVolume(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyFullscreen(bool fullscreen)
    {
        Resolution desktop = Screen.currentResolution;

        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            int width = desktop.width;
            int height = desktop.height;

            if (useReducedWindowedSize)
            {
                width = Mathf.Max(1280, desktop.width - windowedPadding.x);
                height = Mathf.Max(720, desktop.height - windowedPadding.y);
            }

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }

    private void RefreshResolutionLabel()
    {
        if (resolutionLabel == null)
            return;

        Resolution desktop = Screen.currentResolution;
        string mode = Screen.fullScreen ? "Fullscreen" : "Windowed";

        resolutionLabel.text = $"UI Reference: 3840 x 2160\nDisplay: {Screen.width} x {Screen.height} ({mode})\nDesktop: {desktop.width} x {desktop.height}";
    }

    private void ApplyMasterVolume(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void ApplyMusicVolume(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
    }
}