using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MasterVolumeKey = "settings_master_volume";
    private const string MusicVolumeKey = "settings_music_volume";
    private const string SfxVolumeKey = "settings_sfx_volume";

    [Header("Music")]
    [SerializeField] private AudioClip[] musicPlaylist;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private float musicCrossfadeDuration = 1.5f;
    [SerializeField, Range(0f, 1f)] private float musicPlaylistVolume = 0.65f;

    [Header("SFX Pool")]
    [SerializeField] private int pooledSfxSources = 12;

    [Header("Default Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = .5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private Coroutine musicRoutine;
    private int lastMusicIndex = -1;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private readonly List<ActiveSfxSource> activeSfxSources = new List<ActiveSfxSource>();

    private class ActiveSfxSource
    {
        public AudioSource source;
        public Object owner;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateMusicSources();
        CreateSfxSources();
        LoadSavedVolumes();
        ApplyMusicSourceVolumes();
    }

    private void Start()
    {
        if (playMusicOnStart)
            StartMusicPlaylist();
    }

    public void StartMusicPlaylist()
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        if (musicPlaylist == null || musicPlaylist.Length == 0)
            return;

        musicRoutine = StartCoroutine(MusicPlaylistRoutine());
    }

    public void StopMusic()
    {
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
            musicRoutine = null;
        }

        if (musicSourceA != null)
            musicSourceA.Stop();

        if (musicSourceB != null)
            musicSourceB.Stop();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyMusicSourceVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyMusicSourceVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    public void PlaySfx(RandomizedAudioSet audioSet, float volumeMultiplier = 1f)
    {
        int tempIndex = -1;
        PlaySfx(audioSet, ref tempIndex, volumeMultiplier);
    }

    public void PlaySfx(RandomizedAudioSet audioSet, ref int lastIndex, float volumeMultiplier = 1f)
    {
        if (audioSet == null || !audioSet.HasAnyClips)
            return;

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
            return;

        AudioClip clip = audioSet.GetRandomClip(ref lastIndex, out float pitch);
        if (clip == null)
            return;

        source.loop = false;
        source.clip = clip;
        source.pitch = pitch;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * audioSet.VolumeScale * volumeMultiplier);
        source.Play();
    }

    public float GetScaledSfxVolume(float rawVolume = 1f)
    {
        return Mathf.Clamp01(rawVolume * masterVolume * sfxVolume);
    }

    private IEnumerator MusicPlaylistRoutine()
    {
        AudioClip firstClip = GetNextMusicClip();
        if (firstClip == null)
            yield break;

        activeMusicSource.clip = firstClip;
        activeMusicSource.volume = masterVolume * musicVolume * musicPlaylistVolume;
        activeMusicSource.loop = false;
        activeMusicSource.Play();

        while (true)
        {
            if (activeMusicSource.clip == null)
                yield break;

            float clipLength = activeMusicSource.clip.length;
            float waitTime = Mathf.Max(0f, clipLength - musicCrossfadeDuration);

            yield return new WaitForSeconds(waitTime);

            AudioClip nextClip = GetNextMusicClip();
            if (nextClip == null)
                yield break;

            inactiveMusicSource.clip = nextClip;
            inactiveMusicSource.volume = 0f;
            inactiveMusicSource.loop = false;
            inactiveMusicSource.Play();

            float duration = Mathf.Max(0.01f, musicCrossfadeDuration);
            float elapsed = 0f;
            float targetMusicVolume = masterVolume * musicVolume * musicPlaylistVolume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                activeMusicSource.volume = Mathf.Lerp(targetMusicVolume, 0f, t);
                inactiveMusicSource.volume = Mathf.Lerp(0f, targetMusicVolume, t);

                yield return null;
            }

            activeMusicSource.Stop();
            activeMusicSource.volume = 0f;
            inactiveMusicSource.volume = targetMusicVolume;

            SwapMusicSources();
        }
    }

    private AudioClip GetNextMusicClip()
    {
        if (musicPlaylist == null || musicPlaylist.Length == 0)
            return null;

        if (musicPlaylist.Length == 1)
        {
            lastMusicIndex = 0;
            return musicPlaylist[0];
        }

        int nextIndex = lastMusicIndex;
        int safety = 0;

        while (nextIndex == lastMusicIndex && safety < 20)
        {
            nextIndex = Random.Range(0, musicPlaylist.Length);
            safety++;
        }

        lastMusicIndex = nextIndex;
        return musicPlaylist[nextIndex];
    }

    private void ApplyMusicSourceVolumes()
    {
        float scaledMusic = masterVolume * musicVolume * musicPlaylistVolume;

        if (activeMusicSource != null && activeMusicSource.isPlaying)
            activeMusicSource.volume = scaledMusic;

        if (inactiveMusicSource != null && inactiveMusicSource.isPlaying && inactiveMusicSource.volume > 0f)
            inactiveMusicSource.volume = scaledMusic;
    }

    private void LoadSavedVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
    }

    private void CreateMusicSources()
    {
        musicSourceA = CreateChildSource("Music Source A");
        musicSourceB = CreateChildSource("Music Source B");

        musicSourceA.playOnAwake = false;
        musicSourceB.playOnAwake = false;

        musicSourceA.loop = false;
        musicSourceB.loop = false;

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    private void CreateSfxSources()
    {
        sfxSources.Clear();

        for (int i = 0; i < pooledSfxSources; i++)
        {
            AudioSource source = CreateChildSource("SFX Source " + i);
            source.playOnAwake = false;
            source.loop = false;
            sfxSources.Add(source);
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (!sfxSources[i].isPlaying)
                return sfxSources[i];
        }

        return sfxSources.Count > 0 ? sfxSources[0] : null;
    }

    private AudioSource CreateChildSource(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;

        AudioSource source = child.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        return source;
    }

    private void SwapMusicSources()
    {
        AudioSource temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;
    }

    public void SetMusicPlaylist(AudioClip[] newPlaylist, float playlistVolume = 1f, bool restartNow = true)
    {
        musicPlaylist = newPlaylist;
        musicPlaylistVolume = Mathf.Clamp01(playlistVolume);

        if (!restartNow)
            return;

        StopMusic();

        if (musicPlaylist != null && musicPlaylist.Length > 0)
            StartMusicPlaylist();
    }

    public void PlaySfxForOwner(Object owner, RandomizedAudioSet audioSet, ref int lastIndex, float volumeMultiplier = 1f)
    {
        if (audioSet == null || !audioSet.HasAnyClips)
            return;

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
            return;

        AudioClip clip = audioSet.GetRandomClip(ref lastIndex, out float pitch);
        if (clip == null)
            return;

        source.loop = false;
        source.clip = clip;
        source.pitch = pitch;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * audioSet.VolumeScale * volumeMultiplier);
        source.Play();

        ActiveSfxSource existing = activeSfxSources.Find(x => x.owner == owner);
        if (existing != null)
        {
            existing.source = source;
        }
        else
        {
            activeSfxSources.Add(new ActiveSfxSource
            {
                owner = owner,
                source = source
            });
        }
    }

    public void StopTaggedSfx(Object owner)
    {
        if (owner == null)
            return;

        for (int i = activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (activeSfxSources[i].owner != owner)
                continue;

            if (activeSfxSources[i].source != null)
            {
                activeSfxSources[i].source.Stop();
                activeSfxSources[i].source.clip = null;
            }

            activeSfxSources.RemoveAt(i);
        }
    }

    public void PauseMusic()
    {
        if (activeMusicSource != null && activeMusicSource.isPlaying)
            activeMusicSource.Pause();

        if (inactiveMusicSource != null && inactiveMusicSource.isPlaying)
            inactiveMusicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (activeMusicSource != null && activeMusicSource.clip != null)
            activeMusicSource.UnPause();

        if (inactiveMusicSource != null && inactiveMusicSource.clip != null)
            inactiveMusicSource.UnPause();
    }
}