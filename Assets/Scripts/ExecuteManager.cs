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
    public List<IEnumerator> rollEffectStack = new List<IEnumerator>();
    public List<IEnumerator> battleOverEffectStack = new List<IEnumerator>();

    public void RevealCard(Card cardToReveal)
    {
        currentCard = cardToReveal;
        currentCard.gameObject.SetActive(true);
        //StartCoroutine(currentCard.GetComponent<AutoFade>().FadeEffect(revealFadeInTime, 0f));

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
    }

    public void StopRevealCard()
    {
        // deactivate card
        currentCard.gameObject.SetActive(false);
        //StartCoroutine(currentCard.GetComponent<AutoFade>().FadeEffect(0f, revealFadeOutTime));
        currentCard.transform.localScale = currentCard.defaultScale;

        // reset current card var
        currentCard = null;

        // refresh fields
        GameManager.instance.fieldManager.Refresh();
    }

    public void CardUseAbl(Card card)
    {
        StartCoroutine(CardUseAbility(card));
    }
    /// <summary>
    /// Triggers all necessary animations and switches after an ability is used
    /// </summary>
    /// <param name="card"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public IEnumerator CardUseAbility(Card card)
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

            // enabling presenter
            card.unit.unitPresenter.gameObject.SetActive(true);
        }
        
        // Playing unit animation
        yield return StartCoroutine(card.unit.EntranceAnimation());

        // Deactivating the card
        GameManager.instance.executeManager.StopRevealCard();

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

        // waiting until all effects are resolved
        yield return StartCoroutine(TriggerEffects());

        // refreshing field colors
        GameManager.instance.fieldManager.Refresh();

        // ending the turn 
        card.player.EndTurn();
    }

    public IEnumerator TriggerEffects(string effectType = "onPlay")
    {
        List<IEnumerator> stack = effectStack;

        // choosing the correct effect stack
        switch (effectType)
        {
            case "onRoll":
                stack = rollEffectStack;
                break;
            case "onBattleEnd":
                stack = battleOverEffectStack;
                break;
        }

        foreach (IEnumerator effect in stack)
        {
            yield return StartCoroutine(effect);
        }

        stack.Clear();
    }
}
