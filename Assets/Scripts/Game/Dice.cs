using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class Dice : MonoBehaviour
{
    [Header("Dice UI Elements")]
    public Button die1Button;
    public Button die2Button;
    public Button skipButton;
    public Button bothButton;
    public Image die1Image;
    public Image die2Image;

    [Header("Dice Faces (UI Sprites)")]
    public Sprite[] diceFaces; // index 0 = face "1", index 5 = face "6"

    private System.Random rng = new System.Random();
    public int LastRoll1 { get; private set; }
    public int LastRoll2 { get; private set; }

    public int steps;

    void Start()
    {
        // Assign button click events
        die1Button.onClick.AddListener(() => OnDieClicked(0));
        die2Button.onClick.AddListener(() => OnDieClicked(1));
        skipButton.onClick.AddListener(() => OnDieClicked(2));
        bothButton.onClick.AddListener(() => OnDieClicked(3));
    }

    public void Roll()
    {
        LastRoll1 = rng.Next(1, 7); // 1 to 6
        LastRoll2 = rng.Next(1, 7);

        // Update UI images
        die1Image.sprite = diceFaces[LastRoll1 - 1];
        die2Image.sprite = diceFaces[LastRoll2 - 1];
    }

    private void OnDieClicked(int dieIndex)
    {

        switch (dieIndex)
        {
            case 0: steps = LastRoll1; break;
            case 1: steps = LastRoll2; break;
            case 2: steps = 0; break;
            case 3: steps = LastRoll1 + LastRoll2; break;
            default: Debug.Log("not valid die choice"); break;
        }

        GameManager.Instance.HandleDiceChoice(steps);
    }
    public bool IsDouble()
    {
        return LastRoll1 == LastRoll2;
    }

}
