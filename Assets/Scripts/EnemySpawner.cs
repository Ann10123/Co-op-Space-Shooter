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

        [Header("Complexity wave")]
        public float enemySizeMultiplier; 
        public int enemyHealth;
        public int scorePerEnemy;
    }

    [SerializeField] private GameObject defaultEnemyPrefab;


    [Header("Wave")]
    public List<Wave> waves;

    [Header("Safe Spawn Settings")]
    public float spawnRadius = 8f;
    public float safeRadius = 2.5f;
    public int maxSpawnAttempts = 10;

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
            GameManager.Instance.TriggerWin();
        }
    }

    private Vector2 GetSafeSpawnPosition()
    {
        Vector2 potentialPosition = Vector2.zero;
        bool isSafe = false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            potentialPosition = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(potentialPosition, safeRadius);

            bool foundPlayer = false;

            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag("Players")) 
                {
                    foundPlayer = true;
                    break; 
                }
            }

            if (!foundPlayer)
            {
                isSafe = true;
                break;
            }
        }
        return potentialPosition;
    }

    private void SpawnEnemy(Wave wave)
    {
        GameObject prefabToSpawn = defaultEnemyPrefab;
        if (wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0)
        {
            prefabToSpawn = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Length)];
        }
        if (prefabToSpawn == null) return;

        Vector2 safePosition = GetSafeSpawnPosition();
        GameObject enemy = Instantiate(prefabToSpawn, safePosition, Quaternion.identity);

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