using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Unit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{   
    // Card this unit is represents on the board
    [HideInInspector] public Card card;

    [Header("refs")]
    [SerializeField] Image sprite;
    [SerializeField] TextMeshProUGUI healthValue;
    [HideInInspector] public Field currentField;
    public OrderMarker orderMarker;
    [SerializeField] Image unitHighlight;

    [Header("power")]
    [SerializeField] GameObject powerUI;
    [SerializeField] TextMeshProUGUI powerValue;

    [Header("health")]
    [SerializeField] Color healthValueColor = new Color(0.86f, 0.63f, 0.83f, 1f);
    [HideInInspector] public bool stunned = false;
    [SerializeField] GameObject stunIndicator;
    [SerializeField] GameObject healthBar;

    [Header("movement and input")]
    [HideInInspector] public bool readyToMove = false;
    bool mouseOver;
    Vector3 hoveredScale;
    Vector3 defaultScale;
    Vector3 moveStartingPos;

    [Header("unit visuals")]
    [HideInInspector] public bool faded = false;
    [SerializeField] float fadedAlpha;
    [SerializeField] GameObject unitUI;

    [Header("idle animation")]
    [SerializeField] float shakeTime;
    [SerializeField] float shakeIntensity;
    [SerializeField] float shakeFrequency;

    [Header("ability animation")]
    [SerializeField] float jumpHeight;
    [SerializeField] float jumpTime;
    [SerializeField] TextMeshProUGUI skillText;

    void Start()
    {
        // set scales
        defaultScale = transform.localScale;
        hoveredScale = defaultScale * 1.2f;
    }

    public void InitUnit(Card cardToInitialize, Field field)
    {
        // Getting card data
        card = cardToInitialize;
        cardToInitialize.unit = this;
        sprite.sprite = card.cardData.unitSprite;
        RefreshUnitVisuals();

        // Setting field position
        currentField = field;
    }

    public void EnableOrderMarker(bool enable, int orderNumber = 0)
    {
        orderMarker.gameObject.SetActive(enable);

        if (orderNumber != 0) orderMarker.SetNumber(orderNumber);
    }

    public void RefreshUnitVisuals()
    {
        // HEALTH
        int currentHP = card.cardData.health - card.damageToHP;
        healthValue.color = (card.damageToHP > 0) ? Color.red : healthValueColor; // number gets red if damaged
        healthValue.text = currentHP.ToString() + "/" + card.cardData.health.ToString();

        // POWER
        if (card.currentPower > 0)
        {
            powerUI.SetActive(true);
            powerValue.text = card.currentPower.ToString();
        }
        else powerUI.SetActive(false);

        // FADE
        unitUI.SetActive(!faded);
        Color spriteColor = (faded) ? new Color(sprite.color.r, sprite.color.g, sprite.color.b, fadedAlpha) : new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
        sprite.color = spriteColor;
        stunIndicator.GetComponent<Image>().color = spriteColor;
        // cancelling card preview if it was on right before fading
        if (faded) 
        {
            ViewCard(false);
            HighlightUnit(false);
        }

        // STUN
        if (stunIndicator != null) stunIndicator.SetActive(stunned);
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = !stunned;
        healthBar.SetActive(stunned);
    }

    /// <summary>
    /// Visually highlightes the unit
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void HighlightUnit(bool isHighlighted)
    {
        unitHighlight.gameObject.SetActive(isHighlighted);
    }

    /// <summary>
    /// Makes card appear above the unit, so that player can read it
    /// </summary>
    void ViewCard(bool isViewed)
    {
        GameManager.instance.managerUI.PreviewCard(isViewed, card.cardData, card.player, transform.position);        
    }

    public IEnumerator TakeDamage(int damage)
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("HitSFX");

        damage = (card.GetCurrerntHealth() > damage) ? damage : card.GetCurrerntHealth();
        card.damageToHP += damage;
        card.Refresh();
        stunned = true;
        RefreshUnitVisuals();

        // do death check
        bool isDead = card.GetCurrerntHealth() == 0;
        Debug.Log("Unit: " + card.cardData.name + " is dead (" + isDead + ")");

        // do damage VFX and SFX 
        yield return ShakeAnim(isDead);

        // give random food token if dead
        if (isDead)
        {
            card.player.playerUI.AddRandomFoodToken(true);
            yield return new WaitForSeconds(1.5f);
        }
    }

    /// <summary>
    /// Removes the unit off the board
    /// </summary>
    public void RemoveFromBoard()
    {
        // removes unit reference from field
        int unitSlot = (currentField.units[0] == this) ? 0 : 1;
        currentField.units[unitSlot] = null;
        Debug.Log("Removed unit gameObject from board");
    }

    /// <summary>
    /// Making unit shake left and right when taking damage
    /// </summary>
    /// <returns></returns>
    IEnumerator ShakeAnim(bool isDead)
    {
        float t = 0;
        Vector3 startingPos = transform.localPosition;

        // colors for fading out the unit if was killed
        Color spriteColor = sprite.color;
        Color fadedColor = new Color(spriteColor.r, spriteColor.g, spriteColor.b, 0f);

        while (t < shakeTime)
        {
            t += Time.deltaTime;

            float progress = t / shakeTime;          
            float damper = 1f - progress;         
            damper *= damper;                       

            float offsetX = Mathf.Sin(t * shakeFrequency) * shakeIntensity * damper;
            transform.localPosition = startingPos + new Vector3(offsetX, 0f, 0f);

            // Fade out unit if the damage was deadly
            if (isDead) sprite.color = Color.Lerp(spriteColor, fadedColor, progress);

            yield return null;
        }

        transform.localPosition = startingPos;
    }

    public IEnumerator AbilityAnimation(Ability ability)
    {
        // disabling idle animation
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = false;

        // setting starting anim variables
        // position variables
        float t = 0;
        Vector3 startingPosition = sprite.transform.localPosition;
        Vector3 targetPosition = startingPosition + new Vector3(0, jumpHeight, 0);

        // ability text variables variables
        skillText.gameObject.SetActive(true);
        skillText.text = ability.abilityData.name;
        skillText.color = new Color(skillText.color.r, skillText.color.g, skillText.color.b, 1f);
        Color skillTextColor = skillText.color;
        Color skillTextTargetColor = new Color(skillTextColor.r, skillTextColor.g, skillTextColor.b, 0f);

        // jumping up
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            float clampedT = t / jumpTime;
            float coolT = clampedT * clampedT;

            sprite.transform.localPosition = Vector3.Lerp(startingPosition, targetPosition, coolT);
            yield return null;
        }

        t = 0f;
        // landing and fading text away
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            float clampedT = t / jumpTime;
            float coolT = 1 - (1 -clampedT) * (1 - clampedT);

            sprite.transform.localPosition = Vector3.Lerp(targetPosition, startingPosition, coolT);
            //skillText.color = Color.Lerp(skillTextColor, skillTextTargetColor, coolT);
            yield return null;
        }

        // snapping values to correct ones
        sprite.transform.localPosition = startingPosition;

        yield return new WaitForSeconds(jumpTime * 2);
        // deactivating ability text
        skillText.gameObject.SetActive(false);

        // enabling idle animation
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = true;
    }

    // ===============
    // INPUT
    // ===============

    public void PreviewCard(bool mouseOver)
    {
        // nothing happens when unit is selected during execute state
        if (GameManager.instance.executeManager.currentCard != null && GameManager.instance.executeManager.currentCard.unit == this
            && GameManager.instance.executeManager.readyRevealCard == false || faded) return;


        ViewCard(mouseOver);
        HighlightUnit(mouseOver);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PreviewCard(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PreviewCard(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!readyToMove) return;
        
        moveStartingPos = transform.position;
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!readyToMove) return;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            transform.position = hit.point;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!readyToMove) return;
        
         // Checking if unit ended drag around an empty active slot 
         if (GameManager.instance.fieldManager.CheckUnitMove(this) == false)
         { 
                // if there was no slot put unit back
                transform.position = moveStartingPos;
         }

         PreviewCard(false);
        
    }
}
