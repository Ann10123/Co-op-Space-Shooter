using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    public float speed = 15f;
    public float lifeTime = 4f; // Куля автоматично зникне через 4 секунди, якщо нікуди не влучить

    public override void OnNetworkSpawn()
    {
        // РУХОМ КЕРУЄ ТІЛЬКИ СЕРВЕР
        if (IsServer)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Штовхаємо кулю вперед (вгору щодо її власного розвороту)
                rb.linearVelocity = transform.up * speed;
            }

            // Запускаємо таймер знищення, щоб кулі не забивали пам'ять
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    // ЗІТКНЕННЯ: Що робити, коли куля в щось влучає
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Перевіряємо влучання тільки на сервері
        if (!IsServer) return;

        // Перевіряємо, чи влучили ми саме у ворога
        if (collision.CompareTag("Enemy"))
        {
            // Шукаємо скрипт Enemy на об'єкті, в який влучили
            EnemyController enemyScript = collision.GetComponent<EnemyController>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(5); // Завдаємо 1 одиницю шкоди
            }

            // Знищуємо кулю після влучання
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    private void DestroyBullet()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // Видаляємо з мережі для всіх гравців
        }
    }
}