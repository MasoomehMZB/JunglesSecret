using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<Player> players = new List<Player>(); // Add all players
    public Transform PlayerParent;
    private int currentPlayerIndex = 0;

    public Dice dice;
    public Key key;

    private Player currentGuesser;
    private Player currentPlayer;

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

        // get players

        players.Clear();
        players = PlayerParent.GetComponentsInChildren<Player>().ToList();

        StartTurn();

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
        GuessModeActive = true;
        currentGuesser = player;
        Chest.EnableHighlight(true); // Allow chest selection
        Debug.Log($"{player.name} is guessing for symbol: {GetCurrentCardSymbol()}");
    }

    // Called when player chooses a chest
    public IEnumerator GuessChest(Chest chosenChest)
    {
        // Show the symbol they picked
        chosenChest.RevealChosenSymbol();

        // Wait for 1.5 seconds so player can see it
        

        // Check if guess matches
        if (chosenChest.symbolID == GetCurrentCardSymbol())
        {
            Debug.Log($"{currentGuesser.name} guessed correctly!");
            TryClaimCard(currentGuesser, chosenChest);
        }
        else
        {
            currentGuesser.transform.position = SpawnArea.transform.position;
            Debug.Log($"{currentGuesser.name} guessed wrong!");
        }
        yield return new WaitForSeconds(1.5f);
        // Hide symbol after result
        chosenChest.HideSymbol();

        // End guess mode
        StopGuessMode();
    }

    public void StopGuessMode()
    {
        GuessModeActive = false;
        currentGuesser = null;
        Chest.EnableHighlight(false);
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

    //-----------------------------------------------------------------------------------
    private bool waitingForMovementChoice = false;
    public bool TeleportModeActive { get; private set; } = false;
    public bool GuessModeActive { get; private set; } = false;

    public void StartTurn()
    {   
        waitingForMovementChoice = true;
        currentPlayer = players[currentPlayerIndex];
        Debug.Log($"--- {currentPlayer.name}'s Turn ---");

        dice.Roll();

        if (dice.IsDouble())
        {
            Debug.Log("Double rolled! Teleport mode activated.");
            EnableTeleportMode();
        }
    }
    public void HandleDiceChoice(int chosenSteps)
    {

        Debug.Log("in hab=ndle dice choice1.");
        if (!waitingForMovementChoice) return;

        if (TeleportModeActive) DisableTeleportMode();

        Player currentPlayer = players[currentPlayerIndex];
        Debug.Log($"{currentPlayer.name} chose {chosenSteps} steps");

        waitingForMovementChoice = false;
        currentPlayer.RequestMove(chosenSteps);
    }

    public void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn();
    }
   // -------------------------------------------------------------------------------------------------

    public void TeleportTo(GameObject target)
    {
        DisableTeleportMode();
        if (currentPlayer == null)
        {
            Debug.LogWarning("Teleport attempted without a current player.");
            return;
        }

        // Align player bottom to tile center
        Vector3 bottomCenter = currentPlayer.GetPlayerBottomCenter();
        Vector3 offset = currentPlayer.transform.position - bottomCenter;
        currentPlayer.transform.position = target.transform.position + offset;

        currentPlayer.RequestMove(0);
        
    }
    public void EnableTeleportMode()
    {
        TeleportModeActive = true;
        Chest.EnableHighlight(true);

        if (key != null)
            key.SetHighlight(true);

        Debug.Log($"is in teleport mode — choose a destination.");
    }

    public void DisableTeleportMode()
    {
        TeleportModeActive = false;

        Chest.EnableHighlight(false);

        if (key != null)
            key.SetHighlight(false);

        waitingForMovementChoice = false;
    }

}
