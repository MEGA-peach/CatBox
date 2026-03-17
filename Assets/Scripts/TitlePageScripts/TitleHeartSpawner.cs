using System.Collections;
using UnityEngine;

public class TitleHeartSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Timing")]
    [SerializeField] private float firstSpawnDelay = 5f;
    [SerializeField] private float minDelay = 10f;
    [SerializeField] private float maxDelay = 60f;

    [Header("Spawn Variation")]
    [SerializeField] private float spawnOffsetX = 0.1f;
    [SerializeField] private float spawnOffsetY = 0.05f;

    private void Start()
    {
        StartCoroutine(HeartRoutine());
    }

    private IEnumerator HeartRoutine()
    {
        // First heart after a short delay
        yield return new WaitForSeconds(firstSpawnDelay);
        SpawnHeart();

        // Normal random spawning
        while (true)
        {
            float waitTime = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(waitTime);

            SpawnHeart();
        }
    }

    private void SpawnHeart()
    {
        if (heartPrefab == null || spawnPoint == null)
            return;

        Vector3 offset = new Vector3(
            Random.Range(-spawnOffsetX, spawnOffsetX),
            Random.Range(-spawnOffsetY, spawnOffsetY),
            0f
        );

        GameObject heartObject = Instantiate(
            heartPrefab,
            spawnPoint.position + offset,
            Quaternion.identity
        );

        FloatingSpriteEffect floatingEffect = heartObject.GetComponent<FloatingSpriteEffect>();
        if (floatingEffect != null)
        {
            floatingEffect.Play();
        }
    }
}