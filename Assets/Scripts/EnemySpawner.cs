using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance;

    [System.Serializable]
    public struct Wave
    {
        public string waveName;
        public GameObject[] enemyPrefabs;
        public int enemyCount;
        public float spawnInterval;
        public float delayBeforeWave;

        [Header("Налаштування складності хвилі")]
        public float enemySizeMultiplier; 
        public int enemyHealth;
        public int scorePerEnemy;
    }

    [Header("Налаштування за замовчуванням")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    public float spawnRadius = 8f;

    [Header("Налаштування Хвиль")]
    public List<Wave> waves;

    private int currentWaveIndex = 0;
    private Coroutine waveCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartWaves();
    }

    public void StartWaves()
    {
        if (!IsServer) return;

        StopAllCoroutines();
        waveCoroutine = null;
        currentWaveIndex = 0;

        waveCoroutine = StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (currentWaveIndex < waves.Count)
        {
            Wave currentWave = waves[currentWaveIndex];
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowWaveAnnouncementClientRpc(currentWave.waveName);
            }
            yield return new WaitForSeconds(currentWave.delayBeforeWave);

            for (int i = 0; i < currentWave.enemyCount; i++)
            {
                SpawnEnemy(currentWave);
                yield return new WaitForSeconds(currentWave.spawnInterval);
            }

            while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
            {
                yield return new WaitForSeconds(1f);
            }
            currentWaveIndex++;
        }

        if (GameManager.Instance != null)
        {
            //GameManager.Instance.TriggerGameOver(true);
        }
    }

    private void SpawnEnemy(Wave wave)
    {
        GameObject prefabToSpawn = defaultEnemyPrefab;
        if (wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0)
        {
            prefabToSpawn = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Length)];
        }

        if (prefabToSpawn == null) return;

        Vector2 randomPosition = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
        GameObject enemy = Instantiate(prefabToSpawn, randomPosition, Quaternion.identity);

        float finalMultiplier = wave.enemySizeMultiplier > 0 ? wave.enemySizeMultiplier : 1f;
        enemy.transform.localScale = prefabToSpawn.transform.localScale * finalMultiplier;

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null && wave.enemyHealth > 0)
        {
            enemyController.SetMaxHealth(wave.enemyHealth);
            enemyController.SetScoreValue(wave.scorePerEnemy);
        }

        enemy.GetComponent<NetworkObject>().Spawn();
    }

    public void ResetSpawner()
    {
        if (!IsServer) return;

        StopAllCoroutines();
        waveCoroutine = null;

        currentWaveIndex = 0;

        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in activeEnemies)
        {
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
            else
            {
                Destroy(enemy);
            }
        }
        waveCoroutine = StartCoroutine(WaveRoutine());
    }
}