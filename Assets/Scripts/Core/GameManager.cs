using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    //private List<Player> players = new List<Player>(); // Add all players here
    //private int currentPlayerTurn = 0;
    private Player currentGuesser;

    [SerializeField] private Cards cards;

    [SerializeField] private List<Chest> chestTiles = new List<Chest>();
    public Transform chestParent;

    public List<SymbolDef> symbolDefs = new List<SymbolDef>();
    private Dictionary<string, Chest> symbolToChest = new Dictionary<string, Chest>();

    public GameObject SpawnArea;


    [Serializable]
    public class SymbolDef
    {
        public string id;
        public Sprite sprite;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupBoard();
        // ... other startup logic (deck setup etc) goes after this
    }

    public void SetupBoard()
    {

        // 1) Ensure we have tree tiles
        if (chestTiles == null) chestTiles = new List<Chest>();

        if (chestTiles.Count == 0)
        {
            if (chestParent != null)
            {
                chestTiles = chestParent.GetComponentsInChildren<Chest>().ToList();
            }
            else
            {
                Debug.LogWarning("GameManager.SetupBoard() - No tree tiles assigned and no chestParent set.");
            }
        }

        // 2) Validate symbol defs
        if (symbolDefs == null || symbolDefs.Count == 0)
        {
            Debug.LogError("GameManager.SetupBoard() - No symbol definitions set in GameManager (symbolDefs list is empty).");
            return;
        }

        if (symbolDefs.Select(s => s.id).Distinct().Count() != symbolDefs.Count)
        {
            Debug.LogWarning("GameManager.SetupBoard() - Some symbol IDs are duplicated. Symbol IDs should be unique.");
        }

        // 3) Decide how many assignments to make
        int assignCount = chestTiles.Count;

        if (symbolDefs.Count < assignCount)
        {
            Debug.LogWarning($"GameManager.SetupBoard() - Fewer symbols ({symbolDefs.Count}) than trees ({assignCount}). Symbols will be reused to fill the board.");
        }
        else if (symbolDefs.Count > assignCount)
        {
            Debug.LogWarning($"GameManager.SetupBoard() - More symbols ({symbolDefs.Count}) than trees ({assignCount}). Only {assignCount} symbols will be used this round.");
        }

        // 4) Build index lists and shuffle
        List<int> symbolIndices = Enumerable.Range(0, symbolDefs.Count).ToList();
        List<int> chestIndices = Enumerable.Range(0, chestTiles.Count).ToList();

         Shuffle(symbolIndices);
         Shuffle(chestIndices);
        

        // 5) Assign symbols to chests 
        symbolToChest.Clear();
        for (int i = 0; i < assignCount; i++)
        {
            cards.cardDeck.Enqueue(symbolDefs[symbolIndices[i % symbolIndices.Count]]);

            Chest chest = chestTiles[chestIndices[i]];
            SymbolDef chosenSymbol = symbolDefs[symbolIndices[i % symbolIndices.Count]]; 

            chest.symbolID = chosenSymbol.id;
            chest.symbolSprite = chosenSymbol.sprite;
            chest.HideSymbol(); // make sure it's hidden at the start

            // rename the GameObject in editor for easier debugging (optional)
            #if UNITY_EDITOR
            chest.gameObject.name = $"Chest_{chosenSymbol.id}";
            #endif

            // register mapping (last assignment wins if duplicates)
            if (!symbolToChest.ContainsKey(chosenSymbol.id))
                symbolToChest.Add(chosenSymbol.id, chest);
            else
                symbolToChest[chosenSymbol.id] = chest;
        }

        // card deck setup
        cards.RevealCard();

        Debug.Log($"GameManager.SetupBoard() - Assigned {assignCount} symbol(s) to trees.");
    }

    // Helper: randomness using UnityEngine.Random (non-deterministic)
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T tmp = list[k];
            list[k] = list[n];
            list[n] = tmp;
        }
    }

    //-------------------------------------------------------------------------------------------------------

    // Called when player lands on the Key tile
    public void StartGuessMode(Player player)
    {
        currentGuesser = player;
        Chest.EnableGuessMode(); // Allow chest selection
        Debug.Log($"{player.name} is guessing for symbol: {GetCurrentCardSymbol()}");
    }

    // Called when player chooses a chest
    public void GuessChest(Chest chosenChest)
    {
        if (currentGuesser == null)
        {
            Debug.LogWarning("No player is in guess mode.");
            return;
        }

        // Check if guess matches
        if (chosenChest.symbolID == GetCurrentCardSymbol())
        {
            Debug.Log($"{currentGuesser.name} guessed correctly!");
            // Award card to currentGuesser
            TryClaimCard(currentGuesser, chosenChest);
        }
        else
        {
            Debug.Log($"{currentGuesser.name} guessed wrong!");
        }

        // End guessing
        StopGuessMode();
    }

    public void StopGuessMode()
    {
        currentGuesser = null;
        Chest.DisableGuessMode();
    }

    private string GetCurrentCardSymbol()
    {
        return cards.CurrentCard.id;
    }

    private void TryClaimCard(Player player, Chest chest)
    {
        // Add to player's score
        // Remove card from deck
    }

}
