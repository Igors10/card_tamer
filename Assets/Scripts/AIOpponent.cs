using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    Player playerObj;
    [SerializeField] AIConfigObj config;
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

        // adding random rats
        int randomStartingCard = Random.Range(0, config.startingCardOptions.Count);
        for (int i = 0; i < 4; i++)
        {
            AIstartingHandCards.Add(config.startingCardOptions[randomStartingCard]);
        }

        // adding random special
        int randomSpecialCard = Random.Range(0, config.startingSpecialOptions.Count);
        AIstartingHandCards.Add(config.startingSpecialOptions[randomSpecialCard]);

        GameManager.instance.cardGenerator.CreateStartingHand(AIstartingHandCards, playerObj);
    }

    public void AIStartTurn()
    {
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                StartCoroutine(PlaceRandomCard());
                break;

            case GameState.PLANNING:
                PlanCards();
                break;

            case GameState.EXECUTING:
                StartCoroutine(ResolvePlannedCard());
                break;

            case GameState.BATTLING:
                break;
            case GameState.BUYING:
                break;

        }
    }

    void AIEndTurn()
    {
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
        yield return new WaitForSeconds(config.placingDelay);

        // Getting all empty fields
        List<Field> availableFields = GameManager.instance.fieldManager.FindEmptyFields(playerObj.fields);

        // if no cards or no empty space, AI ends turn and is ready with placing
        if (playerObj.cardsInHand.Count < 1 || availableFields.Count <= 0) { AIReady();  yield break; }

        // chosing card to place
        int randomCard = Random.Range(0, playerObj.cardsInHand.Count);
        Card cardToPlay = playerObj.cardsInHand[randomCard];

        // choosing random field
        int randomField = Random.Range(0, availableFields.Count);
        Field fieldToSpawnOn = availableFields[randomField];

        // Spawning a unit
        GameManager.instance.fieldManager.SpawnUnit(cardToPlay, fieldToSpawnOn);
        GameManager.instance.handManager.AddCardToField(cardToPlay, playerObj);

        // Ending the turn
        yield return new WaitForSeconds(config.placingDelay);
        AIEndTurn();
    }

    // =========================================================
    // =================== PLANNING ============================

    /// <summary>
    /// Randomly shuffles AI cards to simulate planning
    /// </summary>
    void PlanCards()
    {
        // Shuffling cards that are on field
        int n = playerObj.cardsOnField.Count;

        while (n > 1)
        {
            n--;
            int randomCard = Random.Range(0, n + 1);
            (playerObj.cardsOnField[randomCard], playerObj.cardsOnField[n]) = (playerObj.cardsOnField[n], playerObj.cardsOnField[randomCard]);
        }

        // after cards are shuffled opponent is ready to move to next game phase
        GameManager.instance.opponent.endStateReady = true;
    }

    // =========================================================
    // =================== EXECUTING============================

    IEnumerator ResolvePlannedCard()
    {
        yield return new WaitForSeconds(config.executingRevealDelay);

        // REVEALS THE CARD
        // Prepares the card and reveals it immideately
        GameManager.instance.executeManager.NextCardReady();
        GameManager.instance.executeManager.RevealCard();

        yield return new WaitForSeconds(config.executingAbilityDelay);

        // SELECTS AN ABILITY
        Card cardResolving = GameManager.instance.executeManager.currentCard;

        // Getting all cards active abilities
        List<Ability> cardAbilities = new List<Ability>();
        for (int i = 0; i < cardResolving.abilities.Length; i++)
        {
            if (cardResolving.abilities[i].abilityData.isPassive == false) cardAbilities.Add(cardResolving.abilities[i]);
        }

        // Picking one random card's active ability
        int randomAbility = Random.Range(0, cardAbilities.Count);
        Ability abilityToUse = cardAbilities[randomAbility];
        abilityToUse.SelectAbility(true);

        yield return new WaitForSeconds(config.executingAbilityDelay);

        // MOVES THE UNIT
        // Get all available fields
        List<Field> availableFields = new List<Field>();

        for (int i = 0; i < 4; i++)
        {
            if (playerObj.fields[i].unitSlots[0].gameObject.activeSelf 
                || playerObj.fields[i].unitSlots[1].gameObject.activeSelf 
                || playerObj.fields[i] == cardResolving.unit.currentField)
            {
                availableFields.Add(playerObj.fields[i]);
            }
        }

        // Picking random field
        int randomField = Random.Range(0, availableFields.Count);

        // if field picked is different from one unit already occupying move unit there
        if (availableFields[randomField] != cardResolving.unit.currentField)
        {
            int enabledSlotID = (availableFields[randomField].unitSlots[0].gameObject.activeSelf) ? 0 : 1;
            GameManager.instance.fieldManager.MoveUnit(cardResolving.unit, availableFields[randomField], enabledSlotID, false);
            yield return new WaitForSeconds(config.executingAbilityDelay);
        }

        // USES AN ABILITY
        abilityToUse.UseAbility();
        GameManager.instance.executeManager.StopRevealCard();
        GameManager.instance.fieldManager.DisableAllSlots();

        yield return new WaitForSeconds(config.executingAbilityDelay);

        // ENDS THE TURN
        AIEndTurn();
    }

    // =========================================================
}





