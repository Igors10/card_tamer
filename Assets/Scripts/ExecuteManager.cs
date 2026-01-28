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

        // Telling player to choose an ability
        GameManager.instance.managerUI.NewHint("Pick one of card's abilities");
    }

    public void NextCardReady()
    {
        currentCard = plannedCardStack[0];
        // putting card under execute state UI and activating it
        currentCard.transform.SetParent(nextCardButton.transform, false);
        currentCard.gameObject.SetActive(true);

        // making scale and position match the button
        currentCard.transform.localScale = new Vector3(readyCardScale, readyCardScale, currentCard.transform.localScale.z);
        currentCard.transform.localPosition = nextCardPos.localPosition;

        readyRevealCard = true;
        nextCardButton.glow.SetActive(true);
    }

    public void StopRevealCard()
    {
        currentCard.unit.HighlightUnit(false);
        currentCard.gameObject.SetActive(false);
        plannedCardStack.Remove(plannedCardStack[0]);
        currentCard = null;
    }
}
