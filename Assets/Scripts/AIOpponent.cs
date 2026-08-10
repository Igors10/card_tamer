using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    [Header("refs")]
    Player playerObj;
    [SerializeField] AIConfigObj config;
    Coroutine currentAction;
    CardGenerator generator;

    [Header("Bot identity")]
    public OpponentIndentity chosenIdentity;
    List<UnitPreset> basicUnitPresets = new List<UnitPreset>();
    List<UnitPreset> specialUnitPresets = new List<UnitPreset>();


    void Awake()
    {
        // Checking if match is offline
        if (GameManager.instance.playerConfig.offlineMatch == false) Destroy(this);
        else playerObj = GetComponent<Player>();

        InitAIOpponent();
    }

    private void Start()
    {
        // picking random color
        playerObj.playerColor = GameManager.instance.cardDatabase.allPlayerColors[Random.Range(0, GameManager.instance.cardDatabase.allPlayerColors.Count)];
    }

    /// <summary>
    /// Selects one of premade identities with specific card theme for the match
    /// </summary>
    void InitAIOpponent()
    {
        // picking one of the premade player identities (cardsets) for the opponent
        if (GameData.randomOpponentIdentity == true || chosenIdentity == null) chosenIdentity = GameManager.instance.cardDatabase.GetRandomCardSet();
        playerObj.playerUI.playerName.text = chosenIdentity.opponentName + " (Bot)";
    }

    public void CreateAIStartingHand()
    {
        generator = GameManager.instance.cardGenerator;

        for (int i = 0; i < GameManager.instance.startingCardAmount; i++)
        {
            AddBasic();
        }
    }

    public void AddBasic()
    {
        // refills the list if its empty
        if (basicUnitPresets == null || basicUnitPresets.Count == 0) basicUnitPresets = new List<UnitPreset>(chosenIdentity.basicUnits);
   
        // choosing random ability
        AbilityObj newAbility = (Random.value > 0.5f) ? generator.PickRandomAbility("default") : generator.PickRandomAbility("basic");
        // choosing random unit preset
        UnitPreset newUnitPreset = basicUnitPresets[Random.Range(0, basicUnitPresets.Count)];
        basicUnitPresets.Remove(newUnitPreset);

        // constructing cardData
        CreatureObj newCreatureObj = generator.ConstructNewCard(newUnitPreset.unitName, newUnitPreset.sprite, newAbility, 
            Colors.instance.GetRandomSecondaryColor(), Colors.instance.GetRandomSecondaryColor());
        // Adding card to the player
        generator.CreateCard(newCreatureObj, playerObj);
    }

    public void AddSpecial()
    {
        // refills the list if its empty
        if (specialUnitPresets.Count == 0 || specialUnitPresets == null) specialUnitPresets = new List<UnitPreset>(chosenIdentity.specialUnits);

        // choosing random ability
        AbilityObj newAbility = generator.PickRandomAbility("special");
        // choosing random unit preset
        UnitPreset newUnitPreset = specialUnitPresets[Random.Range(0, specialUnitPresets.Count)];
        specialUnitPresets.Remove(newUnitPreset);

        // constructing cardData
        CreatureObj newCreatureObj = generator.ConstructNewCard(newUnitPreset.unitName, newUnitPreset.sprite, newAbility, 
            Colors.instance.GetRandomSecondaryColor(), Colors.instance.GetRandomSecondaryColor());
        // Adding card to the player
        generator.CreateCard(newCreatureObj, playerObj);
    }

    public void AIStartTurn()
    {
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                currentAction = StartCoroutine(PlaceRandomCard());
                break;

            case GameState.DISCARDING:
                currentAction = StartCoroutine(DiscardOpponentUnits());
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
    // ===================== DISCARDING=========================

    IEnumerator DiscardOpponentUnits()
    {
        // waiting until discard becomes available
        //while (GameManager.instance.discardManager.discardAvailable == false) yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(3.5f);
        Debug.Log("AIOpponent: requesting auto discard");

        // request autodiscard
        StartCoroutine(GameManager.instance.discardManager.AutoDiscard(GameManager.instance.GetOpponentOfPlayer(playerObj)));
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
            /*
            if (playerObj.shopStars >= nextStarAmount && starsUpgraded == false && nextStarAmount < 2) 
            {
                playerObj.maxStars = nextStarAmount; 
                playerObj.shopStars -= nextStarAmount;
                starsUpgraded = true;
            } */

            // if has enough stars and no specials the AI will buy a special card
            if (CountSpecialCards() < 1 && playerObj.shopStars >= shop.drawSpecialPrice && playerObj.cardsInHand.Count < GameManager.instance.maxHandSize)
            {
                AddSpecial();
                playerObj.shopStars -= shop.drawSpecialPrice;
            }
            // otherwise it will buy regular card (if has enough stars)
            else if (playerObj.shopStars >= shop.drawCardPrice && playerObj.cardsInHand.Count < GameManager.instance.maxHandSize)
            {
                AddBasic();
                playerObj.shopStars -= shop.drawCardPrice;
            }
            // stops shopping only if it can't afford anything
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
            if (card.cardData.ability[0].isSpecial) specialCardAmount++;
        }

        return specialCardAmount;
    }

    // =========================================================
}





