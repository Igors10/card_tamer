using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    
    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("Healthbar")]
    int damageToHP; // how much hp is currently missing
    [SerializeField]Image[] hearts = new Image[10];
    [SerializeField] TextMeshProUGUI healthText;

    [Header("refs")]
    public Ability[] abilities = new Ability[2];
    [SerializeField] RectTransform rt;
    [SerializeField] Image cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject cardVisual;

    [Header("highlight")]
    [SerializeField] GameObject glowEffect;
    Vector3 defaultScale = new Vector3();
    Vector3 highlightedScale = new Vector3();
    float highlightOffsetY = 220f;
    Vector3 originalHandPosition = new Vector3();
    Vector3 highlightedHandPosition = new Vector3();
    float cardRotation = 0f;
    int hierarchyIndex = 0;

    [Header("Drag")]
    [HideInInspector] public bool isDragged;
    float dragFollowSpeed = 0.15f;

    private void Start()
    {
        SetScales();
    }

    void SetScales()
    {
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * 1.5f;
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
        for (int i = 0; i < abilities.Length; i++) { abilities[i].InitAbility(cardData.ability[i]); }
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
        cardSprite.sprite = cardData.creatureSprite;

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
        // Putting the card in "reading mode" when hovering over it in hand
        // Scale
        transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
        // Rotation
        transform.localRotation = (mouseOver) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 0f, cardRotation);
        // Position
        if (mouseOver) 
        {
            originalHandPosition = cardVisual.transform.position;
            highlightedHandPosition = originalHandPosition + new Vector3(0f, highlightOffsetY, 0f);
            cardVisual.transform.position = highlightedHandPosition;
        }
        else
        {
            cardVisual.transform.position = originalHandPosition;
        }
        // Rendering over other cards
        if (mouseOver) { hierarchyIndex = transform.GetSiblingIndex(); transform.SetAsLastSibling(); }
        else transform.SetSiblingIndex(hierarchyIndex);
        // Enable glow effect
        glowEffect.SetActive(mouseOver);

        // resetting visuals when mouse leaves the card
        if (mouseOver == false) GameManager.instance.handManager.UpdateHandVisuals();
    }
    

    // ====================
    // Drag
    // ====================

    public void StartDrag()
    {
        isDragged = true;
        GameManager.instance.handManager.activeCard = this;
        GameManager.instance.fieldManager.EnableSpawnPoints(true);
    }

    public void EndDrag()
    {
        isDragged = false;
        OnHover(false);
        GameManager.instance.fieldManager.PlayCard();
        GameManager.instance.handManager.activeCard = null;
    }

    /// <summary>
    /// Drags the card around with mouse cursor
    /// </summary>
    public void Drag()
    {
        if (isDragged == false) return;

        transform.position = Vector2.Lerp(transform.position, Input.mousePosition, dragFollowSpeed);
    }
}
