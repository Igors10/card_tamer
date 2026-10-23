using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Unit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    // Card this unit is represents on the board
    [HideInInspector] public Card card;

    [Header("refs")]
    public UnitSprite sprite;
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
    [SerializeField] bool firstSpawnShowcase = true; // zoom in + presenter the first time each unit is played
    public bool FirstSpawnShowcase => firstSpawnShowcase;
    [SerializeField] Image unitColorInd;
    [SerializeField] GameObject targetHighlightObj;

    [Header("power")]
    [SerializeField] GameObject powerUI;
    [SerializeField] Image powerBG;
    public TextMeshProUGUI powerValue;
    [SerializeField] Color buffedPowerColor;
    [SerializeField] Color debuffedPowerColor;
    [SerializeField] Image powerOutline; // shows if the power is buffed or debuffed
    [SerializeField] Material buffedOutlineMaterial;
    [SerializeField] Material debuffedOutlineMaterial;
    [SerializeField] int defaultSizePower = 3; // power at which the counter has its default size
    [SerializeField] float scalePerPower = 0.08f; // how much the counter grows or shrinks per point of power
    [SerializeField] Vector2 powerScaleLimits = new Vector2(0.7f, 1.8f);
    [SerializeField] float powerScaleTime = 0.3f;
    [SerializeField] float powerFlashHold = 0.1f; // how long the buff/debuff color stays before fading back
    [SerializeField] float powerFlashTime = 0.4f;
    Vector3 powerRestScale;
    Color powerBGRestColor;
    float targetPowerScale = -1f;
    Coroutine powerScaleRoutine;
    Coroutine powerFlashRoutine;

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
    bool targeted = false;
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
    [SerializeField] Material defaultMaterial;
    int hierarchyIndex = 0;

    [Header("idle animation")]
    [SerializeField] float shakeTime;
    [SerializeField] float shakeIntensity;
    [SerializeField] float shakeFrequency;

    [Header("ability animation")]
    [SerializeField] float jumpHeight;
    [SerializeField] float jumpTime;
    [SerializeField] float jumpAnticipationTime = 0.08f;
    [SerializeField] float jumpSquash = 0.18f; // squash before jumping and stretch while going up
    [SerializeField] float landSquash = 0.28f;
    bool enteringAnim = true;

    [Header("spawn animation")]
    [SerializeField] float spawnDropHeight = 3f;
    [SerializeField] float spawnDropTime = 0.15f;
    [SerializeField] float spawnSquash = 0.3f;
    [SerializeField] float spawnCameraShake = 0.008f;

    [Header("slide animation")]
    [SerializeField] float slideTime = 0.22f;
    [SerializeField] float slideSquash = 0.15f;
    Coroutine slideRoutine;
    Vector3 slideTarget;

    // sprite resting values, so squash and jump animations always return to the right place
    Vector3 spriteRestPos;
    Vector3 spriteRestScale;
    Coroutine spawnRoutine;
    Coroutine squashRoutine;

    [Header("knockout animation")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravity = 35f;
    [SerializeField] float moveXSpeed = 5f;
    [SerializeField] float rotationSpeed = 360f;

    void Awake()
    {
        spriteRestPos = sprite.transform.localPosition;
        spriteRestScale = sprite.transform.localScale;
        powerRestScale = powerUI.transform.localScale;
        powerBGRestColor = powerBG.color;
    }

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
        RefreshUnitVisuals();
        sprite.RefreshSpriteMaterial(defaultMaterial);

        // Setting field position
        currentField = field;
    }

    /// <summary>
    /// Plays buff or debuff particles on the power counter
    /// </summary>
    public void PlayPowerChangeVFX(int powerChange)
    {
        if (powerChange == 0) return;
        string vfxName = (powerChange > 0) ? "BuffVFX" : "DebuffVFX";
        ParticleManager.instance.SpawnVFX(powerValue.transform.position, vfxName);

        // showing the gained or lost power as a number next to the counter
        powerBonus.StartShowingBonus(powerChange);

        // flashing the counter background green or red
        if (powerFlashRoutine != null) StopCoroutine(powerFlashRoutine);
        powerFlashRoutine = StartCoroutine(FlashPowerBG((powerChange > 0) ? buffedPowerColor : debuffedPowerColor));
    }

    IEnumerator FlashPowerBG(Color flashColor)
    {
        powerBG.color = flashColor;
        yield return new WaitForSeconds(powerFlashHold);

        float t = 0;
        while (t < powerFlashTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / powerFlashTime);
            float coolT = clampedT * clampedT;

            powerBG.color = Color.Lerp(flashColor, powerBGRestColor, coolT);
            yield return null;
        }

        powerBG.color = powerBGRestColor;
        powerFlashRoutine = null;
    }

    /// <summary>
    /// The more power the unit has, the bigger its power counter gets
    /// </summary>
    void RefreshPowerScale()
    {
        float newScale = 1 + (card.currentPower - defaultSizePower) * scalePerPower;
        newScale = Mathf.Clamp(newScale, powerScaleLimits.x, powerScaleLimits.y);
        if (newScale == targetPowerScale) return;

        bool firstRefresh = targetPowerScale < 0;
        targetPowerScale = newScale;

        if (powerScaleRoutine != null) StopCoroutine(powerScaleRoutine);

        // starting power and inactive units snap straight to the right size
        if (firstRefresh || !gameObject.activeInHierarchy)
        {
            powerUI.transform.localScale = powerRestScale * targetPowerScale;
            return;
        }
        powerScaleRoutine = StartCoroutine(ScalePowerUI(powerRestScale * targetPowerScale));
    }

    IEnumerator ScalePowerUI(Vector3 targetScale)
    {
        Vector3 startingScale = powerUI.transform.localScale;

        float t = 0;
        while (t < powerScaleTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / powerScaleTime);
            // ease out with a small overshoot so the size change pops
            float c = 1.7f;
            float coolT = 1 + (c + 1) * Mathf.Pow(clampedT - 1, 3) + c * Mathf.Pow(clampedT - 1, 2);

            powerUI.transform.localScale = Vector3.LerpUnclamped(startingScale, targetScale, coolT);
            yield return null;
        }

        powerUI.transform.localScale = targetScale;
        powerScaleRoutine = null;
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
            powerUI.SetActive(true);
            powerValue.text = card.currentPower.ToString();
            RefreshPowerScale();

            // green outline when buffed, red when debuffed
            powerOutline.enabled = card.currentPower != card.startingPower;
            powerOutline.material = (card.currentPower > card.startingPower) ? buffedOutlineMaterial : debuffedOutlineMaterial;

            // chaning the power indicator colors when buffed or debuffed
            //powerBG.color = (card.currentPower >= card.startingPower) ? buffedPowerColor : debuffedPowerColor;
            //if (card.currentPower == card.startingPower) powerBG.color = Color.white;
        }
        else powerUI.SetActive(false);

        // COLOR
        sprite.RefreshSprite(card.cardData.unitSprite, card.player.playerColor, card.cardData.secondaryColor);
        unitColorInd.color = card.cardData.secondaryColor;

        // FADE
        unitUI.SetActive(!faded);
        float spriteAlpha = (faded) ? fadedAlpha : 1f;
        sprite.SetSpriteAlpha(spriteAlpha);
        stunIndicator.GetComponent<Image>().color = new Color(1f, 1f, 1f, spriteAlpha);
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
        unitHighlight.gameObject.SetActive(isHighlighted);
    }

    public void EnableTarget(bool enable)
    {
        targetHighlightObj.SetActive(enable);
        targeted = enable;
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
        // abilities like Exoskeleton can react to the knock out or prevent it
        List<IEnumerator> knockOutEffects = new List<IEnumerator>();
        bool prevented = GameManager.instance.executeManager.abilityRunner.QueueKnockOutEffects(this, knockOutEffects);
        foreach (IEnumerator effect in knockOutEffects) yield return StartCoroutine(effect);
        if (prevented) yield break;

        stunned = true;

        // knocked out units stop giving aura power
        GameManager.instance.fieldManager.RefreshAuras();

        // do damage animation, VFX and SFX 
        ParticleManager.instance.SpawnVFX(transform.position, "HitVFX");
        AudioManager.instance.PlaySFX("HitSFX");
        yield return StartCoroutine(KnockOutEffect());
    }

    private IEnumerator KnockOutEffect()
    {
        StopSlide();

        Vector3 velocity = new Vector3(
            Random.Range(-moveXSpeed, moveXSpeed), // Randomly flies left or right
            jumpForce,
            jumpForce
        );

        // gravity pulls down and towards the camera
        Vector3 gravityForce = new Vector3(0f, gravity / 2, gravity);
        yield return StartCoroutine(Animations.instance.KnockOutFly(transform, velocity, gravityForce, rotationSpeed, 3f));

        GameManager.instance.handManager.AddCardToDiscarded(card, card.player); // moving the card to discarded

        // removes unit from board when it is off screen
        RemoveFromBoard();
    }


    /// <summary>
    /// Unit hops, shrinks away and its card goes back to the owner's hand
    /// </summary>
    public IEnumerator ReturnToHand()
    {
        StopSlide();
        AudioManager.instance.PlaySFX("UnitMoveSFX");

        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        float returnTime = 0.3f;
        float t = 0;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / returnTime);

            // little hop up, then shrinks to nothing (stretching thin on the way)
            float hop = Mathf.Sin(clampedT * Mathf.PI) * jumpHeight * 0.5f;
            transform.position = startPos + new Vector3(0f, hop, 0f);
            float shrink = 1 - clampedT * clampedT;
            transform.localScale = new Vector3(startScale.x * shrink, startScale.y * Mathf.Lerp(1f, 1.4f, clampedT) * shrink, startScale.z);
            yield return null;
        }

        AudioManager.instance.PlaySFX("CardPlaySFX", 0.6f);

        Field field = currentField;
        GameManager.instance.handManager.ReturnCardToHand(card);

        // unit left in the back slot moves to the front, so the field doesn't count as full
        if (field.units[0] == null && field.units[1] != null) GameManager.instance.fieldManager.MoveUnit(field.units[1], field);

        // returned unit stops giving aura power
        GameManager.instance.fieldManager.Refresh();
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

        // removes unit reference from all units list
        GameManager.instance.fieldManager.unitList.Remove(this);

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

    // ===============
    // JUICE ANIMATIONS
    // ===============

    /// <summary>
    /// Unit drops onto the field and squashes on landing
    /// </summary>
    public void PlaySpawnAnim()
    {
        spawnRoutine = StartCoroutine(SpawnAnim());
    }

    IEnumerator SpawnAnim()
    {
        Transform spriteTransform = sprite.transform;
        Vector3 dropStartPos = spriteRestPos + new Vector3(0f, spawnDropHeight, 0f);

        // falling down
        float t = 0;
        while (t < spawnDropTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / spawnDropTime);
            float coolT = clampedT * clampedT;

            spriteTransform.localPosition = Vector3.Lerp(dropStartPos, spriteRestPos, coolT);
            Animations.instance.SetSquash(spriteTransform, spriteRestScale, -jumpSquash * (1 - coolT * 0.5f)); // stretched while falling
            yield return null;
        }
        spriteTransform.localPosition = spriteRestPos;

        // landing
        GameManager.instance.mainCamera.Shake(spawnCameraShake, 0.15f);
        PlaySquash(spawnSquash, 0.35f);
        spawnRoutine = null;
    }

    void PlaySquash(float amount, float time)
    {
        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(SquashRoutine(amount, time));
    }

    IEnumerator SquashRoutine(float amount, float time)
    {
        yield return Animations.instance.SquashStretch(sprite.transform, spriteRestScale, amount, time);
        squashRoutine = null;
    }

    /// <summary>
    /// Stops spawn and squash animations and puts the sprite back to its resting position and scale
    /// </summary>
    void ResetSpriteAnims()
    {
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }
        if (squashRoutine != null) { StopCoroutine(squashRoutine); squashRoutine = null; }
        sprite.transform.localPosition = spriteRestPos;
        sprite.transform.localScale = spriteRestScale;
    }

    /// <summary>
    /// Smoothly moves the unit to a new position and makes it squash when it arrives
    /// </summary>
    public void SlideTo(Vector3 targetPosition)
    {
        StopSlide();

        // can't animate inactive units, just placing them
        if (!gameObject.activeInHierarchy) { transform.position = targetPosition; return; }

        slideTarget = targetPosition;
        slideRoutine = StartCoroutine(Slide(targetPosition));
    }

    IEnumerator Slide(Vector3 targetPosition)
    {
        Vector3 startingPosition = transform.position;

        float t = 0;
        while (t < slideTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / slideTime);
            float coolT = 1 - Mathf.Pow(1 - clampedT, 3);

            transform.position = Vector3.Lerp(startingPosition, targetPosition, coolT);
            yield return null;
        }

        transform.position = targetPosition;
        slideRoutine = null;
        PlaySquash(slideSquash, 0.3f);
    }

    /// <summary>
    /// Stops sliding and snaps the unit where it was sliding to
    /// </summary>
    void StopSlide()
    {
        if (slideRoutine == null) return;

        StopCoroutine(slideRoutine);
        slideRoutine = null;
        transform.position = slideTarget;
    }

    public IEnumerator EntranceAnimation()
    {
        // disabling idle animation
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = false;

        // making sure spawn animation is not moving the sprite anymore
        ResetSpriteAnims();
        Transform spriteTransform = sprite.transform;

        // making unit appear before other units
        AppearAbove(true);

        // setting starting anim variables
        // position variables
        float t = 0;
        Vector3 startingPosition = spriteRestPos;
        Vector3 targetPosition = startingPosition + new Vector3(0, jumpHeight, 0);

        // ability text variables variables
        skillTextObj.SetActive(true);
        skillText.text = card.cardName;
        skillText.color = new Color(skillText.color.r, skillText.color.g, skillText.color.b, 1f);
        Color skillTextColor = skillText.color;
        Color skillTextTargetColor = new Color(skillTextColor.r, skillTextColor.g, skillTextColor.b, 0f);

        // squashing down before the jump
        while (t < jumpAnticipationTime && !Animations.instance.skipPause)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / jumpAnticipationTime);
            Animations.instance.SetSquash(spriteTransform, spriteRestScale, jumpSquash * clampedT);
            yield return null;
        }

        t = 0;
        // jumping up (stretched at takeoff, back to normal at the top)
        while (t < jumpTime && !Animations.instance.skipPause)
        {
            t += Time.deltaTime;
            float clampedT = t / jumpTime;
            float coolT = clampedT * clampedT;

            sprite.transform.localPosition = Vector3.Lerp(startingPosition, targetPosition, coolT);
            Animations.instance.SetSquash(spriteTransform, spriteRestScale, Mathf.Lerp(-jumpSquash, 0f, Mathf.Clamp01(clampedT)));
            yield return null;
        }
        spriteTransform.localScale = spriteRestScale;

        t = 0f;
        // landing and fading text away
        while (t < jumpTime && !Animations.instance.skipPause)
        {
            t += Time.deltaTime;
            float clampedT = t / jumpTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            sprite.transform.localPosition = Vector3.Lerp(targetPosition, startingPosition, coolT);
            //skillText.color = Color.Lerp(skillTextColor, skillTextTargetColor, coolT);
            yield return null;
        }

        // snapping values to correct ones
        sprite.transform.localPosition = startingPosition;

        // squashing on landing
        PlaySquash(landSquash, 0.35f);

        // pause
        yield return StartCoroutine(Animations.instance.SkippablePause(1.8f));

        // deactivating ability text
        skillTextObj.SetActive(false);
        ResetSpriteAnims();
        // making unit appear in the original place
        AppearAbove(false);

        // enabling idle animation
        sprite.gameObject.GetComponent<CartoonShakeEffect>().enabled = true;
        enteringAnim = false;
    }

    // ===============
    // INPUT
    // ===============

    public void PreviewCard(bool mouseOver)
    {
        // nothing happens when unit is selected during execute state
        if (GameManager.instance.executeManager.currentCard != null && GameManager.instance.executeManager.currentCard.unit == this
            && GameManager.instance.executeManager.readyRevealCard == false || faded || rollingPower || Camera.main.GetComponent<Viewpoint>().zoom
            || targetHighlightObj.activeSelf) return;

        // call for preview card
        ViewCard(mouseOver);

        // click 'juice' effect (not while squashing, the pop would remember the squashed scale)
        if (mouseOver && squashRoutine == null && spawnRoutine == null) Animations.instance.PopAnim(sprite.gameObject, 0.15f, -0.15f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PreviewCard(true);
        TargetOnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TargetOnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TargetOnHover(false);
        PreviewCard(false);
    }

    void TargetOnClick()
    {
        if (!targeted || !GameManager.instance.yourTurn) return;

        GameManager.instance.executeManager.effectTarget = this;
        TargetOnHover(false);
    }

    void TargetOnHover(bool hoverOver)
    {
        if (!targeted || !GameManager.instance.yourTurn) return;

        transform.localScale = (hoverOver) ? hoveredScale : defaultScale;
        HighlightUnit(hoverOver);
        targetHighlightObj.SetActive(!hoverOver);

        //FrameAnim frameAnim = targetHighlightObj.GetComponent<FrameAnim>();
        //frameAnim.PauseAnimation(hoverOver);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!readyToMove) return;
        
        // if the unit was still sliding, it belongs where it was sliding to
        StopSlide();
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
                SlideTo(moveStartingPos);
         }

         PreviewCard(false);
        
    }
}
