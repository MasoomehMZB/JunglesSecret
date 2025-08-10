using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class Cards : MonoBehaviour
{ 
    public SpriteRenderer displayRenderer;
    public Queue<SymbolDef> cardDeck = new Queue<SymbolDef>(); // shuffled deck
    private SymbolDef currentCard;

    public SymbolDef CurrentCard => currentCard;


    public void RevealCard()
    {
        if (cardDeck.Count == 0)
        {
            Debug.Log("No more cards left.");
           // GameManager.Instance.EndGame(); // Trigger end game
            return;
        }

        currentCard = cardDeck.Dequeue();
        displayRenderer.sprite = currentCard.sprite;

        Debug.Log($"Current Card Revealed: {currentCard.id}");
    }


}
