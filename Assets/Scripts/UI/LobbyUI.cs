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
    public Image avatarImage;

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
        AddAvatar();
    }

    void OnReadyClicked()
    {
        Player.localPlayer.SetReady(true); 
        readyButton.interactable = false;
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
    public void AddAvatar()
    {
        if (Player.localPlayer == null) return;
        int idx = Player.localPlayer.characterIndex;
        Debug.Log($"Refreshing avatar for character index: {idx}");
        Sprite idle = CharacterDatabase.Instance?.Get(idx)?.idle;
        if (idle != null) avatarImage.sprite = idle;
    }


}

