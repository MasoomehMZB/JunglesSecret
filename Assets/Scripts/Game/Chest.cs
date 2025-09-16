using System.Collections;
using Mirror;
using UnityEngine;

public class Chest : NetworkBehaviour
{
    [SyncVar] public string symbolID;

    [SerializeField] private SpriteRenderer displayRenderer;
    
    [SerializeField] private GameObject highlightBorder;
    [SerializeField] Animator animator;

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
    public void TargetSetHighlight(NetworkConnectionToClient target, bool state, string trigger)
    {
        SetHighlightLocal(state, trigger);
    }

    public void PlayTeleportAnim()
    {
        animator.Play("TeleportAnimation", -1, 0f);
    }

    public void PlayChooseAnim()
    {
        animator.Play("ChooseAnimation", -1, 0f);
    }

    public void StopAnim()
    {
        animator.Play("Idle", -1, 0f);
    }

    private void SetHighlightLocal(bool state, string type)
    {
        if (state)
        {
            transform.localScale = originalScale * 1.3f;
            if (highlightBorder) highlightBorder.SetActive(true);

            if (type == "teleport") PlayTeleportAnim();
            else if (type == "choose") PlayChooseAnim();
        }
        else
        {
            transform.localScale = originalScale;
            if (highlightBorder) highlightBorder.SetActive(false);

            StopAnim();
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

}



