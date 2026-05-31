using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    private void OnGUI()
    {
        // ЗАХИСТ: Якщо NetworkManager ще не завантажився, просто виходимо
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host (Створити кімнату)"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUILayout.Button("Client (Приєднатися)"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        GUILayout.EndArea();
    }
}