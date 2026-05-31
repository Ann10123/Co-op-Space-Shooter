using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи зі слайдером

public class EnemyController : NetworkBehaviour
{
    // Максимальне здоров'я, яке можна вільно змінювати в Інспекторі Unity
    public int maxHealth = 50;

    // Мережева змінна для синхронізації (тепер вона стартує порожньою)
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    public float speed = 3f;
    [SerializeField] private Slider healthSlider; 

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float searchTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth.Value;
        }
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        if (healthSlider != null)
        {
            healthSlider.value = newVal;
        }
    }

    void Update()
    {
        if (!IsServer) return;

        searchTimer += Time.deltaTime;
        if (searchTimer >= 1f || targetPlayer == null)
        {
            FindClosestPlayer();
            searchTimer = 0f;
        }

        if (targetPlayer != null)
        {
            RotateTowardsTarget();
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        if (targetPlayer != null && rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void FindClosestPlayer()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (var client in connectedClients)
        {
            if (client.PlayerObject != null)
            {
                Transform playerTransform = client.PlayerObject.transform;
                float distance = Vector3.Distance(transform.position, playerTransform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = playerTransform;
                }
            }
        }
        targetPlayer = closest;
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = targetPlayer.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        int newHealth = currentHealth.Value - damage;
        if (newHealth < 0) newHealth = 0;

        currentHealth.Value = newHealth;

        if (currentHealth.Value <= 0)
        {
            if (NetworkObject.IsSpawned)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(10);
                }

                NetworkObject.Despawn(true);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();

        if (player != null)
        {
            NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.IsSpawned)
            {
                playerNetworkObject.Despawn(true);
            }
        }
    }
}