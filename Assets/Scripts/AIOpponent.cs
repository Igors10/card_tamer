using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    Player playerObj;
    [SerializeField] AIConfigObj config;
    Coroutine currentAction;
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
                currentAction = StartCoroutine(PlaceRandomCard());
                break;

                /*
            case GameState.PLANNING:
                PlanCards();
                break;

            case GameState.EXECUTING:
                //currentAction = StartCoroutine(ResolvePlannedCard());
                break;*/

            case GameState.BATTLING:
                break;
            case GameState.BUYING:
                currentAction = StartCoroutine(BuyRandomCard());
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
        yield return new WaitForSeconds(0.5f);

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
        fieldToSpawnOn.PlayCard(cardToPlay, playerObj);
        //GameManager.instance.fieldManager.SpawnUnit(cardToPlay, fieldToSpawnOn);
        //GameManager.instance.handManager.AddCardToField(cardToPlay, playerObj);

        //yield return new WaitForSeconds(1f);
        yield return ChooseRandomAbility();

        // Ending the turn
        AIEndTurn();
    }

    IEnumerator ChooseRandomAbility()
    {
        // REVEALS THE CARD
        // Prepares the card and reveals it immideately
        //GameManager.instance.executeManager.NextCardReady();
        //GameManager.instance.executeManager.RevealCard();

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

        // USES AN ABILITY
        abilityToUse.UseAbility();
      
        yield return new WaitForSeconds(config.executingAbilityDelay);
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

   

    // =========================================================
    // =================== BATTLING ============================

    // add battle AI control

    // =========================================================
    // ===================== BUYING ============================

    IEnumerator BuyRandomCard()
    {
        yield return new WaitForSeconds(1.5f);

        ShopSlot[] slots = GameManager.instance.shopManager.shopSlots;

        Debug.Log("AIOpponent: Opponent is about to buy a card");

        for (int i = 0; i < config.shopRerollsPerTurn + 1; i++)
        {
            yield return new WaitForSeconds(config.buyingDelay);

            List<int> indices = new List<int>();
            for (int a = 0; i < slots.Length; i++) indices.Add(i);

            // shuffle
            for (int a = indices.Count - 1; a > 0; a--)
            {
                int j = Random.Range(0, a + 1);
                (indices[a], indices[j]) = (indices[j], indices[a]);
            }

            // try buying
            foreach (int index in indices)
            {
                Debug.Log("AIOpponent: Opponent tries to buy a slot");
                if (slots[index].gameObject.activeSelf)
                {
                    yield return StartCoroutine(slots[index].BuyCard(playerObj));
                    yield return null;
                }
            }

            // breaks for loop here since we dont need to reroll on last iteration
            if (i == config.shopRerollsPerTurn) break;

            // if couldnt buy anything it will try to reroll
            GameManager.instance.shopManager.RerollShop(playerObj);
        }

        Debug.Log("AIOpponent: opponent went through slots and didnt buy anythng");
        AIEndTurn();
    }

    // =========================================================
}





