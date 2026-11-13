using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        if (GameManager.instance.playerConfig.offlineMatch == false)
        {
            Destroy(this);
            return;
        }
        playerObj = GetComponent<Player>();

        InitAIOpponent();
    }

    private void Start()
    {
        if (playerObj == null) return; // removed itself in Awake (online match)

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
        Sticker newAbility = generator.PickRandomSticker("basic");
        // choosing random unit preset
        UnitPreset newUnitPreset = basicUnitPresets[Random.Range(0, basicUnitPresets.Count)];
        basicUnitPresets.Remove(newUnitPreset);

        // constructing cardData
        CreatureObj newCreatureObj = generator.ConstructNewCard(newUnitPreset.unitName, newUnitPreset.sprite, newAbility, 
            Colors.instance.GetDarkenColor(playerObj.playerColor), Color.blue);
        // Adding card to the player
        generator.CreateCard(newCreatureObj, playerObj);
    }

    public void AddSpecial()
    {
        // refills the list if its empty (identities without special units use their basic units)
        if (specialUnitPresets == null || specialUnitPresets.Count == 0) specialUnitPresets = new List<UnitPreset>(chosenIdentity.specialUnits);
        if (specialUnitPresets.Count == 0) specialUnitPresets = new List<UnitPreset>(chosenIdentity.basicUnits);

        // choosing random ability
        Sticker newAbility = generator.PickRandomSticker("special");
        // choosing random unit preset
        UnitPreset newUnitPreset = specialUnitPresets[Random.Range(0, specialUnitPresets.Count)];
        specialUnitPresets.Remove(newUnitPreset);

        // constructing cardData
        CreatureObj newCreatureObj = generator.ConstructNewCard(newUnitPreset.unitName, newUnitPreset.sprite, newAbility,
            Colors.instance.GetDarkenColor(playerObj.playerColor), Color.blue);
        // Adding card to the player
        generator.CreateCard(newCreatureObj, playerObj);
    }

    public void AIStartTurn()
    {
        switch (GameManager.instance.currentState)
        {
            case GameState.PLACING:
                ArrangeField();
                AIEndTurn();
                break;

            case GameState.BATTLING:
                currentAction = StartCoroutine(AttackRandomCard());
                break;
        }
    }

    // ==================== ATTACKING ============================

    /// <summary>
    /// Attacks a random player card with a random card of its own
    /// </summary>
    IEnumerator AttackRandomCard()
    {
        yield return new WaitForSeconds(1.5f);

        // first game: the bot thinks until the rat is done talking, so the player can read everything
        while (RatBuddy.instance.IsTalking) yield return null;

        Player target = GameManager.instance.player;
        Card attackingCard = playerObj.cardsOnField[Random.Range(0, playerObj.cardsOnField.Count)];
        Card targetCard = target.cardsOnField[Random.Range(0, target.cardsOnField.Count)];
        FightManager.instance.Attack(attackingCard, targetCard);
    }

    public void AIEndTurn()
    {
        // stop any current action
        if (currentAction != null) StopCoroutine(currentAction);

        if (GameManager.instance.player.endStateReady) GameManager.instance.CheckEndState();
        else GameManager.instance.player.StartTurn();
    }

    void AIReady()
    {
        GameManager.instance.opponent.endStateReady = true;
        AIEndTurn();
    }

    // ==================== ARRANGING ============================

    /// <summary>
    /// Puts the field cards in a random order and is ready
    /// </summary>
    public void ArrangeField()
    {
        List<Card> field = playerObj.cardsOnField;
        for (int i = field.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (field[i], field[j]) = (field[j], field[i]);
        }
        GameManager.instance.handManager.UpdateHandVisuals(playerObj);

        playerObj.endStateReady = true;
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
        // forced to play the special card
        if (!playerObj.CanPlayCard(cardToPlay)) cardToPlay = playerObj.GetSpecialInHand();

        // choosing random field
        int randomField = Random.Range(0, availableFields.Count);
        Field fieldToSpawnOn = availableFields[randomField];

        // Spawning a unit
        fieldToSpawnOn.PlayCard(cardToPlay, playerObj);
    }

    // =========================================================
    // ===================== DISCARDING=========================
    // ( autodiscard no longer has input)
    public void DiscardOpponentUnit()
    {
        if (playerObj.cardsInDiscard.Count < 1) return;

        int randomDiscardCardID = Random.Range(0, playerObj.cardsInDiscard.Count);
        Card cardToDiscard = playerObj.cardsInDiscard[randomDiscardCardID];
        cardToDiscard.DestroyCard();
    }

    // =========================================================
    // ===================== BUYING ============================

    public IEnumerator DrawNewCards()
    {
        Debug.Log("AIOpponent: AI is about to draw cards.");
        if (playerObj.endStateReady) { Debug.Log("AIOpponent: AI already bought all the cards"); yield break; }

        ShopManager shop = GameManager.instance.shopManager;
        // same stars as the player: the base amount, +1 for losing the round, Beast compensation
        bool lostRound = playerObj.damageTakenThisRound > GameManager.instance.player.damageTakenThisRound;
        int starAmount = playerObj.shopStars + shop.shopStarsAfterBattle + (lostRound ? 1 : 0) + playerObj.compensationStars;
        playerObj.compensationStars = 0;
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

            // a free sticker is taken first, it costs nothing
            if (shop.StickerPriceFor(playerObj) == 0 && playerObj.cardsInHand.Exists(card => card.cardData.CanAddSticker))
            {
                AddRandomSticker();
                playerObj.shopStars -= shop.TakeStickerPrice(playerObj);
            }
            // if has enough stars and no specials the AI will buy a special card
            else if (!playerObj.HasSpecialCard() && playerObj.shopStars >= shop.drawSpecialPrice && playerObj.cardsInHand.Count < GameManager.instance.maxHandSize)
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
            // leftover stars go to stickers on random cards
            else if (playerObj.shopStars >= shop.stickerPrice && playerObj.cardsInHand.Exists(card => card.cardData.CanAddSticker))
            {
                AddRandomSticker();
                playerObj.shopStars -= shop.TakeStickerPrice(playerObj);
            }
            // stops shopping only if it can't afford anything
            else nothingToBuy = true;

            // checking just to not make the look go infinitely
            yield return null;
        }

        playerObj.endStateReady = true;
        AIEndTurn();
    }

    /// <summary>
    /// A random basic sticker on a random hand card that has room for it
    /// </summary>
    void AddRandomSticker()
    {
        List<Card> cardsWithRoom = playerObj.cardsInHand.FindAll(card => card.cardData.CanAddSticker);
        cardsWithRoom[Random.Range(0, cardsWithRoom.Count)].AddSticker(generator.PickRandomSticker("basic"));
    }
    
    // =========================================================
}





