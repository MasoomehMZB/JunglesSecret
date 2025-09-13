using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Security.Cryptography;

public class Cards : NetworkBehaviour
{ 
    public SpriteRenderer displayRenderer;
    public Queue<string> cardDeck = new Queue<string>(); // shuffled deck

    // card/state sync
    [SyncVar(hook = nameof(OnCardChanged))] public string currentCardId; // id of current card symbol

    [Server]
    public void RevealCard()
    {
        if (cardDeck.Count == 0)
        {
            Debug.Log("No more cards left.");
            GameManager.Instance.EndGame(); // Trigger end game
            return;
        }

        currentCardId = cardDeck.Dequeue();

        OnCardChanged(null, currentCardId);

        Debug.Log($"Current Card Revealed: {currentCardId}");
    }

    void OnCardChanged(string oldId, string newId)
    {
        displayRenderer.sprite = GameManager.Instance.GetSpriteForSymbol(newId);
    }
}
