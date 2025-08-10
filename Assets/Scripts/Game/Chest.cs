using UnityEngine;

public class Chest : MonoBehaviour
{
    public string symbolID;         // Unique ID like "apple", "sun", "bird"
    public Sprite symbolSprite;     // Visual symbol for the tree
    public SpriteRenderer displayRenderer; // The renderer that shows the symbol

    public GameObject highlightBorder;

    private static bool guessModeActive = false;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale; // store default size
    }


    void Start()
    {
        HideSymbol();
    }

    public void OnPlayerLanded(Player player)
    {
        RevealSymbolTo(player);
    }

    void RevealSymbolTo(Player player)
    {

        // Show the symbol on the tile visually
        displayRenderer.sprite = symbolSprite;

        // Optional: trigger a popup UI for the player
        Debug.Log($"Player landed on tree with symbol: {symbolID}");

        // You could call something like:
        // UIManager.Instance.ShowSymbol(symbolSprite);
    }

    public void HideSymbol()
    {
        displayRenderer.sprite = null; // Just hide it completely
    }


    public static void EnableGuessMode()
    {
        guessModeActive = true;
        foreach (Chest chest in FindObjectsOfType<Chest>())
        {
            chest.SetHighlight(true);
        }
    }

    private void SetHighlight(bool state)
    {
        if (state) { 
            transform.localScale = originalScale * 1.3f; // enlarge
            if (highlightBorder != null)
                highlightBorder.SetActive(state);
        }

        else { 
            transform.localScale = originalScale;
            if (highlightBorder != null)
            highlightBorder.SetActive(state);}
    }



    public static void DisableGuessMode()
    {
        guessModeActive = false;
        foreach (Chest chest in FindObjectsOfType<Chest>())
        {
            chest.SetHighlight(false);
        }

    }

    private void OnMouseDown() // Works with mouse or screen tap
    {
        if (guessModeActive)
        {
            GameManager.Instance.GuessChest(this);
            this.RevealChosenSymbol();
        }
    }

    public void RevealChosenSymbol()
    {
        displayRenderer.sprite = symbolSprite;
    }

}
