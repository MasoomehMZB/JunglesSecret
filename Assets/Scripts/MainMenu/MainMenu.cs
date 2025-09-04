using UnityEngine;
using TMPro;
using Mirror;
using System.Net;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;   
    [SerializeField] private GameObject inputPanel; 

    [Header("Dependencies")]
    [SerializeField] private Connect connect;

    [SerializeField] private NetworkManager networkManager;

    [Header("Settings")]
    [SerializeField] private float connectTimeout = 5f; // seconds

    private Coroutine connectRoutine;

    private void Start()
    {
        // Hide IP input until Join button is clicked
        inputPanel.SetActive(false);
        statusText.text = "Welcome! Choose Host or Join.";
    }

    // --- HOST ---
    public void OnHostButton()
    {
        connect.StartHost();

        string hostIP = NetworkUtils.GetLocalIPv4();
        GameSession.Instance.hostIP = hostIP;

        statusText.text = $"Hosting game...\nYour IP: {hostIP}\nWaiting for players.";
    }


    // --- PREPARE JOIN ---
    public void OnJoinButton()
    {
        statusText.text = "Enter the Host's IP Address:";
        inputPanel.SetActive(true);
        ipInputField.text = ""; 
    }

    // --- JOIN WITH INPUT ---


    public void OnConfirmJoin()
    {
        string ip = ipInputField.text.Trim();

        // Empty check
        if (string.IsNullOrEmpty(ip))
        {
            statusText.text = " Please enter an IP address.";
            return;
        }

        // IP validation
        if (!IPAddress.TryParse(ip, out _))
        {
            statusText.text = $" Invalid IP: {ip}";
            return;
        }

        // Start connecting
        connect.JoinFromInput(ip);
        statusText.text = $"Trying to connect to {ip}...";

        // Start timeout coroutine
        if (connectRoutine != null) StopCoroutine(connectRoutine);
        connectRoutine = StartCoroutine(ConnectTimeout(ip));
    }

    private IEnumerator ConnectTimeout(string ip)
    {
        float timer = 0f;
        while (timer < connectTimeout)
        {
            // Check if client successfully connected
            if (NetworkClient.isConnected)
            {
                statusText.text = $" Connected to {ip}";
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Timeout reached, still not connected
        if (!NetworkClient.isConnected)
        {
            statusText.text = $" Failed to connect to {ip} (timeout).";
            // Optionally: stop the client so it doesn’t keep retrying
            networkManager.StopClient();
        }
    }
}

