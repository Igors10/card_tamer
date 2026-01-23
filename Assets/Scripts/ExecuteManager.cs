using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ExecuteManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] NextCardButton nextCardButton;
    [SerializeField] Transform nextCardPos;
    [SerializeField] Transform revealedCardPos;
    public GameObject cardStackObj;

    // cards
    [HideInInspector] public List<Card> plannedCardStack = new List<Card>();
    [HideInInspector] public Card currentCard;

    [Header("revealed card params")]
    [HideInInspector] public bool readyRevealCard = false;
    float readyCardScale = 0.6f;
    Vector3 revealedCardScale = new Vector3(2f, 2f, 1f);

    public void LoadCardStack(List<Card> newCardStack)
    {
        plannedCardStack.Clear();
        plannedCardStack.AddRange(newCardStack);
        NextCardReady(plannedCardStack[0]);
    }

    public void RevealCard()
    {
        readyRevealCard = false;

        // positioning the card
        currentCard.transform.localPosition = revealedCardPos.localPosition;
        currentCard.transform.localScale = revealedCardScale;

        // making card's abilities be ready to be clicked on
        currentCard.ActivateAbilities();

        // highlighting the unit
        currentCard.unit.HighlightUnit(true);
    }

    void NextCardReady(Card card)
    {
        currentCard = card;
        // putting card under execute state UI and activating it
        card.transform.SetParent(nextCardButton.transform, false);
        card.gameObject.SetActive(true);

        // making scale and position match the button
        card.transform.localScale = new Vector3(readyCardScale, readyCardScale, card.transform.localScale.z);
        card.transform.localPosition = nextCardPos.localPosition;

        readyRevealCard = true;
        nextCardButton.glow.SetActive(true);
    }
}
