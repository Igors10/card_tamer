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
    [HideInInspector] public List<string> playedCardNames = new List<string>();
    [HideInInspector] public Card currentCard;


    [Header("revealed card params")]
    [HideInInspector] public bool readyRevealCard = false;
    Vector3 revealedCardScale = new Vector3(1.8f, 1.8f, 1f);

    [Header("card play params")]
    [SerializeField] float zoomIntensity;
    public float zoomTime;

    [Header("Effects")]
    public List<IEnumerator> effectStack = new List<IEnumerator>();

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
        currentCard.unit.DisableRollingUI();

        // deactivate card
        currentCard.gameObject.SetActive(false);
        currentCard.transform.localScale = currentCard.defaultScale;

        // reset current card var
        currentCard = null;

        // refresh fields
        GameManager.instance.fieldManager.Refresh();
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
        // checking if the card being played first time this game
        bool playedFirstTime = !playedCardNames.Contains(card.cardData.name);

        // add to the played card list
        if (playedFirstTime) playedCardNames.Add(card.cardData.name);

        // zooming in on the unit
        if (playedFirstTime)
        {
            GameManager.instance.mainCamera.ZoomIn(card.unit.gameObject, zoomIntensity, zoomTime);
            yield return new WaitForSeconds(zoomTime);
        }
        
        // Playing unit animation
        yield return StartCoroutine(card.unit.AbilityAnimation(ability));

        // zooming out from the unit
        if (playedFirstTime)
        {
            GameManager.instance.mainCamera.ZoomOut();
            yield return new WaitForSeconds(zoomTime);
        }

        // Card effect + power
        yield return StartCoroutine(card.unit.RollPower());        

        // Triggering on play card effects
        GameManager.instance.BroadcastOnCardPlayed(card);

        // Deactivating the card
        GameManager.instance.executeManager.StopRevealCard();

        // waiting until all effects are resolved
        yield return StartCoroutine(TriggerEffects());

        // ending the turn 
        card.player.EndTurn();
    }

    public IEnumerator TriggerEffects()
    {
        foreach (IEnumerator effect in effectStack)
        {
            yield return StartCoroutine(effect);
        }

        effectStack.Clear();
    }
}
