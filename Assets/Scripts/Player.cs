
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public Vector2Int dir = Vector2Int.right;             // Current movement direction
    [SerializeField] private float speed = 1f; // Added default speed value
    private Waypoint currentWaypoint;   // Track current waypoint
    [SerializeField] private float stepAmount = 0.54f;

    // this gets deleted after dice is made
    public int steps = 0;

    //public void MoveSteps(int steps) => StartCoroutine(MoveRoutine(steps));
    public void MoveSteps() => StartCoroutine(MoveRoutine());

    //del
    void Start()
    {
        MoveSteps();
    }

    //IEnumerator MoveRoutine(int steps)
    IEnumerator MoveRoutine()
    {
        while (steps > 0)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + new Vector3(dir.x * stepAmount, dir.y * stepAmount, 0);

            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / speed;
            float elapsed = 0f;

            // Smooth movement between positions
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Snap to final position
            transform.position = targetPos;

            // Get player's circle collider
            BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
            Vector3 bottomCenter = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
            Vector3 offset = transform.position - bottomCenter;


            // Find the waypoint
            Collider2D wpCollider = Physics2D.OverlapCircle(bottomCenter, 0.15f, LayerMask.GetMask("Waypoint"));
            if (wpCollider != null)
            {
                currentWaypoint = wpCollider.GetComponent<Waypoint>();
                if (currentWaypoint != null)
                {
                    // Move player so collider center aligns with waypoint center
                    transform.position = currentWaypoint.transform.position + offset;
                    yield return StartCoroutine(HandleWaypoint(currentWaypoint));
                }
            }
            steps--;

        }

        // After moving all steps
        CheckSpecialTile();
    }

    IEnumerator HandleWaypoint(Waypoint wp)
    {
        bool chosen = false;
        Vector2Int choice = dir; // Default to current direction

        // Show UI and wait for choice
        wp.ShowChoicesUI((Vector2Int selectedDir) => {
            choice = selectedDir;
            chosen = true;
            Debug.Log($"choice is {selectedDir}");
        }, dir);

        while (!chosen)
            yield return null;

        dir = choice;
    }

    void CheckSpecialTile()
    {

        switch(currentWaypoint.name)
        {
            case "Chest":
                // Handle special tile logic here
                Debug.Log("Landed on a special tile!");
                break;
            case "Key":
                // Handle key tile logic here
                Debug.Log("Landed on a key tile!");
                break;
            default:
                // Handle normal tile logic here
                Debug.Log("Landed on a normal tile.");
                break;
            }
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.1f, LayerMask.GetMask("Special"));
        if (hit) hit.SendMessage("OnPlayerLanded", this);
    }

}
