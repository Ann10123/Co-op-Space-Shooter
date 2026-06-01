using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject networkUIContainer;

    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            HideUI();
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            HideUI();
        }
    }

    private void HideUI()
    {
        if (networkUIContainer != null)
        {
            networkUIContainer.SetActive(false);
        }
    }
}