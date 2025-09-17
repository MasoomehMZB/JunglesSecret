using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Collections.Generic;


public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    public GameObject gameOverPanel;
    public Button backButton;
    public TMP_Text scoresText;
    public Image avatarImage;

    void Awake()
    {
        Debug.Log("GameOverUI Awake");
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        gameOverPanel.SetActive(false);
    }

    public void ShowEndGame(List<Player> sortedPlayers, int index)
    {
        Debug.Log("Game Over!in RpcShowEndGame");
        Dictionary<string, int> scores = sortedPlayers
            .OrderByDescending(p => p.cardsWon)
            .ToDictionary(
                p => p.GetColorName(),  // key
                p => p.cardsWon         // value
            );
        gameOverPanel.SetActive(true);
        SetScoreListLocal(scores);
        AddWinnerAvatar(index);

    }

    void OnBackClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void SetScoreListLocal(Dictionary<string, int> scores)
    {
        string result = string.Join(
            "\n",
            scores.Select(kvp => $"{kvp.Key}: {kvp.Value}") 
        );
        scoresText.text = result;
    }

    void AddWinnerAvatar(int characterIndex)
    {
        Sprite idle = CharacterDatabase.Instance?.Get(characterIndex)?.idle;
        if (idle != null) avatarImage.sprite = idle;
    }


}

