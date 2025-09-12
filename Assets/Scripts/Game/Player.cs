using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mirror;


public class Player : NetworkBehaviour
{
    // Movement
    private Vector2Int currentDir = Vector2Int.zero;
    private Vector2Int lastDir = Vector2Int.zero;
    private bool isMoving = false;
    private int requestedSteps = 0;
    private Waypoint currentWaypoint;

    // Flags
    [SyncVar] public bool InSpawnArea = true;
    private bool OnKeyTile = false;
    private bool teleportModeActive;
    private bool guessModeActive;

    // Scores
    [SyncVar] public int cardsWon = 0;

    // References
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stepAmount = 0.54f;
    [SerializeField] private GameObject waypointPrefab;
    private Chest lastChestTile = null;

    // Player Apearance
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor;
    private SpriteRenderer spriteRenderer;

    public static Player localPlayer;  
    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady = false;

    #region Player Initial settings
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    
    public void SetReady(bool ready)
    {
        CmdSetReady(ready);
    }

    [Command]
    void CmdSetReady(bool ready)
    {
        isReady = ready;
    }

    void OnReadyChanged(bool oldValue, bool newValue)
    {

           LobbyUI.Instance.UpdatePlayerReady();
        
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        // Update the SpriteRenderer's color on the client.
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor;
        }
    }
    #endregion

    #region Player Movement


    [Server]
    public void ServerStartMove(int steps)
    {
        requestedSteps = steps;
        StartCoroutine(MoveRoutine()); 
    }

    [Server]
    IEnumerator MoveRoutine()
    {
        // Not in Teleport mode | Not Skipped turn
        if (requestedSteps > 0)
        {   
            isMoving = true;
            bool isDeadEnd = false;
            Vector3 previousPos = transform.position;
            int previousSteps = requestedSteps;

            if (InSpawnArea)
            {
                Vector3 bottomCenter = GetPlayerBottomCenter();
                Vector3 offset = transform.position - bottomCenter;
                transform.position = GameManager.Instance.firstTilePos + offset;
                Debug.Log($"Pawn {netId} position: {transform.position}");

                Physics2D.SyncTransforms();
                yield return null;
            }

            ExitKeyTile();
            ExitChestTile();
            
            // Get the first direction :
            // On the waypoint
            yield return StartCoroutine(CollidewithWaypoint((bool result) =>
            {

                isDeadEnd = result;

            }));

            // Off the waypoint
            if (currentDir == Vector2Int.zero)
            {
                yield return HandleTempWaypoint();
            }

            // Movement loop
            while (requestedSteps > 0)
            {
                Vector3 startPos = transform.position;
                Vector3 targetPos = startPos + new Vector3(currentDir.x * stepAmount, currentDir.y * stepAmount, 0f);

                float distance = Vector3.Distance(startPos, targetPos);
                float duration = distance / speed;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                transform.position = targetPos;                

                //Tell clients about position change
                RpcUpdatePosition(targetPos);

                requestedSteps--;

                if (requestedSteps == 0) continue;

                yield return StartCoroutine(CollidewithWaypoint((bool result) =>
                {
                    isDeadEnd = result;
                }));

                // Handle dead end
                if (isDeadEnd && requestedSteps >= 0)
                {
                    RevertToPreviousState(previousPos, previousSteps);
                    yield return new WaitForSeconds(0.09f);

                    ServerStartMove(previousSteps);
                    yield break;
                }

                
            }
        }

        // General after Movement checks and variable sets

        InSpawnArea = false;

        // For after teleports
        if (teleportModeActive)
        {
            Debug.Log($"current waypoint = {currentWaypoint}");
            ExitChestTile();
        }

        // Wait a frame for collider position to update
        yield return null;
        HitAnotherPlayer();

        CheckSpecialTile();

        yield return new WaitUntil(() => !guessModeActive);

        currentDir = Vector2Int.zero;
        isMoving = false;

        Debug.Log("end turn");

        GameManager.Instance.EndTurn();
    }


    [ClientRpc]
    void RpcUpdatePosition(Vector3 newPos)
    {
        if (isServer) return; // server already has correct position
        transform.position = newPos;
    }


    private void RevertToPreviousState(Vector3 previousPos, int previousSteps)
    {
        transform.position = previousPos;
        requestedSteps = previousSteps;
        currentDir = Vector2Int.zero;
        isMoving = false;
        currentWaypoint = null;
    }
    void ExitChestTile()
    {
        if (lastChestTile != null)
        {
            lastChestTile.RpcHideSymbol();
            lastChestTile = null;
        }
    }
    void ExitKeyTile()
    {
        if (OnKeyTile)
        {
            GameManager.Instance.StopGuessMode();
            OnKeyTile = false;
        }
    }

    #endregion

    #region WayPoint Handling
    IEnumerator HandleWaypoint(Waypoint wp)
    {

        // Only local player sees arrow UI
        if (!isLocalPlayer)
        {
            // Server waits until choice is received from owning client
            yield return new WaitUntil(() => currentDir != Vector2Int.zero);
            yield break;
        }

        bool chosen = false;
        Vector2Int choice = currentDir; // Default to current direction

        // Show UI and wait for choice
        wp.ShowChoicesUI((Vector2Int selectedDir) =>
        {
            choice = selectedDir;
            chosen = true;

        }, currentDir);

        while (!chosen)
            yield return null;

        // Send choice to server
        CmdChooseDirection(choice);
    }

    [Command]
    void CmdChooseDirection(Vector2Int dir)
    {
        currentDir = dir;
        lastDir = dir;
    }

    IEnumerator CollidewithWaypoint(Action<bool> onResult)
    {
        Vector3 bottomCenter = GetPlayerBottomCenter();
        Vector3 offset = transform.position - bottomCenter;

        Collider2D wpCollider = Physics2D.OverlapCircle(bottomCenter, 0.2f, LayerMask.GetMask("Waypoint"));
        if (wpCollider != null)
        {
            currentWaypoint = wpCollider.GetComponent<Waypoint>();
            if (currentWaypoint != null)
            {
                transform.position = currentWaypoint.transform.position + offset;

                bool isDeadEnd = currentWaypoint.IsDeadEnd;

                yield return StartCoroutine(HandleWaypoint(currentWaypoint));

                onResult?.Invoke(isDeadEnd);
                yield break;
            }
        }

        onResult?.Invoke(false); // Not a deadend
    }

    IEnumerator HandleTempWaypoint()
    {
        // Only the owning client should spawn the UI
        if (!isLocalPlayer)
        {
            // Server just waits until a direction is chosen
            yield return new WaitUntil(() => currentDir != Vector2Int.zero);
            yield break;
        }

        Vector3 bottomCenter = GetPlayerBottomCenter();
        GameObject tempGO = Instantiate(waypointPrefab, bottomCenter, Quaternion.identity);
        Waypoint tempWaypoint = tempGO.GetComponent<Waypoint>();

        List<DirectionName> dirOptions = (lastDir == Vector2Int.up || lastDir == Vector2Int.down)
            ? new List<DirectionName> { DirectionName.Up, DirectionName.Down } 
            :new List<DirectionName> { DirectionName.Left, DirectionName.Right };

        tempWaypoint.SetAllowedDirections(dirOptions);

        yield return StartCoroutine(HandleWaypoint(tempWaypoint));

        Destroy(tempGO);

    }

    #endregion

    #region Special Tile Handling

    [Server]
    void CheckSpecialTile()
    {
        Vector3 bottomCenter = GetPlayerBottomCenter();
        Collider2D hit = Physics2D.OverlapCircle(bottomCenter, 0.2f, LayerMask.GetMask("Special"));
        if (hit)
        {
            if (hit.TryGetComponent<Chest>(out Chest chest))
            {
                Debug.Log($"{name} landed on chest {chest.symbolID}");
                chest.OnPlayerLanded(this);
                lastChestTile = chest;

                // Tell all clients to reveal chest symbol
                //RpcShowChest(chest.symbolID);
            }
            else if (hit.TryGetComponent<Key>(out Key key))
            {
                Debug.Log($"{name} landed on KEY");
                OnKeyTile = true;
                key.OnPlayerLanded(this);

                // Tell all clients to show key effect
                //RpcShowKeyEffect();
            }
            else
            {
                Debug.LogWarning("Unknown special tile collided.");
            }
        }
    }


    [Command]
    public void CmdSelectChest(uint chestNetId)
    {
        Debug.Log($"[Server] CmdSelectChest received from player {netId} for chest {chestNetId}");

        // Validate: find the chest on the server
        if (!NetworkServer.spawned.TryGetValue(chestNetId, out NetworkIdentity chestIdentity) || chestIdentity == null)
        {
            Debug.LogWarning($"CmdSelectChest: chest {chestNetId} not found on server.");
            return;
        }

        Chest chest = chestIdentity.GetComponent<Chest>();
        if (chest == null)
        {
            Debug.LogWarning($"CmdSelectChest: chest component missing on identity {chestNetId}.");
            return;
        }

        // Let GameManager decide what this click means
        GameManager.Instance.ServerHandleChestClick(chest, this);
    }

    [Command]
    public void CmdSelectKey(uint keyNetId)
    {
        Debug.Log($"[Server] CmdSelectKey received from player {netId} for key {keyNetId}");

        if (!NetworkServer.spawned.TryGetValue(keyNetId, out NetworkIdentity keyIdentity) || keyIdentity == null)
        {
            Debug.LogWarning($"CmdSelectKey: key {keyNetId} not found on server.");
            return;
        }

        Key key = keyIdentity.GetComponent<Key>();
        if (key == null)
        {
            Debug.LogWarning($"CmdSelectKey: Key component missing on identity {keyNetId}.");
            return;
        }

        GameManager.Instance.ServerHandleKeyClick(key, this);
    }

    public void SetTeleportMode(bool active)
    {
        teleportModeActive = active;
        // Optional: show/hide teleport UI here
    }

    public void SetGuessMode(bool active)
    {
        guessModeActive = active;
        // Optional: show/hide guess UI here
    }

    public void TargetShowGuessResult(NetworkConnectionToClient target, bool success)
    {
        if (success)
            Debug.Log("You guessed correctly!");
        else
            Debug.Log("Wrong guess!");

        // TODO: trigger animations, sounds, UI feedback
    }

    #endregion

    public Vector3 GetPlayerBottomCenter()
    {
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        Vector3 bottomCenter = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        return bottomCenter;
    }

    #region Hit

    [Server]
    void HitAnotherPlayer()
    {
        if (InSpawnArea) {
            Debug.Log("In hit, In Spawn");
            return; }

        Debug.Log("In hit");

        // Get all colliders in range on "Player" layer
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            0.4f,
            LayerMask.GetMask("Player")
        );

        foreach (Collider2D col in colliders)
        {
            if (col == null) continue; // safety check

            if (col.gameObject == gameObject) continue; // skip self

            Player opponent = col.GetComponent<Player>();
            if (opponent != null)
            {
                Debug.Log($"Player {gameObject.name} hit {opponent.gameObject.name}");
                GameManager.Instance.PunishPlayer(opponent);

                // Notify all clients for visual feedback
                RpcShowPlayerHit(opponent.netIdentity.netId);
            }

        }
    }

    [ClientRpc]
    public void RpcShowPlayerHit(uint opponentNetId)
    {
        Debug.Log($"[Client] Player {netIdentity.netId} hit player {opponentNetId}");

        // Optionally: flash opponent, play sound, etc.
        // (You can move this logic to a Player FX script later)
    }

    #endregion

    #region Dice Handling
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        localPlayer = this;

        // Hook up buttons only for the local player
        if (Dice.Instance != null)
        {
            Dice.Instance.die1Button.onClick.AddListener(() => ChooseDice(0));
            Dice.Instance.die2Button.onClick.AddListener(() => ChooseDice(1));
            Dice.Instance.skipButton.onClick.AddListener(() => ChooseDice(2));
            Dice.Instance.bothButton.onClick.AddListener(() => ChooseDice(3));
        }
    }

    private void ChooseDice(int dieIndex)
    {
        CmdChooseDice(dieIndex);
    }

    [Command]
    private void CmdChooseDice(int dieIndex)
    {
        if (Dice.Instance != null)
        {
            Dice.Instance.ApplyChoice(dieIndex);
        }
    }

    [TargetRpc]
    public void TargetSetDiceButtonsActive(NetworkConnection target, bool active)
    {
        if (Dice.Instance != null)
        {
            Dice.Instance.die1Button.interactable = active;
            Dice.Instance.die2Button.interactable = active;
            Dice.Instance.skipButton.interactable = active;
            Dice.Instance.bothButton.interactable = active;
        }
    }

    #endregion
}
