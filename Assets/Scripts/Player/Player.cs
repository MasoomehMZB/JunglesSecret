using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;


public class Player : MonoBehaviour
{
    [SerializeField] private Vector2Int currentDir = Vector2Int.zero;
    [SerializeField] private Vector2Int lastDir = Vector2Int.zero;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stepAmount = 0.54f;
    [SerializeField] private GameObject waypointPrefab;


    private Waypoint currentWaypoint;
    private bool isMoving = false;
    [SerializeField] private int requestedSteps = 0;
    private Chest lastChestTile = null;
    private bool OnKeyTile = false;

    public int steps = 0;


    public int points = 0;

    // Call this from external systems like dice roll
    //public void RequestMove(int steps)
    public void RequestMove()
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

    //del
    void Start()
    {
        StartCoroutine(TestMoveSequence());
    }

    IEnumerator TestMoveSequence()
    {
        steps = 2;
        RequestMove();

        // Wait until the player is no longer moving
        yield return new WaitUntil(() => !isMoving);
        //yield return new WaitForSeconds(3);

        //steps = 1;
        //RequestMove();

        //yield return new WaitUntil(() => !isMoving);

        //steps = 7;
        //RequestMove();
    }

    IEnumerator MoveRoutine()
    {
        if (requestedSteps > 0)
        {
            ExitKeyTile();
        }

        isMoving = true;
        bool isDeadEnd = false;
        Vector3 previousPos = transform.position;
        int previousSteps = requestedSteps;

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

            yield return StartCoroutine(CollidewithWaypoint((bool result) =>
            {
                isDeadEnd = result;
            }));

            if (isDeadEnd && requestedSteps == 0)
            {
                RevertToPreviousState(previousPos, previousSteps);
                Debug.Log("detected");
                yield return new WaitForSeconds(0.09f);

                RequestMove();
                yield break;
            }

            requestedSteps--;
        }

        CheckSpecialTile();
        currentDir = Vector2Int.zero;
        isMoving = false;
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

        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.1f, LayerMask.GetMask("Special"));
        if (hit)
        {
            if (hit.TryGetComponent<Chest>(out Chest chest))
            {
                chest.OnPlayerLanded(this);
                lastChestTile = chest;
            }
            else if (hit.TryGetComponent<Key>(out Key key))
            {
                OnKeyTile = true;
                key.OnPlayerLanded(this);
            }
            else
            {
                Debug.LogWarning("Unknown special tile collided.");
            }
        }
    }

    Vector3 GetPlayerBottomCenter()
    {
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        Vector3 bottomCenter = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        return bottomCenter;
    }

    IEnumerator CollidewithWaypoint(Action<bool> onResult)
    {
        Vector3 bottomCenter = GetPlayerBottomCenter();
        Vector3 offset = transform.position - bottomCenter;

        Collider2D wpCollider = Physics2D.OverlapCircle(bottomCenter, 0.15f, LayerMask.GetMask("Waypoint"));
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

        List<DirectionName> dirOptions = (lastDir == Vector2Int.right || lastDir == Vector2Int.left)
            ? new List<DirectionName> { DirectionName.Left, DirectionName.Right }
            : new List<DirectionName> { DirectionName.Up, DirectionName.Down };

        tempWaypoint.SetAllowedDirections(dirOptions);

        yield return StartCoroutine(HandleWaypoint(tempWaypoint));

        Destroy(tempGO);

    }
}
