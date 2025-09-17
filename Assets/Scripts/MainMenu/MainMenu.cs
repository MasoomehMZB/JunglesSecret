using System.Collections;
using System.Net;
using Mirror;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject inputPanel;

    [SerializeField] private MyNetworkManager networkManager;

    [SerializeField] private float connectTimeout = 5f; // seconds

    private Coroutine connectRoutine;

    private void Start()
    {
        // Hide IP input until Join button is clicked
        inputPanel.SetActive(false);
        statusText.text = "Welcome! \nChoose Start or Join.";
    }

    // --- HOST ---
    public void OnHostButton()
    {
        networkManager.StartHostGame();

        string hostIP = NetworkUtils.GetLocalIPv4();
        GameSession.Instance.hostIP = hostIP;

        statusText.text = $"Hosting game...";
    }


    // --- PREPARE JOIN ---
    public void OnJoinButton()
    {
        statusText.text = "Enter the Host's IP Address";
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
            statusText.text = $" Invalid IP";
            return;
        }

        // Start connecting
        networkManager.JoinGame(ip);
        statusText.text = $"Trying to connect to \n{ip}";

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
                statusText.text = $" Connected to \n{ip}";
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Timeout reached, still not connected
        if (!NetworkClient.isConnected)
        {
            statusText.text = $" Failed to connect to \n{ip} (timeout).";
            networkManager.StopClient();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}

