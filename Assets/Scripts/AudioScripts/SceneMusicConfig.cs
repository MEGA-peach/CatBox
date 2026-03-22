using UnityEngine;

[DisallowMultipleComponent]
public class SceneMusicConfig : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField, Range(0f, 1f)] private float playlistVolume = 0.65f;
    [SerializeField] private bool restartPlaylistWhenSceneLoads = true;

    private void Start()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMusicPlaylist(playlist, playlistVolume, restartPlaylistWhenSceneLoads);
    }
}