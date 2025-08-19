using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] private GameObject highlightBorder;
     Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale; // store default size
    }
    public void OnPlayerLanded(Player player)
    {
        GameManager.Instance.StartGuessMode(player);
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance.TeleportModeActive)
        {
            GameManager.Instance.TeleportTo(gameObject);
        }
    }

    public void SetHighlight(bool state)
    {
        if (state)
        {
            transform.localScale = originalScale * 1.3f; // enlarge
            if (highlightBorder != null)
                highlightBorder.SetActive(state);
        }

        else
        {
            transform.localScale = originalScale;
            if (highlightBorder != null)
                highlightBorder.SetActive(state);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        // Try to get the collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Draw a wireframe cube in collider bounds
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
