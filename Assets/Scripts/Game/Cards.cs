using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Cards : NetworkBehaviour
{
    public SpriteRenderer displayRenderer;
    public Queue<string> cardDeck = new Queue<string>(); // shuffled deck

    // card/state sync
    [SyncVar(hook = nameof(OnCardChanged))] public string currentCardId; // id of current card symbol
    [SyncVar] public int remainingCards;

    [Server]
    public void RevealCard()
    {
        if (cardDeck.Count == 8)
        {
            Debug.Log("No more cards left.");
            GameManager.Instance.GameOver = true;
            return;
        }

        currentCardId = cardDeck.Dequeue();

        remainingCards = cardDeck.Count;

        OnCardChanged(null, currentCardId);

        //Debug.Log($"Current Card Revealed: {currentCardId}");
    }

    void OnCardChanged(string oldId, string newId)
    {
        displayRenderer.sprite = GameManager.Instance.GetSpriteForSymbol(newId);
    }
}
