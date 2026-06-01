using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 50; 
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private NetworkVariable<int> syncedMaxHealth = new NetworkVariable<int>();
    public AudioClip deathSound;

    [Header("UI")]
    public float speed = 3f;
    [SerializeField] private Slider healthSlider;
    public GameObject explosionPrefab;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float searchTimer = 0f;
    private int pointsForKill = 10;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        if (!IsServer) return;

        syncedMaxHealth.Value = newMaxHealth;
        currentHealth.Value = newMaxHealth;
    }
    public void SetScoreValue(int points)
    {
        pointsForKill = points;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (syncedMaxHealth.Value == 0)
            {
                syncedMaxHealth.Value = maxHealth;
                currentHealth.Value = maxHealth;
            }
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = syncedMaxHealth.Value;
            healthSlider.value = currentHealth.Value;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
        syncedMaxHealth.OnValueChanged += OnMaxHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        syncedMaxHealth.OnValueChanged -= OnMaxHealthChanged;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        if (healthSlider != null) healthSlider.value = newVal;
    }

    private void OnMaxHealthChanged(int oldVal, int newVal)
    {
        if (healthSlider != null) healthSlider.maxValue = newVal;
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
                    GameManager.Instance.AddScore(pointsForKill);
                }
                PlayDeathSoundClientRpc(transform.position);
                NetworkObject.Despawn(true);
            }
        }
    }

    [ClientRpc]
    private void PlayDeathSoundClientRpc(Vector3 position)
    {
        if (deathSound != null)
        {
            Vector3 soundPosition = new Vector3(position.x, position.y, Camera.main.transform.position.z);
            AudioSource.PlayClipAtPoint(deathSound, soundPosition);
        }
        if (explosionPrefab != null)
        {
            GameObject effect = Instantiate(explosionPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
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
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckIfAllDead();
                }
            }
        }
    }
}