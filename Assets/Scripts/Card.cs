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
    [SerializeField] UnitSprite cardSprite;
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
    float cardRotation = 0f;
    int hierarchyIndex = 0;
    bool hovered = false;

    [Header("Drag")]
    [HideInInspector] public bool isDragged;
    float dragFollowSpeed = 0.15f;

    [Header("Juice")]
    [SerializeField] Image dragShadow;
    [SerializeField] float layoutEaseSpeed = 14f; // how fast cards move to their place in the hand
    [SerializeField] float visualEaseSpeed = 18f; // how fast hover / drag visuals change
    [SerializeField] float dragLiftScale = 1.08f; // extra scale while dragging
    [SerializeField] float maxDragTilt = 15f;
    [SerializeField] float dragTiltPerSpeed = 0.012f; // degrees of tilt per pixel/second of movement
    [SerializeField] Vector2 dragShadowOffset = new Vector2(14f, -18f);
    [SerializeField] float dragShadowAlpha = 0.35f;
    Vector3 handTargetPosition;
    bool inHandLayout = false; // when true the card eases to its place in the hand fan
    float dragTilt = 0f;
    float dragLift = 0f; // 0 = resting, 1 = fully lifted
    Vector3 lastDragPosition;
    Vector3 shadowRestPosition;

    [Header("Gameplay")]
    [HideInInspector] public int currentPower = 0;
    [HideInInspector] public int startingPower = 0;
    [HideInInspector] public int auraPower = 0; // part of currentPower that comes from auras, managed by FieldManager.RefreshAuras
    bool specialCostPaid = false;

    private void Awake()
    {
        if (dragShadow != null)
        {
            shadowRestPosition = dragShadow.rectTransform.localPosition;
            SetShadowAlpha(0f);
        }
    }

    private void Start()
    {
        SetScales();
    }

    void SetScales()
    {
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * 1.2f;
        dragScale = defaultScale * 1.1f;
    }

    private void Update()
    {
        Drag();
        AnimateHandLayout();
        AnimateVisual();
    }

    /// <summary>
    /// Sets where the card should be in the hand, the card then eases there
    /// </summary>
    public void SetHandLayout(Vector3 localPosition, float rotationAngle)
    {
        handTargetPosition = localPosition;
        cardRotation = rotationAngle;
        inHandLayout = true;
    }

    public void ExitHandLayout()
    {
        inHandLayout = false;
    }

    /// <summary>
    /// Eases the card to its place and rotation in the hand fan
    /// </summary>
    void AnimateHandLayout()
    {
        if (!inHandLayout) return;

        float easeT = 1 - Mathf.Exp(-layoutEaseSpeed * Time.deltaTime);

        // straightening the card while it is being read or dragged
        float targetRotation = (hovered || isDragged) ? 0f : cardRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0f, 0f, targetRotation), easeT);

        if (!isDragged) transform.localPosition = Vector3.Lerp(transform.localPosition, handTargetPosition, easeT);
    }

    /// <summary>
    /// Eases hover and drag visuals (scale, raise, tilt, shadow)
    /// </summary>
    void AnimateVisual()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;
        float easeT = 1 - Mathf.Exp(-visualEaseSpeed * deltaTime);
        Transform visual = cardVisual.transform;

        // scale and raise
        bool raised = hovered || isDragged;
        Vector3 targetScale = (isDragged) ? highlightedScale * dragLiftScale : (hovered) ? highlightedScale : defaultScale;
        Vector3 targetPosition = (raised) ? transform.InverseTransformVector(new Vector3(0f, highlightOffsetY, 0f)) : Vector3.zero;
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, easeT);
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPosition, easeT);

        // tilting towards the direction the card is dragged
        float targetTilt = 0f;
        if (isDragged)
        {
            float speedX = (transform.position.x - lastDragPosition.x) / deltaTime;
            targetTilt = Mathf.Clamp(-speedX * dragTiltPerSpeed, -maxDragTilt, maxDragTilt);
        }
        lastDragPosition = transform.position;
        dragTilt = Mathf.Lerp(dragTilt, targetTilt, easeT);
        visual.localRotation = Quaternion.Euler(0f, 0f, dragTilt);

        // shadow under the lifted card
        dragLift = Mathf.Lerp(dragLift, (isDragged) ? 1f : 0f, easeT);
        if (dragShadow != null)
        {
            dragShadow.rectTransform.localPosition = shadowRestPosition + (Vector3)dragShadowOffset * dragLift;
            SetShadowAlpha(dragShadowAlpha * dragLift);
        }
    }

    void SetShadowAlpha(float alpha)
    {
        Color color = dragShadow.color;
        dragShadow.color = new Color(color.r, color.g, color.b, alpha);
        dragShadow.enabled = alpha > 0.005f;
    }

    /// <summary>
    /// Instantly puts hover and drag visuals back to the resting state
    /// </summary>
    void SnapVisual()
    {
        hovered = false;
        dragTilt = 0f;
        dragLift = 0f;
        cardVisual.transform.localScale = defaultScale;
        cardVisual.transform.localPosition = Vector3.zero;
        cardVisual.transform.localRotation = Quaternion.identity;
        if (dragShadow != null)
        {
            dragShadow.rectTransform.localPosition = shadowRestPosition;
            SetShadowAlpha(0f);
        }
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
        cardSprite.RefreshSprite(cardData.unitSprite, player.playerColor, cardData.secondaryColor);

        // BACKGROUND
        //cardBackground.color = GameManager.instance.fieldManager.DesaturateColor(player.playerColor, 0.4f);

        // SPECIAL OR NOT
        specialIcon.SetActive(cardData.ability[0].isSpecial);
    }

    /// <summary>
    /// Removes card from game
    /// </summary>
    public void DestroyCard()
    {
        Debug.Log("Card: Card is getting destroyed.");

        // reporting to player a unit died
        player.deadUnitsThisRound++;

        // Remove card from field cards (card cant really be destroyed when they are in hand)
        player.cardsInDiscard.Remove(this);
        player.cardsOnField.Remove(this);
        player.cardsInHand.Remove(this);

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

            // Putting the card in "reading mode" when hovering over it in hand (scale, raise and rotation get eased in Update)
            hovered = mouseOver;
            // Rendering over other cards
            if (mouseOver) { hierarchyIndex = transform.GetSiblingIndex(); transform.SetAsLastSibling(); }
            else transform.SetSiblingIndex(hierarchyIndex);
            
            // Highlighting the units potentially affected by card's effect
            if (GameManager.instance.currentState == GameState.PLACING)
            {
                if (mouseOver) GameManager.instance.BroadcastOnCardHovered(this);
                else GameManager.instance.fieldManager.StopHighlightUnits();
            }

            // resetting visuals when mouse leaves the card
            if (mouseOver == false) GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);
        }
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
        if (GameManager.instance.executeManager.currentCard != null || GameManager.instance.currentState != GameState.PLACING
            || !GameManager.instance.yourTurn || player.cardPlayed) return;

        isDragged = true;
        lastDragPosition = transform.position;
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

        if (GameManager.instance.currentState == GameState.PLACING && player.cardPlayed == false)
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
    /// Triggers when card enters the play
    /// </summary>
    public void GetStartingPower()
    {
        int powerToGain = abilities[0].abilityData.power;
        startingPower = powerToGain;
        GainPower(powerToGain, false);
    }

    /// <summary>
    /// Adds this much power to current unit power amount
    /// </summary>
    /// <param name="power"></param>
    /// 
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
        // moving card to field cards
        GameManager.instance.handManager.AddCardToField(this, player);

        // resetting visual params
        ExitHandLayout();
        SnapVisual();
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
    /*
    public bool SpecialCost()
    {
        // checking if card requires a star to be played
        if (cardData.ability[0].isSpecial == false || specialCostPaid) return true;

        // checking if player has a star
        if (player.currentStars < 1) return false;

        // taking the star
        player.currentStars--;
        specialCostPaid = true;
        player.playerUI.Refresh();
        return true;
    }*/

    /// <summary>
    /// Triggers at the end of round and resets all temporary card's attributes (like hp or power)
    /// </summary>
    public void CardEndRound()
    {
        // regenerating all health and resetting the power
        currentPower = 0;
        auraPower = 0;
        specialCostPaid = false;

        //unit.RefreshUnitVisuals();
    }

}
