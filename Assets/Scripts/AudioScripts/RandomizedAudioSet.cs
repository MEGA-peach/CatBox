using UnityEngine;

[System.Serializable]
public class RandomizedAudioSet
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1f;
    [SerializeField] private bool allowImmediateRepeat = false;

    public bool HasAnyClips
    {
        get
        {
            return clips != null && clips.Length > 0;
        }
    }

    public float VolumeScale => volumeScale;

    public AudioClip GetRandomClip(ref int lastIndex, out float pitch)
    {
        pitch = Random.Range(minPitch, maxPitch);

        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
        {
            lastIndex = 0;
            return clips[0];
        }

        int index = lastIndex;

        if (allowImmediateRepeat)
        {
            index = Random.Range(0, clips.Length);
        }
        else
        {
            int safety = 0;

            while (index == lastIndex && safety < 20)
            {
                index = Random.Range(0, clips.Length);
                safety++;
            }
        }

        lastIndex = index;
        return clips[index];
    }
}