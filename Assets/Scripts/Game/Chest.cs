using System.Collections;
using Mirror;
using UnityEngine;

public class Chest : NetworkBehaviour
{
    [SyncVar] public string symbolID;

    [SerializeField] private SpriteRenderer displayRenderer;
    [SerializeField] private GameObject highlightBorder;

    private Vector3 originalScale;

    void Awake() => originalScale = transform.localScale;

    [Server]
    public void OnPlayerLanded(Player player)
    {
        if (player != null && player.connectionToClient != null)
            TargetShowSymbol(player.connectionToClient, symbolID);
    }

    [TargetRpc]
    void TargetShowSymbol(NetworkConnectionToClient target, string id)
    {
        displayRenderer.sprite = GameManager.Instance.GetSpriteForSymbol(id);
    }

    [TargetRpc]
    public void TargetHideSymbol(NetworkConnectionToClient target)
    {
        displayRenderer.sprite = null;
    }


    [TargetRpc]
    public void TargetSetHighlight(NetworkConnectionToClient target, bool state)
    {
        SetHighlightLocal(state);
    }

   
    private void SetHighlightLocal(bool state)
    {
        Debug.Log($"[Client] Highlighting chest {name} = {state}");
        if (state)
        {
            transform.localScale = originalScale * 1.3f;
            if (highlightBorder) highlightBorder.SetActive(true);
        }
        else
        {
            transform.localScale = originalScale;
            if (highlightBorder) highlightBorder.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (!NetworkClient.active) return;

        var conn = NetworkClient.connection;
        if (conn == null || conn.identity == null)return;
        
        Player localPlayer = conn.identity.GetComponent<Player>();
        if (localPlayer == null) return;
        
        // Forward the chest click to the player's Command (send chest netId)
        uint chestId = netId; 
        Debug.Log($"[Client] Forwarding chest click to local player. chestNetId={chestId}");
        localPlayer.CmdSelectChest(chestId);
    }

    [ClientRpc]
    public void RpcRevealChosenSymbol()
    {
        displayRenderer.sprite = GameManager.Instance.GetSpriteForSymbol(symbolID);
    }

    [ClientRpc]
    public void RpcHideSymbol()
    {
        displayRenderer.sprite = null;
    }

    //[ClientRpc]
    //void RpcPlayRevealFx()
    //{
    //    // optional: particles/sound when permanently revealed
    //}

}



