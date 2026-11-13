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
    [SerializeField] GameObject effectStickerObj; // the unit's own sticker pops up under it when it triggers (fades in with AutoFade)
    [SerializeField] Ability effectSticker;
    [SerializeField] float effectStickerFadeOut = 0.3f;
    public AutoFade unitPresenter;
    [SerializeField] bool firstSpawnShowcase = true; // zoom in + presenter the first time each unit is played
    public bool FirstSpawnShowcase => firstSpawnShowcase;
    [SerializeField] Image unitColorInd;
    [SerializeField] GameObject targetHighlightObj;

    [Header("power")]
    [SerializeField] GameObject powerUI;
    [SerializeField] Image powerBG;
    [SerializeField] Color specialPowerColor = new Color(0.78f, 0.55f, 0.96f);
    public TextMeshProUGUI powerValue;
    [SerializeField] Image powerOutline; // shows if the power is buffed or debuffed
    [SerializeField] Material buffedOutlineMaterial; // outline color comes from Colors (buff / debuff color)
    [SerializeField] Material debuffedOutlineMaterial;
    static Material buffOutline, debuffOutline; // copies of the outline materials in the Colors colors, shared by all units
    [SerializeField] int defaultSizePower = 3; // power at which the counter has its default size
    [SerializeField] float scalePerPower = 0.08f; // how much the counter grows or shrinks per point of power
    [SerializeField] Vector2 powerScaleLimits = new Vector2(0.7f, 1.8f);
    [SerializeField] float powerScaleTime = 0.3f;
    [SerializeField] float powerFlashHold = 0.1f; // how long the buff/debuff color stays before fading back
    [SerializeField] float powerFlashTime = 0.4f;
    [SerializeField] Vector2 cardCornerOffset = new Vector2(120f, 170f); // counter on the top corner of the shown card (right for yours, left for the opponent's)
    [SerializeField] float cardCounterScale = 70f; // counter size on the card (the card is in pixels, the unit's canvas in world units)
    bool counterOnCard;

    [Header("fight result on the power counter (right before the clash)")]
    [SerializeField] float resultTime = 0.45f;
    [SerializeField] float winnerCounterScale = 1.35f; // stronger card's counter grows this much
    [SerializeField] float loserCounterScale = 0.75f; // weaker card's counter shrinks to this
    [SerializeField] float loserDarken = 0.45f; // and gets this much darker
    [SerializeField] float loserShake = 10f;    Vector3 powerRestScale;
    Vector3 powerBonusRestScale;
    Color powerBGRestColor;
    float targetPowerScale = -1f;
    Coroutine powerScaleRoutine;
    Coroutine powerFlashRoutine;

    [Header("power rolling")]
    public D6[] dice;
    [SerializeField] float diceDistance;
    [SerializeField] float dieOffsetY;
    [SerializeField] float afterDiePause;
    [SerializeField] Vector3 powerUpDiceStart = new Vector3(3.6f, 1f, 0f); // first power-up die, right of the unit
    [SerializeField] int powerUpDicePerRow = 3;
    [SerializeField] float powerUpDiceSpacing = 1.8f;
    [SerializeField] float diceStopInterval = 0.25f; // after the click the dice stop one by one with this pause
    [SerializeField] GameObject bonusPowerObj;
    [SerializeField] TextMeshProUGUI bonusPowerValue;

    [Header("state")]
    [HideInInspector] public bool stunned = false;
    [HideInInspector] public bool knockedOut = false; // flying off the board, waiting to be removed
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

    [Header("special spawn animation")]
    [SerializeField] float specialDropHeightMod = 1.5f; // special units fall from a bit higher and slower, with a little twist on the way down
    [SerializeField] float specialDropTimeMod = 1.5f;
    [SerializeField] float specialSpins = 0.15f;
    [SerializeField] float specialSquashMod = 1.4f;
    [SerializeField] float specialCameraShakeMod = 4f;
    [SerializeField] float specialCameraShakeTime = 0.3f;

    [Header("slide animation")]
    [SerializeField] float slideTime = 0.22f;
    [SerializeField] float slideSquash = 0.15f;
    Coroutine slideRoutine;
    Vector3 slideTarget;

    // sprite resting values, so squash and jump animations always return to the right place
    Vector3 spriteRestPos;
    Vector3 spriteRestScale;
    Quaternion spriteRestRotation;
    Coroutine spawnRoutine;
    Coroutine squashRoutine;

    [Header("knockout animation")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float gravity = 35f;
    [SerializeField] float moveXSpeed = 5f;
    [SerializeField] float rotationSpeed = 360f;

    public bool IsSpecial => card != null && card.cardData.IsSpecial;
    public bool IsSpawning => spawnRoutine != null; // still dropping onto the field

    void Awake()
    {
        spriteRestPos = sprite.transform.localPosition;
        spriteRestScale = sprite.transform.localScale;
        spriteRestRotation = sprite.transform.localRotation;
        powerRestScale = powerUI.transform.localScale;
        powerBonusRestScale = powerBonus.transform.localScale;
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

        // power counter in the owner's color (buff/debuff flashes fade back to it)
        powerBGRestColor = card.player.playerColor;
        powerBG.color = powerBGRestColor;

        // the opponent's counter sits on the right of the unit (yours on the left, both on the outer side)
        if (card.player != GameManager.instance.player)
        {
            foreach (Transform counterPart in new Transform[] { powerUI.transform, powerBonus.transform })
            {
                Vector3 pos = counterPart.localPosition;
                counterPart.localPosition = new Vector3(Mathf.Abs(pos.x), pos.y, pos.z);
            }
        }

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
        ParticleManager.instance.SpawnVFX(powerValue.transform.position, vfxName, counterOnCard);

        // showing the gained or lost power as a number next to the counter
        powerBonus.StartShowingBonus(powerChange);

        // flashing the counter background green or red
        if (powerFlashRoutine != null) StopCoroutine(powerFlashRoutine);
        powerFlashRoutine = StartCoroutine(FlashPowerBG((powerChange > 0) ? Colors.instance.buffColor : Colors.instance.debuffColor));
    }

    /// <summary>
    /// Outline material for a buffed or debuffed counter, in the buff / debuff color from Colors
    /// </summary>
    Material OutlineMaterial(bool buffed)
    {
        if (buffOutline == null)
        {
            buffOutline = new Material(buffedOutlineMaterial);
            buffOutline.SetColor("_SolidOutline", Colors.instance.buffColor);
        }
        if (debuffOutline == null)
        {
            debuffOutline = new Material(debuffedOutlineMaterial);
            debuffOutline.SetColor("_SolidOutline", Colors.instance.debuffColor);
        }
        return buffed ? buffOutline : debuffOutline;
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
        // POWER (cards start a fight with 0, the counter shows it anyway)
        if (card.currentPower >= 0 && stunned == false)
        {
            powerUI.SetActive(true);
            powerValue.text = card.currentPower.ToString();
            RefreshPowerScale();

            // buff colored outline when buffed, debuff colored when debuffed
            powerOutline.enabled = card.currentPower != card.startingPower;
            powerOutline.material = OutlineMaterial(card.currentPower > card.startingPower);

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
        if (GameManager.instance.DebugKeysEnabled && Input.GetKeyDown(KeyCode.K)) StartCoroutine(KnockOut());
    }

    public IEnumerator KnockOut()
    {
        // abilities like Exoskeleton can react to the knock out or prevent it
        List<IEnumerator> knockOutEffects = new List<IEnumerator>();
        bool prevented = GameManager.instance.executeManager.abilityRunner.QueueKnockOutEffects(this, knockOutEffects);
        foreach (IEnumerator effect in knockOutEffects) yield return StartCoroutine(effect);
        if (prevented) yield break;

        stunned = true;
        knockedOut = true;

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
        Field field = currentField;
        RemoveFromBoard();
        if (field != null) GameManager.instance.fieldManager.MoveBackUnitForward(field);
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
        GameManager.instance.fieldManager.MoveBackUnitForward(field);

        // returned unit stops giving aura power
        GameManager.instance.fieldManager.Refresh();
    }

    /// <summary>
    /// Removes the unit off the board
    /// </summary>
    /// <summary>
    /// The power counter stops following the unit and stays where it is now (units fly around during the clash).
    /// It has to stay inside the unit's canvas, so it's held in place every frame instead of moved out
    /// </summary>
    /// <summary>
    /// The power counter and the gained power numbers move onto the top corner of the shown card
    /// </summary>
    public void MovePowerCounterToCard(Card shownCard)
    {
        bool mirrored = card.player != GameManager.instance.player;
        Vector3 cornerPos = new Vector3(mirrored ? -cardCornerOffset.x : cardCornerOffset.x, cardCornerOffset.y, 0f);
        float scaleFactor = cardCounterScale / powerRestScale.x;

        foreach (Transform part in new Transform[] { powerUI.transform, powerBonus.transform })
        {
            part.SetParent(shownCard.Visual, false);
            part.localPosition = cornerPos;
            part.localRotation = Quaternion.identity;
            part.SetAsLastSibling();

            // on the card they're drawn in the card's order (above the card), not above everything
            Canvas sortingCanvas = part.GetComponent<Canvas>();
            if (sortingCanvas != null) sortingCanvas.overrideSorting = false;
        }

        powerRestScale *= scaleFactor;
        powerBonusRestScale *= scaleFactor;
        powerUI.transform.localScale = powerRestScale * ((targetPowerScale > 0) ? targetPowerScale : 1f);
        powerBonus.transform.localScale = powerBonusRestScale;
        counterOnCard = true;
    }

    /// <summary>
    /// Right before the clash the counters show who is stronger: the winner's pops bigger with a green outline and particles,
    /// the loser's shrinks, darkens and shakes
    /// </summary>
    public IEnumerator ShowFightResult(bool winning)
    {
        // nothing else changes the counter from here
        if (powerScaleRoutine != null) StopCoroutine(powerScaleRoutine);
        if (powerFlashRoutine != null) StopCoroutine(powerFlashRoutine);
        powerScaleRoutine = null;
        powerFlashRoutine = null;

        Transform counter = powerUI.transform;
        Vector3 startScale = counter.localScale;
        Vector3 targetScale = startScale * (winning ? winnerCounterScale : loserCounterScale);
        Color startColor = powerBG.color;
        Color targetColor = winning ? powerBGRestColor : Color.Lerp(powerBGRestColor, Color.black, loserDarken);

        if (winning)
        {
            powerOutline.enabled = true;
            powerOutline.material = OutlineMaterial(true);
            ParticleManager.instance.SpawnVFX(powerValue.transform.position, "BuffVFX", counterOnCard);
        }
        else Animations.instance.ShakeAnim(counter.gameObject, loserShake, resultTime);

        float t = 0;
        while (t < resultTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / resultTime);

            // winner overshoots and settles, loser just sinks
            float c = 2.5f;
            float scaleT = winning ? 1 + (c + 1) * Mathf.Pow(clampedT - 1, 3) + c * Mathf.Pow(clampedT - 1, 2) : 1 - (1 - clampedT) * (1 - clampedT);
            counter.localScale = Vector3.LerpUnclamped(startScale, targetScale, scaleT);
            powerBG.color = Color.Lerp(startColor, targetColor, clampedT);
            yield return null;
        }
        counter.localScale = targetScale;
        powerBG.color = targetColor;
    }

    /// <summary>
    /// The triggering sticker pops up under the unit (customText replaces its description)
    /// </summary>
    public void ShowEffectSticker(Ability strip, string customText = null)
    {
        effectSticker.InitAbility(new Sticker(strip.abilityData, strip.onRight), card);
        if (!string.IsNullOrEmpty(customText)) effectSticker.SetText(customText);

        // turning it off first restarts the fade in (and stops a fade out left from the previous effect)
        effectStickerObj.SetActive(false);
        effectStickerObj.SetActive(true);
        Animations.instance.PopAnim(effectStickerObj, 0.3f, 0.15f);
    }

    public void HideEffectSticker()
    {
        if (effectStickerObj.activeInHierarchy) effectStickerObj.GetComponent<AutoFade>().FadeOut(effectStickerFadeOut);
    }

    void OnDestroy()
    {
        // the counter on the card goes with the unit
        if (!counterOnCard) return;
        if (powerUI != null) Destroy(powerUI);
        if (powerBonus != null) Destroy(powerBonus.gameObject);
    }

    public void PinPowerCounter()
    {
        powerPinned = true;
        pinnedPowerPos = powerUI.transform.position;
        pinnedBonusPos = powerBonus.transform.position;
        pinnedUnitScale = transform.localScale;
    }

    bool powerPinned;
    Vector3 pinnedPowerPos;
    Vector3 pinnedBonusPos;
    Vector3 pinnedUnitScale;

    void LateUpdate()
    {
        // a counter on the card doesn't move with the unit anyway
        if (!powerPinned || counterOnCard) return;

        // same place and same size, whatever the unit does (it grows and squashes during the clash)
        powerUI.transform.position = pinnedPowerPos;
        powerBonus.transform.position = pinnedBonusPos;
        Vector3 scale = transform.localScale;
        Vector3 scaleFix = new Vector3(pinnedUnitScale.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), pinnedUnitScale.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f);
        float powerScale = (targetPowerScale > 0) ? targetPowerScale : 1f;
        powerUI.transform.localScale = Vector3.Scale(powerRestScale * powerScale, scaleFix);
        powerBonus.transform.localScale = Vector3.Scale(powerBonusRestScale, scaleFix);
    }

    public void RemoveFromBoard()
    {
        // removes unit reference from field (fighting units aren't on a field)
        if (currentField != null)
        {
            int unitSlot = (currentField.units[0] == this) ? 0 : 1;
            currentField.units[unitSlot] = null;
        }
        Debug.Log("Removed unit gameObject from board");

        // removes unit reference from all units list
        GameManager.instance.fieldManager.unitList.Remove(this);

        // remove the gameObject
        if (this.gameObject != null) Destroy(this.gameObject);
    }

    public IEnumerator RollPower()
    {
        int numberOfDice = (card.cardData.IsSpecial) ? 2 : 1;
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

    /// <summary>
    /// Shows one die for every power-up, in a row to the right of the unit
    /// </summary>
    public void ShowPowerUpDice(int count)
    {
        // more power-ups than dice: copying the first die
        List<D6> allDice = new List<D6>(dice);
        while (allDice.Count < count) allDice.Add(Instantiate(dice[0], dice[0].transform.parent));
        dice = allDice.ToArray();

        for (int i = 0; i < dice.Length; i++)
        {
            bool shown = i < count;
            if (!shown) { dice[i].DisableDie(); continue; }

            dice[i].gameObject.SetActive(true);
            int column = i % powerUpDicePerRow;
            int row = i / powerUpDicePerRow;
            dice[i].transform.localPosition = powerUpDiceStart + new Vector3(column * powerUpDiceSpacing, -row * powerUpDiceSpacing, 0f);
        }
    }

    /// <summary>
    /// All shown power-up dice roll, one click stops the first one and the others stop one by one after it.
    /// Every result is added to the unit's power
    /// </summary>
    public IEnumerator RollPowerUps(int count)
    {
        if (count <= 0) yield break;
        rollingPower = true;

        // only the first die waits for the click (online: the StopDice command), so both games stop them in the same order
        List<Coroutine> rolls = new List<Coroutine>();
        for (int i = 0; i < count; i++) rolls.Add(StartCoroutine(dice[i].RollAnimation(i == 0)));
        yield return rolls[0];

        for (int i = 1; i < count; i++)
        {
            yield return new WaitForSeconds(diceStopInterval);
            dice[i].Stop();
            yield return rolls[i];
        }

        for (int i = 0; i < count; i++) dice[i].Glow(true);
        rollingPower = false;
    }

    /// <summary>
    /// Base power dice: special cards roll 2 if FightManager allows it, the others 1
    /// </summary>
    public int BaseDiceCount => (card.cardData.IsSpecial && FightManager.instance.SpecialRollsTwoDice) ? 2 : 1;

    /// <summary>
    /// Base power: the dice roll at the same time until the click (one click stops them all)
    /// and land on the results (rolled beforehand with GameRandom)
    /// </summary>
    public IEnumerator RollBasePower(int[] results)
    {
        ShowPowerUpDice(results.Length);

        // opponent's dice are on the left of the unit
        if (card.player != GameManager.instance.player)
        {
            for (int i = 0; i < results.Length; i++)
            {
                Vector3 position = dice[i].transform.localPosition;
                dice[i].transform.localPosition = new Vector3(-position.x, position.y, position.z);
            }
        }

        rollingPower = true;
        List<Coroutine> rolls = new List<Coroutine>();
        for (int i = 0; i < results.Length; i++) rolls.Add(StartCoroutine(dice[i].RollAnimation(true, results[i])));
        foreach (Coroutine roll in rolls) yield return roll;
        rollingPower = false;

        // the dice stay for a bit and fade away (the fight goes on meanwhile)
        for (int i = 0; i < results.Length; i++)
        {
            dice[i].Glow(true);
            dice[i].StartCoroutine(dice[i].FadeAway());
        }
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
        // special units drop from higher, slower and spin on the way down
        bool special = IsSpecial;
        float dropHeight = (special) ? spawnDropHeight * specialDropHeightMod : spawnDropHeight;
        float dropTime = (special) ? spawnDropTime * specialDropTimeMod : spawnDropTime;

        Transform spriteTransform = sprite.transform;
        Vector3 dropStartPos = spriteRestPos + new Vector3(0f, dropHeight, 0f);

        // falling down
        float t = 0;
        while (t < dropTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / dropTime);
            float coolT = clampedT * clampedT;

            spriteTransform.localPosition = Vector3.Lerp(dropStartPos, spriteRestPos, coolT);
            Animations.instance.SetSquash(spriteTransform, spriteRestScale, -jumpSquash * (1 - coolT * 0.5f)); // stretched while falling
            if (special) spriteTransform.localRotation = spriteRestRotation * Quaternion.Euler(0f, 0f, 360f * specialSpins * (1 - coolT));
            yield return null;
        }
        spriteTransform.localPosition = spriteRestPos;
        spriteTransform.localRotation = spriteRestRotation;

        // landing
        if (special)
        {
            GameManager.instance.mainCamera.Shake(spawnCameraShake * specialCameraShakeMod, specialCameraShakeTime);
            PlaySquash(spawnSquash * specialSquashMod, 0.45f);

            // purple poof and sparkles (sound is the same CardPlaySFX as every unit, played in FieldManager.SpawnUnit)
            ParticleManager.instance.SpawnVFX(transform.position, "SpawnVFX", false, specialPowerColor);
            ParticleManager.instance.SpawnVFX(transform.position, "BuffVFX", false, specialPowerColor);
        }
        else
        {
            GameManager.instance.mainCamera.Shake(spawnCameraShake, 0.15f);
            PlaySquash(spawnSquash, 0.35f);
        }
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
        sprite.transform.localRotation = spriteRestRotation;
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
        if (!targeted || !GameManager.instance.executeManager.LocalPlayerChooses) return;

        GameManager.instance.executeManager.PickTarget(this);
        TargetOnHover(false);
    }

    void TargetOnHover(bool hoverOver)
    {
        if (!targeted || !GameManager.instance.executeManager.LocalPlayerChooses) return;

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
