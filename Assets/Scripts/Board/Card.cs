using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    [HideInInspector] public string cardName;
    
    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("refs")]
    public Ability[] abilities = new Ability[CreatureObj.maxStickers]; // sticker strips, first one is the base sticker
    [SerializeField] float[] stripHeights = { 92f, 66f, 46f }; // strip height for a card with 1, 2, 3 stickers
    [SerializeField] float[] stripFontSizes = { 34f, 28f, 24f };
    [SerializeField] TextMeshProUGUI powerText; // base power on the star
    [SerializeField] Image powerBG;
    [SerializeField] Color specialPowerColor = new Color(0.78f, 0.55f, 0.96f);
    Color defaultPowerColor;
    bool defaultPowerColorSaved = false;
    [SerializeField] RectTransform rt;
    [SerializeField] UnitSprite cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject cardVisual;
    [SerializeField] Image cardBackground;
    public OrderMarker orderMarker;
    [SerializeField] GameObject specialIcon;
    [SerializeField] Image cardBack; // opponent's face down cards show only the back
    [FormerlySerializedAs("hiddenIcon")]
    [SerializeField] GameObject revealedIcon; // your own face up field cards show this, so you know the opponent can see them
    [SerializeField] GameObject phantomLabel; // only the owner sees which card is the phantom
    public Transform Visual => cardVisual.transform; // everything drawn on the card (the fighting unit's power counter goes here too)
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
    [SerializeField] float cheerHeight = 25f; // how high the drawing hops when its side wins a fight
    Vector3 handTargetPosition;
    bool inHandLayout = false; // when true the card eases to its place in the hand fan
    float dragTilt = 0f;
    float dragLift = 0f; // 0 = resting, 1 = fully lifted
    Vector3 lastDragPosition;
    Vector3 shadowRestPosition;
    Vector3 visualRestScale; // resting scale of cardVisual, relative to the card root

    [Header("Buff indicators")]
    [SerializeField] TextMeshProUGUI buffIndicatorTemplate; // copied for every possible sticker bonus, they stack in the top right ("+4")
    readonly List<TextMeshProUGUI> buffIndicators = new List<TextMeshProUGUI>();
    readonly List<string> buffTexts = new List<string>();
    readonly List<string> shownBuffTexts = new List<string>();

    [Header("Gameplay")]
    [HideInInspector] public int currentPower = 0;
    [HideInInspector] public int startingPower = 0;
    [HideInInspector] public int auraPower = 0; // part of currentPower that comes from auras, managed by FieldManager.RefreshAuras
    [HideInInspector] public bool faceDown; // on the field, hidden from the opponent until it fights
    [HideInInspector] public bool isPhantom; // Phantom's copy: looks the same, only its owner knows which one it is
    [HideInInspector] public Card twin; // Phantom card and its copy point at each other
    [HideInInspector] public bool destroyedCardThisRound; // Beast: already destroyed a card this round
    [HideInInspector] public bool onDisplay; // fighting: shown big on the side of the screen, its place in the row waits for it
    bool specialCostPaid = false;

    private void Awake()
    {
        // scales are captured in Awake, because opponent cards sit in an inactive hand and only wake up
        // when played, by then Start would run after RevealCard already scaled the card up
        SetScales();

        if (dragShadow != null)
        {
            shadowRestPosition = dragShadow.rectTransform.localPosition;
            SetShadowAlpha(0f);
        }
    }

    void SetScales()
    {
        defaultScale = transform.localScale;
        highlightedScale = defaultScale * 1.2f;
        dragScale = defaultScale * 1.1f;
        visualRestScale = cardVisual.transform.localScale;
    }

    private void Update()
    {
        Drag();
        AnimateHandLayout();
        AnimateVisual();
        RefreshBuffIndicators();
    }

    /// <summary>
    /// Battle: the sticker bonuses this card would get if it fought now (attacking on its owner's turn, defending otherwise).
    /// Bonuses that depend on the opposing card aren't shown, the opponent's face down cards show nothing
    /// </summary>
    void RefreshBuffIndicators()
    {
        if (buffIndicatorTemplate == null) return;

        buffTexts.Clear();
        GameManager gm = GameManager.instance;
        bool show = gm != null && player != null && gm.currentState == GameState.BATTLING && !onDisplay && !HiddenFromLocalPlayer && player.cardsOnField.Contains(this);
        if (show)
        {
            bool attacking = gm.yourTurn == (player == gm.player);
            foreach ((Ability strip, PowerUpEffect effect) in GetEffects<PowerUpEffect>())
            {
                if (!effect.CouldTrigger(this, attacking)) continue;
                // bonuses that depend on the opposing card (Flanker) get an asterisk, they're only a maybe
                buffTexts.Add("+" + effect.amount + ((effect.maxAmount > effect.amount) ? "-" + effect.maxAmount : "") + (effect.DependsOnOpposingCard ? "*" : ""));
            }

            // Cheerers on the field give the side cards power (a Cheerer on the side too)
            if (FightManager.instance.IsSideCard(this))
            {
                foreach (Card card in player.cardsOnField)
                {
                    foreach ((Ability strip, NeighbourPowerUpEffect effect) in card.GetEffects<NeighbourPowerUpEffect>()) buffTexts.Add("+" + effect.amount);
                }
            }
        }

        // the texts only change when the bonuses do
        bool same = buffTexts.Count == shownBuffTexts.Count;
        for (int i = 0; same && i < buffTexts.Count; i++) same = buffTexts[i] == shownBuffTexts[i];
        if (same) return;
        shownBuffTexts.Clear();
        shownBuffTexts.AddRange(buffTexts);

        while (buffIndicators.Count < buffTexts.Count) buffIndicators.Add(Instantiate(buffIndicatorTemplate, buffIndicatorTemplate.transform.parent));
        for (int i = 0; i < buffIndicators.Count; i++)
        {
            bool shown = i < buffTexts.Count;
            buffIndicators[i].gameObject.SetActive(shown);
            if (shown) buffIndicators[i].text = buffTexts[i];
        }
    }

    /// <summary>
    /// Sets where the card should be in the hand, the card then eases there
    /// </summary>
    public void SetHandLayout(Vector3 localPosition, float rotationAngle)
    {
        handTargetPosition = localPosition;
        cardRotation = rotationAngle;

        // a fighting card stays on display, it moves back when the fight is over
        inHandLayout = !onDisplay;
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
    /// Glides into its place in the row (position, size and tilt together, smooth start and end), then the hand layout takes over
    /// </summary>
    public IEnumerator EaseIntoHandLayout(float time)
    {
        inHandLayout = false;
        Vector3 startPos = transform.localPosition;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.localRotation;

        // stops if the card goes on display again (it fights)
        float t = 0;
        while (t < time && !onDisplay)
        {
            t += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t / time);
            // the target is read every frame, the row can change meanwhile
            transform.localPosition = Vector3.Lerp(startPos, handTargetPosition, smoothT);
            transform.localScale = Vector3.Lerp(startScale, defaultScale, smoothT);
            transform.localRotation = Quaternion.Slerp(startRotation, Quaternion.Euler(0f, 0f, cardRotation), smoothT);
            yield return null;
        }
        if (onDisplay) yield break;
        transform.localScale = defaultScale;
        inHandLayout = true;
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
        // visual is a child of the card, so its scale is relative to the card root
        Vector3 targetScale = (isDragged) ? visualRestScale * 1.2f * dragLiftScale : (hovered) ? visualRestScale * 1.2f : visualRestScale;
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
        cardVisual.transform.localScale = visualRestScale;
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
    /// Shows the card's stickers on the strips and the base power on the star
    /// </summary>
    public void AssignAbilies()
    {
        // fewer stickers = bigger strips, so the text stays readable
        int stickerCount = Mathf.Clamp(cardData.stickers.Count, 1, stripHeights.Length);
        for (int i = 0; i < abilities.Length; i++)
        {
            Sticker sticker = (i < cardData.stickers.Count) ? cardData.stickers[i] : null;
            abilities[i].InitAbility(sticker, this);
            abilities[i].SetSize(stripHeights[stickerCount - 1], stripFontSizes[stickerCount - 1]);
        }

        // base power, special cards have a purple star (saved lazily, opponent cards can be initialized before Awake)
        AbilityObj baseSticker = cardData.BaseSticker;
        powerText.text = baseSticker.GetPowerText();
        if (!defaultPowerColorSaved) { defaultPowerColor = powerBG.color; defaultPowerColorSaved = true; }
        powerBG.color = (baseSticker.isSpecial) ? specialPowerColor : defaultPowerColor;
    }

    public void AssignCardData(CreatureObj newCardData, Player owner)
    {
        cardData = newCardData;
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

        // STICKERS (cards can get new stickers in the workshop)
        AssignAbilies();

        // BACKGROUND
        //cardBackground.color = GameManager.instance.fieldManager.DesaturateColor(player.playerColor, 0.4f);

        // special cards are shown with a purple power counter now (Ability), not with the star icon
        specialIcon.SetActive(false);

        // FACE DOWN
        RefreshFace();
    }

    /// <summary>
    /// Workshop: puts an add-on sticker on the card (up to CreatureObj.maxStickers)
    /// </summary>
    public void AddSticker(Sticker sticker)
    {
        if (!cardData.CanAddSticker) return;

        cardData.stickers.Add(sticker);
        Refresh();
    }

    /// <summary>
    /// Takes a sticker off the card (Lucky gets used up instead of the card)
    /// </summary>
    public void RemoveSticker(Sticker sticker)
    {
        cardData.stickers.Remove(sticker);
        Refresh();
    }

    /// <summary>
    /// Effects of type T on the card's stickers, with the strip they are on (the strip knows the sticker's side)
    /// </summary>
    public List<(Ability strip, T effect)> GetEffects<T>() where T : AbilityEffect
    {
        List<(Ability, T)> found = new List<(Ability, T)>();
        foreach (Ability strip in abilities)
        {
            if (strip == null || strip.abilityData == null) continue;
            foreach (AbilityEffect effect in strip.abilityData.effects)
            {
                if (effect is T typed) found.Add((strip, typed));
            }
        }
        return found;
    }

    public bool HasEffect<T>() where T : AbilityEffect => GetEffects<T>().Count > 0;

    /// <summary>
    /// Sticker that holds an effect of type T, or null
    /// </summary>
    public Sticker GetStickerWith<T>() where T : AbilityEffect => cardData.stickers.Find(sticker => sticker.ability.effects.Exists(effect => effect is T));

    /// <summary>
    /// Turns the card face down (hidden from the opponent) or face up
    /// </summary>
    public void SetFaceDown(bool isFaceDown)
    {
        faceDown = isFaceDown;
        RefreshFace();
    }

    /// <summary>
    /// The local player doesn't know this card (opponent's face down card)
    /// </summary>
    public bool HiddenFromLocalPlayer => faceDown && player != GameManager.instance.player;

    void RefreshFace()
    {
        // the opponent's face down cards only show the back, your own get a "hidden" icon
        bool showBack = HiddenFromLocalPlayer;
        cardBack.gameObject.SetActive(showBack);
        cardBack.color = player.playerColor;
        bool onField = GameManager.instance.currentState != GameState.BUYING && player.cardsOnField.Contains(this);
        revealedIcon.SetActive(!faceDown && onField && player == GameManager.instance.player);

        // sticker strips are hidden under the back (the special star would stick out and give the card away)
        foreach (Ability strip in abilities) strip.gameObject.SetActive(strip.abilityData != null && !showBack);
        phantomLabel.SetActive(isPhantom && player == GameManager.instance.player);
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
    public static Card hoveredCard; // card under the mouse (right click zooms it instead of cancelling)

    public void OnHover(bool mouseOver)
    {
        if (mouseOver) hoveredCard = this;
        else if (hoveredCard == this) hoveredCard = null;

        // highlighting cards that can be attacked
        if (FightManager.instance != null && FightManager.instance.IsTarget(this)) HighlightCard(mouseOver);

        Player localPlayer = GameManager.instance.player;
        if (localPlayer.cardsInHand.Contains(this) || localPlayer.cardsOnField.Contains(this))
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
        // only your own field cards: arranging until you press ready, attacking on your turn
        GameState state = GameManager.instance.currentState;
        if (player != GameManager.instance.player || !player.cardsOnField.Contains(this)) return;
        if (state == GameState.PLACING && (player.endStateReady || !GameManager.instance.readyButton.button.interactable)) return; // online Ready is on its way
        if (state == GameState.BATTLING && !FightManager.instance.CanAttack(player)) return;
        if (state == GameState.BUYING) return;

        isDragged = true;
        lastDragPosition = transform.position;
        GameManager.instance.handManager.activeCard = this;
    }

    public void EndDrag()
    {
        if (!isDragged) return;

        isDragged = false;
        OnHover(false);
        GameManager.instance.handManager.activeCard = null;

        switch (GameManager.instance.currentState)
        {
            // the card takes the place where it was dropped
            case GameState.PLACING:
                GameManager.instance.handManager.ReorderField(player);
                break;

            // dragged up out of the field: attacks, otherwise goes back to its place
            case GameState.BATTLING:
                if (!FightManager.instance.TryStartAttack(this)) GameManager.instance.handManager.UpdateHandVisuals(player);
                break;
        }
    }

    /// <summary>
    /// Clicked: picks this card as the attack target if one is being chosen
    /// </summary>
    public void OnClick()
    {
        // battle: attack target, workshop: card to put the new sticker on
        if (FightManager.instance.TryPickTarget(this)) return;
        GameManager.instance.managerUI.workshop.TryPutSticker(this);
    }

    /// <summary>
    /// Right click: a big copy of the card shows in the middle of the screen until the mouse leaves the card
    /// </summary>
    public void Zoom(bool zoomIn)
    {
        // the opponent's face down cards only show their back
        if (zoomIn && faceDown && player != GameManager.instance.player) return;

        ManagerUI ui = GameManager.instance.managerUI;
        if (zoomIn) ui.ZoomCard(this);
        else if (ui.ZoomedCard == this) ui.ZoomCard(null);
    }

    /// <summary>
    /// Drawing on the card hops up and down
    /// </summary>
    public void Cheer()
    {
        if (gameObject.activeInHierarchy) StartCoroutine(CheerAnim());
    }

    IEnumerator CheerAnim()
    {
        Transform art = cardSprite.transform;
        Vector3 restPos = art.localPosition;
        float hopTime = 0.22f;
        for (int hop = 0; hop < 2; hop++)
        {
            float t = 0;
            while (t < hopTime)
            {
                t += Time.deltaTime;
                art.localPosition = restPos + new Vector3(0f, Mathf.Sin(Mathf.Clamp01(t / hopTime) * Mathf.PI) * cheerHeight, 0f);
                yield return null;
            }
        }
        art.localPosition = restPos;
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
        // base power comes from the d6 roll in the fight (AddBasePower), the rest from stickers
        startingPower = 0;
        GainPower(0, false);
    }

    /// <summary>
    /// Adds this much power to current unit power amount
    /// </summary>
    /// <param name="power"></param>
    /// 
    /// <summary>
    /// Base power from the d6 roll: it counts as starting power, so no gain juice and no buff outline
    /// </summary>
    public void AddBasePower(int power)
    {
        startingPower += power;
        GainPower(power, false);
    }

    public void GainPower(int power, bool animated = true)
    {
        Debug.Log("Card: " + cardData.name + "gains " + power + " power.");

        // pop animation to signify that something has changed
        // starting power isn't animated, so only buffs and debuffs get particles
        if (animated)
        {
            Animations.instance.PopAnim(unit.powerValue.gameObject, 0.4f, 0.3f);
            unit.PlayPowerChangeVFX(power);

            // Playing buff/debuff sfx
            string buffSFXname = (power > 0) ? "BuffSFX" : "DebuffSFX";
            AudioManager.instance.PlaySFX(buffSFXname, 0.5f);
        }

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
        destroyedCardThisRound = false;
        onDisplay = false;
        SetFaceDown(false);

        //unit.RefreshUnitVisuals();
    }

}
