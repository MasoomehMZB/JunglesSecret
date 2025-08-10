using UnityEngine;

public class Dice : MonoBehaviour
{
    public SpriteRenderer die1Image;
    public SpriteRenderer die2Image;
    public Sprite[] diceFaces; // 0 = face "1", 5 = face "6"

    private System.Random rng = new System.Random();

    public int LastRoll1 { get; private set; }
    public int LastRoll2 { get; private set; }

    public (int, int) Roll()
    {
        LastRoll1 = rng.Next(1, 7); // 1 to 6
        LastRoll2 = rng.Next(1, 7);

        // Update UI images
        die1Image.sprite = diceFaces[LastRoll1 - 1];
        die2Image.sprite = diceFaces[LastRoll2 - 1];

        Debug.Log($"Rolled {LastRoll1} and {LastRoll2}");
        return (LastRoll1, LastRoll2);
    }

    public bool IsDouble()
    {
        return LastRoll1 == LastRoll2;
    }

    public int Total()
    {
        return LastRoll1 + LastRoll2;
    }
}
