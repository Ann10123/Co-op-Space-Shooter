using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject pausePanel;

    [Header("Prefabs")]
    public GameObject playerPrefab; 

    private NetworkVariable<int> score = new NetworkVariable<int>(0);
    private NetworkVariable<int> surviveTime = new NetworkVariable<int>(0);

    [Header("Wave UI")]
    public TextMeshProUGUI waveText;

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
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !IsServer) return;

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

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ResetSpawner();
        }
        var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        foreach (var netObj in spawnedObjects)
        {
            if (netObj != null && netObj.CompareTag("Enemy"))
            {
                netObj.Despawn(true); 
            }
        }
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) 
            {
                GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId, true);
            }
            else 
            {
                client.PlayerObject.gameObject.SetActive(true);
                client.PlayerObject.transform.position = Vector3.zero;
            }
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

    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    [ClientRpc]
    public void ShowWaveAnnouncementClientRpc(string waveName)
    {
        if (waveText != null)
        {
            StartCoroutine(WaveAnnouncementRoutine(waveName));
        }
    }

    private System.Collections.IEnumerator WaveAnnouncementRoutine(string waveName)
    {
        waveText.text = waveName; 
        waveText.gameObject.SetActive(true); 

        yield return new WaitForSeconds(2.5f);

        waveText.gameObject.SetActive(false); 
    }
}