using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Rendering;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    
    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("Healthbar")]
    [HideInInspector] public int damageToHP; // how much hp is currently missing
    [SerializeField]Image[] hearts = new Image[10];
    [SerializeField] TextMeshProUGUI healthText;

    [Header("refs")]
    public Ability[] abilities = new Ability[2];
    [SerializeField] RectTransform rt;
    [SerializeField] Image cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject cardVisual;
    public OrderMarker orderMarker;
    [HideInInspector] public Unit unit;

    [Header("highlight")]
    [SerializeField] GameObject glowEffect;
    Vector3 defaultScale = new Vector3();
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
        for (int i = 0; i < abilities.Length; i++) { abilities[i].InitAbility(cardData.ability[i], this); }
    }

    public void AssignCardData(CreatureObj newCardData)
    {
        cardData = newCardData;
        AssignAbilies();
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
        cardSprite.sprite = cardData.cardSprite;

        // HEALTH (number)
        int currentHP = cardData.health - damageToHP;
        if (currentHP < 0) currentHP = 0;

        healthText.text = currentHP.ToString();

        // HEALTH (heart visuals)
        for (int a = hearts.Length - 1; a >= 0; a--)
        {
            // Showing amount of hearts corresponding to max hp value
            hearts[a].gameObject.SetActive(a < cardData.health);

            // Coloring hearts black according to damage taken
            Color fullHeartColor = new Color(1f, 1f, 1f, 1f);
            Color emptyHeartColor = new Color(0f, 0f, 0f, 1f);
            Color heartColor = (cardData.health - damageToHP < a) ? fullHeartColor : emptyHeartColor;
        }
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
        if (GameManager.instance.currentState == GameState.PLACING)
        {
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
            glowEffect.SetActive(mouseOver);

            // resetting visuals when mouse leaves the card
            if (mouseOver == false) GameManager.instance.handManager.UpdateHandVisuals();
        }
        else if (GameManager.instance.currentState == GameState.PLANNING)
        {
            // highlights unit
            // unit.HighlightUnit(mouseOver);
        }

    }
    

    // ====================
    // Drag
    // ====================

    public void StartDrag()
    {
        isDragged = true;
        GameManager.instance.handManager.activeCard = this;

        if (GameManager.instance.currentState == GameState.PLACING) 
            GameManager.instance.fieldManager.EnableSpawnSlots();

        if (GameManager.instance.currentState == GameState.PLANNING)
        {
            // making the card appear above other cards and be bigger while dragged
            transform.localScale = dragScale;
            transform.SetAsLastSibling();
        }
    }

    public void EndDrag()
    {
        isDragged = false;
        OnHover(false);

        if (GameManager.instance.currentState == GameState.PLACING)
        {
            GameManager.instance.fieldManager.PlayCard(this);
            GameManager.instance.handManager.activeCard = null;
        }
        else if (GameManager.instance.currentState == GameState.PLANNING)
        {
            // card size back to default
            transform.localScale = defaultScale;

            // sorting cards based on their position
            GameManager.instance.planningManager.SortCards();
            GameManager.instance.planningManager.UpdateFieldHandVisuals();
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
        foreach (Ability ability in abilities) { ability.Activate(true); }
    }

    /// <summary>
    /// Deselectes both abilities and cancels any field highlighted slots
    /// </summary>
    public void ResetAbilities()
    {
        foreach (Ability ability in abilities) { ability.SelectAbility(false); }

        GameManager.instance.fieldManager.DisableAllSlots();
    }

    // =====================
    // Gameplay
    // =====================

    /// <summary>
    /// Adds this much power to current unit power amount
    /// </summary>
    /// <param name="power"></param>
    public void GainPower(int power)
    {
        Debug.Log("Card: " + cardData.name + "gains " + power + " power.");

        currentPower += power;
        unit.RefreshUnitVisuals();
    }

    /// <summary>
    /// Goes through list of this cards abilities and triggers one flagged as selected
    /// </summary>
    public void UseSelectedAbility()
    {
        foreach (Ability ability in abilities) if (ability.selected) ability.UseAbility();
    }

    /// <summary>
    /// Triggers at the end of round and resets all temporary card's attributes (like hp or power)
    /// </summary>
    public void CardEndRound()
    {
        currentPower = 0;
        damageToHP = 0;

        unit.RefreshUnitVisuals();
    }

}
