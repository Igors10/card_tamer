using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("card play params")]
    [SerializeField] float zoomIntensity;
    public float zoomTime;

    public void RevealCard(Card cardToReveal)
    {
        currentCard = cardToReveal;
        currentCard.gameObject.SetActive(true);

        // playing soundeffect
        AudioManager.instance.PlaySFX("NextCardSFX");

        // positioning the card
        currentCard.transform.position = revealedCardPos.position;
        currentCard.transform.localScale = revealedCardScale;

        // making card's abilities be ready to be clicked on
        if (GameManager.instance.yourTurn) currentCard.ActivateAbilities();
        // if its not player's turn mirror the card to appear on the opponents side of the screen
        else 
        {
            Camera camera = Camera.main;
            float rightSideX = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.nearClipPlane)).x;
            currentCard.transform.localPosition = new Vector2(rightSideX - currentCard.transform.localPosition.x, currentCard.transform.localPosition.y);
            
        }

        // Telling player to choose an ability
        GameManager.instance.managerUI.NewHint("Pick one of card's abilities");
    }

    public void StopRevealCard()
    {
        // hide die from unit UI
        currentCard.unit.die.DisableDie();

        // deactivate card
        currentCard.gameObject.SetActive(false);
        currentCard.transform.localScale = currentCard.defaultScale;

        // reset current card var
        currentCard = null;
    }

    public void CardUseAbl(Card card, Ability ability)
    {
        StartCoroutine(CardUseAbility(card, ability));
    }
    /// <summary>
    /// Triggers all necessary animations and switches after an ability is used
    /// </summary>
    /// <param name="card"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public IEnumerator CardUseAbility(Card card, Ability ability)
    {
        // zooming in on the unit
        GameManager.instance.mainCamera.ZoomIn(card.unit.gameObject, zoomIntensity, zoomTime);
        yield return new WaitForSeconds(zoomTime * 2 / 3);

        // Playing unit animation
        yield return StartCoroutine(card.unit.AbilityAnimation(ability));

        // Card effect + power
        yield return StartCoroutine(card.unit.RollPower());

        // Deactivating the card
        GameManager.instance.executeManager.StopRevealCard();

        // zooming out from the unit
        GameManager.instance.mainCamera.ZoomOut();
        yield return new WaitForSeconds(zoomTime * 1 / 3);

        // ending the turn 
        card.player.EndTurn();
    }


    /*
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
    }*/
}
