using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Mirror;


public class Player : NetworkBehaviour
{
    // Movement
    private Vector2Int currentDir = Vector2Int.zero;
    [SyncVar]public int dirX;
    [SyncVar]public int dirY;
    private Vector2Int lastDir = Vector2Int.zero;
    bool isMoving = false;
    private int requestedSteps = 0;
    private Waypoint currentWaypoint;
    private Vector3 turnStartPos;
    private int turnStartSteps;
    private Vector2Int turnStartDir;

    [SyncVar(hook = nameof(OnStateChanged))]
    public PlayerState state = PlayerState.Idle;

    public enum PlayerState
    {
        Idle,
        Walking,
        WaitingChoice
    }


    // Flags
    [SyncVar] public bool InSpawnArea = true;
    private bool OnKeyTile = false;
    private bool teleportModeActive;
    private bool guessModeActive;
    [SyncVar] bool Chose = false;

    // Scores
    [SyncVar] public int cardsWon = 0;

    // References
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stepAmount = 0.54f;
    [SerializeField] private GameObject waypointPrefab;
    public Chest lastChestTile = null;

    // Player Apearance
    private SpriteRenderer spriteRenderer;
    public static Player localPlayer;  
    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady = false;
    [SyncVar(hook = nameof(OnCharacterChanged))]
    public int characterIndex = -1;

    // local runtime
    CharacterConfig character;
    Coroutine animationCoroutine;


    #region Player Join
   
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
    #endregion

    #region Player Animation
    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (characterIndex >= 0) ApplyCharacter(characterIndex);
    }

    void OnCharacterChanged(int oldIdx, int newIdx)
    {
        ApplyCharacter(newIdx);
    }

    void ApplyCharacter(int idx)
    {
        character = CharacterDatabase.Instance?.Get(idx);
        if (character == null) return;
        // set idle sprite
        spriteRenderer.sprite = character.idle;
    }

    void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        UpdateAnimationState(newState);
    }

    void UpdateAnimationState(PlayerState newState)
    {
        // stop any running animation
        if (animationCoroutine != null) { StopCoroutine(animationCoroutine); animationCoroutine = null; }

        switch (newState)
        {
            case PlayerState.Idle:
                if (character != null) spriteRenderer.sprite = character.idle;
                break;

            case PlayerState.Walking:
                if (character == null) return;
                Sprite[] frames = character.GetWalkFrames(new Vector2Int(dirX, dirY));
                if (frames != null && frames.Length > 0)
                    animationCoroutine = StartCoroutine(RunFrames(frames, 10));
                break;

            case PlayerState.WaitingChoice:
                // idle while waiting
                if (character != null) spriteRenderer.sprite = character.idle;
                break;
        }
    }


    IEnumerator RunFrames(Sprite[] frames, float fps)
    {
        int idx = 0;
        float delay = 1f / fps;
        while (true)
        {
            spriteRenderer.sprite = frames[idx % frames.Length];
            idx++;
            yield return new WaitForSeconds(delay);
        }
    }

    [Server]
    public void ServerSetCharacter(int idx) => characterIndex = idx;

    [Server]
    void SetState(PlayerState newState, Vector2Int dir)
    {
        dirX = dir.x;
        dirY = dir.y;
        state = newState;
    }


    #endregion

    #region Player Movement

    [Server]
    public void ServerStartMove(int steps)
    {
        if (isMoving) return;

        turnStartPos = transform.position;
        turnStartSteps = steps;
        turnStartDir = lastDir;

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

            if (InSpawnArea)
            {
                Vector3 bottomCenter = GetPlayerBottomCenter();
                Vector3 offset = transform.position - bottomCenter;
                transform.position = GameManager.Instance.firstTile.position + offset;

                Physics2D.SyncTransforms();
                yield return null;
            }

            InSpawnArea = false;
            ExitKeyTile();
            ExitChestTile();

            // Get the first direction :
            // On the waypoint

            // Debug.Log("First direction and requested steps: " + currentDir + requestedSteps);

            yield return StartCoroutine(CollidewithWaypoint((bool result) =>
            {
                isDeadEnd = result;
            }));

            // Off the waypoint
            if (currentDir == Vector2Int.zero)
            {
                yield return StartCoroutine(HandleTempWaypoint());
            }

            // Show walking animation
            SetState(PlayerState.Walking, currentDir);

            // Movement loop
            while (requestedSteps > 0)
            {

                Vector3 startPos = transform.position;
                Vector3 targetPos = startPos + new Vector3(currentDir.x * stepAmount, currentDir.y * stepAmount, 0f);

                // Animate on the server too (host needs animation)
                yield return StartCoroutine(LerpMoveRoutine(targetPos));

                // Tell clients to animate
                RpcMoveTo(targetPos);

                requestedSteps--;

                if (requestedSteps == 0) continue;

                yield return StartCoroutine(CollidewithWaypoint((bool result) =>
                {
                    isDeadEnd = result;
                }));

                SetState(PlayerState.Walking, currentDir);

                // Handle dead end
                if (isDeadEnd && requestedSteps >= 0 && lastDir != Vector2Int.zero)
                {
                    RevertToPreviousState(turnStartPos, turnStartSteps, turnStartDir);
                    yield return new WaitForSeconds(0.5f);
                    Physics2D.SyncTransforms();

                    ServerStartMove(turnStartSteps);
                    yield break;
                }

                
            }
        }

        FinishMovement();
    }
    public void TeleportRoutin()
    {
        InSpawnArea = false;
        lastDir = Vector2Int.zero;
        ExitChestTile();
        FinishMovement();
    }
    public void FinishMovement()
    {
        HitAnotherPlayer();
        CheckSpecialTile();

        StartCoroutine(WaitAndEndTurn());
    }

    private IEnumerator WaitAndEndTurn()
    {       
        yield return new WaitUntil(() => !guessModeActive);
        currentDir = Vector2Int.zero;
        isMoving = false;
        SetState(PlayerState.Idle, currentDir);

        Debug.Log("end turn");
        GameManager.Instance.EndTurn();
    }


    [ClientRpc]
    void RpcMoveTo(Vector3 targetPos)
    {
        StartCoroutine(LerpMoveRoutine(targetPos));
    }

    private IEnumerator LerpMoveRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float duration = Vector3.Distance(startPos, targetPos) / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
    }

    private void RevertToPreviousState(Vector3 previousPos, int previousSteps, Vector2Int previousDir)
    {
        transform.position = previousPos;
        requestedSteps = previousSteps;
        currentDir = Vector2Int.zero;
        lastDir = previousDir;
        isMoving = false;
        currentWaypoint = null;
        SetState(PlayerState.Idle, currentDir);

        // Force clients to snap instantly as well
        RpcForceSnap(previousPos);
    }

    [ClientRpc]
    private void RpcForceSnap(Vector3 snapPos)
    {
        transform.position = snapPos;
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

    [Server]
    IEnumerator CollidewithWaypoint(Action<bool> onResult)
    {      
        Vector3 bottomCenter = GetPlayerBottomCenter();
        Vector3 offset = transform.position - bottomCenter;

        Collider2D wpCollider = Physics2D.OverlapCircle(bottomCenter, 0.1f, LayerMask.GetMask("Waypoint"));
        if (wpCollider != null)
        {           
            currentWaypoint = wpCollider.GetComponent<Waypoint>();
            //Debug.Log($"collide with {currentWaypoint}");

            if (currentWaypoint != null)
            {
                transform.position = currentWaypoint.transform.position + offset;
                bool isDeadEnd = currentWaypoint.IsDeadEnd;
                bool waypointHasUI = currentWaypoint.allowedDirections.Count > 2;

                Chose = false;


                Debug.Log($"waypointHasUI = {waypointHasUI}");

                // Ask client to choose
                TargetShowWaypointUI(connectionToClient, currentWaypoint.transform.position, currentWaypoint.allowedDirections, currentDir);

                if (waypointHasUI)
                    SetState(PlayerState.WaitingChoice, Vector2Int.zero);
                else
                    SetState(PlayerState.WaitingChoice, currentDir);

                // Stop movement until client responds
                yield return new WaitUntil(() => Chose);

                onResult?.Invoke(isDeadEnd);
                yield break;
            }
        }

        onResult?.Invoke(false);
    }

    [Server]
    IEnumerator HandleTempWaypoint()
    {
        Vector3 bottomCenter = GetPlayerBottomCenter();
        GameObject tempGO = Instantiate(waypointPrefab, bottomCenter, Quaternion.identity);
        Waypoint tempWaypoint = tempGO.GetComponent<Waypoint>();

        Debug.Log($"inside HandleTempWaypoint{lastDir}");

        List<DirectionName> dirOptions = (lastDir == Vector2Int.up || lastDir == Vector2Int.down)
            ? new List<DirectionName> { DirectionName.Up, DirectionName.Down }
            : new List<DirectionName> { DirectionName.Left, DirectionName.Right };

        tempWaypoint.SetAllowedDirections(dirOptions);

        // Wait until client picks
        yield return StartCoroutine(RequestDirectionFromClient(tempWaypoint));

        Destroy(tempGO);
    }

    [Server]
    IEnumerator RequestDirectionFromClient(Waypoint wp)
    {
        Chose = false;
        // Ask client to show UI
        TargetShowWaypointUI(connectionToClient, wp.transform.position, wp.allowedDirections, currentDir);
        // Wait until CmdChooseDirection updates currentDir
        yield return new WaitUntil(() => Chose);
    }

    [TargetRpc]
    void TargetShowWaypointUI(NetworkConnection target, Vector3 pos, List<DirectionName> options, Vector2Int excludeDir)
    {
        if (!isLocalPlayer) return;

        //Debug.Log("inside TargetShowWaypointUI");

        Waypoint temp = Instantiate(waypointPrefab, pos, Quaternion.identity).GetComponent<Waypoint>();
        temp.SetAllowedDirections(options);
        temp.IsDeadEnd = options.Count < 2;

        // Show UI and wait for choice
        temp.ShowChoicesUI((Vector2Int chosenDir) =>
        {
            CmdChooseDirection(chosenDir);
            Destroy(temp.gameObject);
        }, excludeDir);

    }

    [Command]
    void CmdChooseDirection(Vector2Int dir)
    {
        if (!isMoving) return;

        currentDir = dir;
        lastDir = dir;

        Chose = true; 
    }


    #endregion

    #region Special Tile Handling

    [Server]
    void CheckSpecialTile()
    {
        Vector3 bottomCenter = GetPlayerBottomCenter();
        Collider2D hit = Physics2D.OverlapCircle(bottomCenter, 0.2f, LayerMask.GetMask("Special"));
       // Debug.Log($"collided with {hit}");
        if (hit)
        {
            if (hit.TryGetComponent<Chest>(out Chest chest))
            {
                //Debug.Log($"{name} landed on chest {chest.symbolID}");
                chest.OnPlayerLanded(this);
                lastChestTile = chest;
            }
            else if (hit.TryGetComponent<Key>(out Key key))
            {
                //Debug.Log($"{name} landed on KEY");
                OnKeyTile = true;
                key.OnPlayerLanded(this);
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
        //Debug.Log($"[Server] CmdSelectChest received from player {netId} for chest {chestNetId}");

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
    }

    public void SetGuessMode(bool active)
    {
        guessModeActive = active;
    }

    public void TargetShowGuessResult(NetworkConnectionToClient target, bool success)
    {
        if (success)
            Debug.Log("You guessed correctly!");
        else
            Debug.Log("Wrong guess!");
    }

    #endregion

    #region Hit

    [Server]
    void HitAnotherPlayer()
    {
        if (InSpawnArea) {
            //Debug.Log("In hit, In Spawn");
            return; }

        //Debug.Log("In hit");

        // Get all colliders in range on "Player" layer
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            0.1f,
            LayerMask.GetMask("Player")
        );

        foreach (Collider2D col in colliders)
        {
            if (col == null) continue; // safety check

            if (col.gameObject == gameObject) continue; // skip self

            Player opponent = col.GetComponent<Player>();
            if (opponent != null)
            {
                //Debug.Log($"Player {gameObject.name} hit {opponent.gameObject.name}");
                GameManager.Instance.PunishPlayer(opponent);
            }

        }
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

    // Helper
    public Vector3 GetPlayerBottomCenter()
    {
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        Vector3 bottomCenter = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        return bottomCenter;
    }

    public string GetColorName()
    {
        return character != null ? character.color : "Unknown";
    }
}
