using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject networkUIContainer;

    // Цей метод ми прив'яжемо до кнопки Host
    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            HideUI();
        }
    }

    // Цей метод ми прив'яжемо до кнопки Client
    public void StartClient()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            HideUI();
        }
    }

    // Метод для приховування кнопок
    private void HideUI()
    {
        if (networkUIContainer != null)
        {
            networkUIContainer.SetActive(false);
        }
    }
}