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
    Vector3 revealedCardScale = new Vector3(1.8f, 1.8f, 1f);

    public void RevealCard(Card cardToReveal)
    {
        currentCard = cardToReveal;

        // playing soundeffect
        AudioManager.instance.PlaySFX("NextCardSFX");

        // positioning the card
        currentCard.transform.position = revealedCardPos.position;
        currentCard.transform.localScale = revealedCardScale;

        // making card's abilities be ready to be clicked on
        if (GameManager.instance.yourTurn) currentCard.ActivateAbilities();
        // if its not player's turn mirror the card to appear on the opponents side of the screen
        else currentCard.transform.localPosition = new Vector2(Screen.width - currentCard.transform.localPosition.x, currentCard.transform.localPosition.y);

        // highlighting the unit
        currentCard.unit.HighlightUnit(true);

        // Telling player to choose an ability
        GameManager.instance.managerUI.NewHint("Pick one of card's abilities");
    }

    public void StopRevealCard()
    {
        currentCard.unit.HighlightUnit(false);
        currentCard.gameObject.SetActive(false);
        currentCard.transform.localScale = currentCard.defaultScale;

        currentCard = null;
    }

    public void LoadCardStack(List<Card> newCardStack, Player player)
    {
        player.plannedCardStack.Clear();
        player.plannedCardStack.AddRange(newCardStack);
    }

    public void NextCardReady()
    {
        // Getting next prepared card of current player
        Player currentPlayer = GameManager.instance.GetCurrentPlayer();
        currentCard = currentPlayer.plannedCardStack[0];

        // putting card under execute state UI and activating it
        currentCard.transform.SetParent(nextCardButton.transform, false);
        currentCard.gameObject.SetActive(true);

        // making scale and position match the button
        currentCard.transform.localScale = new Vector3(readyCardScale, readyCardScale, currentCard.transform.localScale.z);
        currentCard.transform.localPosition = nextCardPos.localPosition;

        readyRevealCard = true;
        if (GameManager.instance.yourTurn) nextCardButton.glow.SetActive(true);
    }
}
