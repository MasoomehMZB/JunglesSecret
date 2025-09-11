using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Dice : NetworkBehaviour
{
    [Header("Dice UI Elements")]
    public Button die1Button;
    public Button die2Button;
    public Button skipButton;
    public Button bothButton;
    public Image die1Image;
    public Image die2Image;

    [Header("Dice Faces (UI Sprites)")]
    public Sprite[] diceFaces;

    private System.Random rng = new System.Random();

    [SyncVar(hook = nameof(OnRollChanged1))] public int LastRoll1;
    [SyncVar(hook = nameof(OnRollChanged2))] public int LastRoll2;

    public int steps;

    public void Start()
    {
        // Hook up button events on client side
        die1Button.onClick.AddListener(() => OnDieClicked(0));
        die2Button.onClick.AddListener(() => OnDieClicked(1));
        skipButton.onClick.AddListener(() => OnDieClicked(2));
        bothButton.onClick.AddListener(() => OnDieClicked(3));
    }

    [Server]
    public void Roll()
    {
        LastRoll1 = rng.Next(1, 7);
        LastRoll2 = rng.Next(1, 7);
        Debug.Log($"dice rolled {LastRoll1} , {LastRoll2}");
    }

    void OnRollChanged1(int oldValue, int newValue)
    {
        Debug.Log($"[Client] die1 updated to {newValue}");
        die1Image.sprite = diceFaces[newValue - 1];
        die1Button.image.sprite = diceFaces[newValue - 1];
    }

    void OnRollChanged2(int oldValue, int newValue)
    {
        Debug.Log($"[Client] die2 updated to {newValue}");
        die2Image.sprite = diceFaces[newValue - 1];
    }

    private void OnDieClicked(int dieIndex)
    {
        if (isServer)
        {
            ApplyChoice(dieIndex);
        }
        else
        {
            CmdSendChoice(dieIndex);
        }
    }

    [Command]
    private void CmdSendChoice(int dieIndex)
    {
        ApplyChoice(dieIndex);
    }

    [Server]
    private void ApplyChoice(int dieIndex)
    {
        if (LastRoll1 == 0 && LastRoll2 == 0)
        {
            Debug.LogWarning($"Dice not rolled yet, ignoring choice{LastRoll1}{LastRoll2}");
            return;
        }

        switch (dieIndex)
        {
            case 0: steps = LastRoll1; break;
            case 1: steps = LastRoll2; break;
            case 2: steps = 0; break;
            case 3: steps = LastRoll1 + LastRoll2; break;
        }

        GameManager.Instance.HandleDiceChoiceServer(steps);
    }


    public bool IsDouble() => LastRoll1 == LastRoll2;
}

//private void OnDieClicked(int dieIndex)
//{
//    int steps = 0;
//    switch (dieIndex)
//    {
//        case 0: steps = LastRoll1; break;
//        case 1: steps = LastRoll2; break;
//        case 2: steps = 0; break;
//        case 3: steps = LastRoll1 + LastRoll2; break;
//        default: Debug.Log("not valid die choice"); break;
//    }

//    // find local player and send the choice to the server
//    if (!NetworkClient.active)
//    {
//        Debug.LogWarning("OnDieClicked: NetworkClient not active");
//        return;
//    }

//    var conn = NetworkClient.connection;
//    if (conn == null || conn.identity == null)
//    {
//        Debug.LogWarning("OnDieClicked: no local player identity");
//        return;
//    }

//    Player localPlayer = conn.identity.GetComponent<Player>();
//    if (localPlayer == null)
//    {
//        Debug.LogWarning("OnDieClicked: local Player component not found");
//        return;
//    }

//    localPlayer.CmdSendDiceChoice(steps); // this Command hits server and calls GameManager.HandleDiceChoiceServer
//}