using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : NetworkBehaviour
{
    public static GameUI Instance;

    [Header("UI References")]
    public List<Image> scoreSlots = new List<Image>();   
    public Image TurnInfoSlot;
    public TMP_Text TurnInfoText;
    public Image CardWonInfoSlot;
    public TMP_Text CardWonInfoText;
    public Button backButton;

    private Dictionary<Player, Image> playerToSlot = new Dictionary<Player, Image>();

    private void Awake()
    {
        Instance = this;

        TurnInfoSlot.enabled = false;
        CardWonInfoSlot.enabled = false;

        foreach (var img in scoreSlots)
        {
            img.enabled = false;
            var txt = img.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = "";
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    [ClientRpc]
    public void InitScores(List<Player> players)
    {
        playerToSlot.Clear();

        for (int i = 0; i < players.Count && i < scoreSlots.Count; i++)
        {
            var player = players[i];
            var slot = scoreSlots[i];

            slot.enabled = true;

            // show initial score
            var txt = slot.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{player.GetColorName()}: {player.cardsWon}";

            playerToSlot[player] = slot;
        }
    }

    [ClientRpc]
    public void UpdateScoreRpc(int score, Player player)
    {
        if (playerToSlot.TryGetValue(player, out var slot))
        {
            var txt = slot.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{player.GetColorName()}: {score}";
        }
    }

    [TargetRpc]
    public void TargetShowTurnRpc(NetworkConnection target, bool isTurn)
    {
        Debug.Log($"in TargetShowTurnRpc is turn = {isTurn}, {target}");
        if (isTurn)

            UpdateInfoLocal("Your Turn", true);
        else
        {
            UpdateInfoLocal("", false);
        }
    }

    [Server]
    public void TargetShowTurnHost(bool isTurn)
    {
        Debug.Log("in TargetShowTurnRpc");
        if (isTurn)

            UpdateInfoLocal("Your Turn", true);
        else
        {
            UpdateInfoLocal("", false);
        }

    }

    void UpdateInfoLocal(string msg, bool state)
    {
        TurnInfoSlot.enabled = state;
        TurnInfoText.text = msg;
    }

    [ClientRpc]
    public void RpcShowCardWon(string colorName, int remaining)
    {
        CardWonInfoSlot.enabled = true;
        CardWonInfoText.text = $"{colorName} won a card, remaining {remaining}";
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

}
