using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Unit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{   
    // Card this unit is represents on the board
    [HideInInspector] public Card card;

    [Header("refs")]
    public Image sprite;
    [SerializeField] TextMeshProUGUI healthValue;
    [HideInInspector] public Field currentField;
    public OrderMarker orderMarker;
    [SerializeField] Image unitHighlight;
    public PowerBonus powerBonus;
    public ParticleSystem cardEffectVFX;
    [HideInInspector] public int unitSlot;
    [SerializeField] GameObject shieldVFX;
    public TextMeshProUGUI skillText;
    public GameObject skillTextObj;
    public AutoFade unitPresenter;

    [Header("power")]
    [SerializeField] GameObject powerUI;
    public TextMeshProUGUI powerValue;

    [Header("power rolling")]
    public D6[] dice;
    [SerializeField] float diceDistance;
    [SerializeField] float dieOffsetY;
    [SerializeField] float afterDiePause;
    [SerializeField] GameObject bonusPowerObj;
    [SerializeField] TextMeshProUGUI bonusPowerValue;

    [Header("state")]
    [HideInInspector] public bool stunned = false;
    bool rollingPower = false;
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
    [SerializeField] Material selectMaterial;
    Material defaultMaterial;
    int hierarchyIndex = 0;

    [Header("idle animation")]
    [SerializeField] float shakeTime;
    [SerializeField] float shakeIntensity;
    [SerializeField] float shakeFrequency;

    [Header("ability animation")]
    [SerializeField] float jumpHeight;
    [SerializeField] float jumpTime;

    [Header("knockout animation")]
    [SerializeField] float jumpForce = 10f;       
    [SerializeField] float gravity = 35f;         
    [SerializeField] float moveXSpeed = 5f;       
    [SerializeField] float rotationSpeed = 360f;  

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
        defaultMaterial = sprite.material;

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
        // POWER
        if (card.currentPower > 0 && stunned == false)
        {
            powerValue.text = card.currentPower.ToString();
        }
        else powerUI.SetActive(false);

        // COLOR
        sprite.color = card.player.playerColor;

        // FADE
        unitUI.SetActive(!faded);
        Color spriteColor = (faded) ? new Color(sprite.color.r, sprite.color.g, sprite.color.b, fadedAlpha) : new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
        sprite.color = spriteColor;
        stunIndicator.GetComponent<Image>().color = new Color(1f, 1f, 1f, spriteColor.a);
        // cancelling card preview if it was on right before fading
        if (faded) 
        {
            ViewCard(false);
            HighlightUnit(false);
        }

        // STUN
        if (stunIndicator != null) stunIndicator.SetActive(stunned);
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = !stunned;
    }

    /// <summary>
    /// Visually highlightes the unit
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void HighlightUnit(bool isHighlighted)
    {
        //sprite.material = (isHighlighted) ? selectMaterial : defaultMaterial;
    }

    public void AppearAbove(bool isAppearingAbove)
    {
        if (isAppearingAbove)
        {
            // Rendering unit over other units 
            hierarchyIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
        }
        else transform.SetSiblingIndex(hierarchyIndex); // back to original rendering layer
    }

    /// <summary>
    /// Makes card appear above the unit, so that player can read it
    /// </summary>
    void ViewCard(bool isViewed)
    {
        GameManager.instance.managerUI.PreviewCard(isViewed, card.cardData, card.player, transform.position);        
    }

    void Update()
    {
        // temp debug to test unit knockout
        if (Input.GetKeyDown(KeyCode.K)) StartCoroutine(KnockOut());
    }

    public IEnumerator KnockOut()
    {
        // checking if shielded
        if (card.shielded) { ShieldedEffect(); yield break; }
        stunned = true;

        // do damage animation, VFX and SFX 
        ParticleManager.instance.SpawnVFX(transform.position, "HitVFX");
        AudioManager.instance.PlaySFX("HitSFX");
        yield return StartCoroutine(KnockOutEffect());
    }

    void ShieldedEffect()
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("HitSFX"); // put different soundeffect

        // activate shield sprite

    }

    private IEnumerator KnockOutEffect()
    {
        Vector3 velocity = new Vector3(
            Random.Range(-moveXSpeed, moveXSpeed), // Randomly flies left or right
            jumpForce,
            jumpForce
        );

        float t = 0f;
        while (t < 3f)
        {
            // Apply gravity to our custom vertical velocity
            velocity.y -= gravity / 2 * Time.deltaTime;
            velocity.z -= gravity * Time.deltaTime;

            // Move the world canvas element based on the velocity
            transform.position += velocity * Time.deltaTime;

            // Spin the card on the Z-axis for that classic look
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

            t += Time.deltaTime;
            yield return null; 
        }

        GameManager.instance.handManager.AddCardToDiscarded(card, card.player); // moving the card to discarded

        // removes unit from board when it is off screen
        RemoveFromBoard();
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
        //GameManager.instance.fieldManager.Refresh();

        // remove the gameObject
        if (this.gameObject != null) Destroy(this.gameObject);
    }

    public IEnumerator RollPower()
    {
        int numberOfDice = (card.cardData.ability[0].isSpecial) ? 2 : 1;
        rollingPower = true;

        powerUI.SetActive(true);

        // rolling dice
        for (int i = 0; i < numberOfDice; i++)
        {
            D6 die = dice[i];

            // enabling die
            die.gameObject.SetActive(true);

            float dieOffsetX = 3 * (i - (numberOfDice - 1) / 2f);
            Vector2 diePos = new Vector2(sprite.transform.localPosition.x + dieOffsetX, sprite.transform.localPosition.y + dieOffsetY);
            die.transform.localPosition = diePos;

            // rolling the die
            if (i + 1 == numberOfDice) yield return StartCoroutine(die.RollAnimation());
            else StartCoroutine(die.RollAnimation()); yield return new WaitForSeconds(0.5f);

            // showing the result
            die.Glow(true);
        }

        // waiting until all roll effects are resolved
        yield return StartCoroutine(GameManager.instance.executeManager.TriggerEffects("onRoll"));

        // disable dice
        DisableRollingUI();

        //yield return new WaitForSeconds(0.1f);
        rollingPower = false;
    }

    public void DisableRollingUI()
    {
        // hide dice
        foreach (D6 die in dice)
        {
            die.DisableDie();
        }

        // hide power message
        bonusPowerObj.SetActive(false);
    }

    public IEnumerator EntranceAnimation(Ability ability)
    {
        // disabling idle animation
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = false;

        // making unit appear before other units
        AppearAbove(true);

        // setting starting anim variables
        // position variables
        float t = 0;
        Vector3 startingPosition = sprite.transform.localPosition;
        Vector3 targetPosition = startingPosition + new Vector3(0, jumpHeight, 0);

        // ability text variables variables
        skillTextObj.SetActive(true);
        skillText.text = card.cardName;
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

        // pause
        yield return new WaitForSeconds(1.8f);

        // deactivating ability text
        skillTextObj.SetActive(false);
        // making unit appear in the original place
        AppearAbove(false);

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
            && GameManager.instance.executeManager.readyRevealCard == false || faded || rollingPower) return;

        // call for preview card
        ViewCard(mouseOver);

        // click 'juice'
        if (mouseOver) Animations.instance.PopAnim(sprite.gameObject, 0.15f, -0.15f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PreviewCard(true);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        HighlightUnit(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HighlightUnit(false);
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
