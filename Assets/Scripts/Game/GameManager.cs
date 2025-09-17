using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    # region Game Objects / State / Variables

    // Game Objects / Transforms
    public Dice dice;
    public Key key;
    public Transform firstTile;
    public Transform chestParent;
    [SerializeField] private Cards cards;

    // Game Object Lists
    public List<Player> players = new List<Player>();
    [SerializeField] private List<Chest> chestTiles = new List<Chest>();

    // Game states / variables
    [SyncVar] private uint currentPlayerNetId;
    [SyncVar] public bool GameOver = false;
    private Player currentGuesser;
    private Player currentPlayer;
    private int currentPlayerIndex = 0;

    // Symbol allocations
    [Serializable]
    public class SymbolDef { public string id; public Sprite sprite; }
    public List<SymbolDef> symbolDefs = new List<SymbolDef>();
    private Dictionary<string, Chest> symbolToChest = new Dictionary<string, Chest>();
    private Dictionary<string, Sprite> symbolMap;

    // Movement flags
    private bool waitingForMovementChoice = false;
    [SyncVar(hook = nameof(OnTeleportModeChanged))]
    private bool _teleportModeActive;
    [SyncVar(hook = nameof(OnGuessModeChanged))]
    private bool _guessModeActive;

    #endregion

    #region Unity lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build symbol lookup for all clients (this runs on every instance)
        symbolMap = new Dictionary<string, Sprite>(symbolDefs.Count);
        foreach (var s in symbolDefs)
        {
            if (!string.IsNullOrEmpty(s.id) && s.sprite != null && !symbolMap.ContainsKey(s.id))
                symbolMap.Add(s.id, s.sprite);
        }

    }

    public void InitializeForGameScene()
    {
        // Find the scene objects after game scene is loaded.
        chestParent = GameObject.FindWithTag("ChestParent")?.transform;
        if (chestParent == null) Debug.LogError("Could not find object with tag 'ChestParent'!");

        firstTile = GameObject.FindWithTag("FirstTile")?.transform;
        if (firstTile == null) Debug.LogError("Could not find object with tag 'FirstTile'!");

        if (dice == null)
            dice = FindObjectOfType<Dice>();

        if (cards == null)
            cards = FindObjectOfType<Cards>();

        if (key == null)
            key = FindObjectOfType<Key>();
    }

    #endregion

    #region Helpers 

    // Clients (and Chests) will call this to get sprites locally.
    public Sprite GetSpriteForSymbol(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (symbolMap != null && symbolMap.TryGetValue(id, out var s)) return s;
        return null;
    }
    private string GetCurrentCardSymbol()
    {
        return cards.currentCardId;
    }

    #endregion

    #region Setup / Board initialization (SERVER ONLY)

    // Run on server only
    [Server]
    public void SetupBoard()
    {
        // find chests if not set
        if (chestTiles == null) chestTiles = new List<Chest>();

        if (chestTiles.Count == 0)
        {
            if (chestParent != null)
            {
                chestTiles = chestParent.GetComponentsInChildren<Chest>().ToList();
            }
            else
            {
                Debug.LogWarning("GameManager.SetupBoard() - No chest tiles assigned and no chestParent set.");
            }
        }

        // validate symbols
        if (symbolDefs == null || symbolDefs.Count == 0)
        {
            Debug.LogError("GameManager.SetupBoard() - No symbol definitions set in GameManager.");
            return;
        }

        // shuffle indices
        List<int> symbolIndices = Enumerable.Range(0, symbolDefs.Count).ToList();
        List<int> chestIndices = Enumerable.Range(0, chestTiles.Count).ToList();
        Shuffle(symbolIndices);
        Shuffle(chestIndices);


        // Assign symbols to chests 
        int assignCount = chestTiles.Count;
        symbolToChest = new Dictionary<string, Chest>();
        for (int i = 0; i < assignCount; i++)
        {
            var chosenSymbol = symbolDefs[symbolIndices[i % symbolIndices.Count]];
            Chest chest = chestTiles[chestIndices[i]];
            chest.symbolID = chosenSymbol.id;

            // rename the GameObject in editor for easier debugging (optional)
#if UNITY_EDITOR
            chest.gameObject.name = $"Chest_{chosenSymbol.id}";
#endif

            // register mapping (server-side)
            if (!symbolToChest.ContainsKey(chosenSymbol.id))
                symbolToChest.Add(chosenSymbol.id, chest);
            else
                symbolToChest[chosenSymbol.id] = chest;

            // enqueue card into server-side deck representation (cards should be server-owned)

            cards.cardDeck.Enqueue(chosenSymbol.id);

        }

        // Reveal first card on server and sync id to clients
        if (cards.cardDeck.Count > 0)
        {
            cards.RevealCard();
        }
        else
        {
            Debug.LogError("SetupBoard: Tried to reveal but deck is empty!");
        }
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

    #endregion

    #region Turn management (server authoritative)

    [Server]
    public void StartTurn()
    {
        if (GameOver) return;

        waitingForMovementChoice = true;

        // If players list is empty - probably no connected players
        if (players.Count == 0)
        {
            Debug.LogWarning("StartTurn: no players in players list.");
            return;
        }

        currentPlayer = players[currentPlayerIndex];
        currentPlayerNetId = currentPlayer.netIdentity.netId;

        if (currentPlayer.connectionToClient != null)
        {
            GameUI.Instance.TargetShowTurnRpc(currentPlayer.connectionToClient, true);
        }
        else
        {
            // Host player (no connectionToClient)
            GameUI.Instance.TargetShowTurnHost(true);
        }

        // Enable only the current player's UI
        currentPlayer.TargetSetDiceButtonsActive(currentPlayer.connectionToClient, true);

        // Roll dice server-side
        dice.Roll();

        //Debug.Log($"--- {currentPlayer.name}'s Turn (netId {currentPlayerNetId}) ---");

        if (dice.IsDouble())
        {
            //Debug.Log("Double rolled! Teleport mode activated.");
            EnableTeleportMode();
        }
    }

    [Server]
    public void HandleDiceChoiceServer(int chosenSteps)
    {
        if (!waitingForMovementChoice) return;

        currentPlayer.TargetSetDiceButtonsActive(currentPlayer.connectionToClient, false);

        if (_teleportModeActive) DisableTeleportModeServer();

        //Debug.Log($"Server: currentPlayer chose {chosenSteps} steps");

        waitingForMovementChoice = false;

        // Start move on the player (server side)
        if (chosenSteps > 0) currentPlayer.ServerStartMove(chosenSteps);
        else currentPlayer.FinishMovement();
    }

    [Server]
    public void EndTurn()
    {
        if (GameOver)
        {
            EndGame();
        }

        if (currentPlayer.connectionToClient != null)
        {
            GameUI.Instance.TargetShowTurnRpc(currentPlayer.connectionToClient, false);
        }
        else
        {
            // Host player (no connectionToClient)
            GameUI.Instance.TargetShowTurnHost(false);
        }

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        StartTurn();
    }

    #endregion

    #region Guess / Teleport Modes (server authoritative)

    [Server]
    public void StartGuessMode(Player player)
    {
        Debug.Log("start guess mode");
        _guessModeActive = true;
        currentGuesser = player;

        // Highlight chests for the player only:
        foreach (var chest in chestTiles)
        {
            chest.TargetSetHighlight(player.connectionToClient, true, "choose");
        }
    }

    [Server]
    public void StopGuessMode()
    {
        _guessModeActive = false;
        currentGuesser = null;

        foreach (var chest in chestTiles)
        {
            chest.TargetSetHighlight(currentPlayer.connectionToClient, false, "choose");
        }
    }

    [Server]
    public void EnableTeleportMode()
    {
        _teleportModeActive = true;
        // Highlight all chests and key for current player
        if (currentPlayer != null)
        {
            foreach (var chest in chestTiles)
                chest.TargetSetHighlight(currentPlayer.connectionToClient, true, "teleport");

            if (key != null) key.TargetSetHighlight(currentPlayer.connectionToClient, true);
        }
    }

    [Server]
    public void DisableTeleportModeServer()
    {
        _teleportModeActive = false;
        //Debug.Log("DisableTeleportModeServer called!");
        if (currentPlayer != null)
        {
            foreach (var chest in chestTiles)
                chest.TargetSetHighlight(currentPlayer.connectionToClient, false, "teleport");

            if (key != null) key.TargetSetHighlight(currentPlayer.connectionToClient, false);
        }

        waitingForMovementChoice = false;
    }


    // Called when player chooses a chest
    [Server]
    public IEnumerator GuessChest(Player guesser, Chest chosenChest)
    {
        // Reveal chest for all clients
        chosenChest.RpcRevealChosenSymbol();

        // Wait so players see it
        yield return new WaitForSeconds(1.5f);
        //Debug.Log($"chosen = {chosenChest.symbolID} , card = {GetCurrentCardSymbol()}");
        if (chosenChest.symbolID == GetCurrentCardSymbol())
        {
            Debug.Log($"{guesser.name} guessed correctly!");
            TryClaimCard(guesser, chosenChest);

            // Success FX only for the guesser
            guesser.TargetShowGuessResult(guesser.connectionToClient, true);
        }
        else
        {
            Debug.Log($"{guesser.name} guessed wrong!");
            PunishPlayer(guesser);
            guesser.InSpawnArea = true;

            // Failure FX only for the guesser
            guesser.TargetShowGuessResult(guesser.connectionToClient, false);
        }
        chosenChest.RpcHideSymbol();

        StopGuessMode();
    }

    [Server]
    public void TeleportTo(GameObject target)
    {
        if (currentPlayer == null)
        {
            Debug.LogWarning("Teleport attempted without a current player.");
            return;
        }

        currentPlayer.TargetSetDiceButtonsActive(currentPlayer.connectionToClient, false);

        // Align player bottom to tile center
        Vector3 bottomCenter = currentPlayer.GetPlayerBottomCenter();
        Vector3 offset = currentPlayer.transform.position - bottomCenter;

        currentPlayer.transform.position = target.transform.position + offset;

        // Ensure clients update position (if not using NetworkTransform)
        RpcTeleportPlayer(currentPlayer.netId, currentPlayer.transform.position);

        Physics2D.SyncTransforms();
        // End teleport turn
        //currentPlayer.ServerStartMove(0);
        DisableTeleportModeServer();
        currentPlayer.TeleportRoutin();
    }

    [ClientRpc]
    void RpcTeleportPlayer(uint playerNetId, Vector3 newPos)
    {
        if (NetworkServer.spawned.TryGetValue(playerNetId, out var identity))
        {
            Player p = identity.GetComponent<Player>();
            if (p != null)
            {
                p.transform.position = newPos;
                Debug.Log($"[Client] Teleported {p.name} to {newPos}");
            }
        }
    }

    void OnTeleportModeChanged(bool oldValue, bool newValue)
    {
        foreach (var player in FindObjectsOfType<Player>())
            player.SetTeleportMode(newValue);
    }

    void OnGuessModeChanged(bool oldValue, bool newValue)
    {
        foreach (var player in FindObjectsOfType<Player>())
            player.SetGuessMode(newValue);
    }

    #endregion

    #region Claim Card

    [Server]
    private void TryClaimCard(Player player, Chest chest)
    {
        player.cardsWon++;
        GameUI.Instance.UpdateScoreRpc(player.cardsWon, player);
        GameUI.Instance.RpcShowCardWon(player.GetColorName(), cards.remainingCards);
        cards.RevealCard();
    }

    #endregion

    #region Player / punishment (server only)

    [Server]
    public void PunishPlayer(Player player, int? playerIndex = null)
    {
        if (player.lastChestTile != null)
        {
            player.lastChestTile.RpcHideSymbol();
            player.lastChestTile = null;
        }

        Debug.Log($"{player.name} was punished! Sent back to start.");
        player.transform.position = SpawnArea.Instance.GetSpawnPosition(playerIndex);
        player.InSpawnArea = true;

    }

    #endregion

    #region End Game 
    [Server]
    public void EndGame()
    {

        StopAllCoroutines();

        if (players.Count == 0)
        {
            Debug.LogWarning("EndGame called with no players!");
            return;
        }

        Player winner = players.OrderByDescending(p => p.cardsWon).FirstOrDefault();

        List<Player> sortedPlayers = players.OrderByDescending(p => p.cardsWon).ToList();

        RpcShowEndGame(sortedPlayers, winner.characterIndex);
    }

    [ClientRpc]
    void RpcShowEndGame(List<Player> sortedPlayers, int winnerIndex)
    {
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.ShowEndGame(sortedPlayers, winnerIndex);
        else
            Debug.LogError("GameOverUI not found on client!");
    }

    #endregion

    #region Handle clicks from players (serverside)

    [Server]
    public void ServerHandleChestClick(Chest chest, Player clicker)
    {
        Debug.Log($"[Server] ServerHandleChestClick called by {clicker.name} on chest {chest.symbolID}");

        // If guess mode active — this is someone making a guess
        if (_guessModeActive)
        {
            // Only the guesser should be allowed to guess (optional extra check)
            if (clicker != currentGuesser)
            {
                Debug.LogWarning("ServerHandleChestClick: clicker is not the current guesser.");
                return;
            }

            // start the server coroutine
            StartCoroutine(GuessChest(clicker, chest));
            return;
        }

        // If teleport mode active and clicker is current player, teleport
        if (_teleportModeActive && clicker == currentPlayer)
        {
            TeleportTo(chest.gameObject);
            return;
        }

        Debug.Log($"Chest clicked but no valid mode active for player {clicker.name}");
    }

    [Server]
    public void ServerHandleKeyClick(Key key, Player clicker)
    {
        Debug.Log($"[Server] ServerHandleKeyClick called by {clicker.name} on key");

        if (_teleportModeActive && clicker == currentPlayer)
        {
            TeleportTo(key.gameObject);
            return;
        }

        Debug.Log($"Key clicked but teleport not active or not current player.");
    }

    #endregion
}
