using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    public float speed = 15f;
    public float lifeTime = 4f; 

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = transform.up * speed;
            }
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemyScript = collision.GetComponent<EnemyController>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(5); 
            }
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
            NetworkObject.Despawn(true); 
        }
    }
}