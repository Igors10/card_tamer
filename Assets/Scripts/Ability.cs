using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Ability : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("AbilityData")]
    public AbilityObj abilityData;

    [Header("Attributes")]
    bool ready;
    bool active;
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
    Vector3 defaultScale;
    Vector3 highlightedScale;
    Vector3 pressedScale;

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
    public void InitAbility(AbilityObj ability)
    {
        abilityData = ability;

        // NAME
        name.text = abilityData.name;

        // BACKGROUND
        background.enabled = !abilityData.isPassive; // passive abilities can be identified by not having a colored background

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

    // ====================
    // Input
    // ====================

    /// <summary>
    /// Makes ability be ready to be clicked on and used
    /// </summary>
    /// <param name="isActive"></param>
    public void Activate(bool isActive)
    {
        if (active == false) return; // cannot activate a passive ability

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
        if (!ready) return;
        transform.localScale = (mouseOver) ? highlightedScale : defaultScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ready) return;
        transform.localScale = pressedScale;
        
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!ready) return;
        transform.localScale = defaultScale;

        glowOutline.gameObject.SetActive(true);
        glowOutline.effectColor = glowChosenColor;
        glowOutline.effectDistance = new Vector2(4, -4); // temp

    }
}
