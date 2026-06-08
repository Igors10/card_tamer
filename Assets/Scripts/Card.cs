using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    [HideInInspector] public string cardName;
    
    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("refs")]
    public Ability[] abilities = new Ability[1];
    [SerializeField] RectTransform rt;
    [SerializeField] Image cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject cardVisual;
    [SerializeField] Image cardBackground;
    public OrderMarker orderMarker;
    [SerializeField] GameObject specialIcon;
    [HideInInspector] public Unit unit;
    [HideInInspector] public Player player;
    [SerializeField] DeathParticle deathParticle;

    [Header("highlight")]
    [SerializeField] GameObject glowEffect;
    [HideInInspector] public Vector3 defaultScale = new Vector3();
    [HideInInspector] public Vector3 highlightedScale = new Vector3();
    Vector3 dragScale = new Vector3();
    float highlightOffsetY = 220f;
    Vector3 originalHandPosition = new Vector3();
    Vector3 highlightedHandPosition = new Vector3();
    float cardRotation = 0f;
    int hierarchyIndex = 0;

    [Header("Drag")]
    [HideInInspector] public bool isDragged;
    float dragFollowSpeed = 0.15f;

    [Header("Gameplay")]
    [HideInInspector] public int currentPower = 0;
    bool specialCostPaid = false;
    [HideInInspector] public bool shielded = false;

    private void Start()
    {
        SetScales();
    }

    void SetScales()
    {
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * 1.5f;
        dragScale = defaultScale * 1.1f;
    }

    private void Update()
    {
        Drag();
    }

    /// <summary>
    /// Rotates the card clockwise
    /// </summary>
    /// <param name="rotationAngle"></param>
    public void RotateCard(float rotationAngle)
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
        cardRotation = rotationAngle;
    }

    // ===============
    // Initialize
    // ===============

    /// <summary>
    /// Assign abilities from cardData
    /// </summary>
    public void AssignAbilies()
    {
        abilities[0].InitAbility(cardData.ability[0], this); 
    }

    public void AssignCardData(CreatureObj newCardData, Player owner)
    {
        cardData = newCardData;
        AssignAbilies();
        
        player = owner;
        cardName = newCardData.name;

        Refresh();
    }

    /// <summary>
    ///  makes all the card data and visuals match the creature data and current state
    /// </summary>
    public void Refresh()
    {
        // NAME
        nameText.text = cardData.name;

        // SPRITE
        cardSprite.sprite = cardData.unitSprite;
        cardSprite.color = player.playerColor;

        // BACKGROUND
        //cardBackground.color = GameManager.instance.fieldManager.DesaturateColor(player.playerColor, 0.4f);

        // SPECIAL OR NOT
        specialIcon.SetActive(cardData.isSpecial);
    }

    /// <summary>
    /// Removes card from game
    /// </summary>
    public void DestroyCard()
    {
        Debug.Log("Card: Card is getting destroyed.");

        // playing soundeffect
        AudioManager.instance.PlaySFX("UnitDeathSFX");

        // removing the unit
        DeathParticle newParticle = Instantiate(deathParticle, unit.transform.position, unit.transform.rotation).GetComponent<DeathParticle>();
        newParticle.Init(unit.sprite.sprite, unit.sprite.rectTransform, player);
        unit.RemoveFromBoard();

        // reporting to player a unit died
        player.deadUnitsThisRound++;


        // Remove card from field cards (card cant really be destroyed when they are in hand)
        player.cardsOnField.Remove(this);
        // removes unit gameObject
        Destroy(unit.gameObject);

        Destroy(this.gameObject);
    }

    // ====================
    // Hover Over
    // ====================

    /// <summary>
    /// Function triggers whenever player is hovering over a card with their mouse
    /// </summary>
    /// <param name="mouseOver"></param>
    public void OnHover(bool mouseOver)
    {
        if (GameManager.instance.player.cardsInHand.Contains(this))
        {
            if (GameManager.instance.executeManager.currentCard == this || isDragged) return;

            // Putting the card in "reading mode" when hovering over it in hand
            // Scale
            cardVisual.transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
            // Rotation
            transform.localRotation = (mouseOver) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 0f, cardRotation);
            // Position
            if (mouseOver)
            {
                //originalHandPosition = cardVisual.transform.position;
                highlightedHandPosition = cardVisual.transform.position + new Vector3(0f, highlightOffsetY, 0f);
                cardVisual.transform.position = highlightedHandPosition;
            }
            else
            {
                cardVisual.transform.position = transform.position;
            }
            // Rendering over other cards
            if (mouseOver) { hierarchyIndex = transform.GetSiblingIndex(); transform.SetAsLastSibling(); }
            else transform.SetSiblingIndex(hierarchyIndex);
            // Enable glow effect
            //HightlightCard(mouseOver);

            // resetting visuals when mouse leaves the card
            if (mouseOver == false) GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);
        }
        
        /*
        if (GameManager.instance.currentState == GameState.PLANNING)
        {
            transform.localScale = (mouseOver) ? dragScale : defaultScale;
        }*/
    }
    
    public void HighlightCard(bool isHightlighted)
    {
        glowEffect.SetActive(isHightlighted);
    }

    // ====================
    // Drag
    // ====================

    public void StartDrag()
    {
        if (GameManager.instance.executeManager.currentCard != null || GameManager.instance.currentState != GameState.PLACING || !GameManager.instance.yourTurn) return;

        isDragged = true;
        GameManager.instance.handManager.activeCard = this;
        //transform.SetParent(GameManager.instance.handManager.activeCard.transform, false);

        if (GameManager.instance.currentState == GameState.PLACING && GameManager.instance.yourTurn)
            GameManager.instance.fieldManager.EnableSpawnSlots(0);
    }

    public void EndDrag()
    {
        isDragged = false;
        OnHover(false);
        //transform.SetParent(GameManager.instance.handManager.hand.transform, false);

        if (GameManager.instance.currentState == GameState.PLACING)
        {
            GameManager.instance.fieldManager.PlayCard(this, GameManager.instance.player);
            GameManager.instance.handManager.activeCard = null;
        }
    }

    /// <summary>
    /// Drags the card around with mouse cursor
    /// </summary>
    public void Drag()
    {
        if (isDragged == false) return;

        transform.position = Vector2.Lerp(transform.position, Input.mousePosition, dragFollowSpeed);
    }


    // ====================
    // Abilities
    // ====================

    /// <summary>
    /// Makes both abilites be available for choosing
    /// </summary>
    public void ActivateAbilities()
    {
        
    }

    /// <summary>
    /// Deselectes both abilities and cancels any field highlighted slots
    /// </summary>
    public void ResetAbilities()
    {
        GameManager.instance.fieldManager.DisableAllSlots();
    }

    // =====================
    // Gameplay
    // =====================

    /// <summary>
    /// Adds this much power to current unit power amount
    /// </summary>
    /// <param name="power"></param>
    public void GainPower(int power, bool animated = true)
    {
        Debug.Log("Card: " + cardData.name + "gains " + power + " power.");

        // showing added power as bonus
        //if (power != 0) unit.powerBonus.StartShowingBonus(power); 

        // pop animation to signify that something has changed
        if (animated) Animations.instance.PopAnim(unit.powerValue.gameObject, 0.4f, 0.3f);       

        currentPower += power;
        unit.RefreshUnitVisuals();
        GameManager.instance.fieldManager.Refresh();
    }

    public void PlayCard()
    {
        // checking for stars if special
        if (!SpecialCost()) return;

        // moving card to field cards
        GameManager.instance.handManager.AddCardToField(this, player);

        // resetting visual params
        RotateCard(0);
        transform.localScale = defaultScale;

        // disable ready button before an ability is chosen
        GameManager.instance.readyButton.gameObject.SetActive(false);

        // make abilities available for selecting 
        GameManager.instance.executeManager.RevealCard(this);

        // using the ability
        abilities[0].UseAbility();
    }

    /// <summary>
    /// Returns true if player can afford to play the card
    /// </summary>
    /// <returns></returns>
    public bool SpecialCost()
    {
        // checking if card requires a star to be played
        if (cardData.isSpecial == false || specialCostPaid) return true;

        // checking if player has a star
        if (player.currentStars < 1) return false;

        // taking the star
        player.currentStars--;
        specialCostPaid = true;
        player.playerUI.Refresh();
        return true;
    }

    /// <summary>
    /// Triggers at the end of round and resets all temporary card's attributes (like hp or power)
    /// </summary>
    public void CardEndRound()
    {
        // regenerating all health and resetting the power
        currentPower = 0;
        specialCostPaid = false;
        shielded = false;

        unit.RefreshUnitVisuals();
    }

}
