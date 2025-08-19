using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;


public class Player : MonoBehaviour
{
    // Movement variables
    [SerializeField] private Vector2Int currentDir = Vector2Int.zero;
    [SerializeField] private Vector2Int lastDir = Vector2Int.zero;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stepAmount = 0.54f;
    [SerializeField] private GameObject waypointPrefab;
    private Waypoint currentWaypoint;

    private int requestedSteps = 0;
    private Chest lastChestTile = null;
   
    // Flags
    public bool InSpawnArea = true;
    private bool OnKeyTile = false;
    private bool isMoving = false;

    // Scores
    public int cardsWon = 0;


    public void RequestMove(int steps)
    {
        if (!isMoving)
        {
            requestedSteps = steps;
            StartCoroutine(MoveRoutine());
        }
        else
        {
            Debug.LogWarning("Player is already moving.");
        }
    }

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
                transform.position = GameManager.Instance.firstTile.transform.position + offset;
                
                Physics2D.SyncTransforms();
                yield return null;
            }

            if (OnKeyTile) ExitKeyTile();
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

            // --Movement--
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

                    RequestMove(previousSteps);
                    yield break;
                }

                
            }
        }

        // For after teleports
        if (GameManager.Instance.TeleportModeActive)
        {
            InSpawnArea = false;
            Debug.Log($"current waypoint = {currentWaypoint}");
            ExitChestTile();
        }

        // Wait a frame for collider position to update
        yield return null;
        HitAnotherPlayer();

        CheckSpecialTile();

        yield return new WaitUntil(() => !GameManager.Instance.GuessModeActive);

        currentDir = Vector2Int.zero;
        isMoving = false;

        Debug.Log("end turn");

        GameManager.Instance.EndTurn();
    }

    private void RevertToPreviousState(Vector3 previousPos, int previousSteps)
    {
        transform.position = previousPos;
        requestedSteps = previousSteps;
        currentDir = Vector2Int.zero;
        isMoving = false;
        currentWaypoint = null;
    }

    IEnumerator HandleWaypoint(Waypoint wp)
    {
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

        currentDir = choice;
        lastDir = choice;
    }

    void ExitChestTile()
    {
        if (lastChestTile != null)
        {
            lastChestTile.HideSymbol();
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
            }
            else if (hit.TryGetComponent<Key>(out Key key))
            {
                Debug.Log($"{name} landed on KEY");
                OnKeyTile = true;
                key.OnPlayerLanded(this);
            }
            else
            {
                Debug.LogWarning("Unknown special tile collided.");
            }
        }
    }

    public Vector3 GetPlayerBottomCenter()
    {
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        Vector3 bottomCenter = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        return bottomCenter;
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

    void HitAnotherPlayer()
    {
        if (InSpawnArea) return;

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
            }
        }
    }


}
