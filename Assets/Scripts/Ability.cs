using Mono.Cecil;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Ability : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("AbilityData")]
    public AbilityObj abilityData;
    [HideInInspector] public Card card;

    [Header("Attributes")]
    bool ready;
    bool passive;
    [SerializeField] TextMeshProUGUI name;
    [SerializeField] GameObject speedIcon;
    [SerializeField] GameObject powerIcon;
    [SerializeField] TextMeshProUGUI speedValue;
    [SerializeField] TextMeshProUGUI powerValue;
    [SerializeField] TextMeshProUGUI effectDesc;

    [Header("refs")]
    [SerializeField] Image background;

    [Header("Effect text params")]
    [SerializeField] float defaultEffectX;
    [SerializeField] float defaultEffectWidth;
    [SerializeField] float noPowerEffectX;
    [SerializeField] float noPowerEffectWidth;
    [SerializeField] float passiveEffectX;
    [SerializeField] float passiveEffectWidth;

    [Header("Input juice params")]
    [SerializeField] Outline glowOutline;
    [SerializeField] Color glowReadyColor;
    [SerializeField] Color glowChosenColor;
    Vector2 glowDefaultSize = new Vector2(3, -3);
    Vector2 glowChosenSize = new Vector2(4, -4);
    Vector3 defaultScale;
    Vector3 highlightedScale;
    Vector3 pressedScale;
    [HideInInspector] public bool selected = false;
    // saved unit position
    Field savedField;
    int savedSlot;

    void Start()
    {
        // setting scales 
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * 1.2f;
        pressedScale = defaultScale * 0.8f;
    }

    // ====================
    // Initialization
    // ====================

    /// <summary>
    /// Applying data from ability scriptable object to this ability instance
    /// </summary>
    public void InitAbility(AbilityObj ability, Card cardAssignTo)
    {
        abilityData = ability;

        // Getting reference to the card
        card = cardAssignTo;

        // NAME
        name.text = abilityData.name;

        // PASSIVE OR ACTIVE
        background.enabled = !abilityData.isPassive; // passive abilities can be identified by not having a colored background
        passive = abilityData.isPassive;

        // SPEED
        speedIcon.SetActive(abilityData.isPassive == false);
        speedValue.text = abilityData.speed.ToString();

        // POWER
        powerIcon.SetActive(abilityData.power > 0 && abilityData.isPassive == false);
        powerValue.text = abilityData.power.ToString();

        // EFFECT DESCRIPTION
        effectDesc.text = abilityData.abilityDescription;

        // EFFECT FORMATING
        // text alignment
        effectDesc.alignment = (abilityData.isPassive) ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;

        // textbox size and position
        float effectX = defaultEffectX;
        float effectWidth = defaultEffectWidth;
        // * (passive ability)
        if (abilityData.isPassive)
        {
            effectX = passiveEffectX;
            effectWidth = passiveEffectWidth;
        }
        // * (no power ability)
        else if (abilityData.power <= 0)
        {
            effectX = noPowerEffectX;
            effectWidth = noPowerEffectWidth;
        }
  
        // applying formating (making it take as much space as possible if some other elements are disabled)
        effectDesc.transform.localPosition = new Vector3(effectX, effectDesc.transform.localPosition.y, 0f);
        effectDesc.rectTransform.sizeDelta = new Vector2(effectWidth, effectDesc.rectTransform.sizeDelta.y);

    }

    // =====================
    // Effect
    // =====================


    /// <summary>
    /// Applies the effect of the ability to the unit(s)
    /// </summary>
    public void UseAbility()
    {
        Debug.Log("Ability: " + card.name + " uses " + abilityData.name);

        // Gaining power if any
        if (abilityData.power > 0)
            card.GainPower(abilityData.power);

        // Gaining block if any
        if (abilityData.block > 0) 
            card.unit.currentField.GainBlock(abilityData.block);

        // Effect
        // find a way to have different effects here
    }

    // ====================
    // Input
    // ====================

    /// <summary>
    /// Makes ability be ready to be clicked on and used
    /// </summary>
    /// <param name="isActive"></param>
    public void Activate(bool isActive)
    {
        if (passive) return; // cannot activate a passive ability

        ready = isActive;
        glowOutline.enabled = isActive;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool mouseOver)
    {
        if (!ready || selected) return;
        transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ready || selected) return;
        transform.localScale = pressedScale;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!ready || selected) return; 
        SelectAbility(true);
    }

    public void SelectAbility(bool isSelected)
    {
        // resetting units position
        if (savedField != null) GameManager.instance.fieldManager.MoveUnit(card.unit, savedField, savedSlot, false);

        // resets the abilities in case another ability was selected
        if (isSelected) card.ResetAbilities();

        // marking as selected
        selected = isSelected;

        // applying selected effects
        glowOutline.enabled = isSelected;
        glowOutline.effectColor = (isSelected) ? glowChosenColor : glowReadyColor;
        glowOutline.effectDistance = (isSelected) ? glowChosenSize : glowDefaultSize;
        transform.localScale = (isSelected) ? highlightedScale : defaultScale;

        // Highlighting fields for movement
        if (isSelected) GameManager.instance.fieldManager.EnableMoveSlots(card.unit.currentField, abilityData.speed);

        // Making the unit ready to move
        card.unit.readyToMove = isSelected;

        // Saving units starting position
        savedField = card.unit.currentField;
        savedSlot = GameManager.instance.fieldManager.GetUnitSlot(card.unit);

        // Enables 'use' button
        string buttonText = "Use " + name.text;
        GameManager.instance.readyButton.gameObject.SetActive(isSelected);
        GameManager.instance.readyButton.UpdateButtonState(buttonText);

        // Telling the player to click "use ability"
        GameManager.instance.NewHint("Move the creature (or keep the position as it is) and click 'use' when ready");
    }
}
