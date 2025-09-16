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
    public TMP_Text infoText;                           
    public Button backButton;                            

    private void Awake()
    {
        Instance = this;
        // Hide all score slots initially
        foreach (var slot in scoreSlots)
            slot.gameObject.SetActive(false);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    [ClientRpc]
    public void RpcUpdateAllScores()
    {
        var players = GameManager.Instance.players;

        for (int i = 0; i < scoreSlots.Count; i++)
        {
            if (i < players.Count)
            {
                Player p = players[i];
                var slot = scoreSlots[i];
                slot.gameObject.SetActive(true);

                CharacterConfig cfg = CharacterDatabase.Instance.Get(p.characterIndex);
                if (cfg != null)
                {
                    // Update text (cardsWon)
                    TMP_Text txt = slot.GetComponentInChildren<TMP_Text>();
                    if (txt != null)
                        txt.text = $"{cfg.color}: {p.cardsWon.ToString()}";
                }
            }
            else
            {
                scoreSlots[i].gameObject.SetActive(false);
            }
        }
    }

    [TargetRpc]
    public void TargetShowTurnMessage(NetworkConnection target)
    {
        if (infoText != null)
            infoText.text = "Your Turn!";
    }

    [ClientRpc]
    public void RpcShowCardWon(string colorName, int remaining)
    {
        if (infoText != null)
            infoText.text = $"{colorName} won a card, remaining {remaining}";
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
