using Mirror;
using UnityEngine;

public class Key : NetworkBehaviour
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
        if (!NetworkClient.active) return;

        var conn = NetworkClient.connection;
        if (conn == null || conn.identity == null) return;

        Player localPlayer = conn.identity.GetComponent<Player>();
        if (localPlayer == null) return;

        uint keyId = netId;
        // Debug.Log($"[Client] Forwarding key click to local player. keyNetId={keyId}");
        localPlayer.CmdSelectKey(keyId);
    }

    [TargetRpc]
    public void TargetSetHighlight(NetworkConnectionToClient target, bool state)
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
