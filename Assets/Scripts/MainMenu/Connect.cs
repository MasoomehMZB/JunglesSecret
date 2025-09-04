using UnityEngine;
using Mirror;

public class Connect : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    // --- HOST ---
    public void StartHost()
    {
        networkManager.StartHost();
        Debug.Log("Host started & advertising");
    }

    // --- JOIN (by user input) ---
    public void JoinFromInput(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("No IP entered.");
            return;
        }

        //networkManager.networkAddress = ip;
        networkManager.networkAddress = "LocalHost";
        networkManager.StartClient();
        Debug.Log("Trying to connect to server at: " + ip);
    }
}
