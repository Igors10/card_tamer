using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ExecuteManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] NextCardButton nextCardButton;
    [SerializeField] Transform nextCardPos;
    [SerializeField] Transform revealedCardPos;
    [SerializeField] GameObject cardStackObj;

    // cards
    List<Card> plannedCardStack = new List<Card>();
    public Card currentCard;

    [HideInInspector] public bool readyRevealCard = false;
    float readyCardScale;

    public void LoadCardStack(List<Card> newCardStack)
    {
        plannedCardStack.Clear();
        plannedCardStack.AddRange(newCardStack);
        NextCardReady(plannedCardStack[0]);
    }

    public void RevealCard()
    {
        readyRevealCard = false;

        currentCard.transform.localPosition = revealedCardPos.localPosition;
        currentCard.transform.localScale = currentCard.highlightedScale;
        currentCard.ActivateAbilities();
    }

    void NextCardReady(Card card)
    {
        currentCard = card;
        // putting card under execute state UI and activating it
        card.transform.SetParent(nextCardButton.transform, false);
        card.gameObject.SetActive(true);

        // making scale and position match the button
        card.transform.localScale = new Vector3(readyCardScale, readyCardScale, card.transform.localScale.z);
        card.transform.localPosition = nextCardButton.transform.localPosition;

        readyRevealCard = true;
        nextCardButton.glow.SetActive(true);
    }
}
