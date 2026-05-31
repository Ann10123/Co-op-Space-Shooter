using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject pausePanel;
    private NetworkVariable<int> score = new NetworkVariable<int>(0);
    private NetworkVariable<int> surviveTime = new NetworkVariable<int>(0);

    private float timer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        Time.timeScale = 1f;
    }

    public override void OnNetworkSpawn()
    {
        score.OnValueChanged += UpdateScoreUI;
        surviveTime.OnValueChanged += UpdateTimerUI;

        UpdateScoreUI(0, score.Value);
        UpdateTimerUI(0, surviveTime.Value);
    }

    void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= 1f) 
        {
            surviveTime.Value++;
            timer = 0f;
        }
    }

    public void AddScore(int points)
    {
        if (!IsServer) return;
        score.Value += points;
    }

    private void UpdateScoreUI(int oldVal, int newVal)
    {
        if (scoreText != null) scoreText.text = "Score: " + newVal;
    }
    private void UpdateTimerUI(int oldVal, int newVal)
    {
        if (timerText != null) timerText.text = "Time: " + newVal + " s";
    }

    public void TogglePause()
    {
        if (pausePanel != null)
        {
            bool isCurrentlyPaused = pausePanel.activeSelf;
            bool newPauseState = !isCurrentlyPaused;
            pausePanel.SetActive(newPauseState);
            Time.timeScale = newPauseState ? 0f : 1f;
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc()
    {
        if (!IsServer) return;

        score.Value = 0;
        surviveTime.Value = 0;
        timer = 0f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
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

        GameObject[] players = GameObject.FindGameObjectsWithTag("Players");
        foreach (GameObject player in players)
        {
            player.transform.position = Vector3.zero; 
        }

        ResetGameUIClientRpc();
    }

    [ClientRpc]
    private void ResetGameUIClientRpc()
    {
        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
}