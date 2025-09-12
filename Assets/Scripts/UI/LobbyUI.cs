using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Collections;

public class LobbyUI : NetworkBehaviour
{
    public static LobbyUI Instance;

    public GameObject lobbyPanel;
    public Button readyButton;
    public Button backButton;
    public Button startButton; 
    public TMP_Text ReadyCount;

    [SyncVar (hook = nameof(OnGameStarted))] bool gameStarted = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        backButton.onClick.AddListener(OnBackClicked);
        startButton.onClick.AddListener(OnStartClicked);

        // Only host sees Start button
        startButton.gameObject.SetActive(NetworkServer.active);
    }

    void OnReadyClicked()
    {
        Player.localPlayer.SetReady(true); 
        readyButton.interactable = false;
        readyButton.GetComponent<Image>().color = Color.green;
    }

    void OnBackClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        UpdatePlayerReady();
    }

    void OnStartClicked()
    {
        if (AllPlayersReady())
        {
            lobbyPanel.SetActive(false); 
            gameStarted = true;
            GameManager.Instance.StartTurn();
        }
        else
        {
            Debug.Log("Not all players are ready!");
        }
    }

    public bool AllPlayersReady()
    {
        return GameManager.Instance.players.All(p => p.isReady);
    }

    public void UpdatePlayerReady()
    {
        var players = FindObjectsOfType<Player>();
        int readyCount = players.Count(p => p.isReady);
        ReadyCount.text = $"Ready Players: {readyCount}/{players.Length}";
    }

    void OnGameStarted(bool oldValue, bool newValue)
    {
        if (gameStarted)
        {
            lobbyPanel.SetActive(false);
        }
    }

}

