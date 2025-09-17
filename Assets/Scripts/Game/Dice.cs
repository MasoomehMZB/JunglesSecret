using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Dice : NetworkBehaviour
{
    public static Dice Instance;

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

    void Awake()
    {
        Instance = this;
    }

    [Server]
    public void Roll()
    {
        LastRoll1 = rng.Next(1, 7);
        LastRoll2 = rng.Next(1, 7);
        //Debug.Log($"dice rolled {LastRoll1} , {LastRoll2}");
    }

    void OnRollChanged1(int oldValue, int newValue)
    {
        die1Image.sprite = diceFaces[newValue - 1];
    }

    void OnRollChanged2(int oldValue, int newValue)
    {
        die2Image.sprite = diceFaces[newValue - 1];
    }

    [Server]
    public void ApplyChoice(int dieIndex)
    {
        if (LastRoll1 == 0 && LastRoll2 == 0)
        {
            //Debug.LogWarning($"Dice not rolled yet, ignoring choice{LastRoll1}{LastRoll2}");
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
