using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

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

    [Header("movement and input")]
    [HideInInspector] public bool readyToMove = false;
    bool isDragged = false;
    float dragFollowSpeed = 0.15f;
    Vector3 hoveredScale;
    Vector3 defaultScale;
    Vector3 moveStartingPos;

    [Header("unit visuals")]
    [HideInInspector] public bool faded = false;
    [SerializeField] float fadedAlpha;
    [SerializeField] GameObject unitUI;

    [Header("animation")]
    [SerializeField] float shakeTime;
    [SerializeField] float shakeIntensity;
    [SerializeField] float shakeFrequency;

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
        healthValue.text = currentHP.ToString();

        // POWER
        if (card.currentPower > 0)
        {
            powerUI.SetActive(true);
            powerValue.text = card.currentPower.ToString();
        }
        else powerUI.SetActive(false);

        // FADE
        unitUI.SetActive(!faded);
        sprite.color = (faded) ? new Color(sprite.color.r, sprite.color.g, sprite.color.b, fadedAlpha) : new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
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
        if (GameManager.instance.currentState == GameState.PLANNING && card.player == GameManager.instance.player)
        {
            card.HighlightCard(isViewed);
        }
        else
        {
            GameManager.instance.managerUI.PreviewCard(isViewed, this);
        }
    }

    public IEnumerator TakeDamage(int damage)
    {
        damage = (card.GetCurrerntHealth() > damage) ? card.GetCurrerntHealth() : damage;
        card.damageToHP += damage;
        card.Refresh();
        RefreshUnitVisuals();

        // do damage VFX and SFX 
        yield return ShakeAnim();

        // do death check
    }

    void Death()
    {
        // destroy unit
    }

    /// <summary>
    /// Making unit shake left and right when taking damage
    /// </summary>
    /// <returns></returns>
    IEnumerator ShakeAnim()
    {
        float t = 0;
        Vector3 startingPos = transform.localPosition;

        while (t < shakeTime)
        {
            t += Time.deltaTime;

            float progress = t / shakeTime;          
            float damper = 1f - progress;         
            damper *= damper;                       

            float offsetX = Mathf.Sin(t * shakeFrequency) * shakeIntensity * damper;
            transform.localPosition = startingPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        transform.localPosition = startingPos;
    }

    // ===============
    // INPUT
    // ===============

    public void OnHover(bool mouseOver)
    {
        // nothing happens when unit is selected during execute state
        if (GameManager.instance.executeManager.currentCard != null && GameManager.instance.executeManager.currentCard.unit == this
            && GameManager.instance.executeManager.readyRevealCard == false) return;


        ViewCard(mouseOver);
        HighlightUnit(mouseOver);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
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

         OnHover(false);
        
    }
}
