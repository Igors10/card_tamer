using FishNet.Demo.AdditiveScenes;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    Player playerObj;
    [SerializeField] AIConfigObj config;
    Coroutine currentAction;
    CardGenerator generator;
    void Awake()
    {
        // Checking if match is offline
        if (GameManager.instance.playerConfig.offlineMatch == false) Destroy(this);
        else playerObj = GetComponent<Player>();
    }

    private void Start()
    {
        CreateAIStartingHand();
    }

    void CreateAIStartingHand()
    {
        List<CreatureObj> AIstartingHandCards = new List<CreatureObj>();
        generator = GameManager.instance.cardGenerator;

        for (int i = 0; i < GameManager.instance.startingCardAmount; i++)
        {
            // choosing random card
            CreatureObj newCardToAdd = (Random.value > 0.5f) ? generator.basicCardData : generator.PickRandomCard("basic");
            AIstartingHandCards.Add(newCardToAdd);
        }

        GameManager.instance.cardGenerator.CreateStartingHand(AIstartingHandCards, playerObj);
    }

    public void AIStartTurn()
    {
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                currentAction = StartCoroutine(PlaceRandomCard());
                break;
        }
    }

    public void AIEndTurn()
    {
        // stop any current action
        StopCoroutine(currentAction);

        if (GameManager.instance.player.endStateReady) GameManager.instance.CheckEndState();
        else GameManager.instance.StartTurn();
    }

    void AIReady()
    {
        GameManager.instance.opponent.endStateReady = true;
        AIEndTurn();
    }

    // ==================== PLACING ============================

    /// <summary>
    /// Places a random card from AI hand to an empty field slot
    /// </summary>
    /// <returns></returns>
    IEnumerator PlaceRandomCard()
    {
        yield return new WaitForSeconds(3.5f);

        // Getting all empty fields
        List<Field> availableFields = GameManager.instance.fieldManager.FindEmptyFields(playerObj.fields);

        // if no cards or no empty space, AI ends turn and is ready with placing
        if (playerObj.cardsInHand.Count < 1 || availableFields.Count <= 0) { AIReady(); yield break; }

        // chosing card to place
        int randomCard = Random.Range(0, playerObj.cardsInHand.Count);
        Card cardToPlay = playerObj.cardsInHand[randomCard];

        // choosing random field
        int randomField = Random.Range(0, availableFields.Count);
        Field fieldToSpawnOn = availableFields[randomField];

        // Spawning a unit
        fieldToSpawnOn.PlayCard(cardToPlay, playerObj);
    }

    // =========================================================
    // ===================== BUYING ============================

    public IEnumerator DrawNewCards()
    {
        Debug.Log("AIOpponent: AI is about to draw cards.");
        if (playerObj.endStateReady) { Debug.Log("AIOpponent: AI already bought all the cards"); yield break; }

        ShopManager shop = GameManager.instance.shopManager;
        int starAmount = playerObj.shopStars + shop.shopStarsAfterBattle + playerObj.deadUnitsThisRound;
        Debug.Log("AIOpponent: AI opponent has " + starAmount + " stars");
        playerObj.shopStars = starAmount;
        int nextStarAmount = playerObj.maxStars + 1;
        bool starsUpgraded = false;

        bool nothingToBuy = false;

        while (nothingToBuy == false)
        {
            if (playerObj.shopStars >= nextStarAmount && starsUpgraded == false) // upgrade stars
            {
                playerObj.maxStars = nextStarAmount; // do enemy AI shopping
                playerObj.shopStars -= nextStarAmount;
                starsUpgraded = true;
            }
            else if (CountSpecialCards() < playerObj.maxStars && playerObj.shopStars >= shop.drawSpecialPrice && playerObj.cardsInHand.Count < GameManager.instance.maxHandSize)
            {
                generator.CreateCard(generator.PickRandomCard("special"), playerObj);
                playerObj.shopStars -= shop.drawSpecialPrice;
            }
            else if (playerObj.shopStars >= shop.drawCardPrice && playerObj.cardsInHand.Count < GameManager.instance.maxHandSize)
            {
                generator.CreateCard(generator.PickRandomCard("basic"), playerObj);
                playerObj.shopStars -= shop.drawCardPrice;
            }
            else nothingToBuy = true;

            // checking just to not make the look go infinitely
            yield return null;
        }

        playerObj.endStateReady = true;
        AIEndTurn();
    }
    
    // Counts how many special cards opponent has
    int CountSpecialCards()
    {
        int specialCardAmount = 0;
        foreach (Card card in playerObj.cardsInHand)
        {
            if (card.cardData.isSpecial) specialCardAmount++;
        }

        return specialCardAmount;
    }

    // =========================================================
}





