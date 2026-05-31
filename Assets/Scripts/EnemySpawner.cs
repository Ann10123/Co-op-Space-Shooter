using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Налаштування префабу")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Налаштування випадкового часу")]
    public float minSpawnTime = 1f; // Мінімальний час очікування (1 секунда)
    public float maxSpawnTime = 5f; // Максимальний час очікування (5 секунд)

    [Header("Налаштування радіусу")]
    public float spawnRadius = 8f;

    private float nextSpawnTime; // Змінна, яка зберігає час, коли має з'явитися наступний ворог
    private float timer;         // Наш внутрішній лічильник часу

    public override void OnNetworkSpawn()
    {
        // Якщо ми на сервері, одразу задаємо випадковий час для першого ворога
        if (IsServer)
        {
            SetRandomNextSpawnTime();
        }
    }

    void Update()
    {
        // Роботу з таймером виконує ТІЛЬКИ сервер
        if (!IsSpawned || !IsServer) return;

        // Рахуємо час, який пройшов
        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            SpawnEnemy();
            timer = 0f;
            SetRandomNextSpawnTime();
        }
    }

    private void SetRandomNextSpawnTime()
    {
        // Обираємо випадкове число в заданих межах (наприклад, між 1 та 5 секундами)
        nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }

    private void SpawnEnemy()
    {
        // Генеруємо випадкову точку на карті
        Vector2 randomPosition = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        // Створюємо об'єкт
        GameObject enemy = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);

        // Спавнимо в мережі для всіх гравців
        enemy.GetComponent<NetworkObject>().Spawn();
    }
}