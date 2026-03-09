using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsMenuController : MonoBehaviour
{
    private const string MasterVolumeKey = "settings_master_volume";
    private const string MusicVolumeKey = "settings_music_volume";
    private const string SfxVolumeKey = "settings_sfx_volume";
    private const string ResolutionIndexKey = "settings_resolution_index";

    [Header("References")]
    [SerializeField] private PauseMenuController pauseMenuController;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;

    [Header("Resolution Options")]
    [Tooltip("Uses clean integer scale options based on the 480x256 reference resolution.")]
    [SerializeField] private bool includeFiveXOption = true;

    private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();
    private bool suppressCallbacks;

    private void Awake()
    {
        BuildResolutionList();
        HookUpUIEvents();
        LoadSavedSettings();
    }

    private void BuildResolutionList()
    {
        availableResolutions.Clear();

        // Pixel-perfect integer scales of 480x256
        availableResolutions.Add(new Vector2Int(480, 256));   // 1x
        availableResolutions.Add(new Vector2Int(960, 512));   // 2x
        availableResolutions.Add(new Vector2Int(1440, 768));  // 3x
        availableResolutions.Add(new Vector2Int(1920, 1024)); // 4x

        if (includeFiveXOption)
            availableResolutions.Add(new Vector2Int(2400, 1280)); // 5x

        if (resolutionDropdown == null)
            return;

        List<string> options = new List<string>();

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Vector2Int res = availableResolutions[i];
            int scale = i + 1;
            options.Add($"{res.x} x {res.y} ({scale}x)");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();
    }

    private void HookUpUIEvents()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

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

        int resolutionIndex = GetSavedResolutionIndex();
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, Mathf.Max(0, availableResolutions.Count - 1));

        if (resolutionDropdown != null && availableResolutions.Count > 0)
        {
            resolutionDropdown.value = resolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;

        ApplyResolution(resolutionIndex);
        ApplyMasterVolume(masterVolume);
        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);

        suppressCallbacks = false;
    }

    public void OnBackPressed()
    {
        pauseMenuController?.CloseSettingsToPauseMenu();
    }

    public void OnRevertDefaultsPressed()
    {
        suppressCallbacks = true;

        int defaultResolutionIndex = GetDefaultResolutionIndex();

        if (resolutionDropdown != null && availableResolutions.Count > 0)
        {
            resolutionDropdown.value = defaultResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = defaultMasterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = defaultMusicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = defaultSfxVolume;

        suppressCallbacks = false;

        ApplyResolution(defaultResolutionIndex);
        ApplyMasterVolume(defaultMasterVolume);
        ApplyMusicVolume(defaultMusicVolume);
        ApplySfxVolume(defaultSfxVolume);

        SaveResolutionIndex(defaultResolutionIndex);
        PlayerPrefs.SetFloat(MasterVolumeKey, defaultMasterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, defaultMusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, defaultSfxVolume);
        PlayerPrefs.Save();
    }

    private void OnResolutionChanged(int index)
    {
        if (suppressCallbacks)
            return;

        ApplyResolution(index);
        SaveResolutionIndex(index);
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

    private void ApplyResolution(int index)
    {
        if (availableResolutions.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
        Vector2Int res = availableResolutions[index];

        Screen.SetResolution(res.x, res.y, Screen.fullScreenMode);
    }

    private void ApplyMasterVolume(float value)
    {
        // Placeholder for future audio manager hookup.
        // Example later:
        // AudioManager.Instance.SetMasterVolume(value);
    }

    private void ApplyMusicVolume(float value)
    {
        // Placeholder for future audio manager hookup.
        // Example later:
        // AudioManager.Instance.SetMusicVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        // Placeholder for future audio manager hookup.
        // Example later:
        // AudioManager.Instance.SetSfxVolume(value);
    }

    private int GetSavedResolutionIndex()
    {
        if (!PlayerPrefs.HasKey(ResolutionIndexKey))
            return GetDefaultResolutionIndex();

        return PlayerPrefs.GetInt(ResolutionIndexKey);
    }

    private void SaveResolutionIndex(int index)
    {
        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.Save();
    }

    private int GetDefaultResolutionIndex()
    {
        if (availableResolutions.Count == 0)
            return 0;

        int currentWidth = Screen.width;
        int currentHeight = Screen.height;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].x == currentWidth &&
                availableResolutions[i].y == currentHeight)
            {
                return i;
            }
        }

        // Default to 4x if available, otherwise highest valid option.
        int preferredIndex = 3;
        return Mathf.Clamp(preferredIndex, 0, availableResolutions.Count - 1);
    }
}