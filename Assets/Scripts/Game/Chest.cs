using UnityEngine;

public class Chest : MonoBehaviour
{
    public string symbolID;         // Unique ID like "apple", "sun", "bird"
    public Sprite symbolSprite;     // Visual symbol for the tree

    [SerializeField] private SpriteRenderer displayRenderer; // The renderer that shows the symbol
    [SerializeField] private GameObject highlightBorder;

    Vector3 originalScale;

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
        displayRenderer.sprite = symbolSprite;
    }

    public void HideSymbol()
    {
        displayRenderer.sprite = null; // Just hide it completely
    }


    public static void EnableHighlight(bool choice)
    {
        foreach (Chest chest in FindObjectsOfType<Chest>())
        {
            chest.SetHighlight(choice);
        }
    }

    public void SetHighlight(bool state)
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


    private void OnMouseDown()
    {
        if (GameManager.Instance.GuessModeActive)
        {
            StartCoroutine(GameManager.Instance.GuessChest(this));
        }
        else if (GameManager.Instance.TeleportModeActive)
        {
            GameManager.Instance.TeleportTo(gameObject);
        }
    }

    public void RevealChosenSymbol()
    {
        displayRenderer.sprite = symbolSprite;
    }

}
