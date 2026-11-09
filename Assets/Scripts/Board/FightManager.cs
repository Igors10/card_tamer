using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle phase: players take turns attacking one opponent card with one of their own.
/// Attack: card dragged up -> unit spawns -> arrow -> target picked -> both revealed -> base power rolled -> stickers add power -> stronger one wins
/// </summary>
public class FightManager : MonoBehaviour
{
    public static FightManager instance;

    [Header("refs")]
    [SerializeField] Transform playerSpot; // where the local player's fighting unit stands
    [SerializeField] Transform opponentSpot;
    [SerializeField] TargetArrow targetArrow;
    [SerializeField] GameObject guessPanel; // Seer: known opponent cards are shown here to pick from
    [SerializeField] Transform guessRow;
    [SerializeField] RectTransform playerCardSpot; // fighting cards are shown big here: yours on the left
    [SerializeField] RectTransform opponentCardSpot; // opponent's on the right
    [SerializeField] float displayScale = 1.1f;
    [SerializeField] float cardMoveTime = 0.35f;
    [SerializeField] float cardReturnTime = 0.3f; // after the fight (and the exit swing) the card glides back to its place in the row

    [Header("card exit (the entrance backwards, softer)")]
    [SerializeField] float exitTime = 0.22f;
    [SerializeField] float exitDistance = 0.7f; // how much of the entrance arc it goes back along (1 = all the way to the start point)
    [SerializeField] float exitTilt = 0.5f; // tilts back this part of the entrance start tilt
    [SerializeField] float exitScale = 0.6f; // shrinks to this, relative to the display scale
    [SerializeField] float chosenCardShowTime = 0.8f; // opponent's chosen card lights up in its row this long
    [SerializeField] float targetShowTime = 1.2f; // arrow to the opponent's target stays this long

    [Header("card entrance (shown like a card in a board game, mirrored for the opponent)")]
    [SerializeField] bool moveToEntranceStart = true; // off: the card jumps to the start point instead of floating there
    [SerializeField] Vector2 entranceStartOffset = new Vector2(-260f, -280f); // start point from the display spot (x mirrored for the opponent)
    [SerializeField] float entranceStartScale = 0.3f; // card scale at the start, relative to the display scale
    [SerializeField] float entranceStartAngle = 60f; // card tilt at the start, it rotates up from here
    [SerializeField] float entranceEndAngle = -12f; // stays a bit tilted the other way while on display
    [SerializeField] float entranceOvershoot = 2.2f; // how far the rotation and scale swing past the end before settling
    [SerializeField] float entranceTime = 0.28f;
    [SerializeField] float entranceShake = 0.07f; // screen shake when the card lands

    [Header("card entrance juice")]
    [SerializeField] float windupTime = 0.1f; // the card pulls back before swinging
    [SerializeField] float windupAngle = 15f; // tilts this much further back
    [SerializeField] float windupShrink = 0.85f;
    [SerializeField] float swingStretch = 0.22f; // stretches while swinging, most at full speed
    [SerializeField] float afterimageInterval = 0.03f; // fading copies in the owner's color left behind while swinging
    [SerializeField] float afterimageFade = 0.22f;
    [SerializeField] float afterimageAlpha = 0.45f;
    [SerializeField] float landHitStop = 0.06f; // everything freezes for a split second when it lands
    [SerializeField] float landSquash = 0.22f;
    [SerializeField] float landWobbleAngle = 7f; // the tilt wobbles around its end angle and settles
    [SerializeField] float landWobbleTime = 0.45f;
    [SerializeField] float shockwaveScale = 1.35f; // a pulse in the owner's color grows out of the card and fades
    [SerializeField] float shockwaveTime = 0.35f;
    [SerializeField] float otherCardKnock = 9f; // the other shown card gets knocked a bit
    [SerializeField] float pauseBeforeUnit = 0.3f; // the card settles before its unit appears (so the unit doesn't look like it comes out of the card)

    [Header("input")]
    [SerializeField] float attackDropHeight = 0.35f; // card has to be dropped above this part of the screen to attack

    [Header("timing")]
    [SerializeField] float afterSpawnPause = 0.6f;
    [SerializeField] float powerGainPause = 0.5f; // after each power gain from a sticker
    [SerializeField] float beforeResultPause = 0.6f;
    [SerializeField] float afterFightPause = 0.8f;

    [Header("clash (fighting units fly at each other)")]
    [SerializeField] float clashLiftTime = 0.3f; // units lift up into the air
    [SerializeField] float clashLiftScale = 1.25f;
    [SerializeField] float clashLiftHeight = 0.6f;
    [SerializeField] float clashWindupTime = 0.35f; // pull back before flying
    [SerializeField] float clashWindupDistance = 1.4f;
    [SerializeField] float clashWindupSquash = 0.2f; // units squash down while pulling back
    [SerializeField] float clashWindupTremble = 0.08f; // and shake harder and harder
    [SerializeField] float clashWindupHold = 0.12f; // fully pulled back for a moment before letting go
    [SerializeField] float clashFlyTime = 0.18f;
    [SerializeField] float clashFlyStretch = 0.25f; // units stretch out while flying
    [SerializeField] float clashHitGap = 1.2f; // distance between the units when they hit
    [SerializeField] float clashHitStop = 0.1f; // everything freezes for a split second at the hit
    [SerializeField] float clashHitSquash = 0.35f;
    [SerializeField] float winnerRecoilDistance = 1.5f; // winner gets knocked back this far from the hit (and stays there)
    [SerializeField] float winnerRecoilHop = 0.5f;
    [SerializeField] float winnerRecoilTime = 0.25f;
    [SerializeField] float stickerMessagePause = 1.2f; // guard, mix up and similar messages stay this long

    // current fight
    [HideInInspector] public Unit attacker;
    [HideInInspector] public Unit defender;
    Card choosingFor; // local player's card waiting for a target
    List<Card> choiceOptions; // cards the local player can click right now (ChooseCard), null when nothing is asked
    Card chosenCard; // result of ChooseCard
    bool fighting;

    void Awake()
    {
        instance = this;
    }

    // ===============
    // STARTING AN ATTACK
    // ===============

    /// <summary>
    /// True if the player can start an attack right now
    /// </summary>
    public bool CanAttack(Player p)
    {
        GameManager gm = GameManager.instance;
        return gm.currentState == GameState.BATTLING && !fighting && !preparingAttack && choosingFor == null && !attackSent
            && (p == gm.player) == gm.yourTurn && !gm.gameOver;
    }

    /// <summary>
    /// Local player dropped a field card: attacks if it was dragged high enough. Returns false if nothing happened
    /// </summary>
    public bool TryStartAttack(Card card)
    {
        if (!CanAttack(card.player) || Input.mousePosition.y < Screen.height * attackDropHeight) return false;

        // online the other player sees the attacker come out right away: both games start it when the command comes back
        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            preparingAttack = true;
            MatchNetwork.instance.SendAttackStart(card.player.cardsOnField.IndexOf(card));
            return true;
        }

        StartCoroutine(LocalAttackIntro(card));
        return true;
    }

    bool preparingAttack; // the attacker is coming out, the target can be picked after its entrance

    /// <summary>
    /// Online: an AttackStart command arrived. The local player's attacker comes out and then they pick a target,
    /// the other player's attacker comes out and the game waits for their target (Attack) or cancel
    /// </summary>
    public void ApplyAttackStart(Card card)
    {
        if (card.player == GameManager.instance.player) StartCoroutine(LocalAttackIntro(card));
        else StartCoroutine(RemoteAttackIntro(card));
    }

    /// <summary>
    /// The card is revealed (it attacks) and goes on display, its unit comes out with its entrance, then the player picks a target
    /// </summary>
    IEnumerator LocalAttackIntro(Card card)
    {
        preparingAttack = true;
        yield return Reveal(card);
        yield return BringOut(card, false, unit => attacker = unit);
        preparingAttack = false;

        choosingFor = card;
        targetArrow.Show(attacker.transform);
        GameManager.instance.managerUI.NewHint("Choose an opponent card to attack (right click to cancel)");
    }

    public bool IsTarget(Card card) => choosingFor != null && card.player != choosingFor.player && card.player.cardsOnField.Contains(card);

    /// <summary>
    /// Local player clicked a card while choosing a target (or a card to swap places with). Returns true if it was used
    /// </summary>
    public bool TryPickTarget(Card card)
    {
        // answering a ChooseCard question
        if (choiceOptions != null)
        {
            if (!choiceOptions.Contains(card)) return false;
            PickChoice(choiceOptions.IndexOf(card));
            return true;
        }

        if (!IsTarget(card) || attackCancelSent) return false;

        card.HighlightCard(false);
        Card attackingCard = choosingFor;
        choosingFor = null;
        targetArrow.Hide();
        PlayBattleMusic();

        // online the attack goes to the server first, both games start the fight when it comes back (ApplyAttack)
        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            attackSent = true;
            MatchNetwork.instance.SendAttack(attackingCard.player.cardsOnField.IndexOf(attackingCard), card.player.cardsOnField.IndexOf(card));
            return true;
        }

        StartCoroutine(Fight(attackingCard, card));
        return true;
    }

    /// <summary>
    /// Online: an attack command arrived. The sender's own attacker is already out (spawned for the arrow)
    /// </summary>
    public void ApplyAttack(Card attackingCard, Card target)
    {
        if (attackSent && attacker != null && attacker.card == attackingCard)
        {
            attackSent = false;
            StartCoroutine(Fight(attackingCard, target));
        }
        else if (remoteAttacker == attackingCard) StartCoroutine(RemoteAttackTarget(target));
        else Attack(attackingCard, target);
    }

    Card remoteAttacker; // online: the other player's card that came out (AttackStart), waiting for its target
    bool remoteIntroDone;

    /// <summary>
    /// Online: the other player's Attack / AttackCancel can be applied (their attacker is out)
    /// </summary>
    public bool RemoteAttackPending => remoteAttacker != null;

    IEnumerator RemoteAttackIntro(Card card)
    {
        fighting = true;
        remoteAttacker = card;
        remoteIntroDone = false;
        GameManager.instance.managerUI.NewHint(card.player.playerName + " is choosing a target...");
        yield return Reveal(card);
        yield return BringOut(card, true, unit => attacker = unit);
        remoteIntroDone = true;
    }

    /// <summary>
    /// Online: the other player picked a target, the arrow shows it and the fight starts (after their attacker's entrance here)
    /// </summary>
    IEnumerator RemoteAttackTarget(Card target)
    {
        while (!remoteIntroDone) yield return null;
        Card attackingCard = remoteAttacker;
        remoteAttacker = null;
        GameManager.instance.managerUI.NewHint("");

        PlayBattleMusic();
        target.HighlightCard(true);
        targetArrow.Show(attacker.transform, target.transform);
        yield return Animations.instance.SkippablePause(targetShowTime);
        targetArrow.Hide();
        target.HighlightCard(false);

        yield return Fight(attackingCard, target);
    }

    /// <summary>
    /// Battle music plays from the moment an attack target is picked until the next turn starts (Player.StartTurn)
    /// </summary>
    void PlayBattleMusic()
    {
        AudioManager.instance.PlaySoundtrack("BattleTrack");
    }

    /// <summary>
    /// Online: a cancel arrived, the attacker of that player goes back
    /// </summary>
    public void ApplyAttackCancel(Player p)
    {
        if (p == GameManager.instance.player) { attackCancelSent = false; CancelAttack(); }
        else StartCoroutine(RemoteCancel());
    }

    bool attackCancelSent;

    IEnumerator RemoteCancel()
    {
        while (!remoteIntroDone) yield return null;
        Card card = remoteAttacker;
        remoteAttacker = null;
        fighting = false;
        attacker.RemoveFromBoard();
        attacker = null;
        card.currentPower = 0;
        StartCoroutine(ReturnCard(card));
        GameManager.instance.managerUI.NewHint("It's your opponents turn");

        // still their turn, their next attack can come in
        GameManager.instance.waitingForRemote = true;
    }

    bool attackSent; // online: local attack is on its way to the server

    void Update()
    {
        // right click (not on a card, that zooms it) puts the attacker back. Online the cancel goes to the server first
        if (choosingFor != null && !attackCancelSent && Input.GetMouseButtonDown(1) && Card.hoveredCard == null)
        {
            if (GameManager.instance.playerConfig.offlineMatch) CancelAttack();
            else
            {
                attackCancelSent = true;
                MatchNetwork.instance.SendAttackCancel();
            }
        }
    }

    void CancelAttack()
    {
        if (choosingFor == null) return;
        targetArrow.Hide();
        attacker.RemoveFromBoard();
        attacker = null;

        Card card = choosingFor;
        choosingFor = null;
        card.currentPower = 0;
        StartCoroutine(ReturnCard(card));
        GameManager.instance.managerUI.NewHint(GameManager.instance.GetState().defaultHintText);
    }

    /// <summary>
    /// Bot and the online opponent: their card lights up in the row, goes on display, comes out,
    /// then an arrow shows which card it attacks
    /// </summary>
    public void Attack(Card attackingCard, Card target)
    {
        StartCoroutine(OpponentAttack(attackingCard, target));
    }

    IEnumerator OpponentAttack(Card attackingCard, Card target)
    {
        fighting = true;
        yield return Reveal(attackingCard);
        yield return BringOut(attackingCard, true, unit => attacker = unit);

        PlayBattleMusic();
        target.HighlightCard(true);
        targetArrow.Show(attacker.transform, target.transform);
        yield return Animations.instance.SkippablePause(targetShowTime);
        targetArrow.Hide();
        target.HighlightCard(false);

        yield return Fight(attackingCard, target);
    }

    /// <summary>
    /// The chosen card (optionally lit up in its row first) moves to its side of the screen and its unit comes out with the entrance animation
    /// </summary>
    IEnumerator BringOut(Card card, bool showChoice, System.Action<Unit> setUnit)
    {
        if (showChoice)
        {
            card.HighlightCard(true);
            Animations.instance.PopAnim(card.gameObject, 0.3f, 0.25f);
            AudioManager.instance.PlaySFX("NextCardSFX");
            yield return Animations.instance.SkippablePause(chosenCardShowTime);
            card.HighlightCard(false);
        }

        yield return DisplayCard(card);
        yield return Animations.instance.SkippablePause(pauseBeforeUnit);

        Transform spot = (card.player == GameManager.instance.player) ? playerSpot : opponentSpot;
        Unit unit = GameManager.instance.fieldManager.SpawnFightUnit(card, spot.position);
        unit.MovePowerCounterToCard(card);
        card.GetStartingPower();
        setUnit(unit);

        // entrance: drop with squash, sunburst presenter, jump with stretch
        while (unit.IsSpawning) yield return null;
        unit.unitPresenter.gameObject.SetActive(true);
        yield return unit.EntranceAnimation();
    }

    /// <summary>
    /// Card leaves its place in the row (the place waits for it) and moves big to its side of the screen: yours left, opponent's right
    /// </summary>
    IEnumerator DisplayCard(Card card)
    {
        RectTransform spot = (card.player == GameManager.instance.player) ? playerCardSpot : opponentCardSpot;
        homeParents[card] = card.transform.parent;
        card.onDisplay = true;
        card.ExitHandLayout();
        card.transform.SetParent(spot.parent, true);
        card.transform.SetAsLastSibling();

        // floats (or jumps) to the start point small and tilted, then swings up into the display spot
        bool mirrored = card.player != GameManager.instance.player;
        Vector2 offset = mirrored ? new Vector2(-entranceStartOffset.x, entranceStartOffset.y) : entranceStartOffset;
        Vector3 startPos = spot.position + spot.parent.TransformVector(offset);
        Vector3 startScale = Vector3.one * displayScale * entranceStartScale;
        float startAngle = mirrored ? -entranceStartAngle : entranceStartAngle;
        float endAngle = mirrored ? -entranceEndAngle : entranceEndAngle;
        if (moveToEntranceStart) yield return MoveCard(card.transform, startPos, startScale, startAngle);
        else
        {
            card.transform.position = startPos;
            card.transform.localScale = startScale;
            card.transform.localRotation = Quaternion.Euler(0f, 0f, startAngle);
        }
        yield return CardEntrance(card, startPos, spot.position, startAngle, endAngle);
    }

    /// <summary>
    /// The card swings up into its spot like a card shown in a board game: pulls back, then rotates past upright to a small tilt
    /// the other way and grows (stretching, leaving afterimages), overshooting a bit. Lands with a freeze, squash, wobble,
    /// a pulse, particles, shake and a thud. Moving up a bit faster than sideways gives the arc
    /// </summary>
    IEnumerator CardEntrance(Card card, Vector3 startPos, Vector3 endPos, float startAngle, float endAngle)
    {
        Transform cardTransform = card.transform;
        Color ghostColor = card.player.playerColor;

        // WIND-UP: pulls back (further tilt, a bit smaller, a bit away from the spot)
        Vector3 restScale = cardTransform.localScale;
        Vector3 windupPos = startPos + (startPos - endPos).normalized * 35f * cardTransform.parent.lossyScale.x;
        float windupTilt = startAngle + Mathf.Sign(startAngle) * windupAngle;
        AudioManager.instance.PlaySFX("PaperSFX", 0.8f, 1.4f);
        float t = 0;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            float coolT = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / windupTime), 2);
            cardTransform.position = Vector3.Lerp(startPos, windupPos, coolT);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, windupTilt, coolT));
            cardTransform.localScale = Vector3.Lerp(restScale, restScale * windupShrink, coolT);
            yield return null;
        }

        // SWING: from the pulled back pose into the spot, stretching at full speed and leaving afterimages
        Vector3 swingStartPos = cardTransform.position;
        Vector3 swingStartScale = cardTransform.localScale;
        Vector3 endScale = Vector3.one * displayScale;
        float nextGhost = 0f;
        t = 0;
        while (t < entranceTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / entranceTime);
            float upT = 1 - Mathf.Pow(1 - clampedT, 3);
            float sideT = 1 - Mathf.Pow(1 - clampedT, 2);
            float swingT = EaseOutBack(clampedT);

            cardTransform.position = new Vector3(Mathf.LerpUnclamped(swingStartPos.x, endPos.x, sideT), Mathf.LerpUnclamped(swingStartPos.y, endPos.y, upT), endPos.z);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(windupTilt, endAngle, swingT));
            Vector3 scale = Vector3.LerpUnclamped(swingStartScale, endScale, swingT);
            float stretch = swingStretch * Mathf.Sin(clampedT * Mathf.PI) * (1 - clampedT);
            cardTransform.localScale = new Vector3(scale.x * (1 - stretch * 0.5f), scale.y * (1 + stretch), scale.z);

            if (t >= nextGhost && clampedT < 0.8f)
            {
                nextGhost = t + afterimageInterval;
                StartCoroutine(FadeGhost(card.CreateGhost(new Color(ghostColor.r, ghostColor.g, ghostColor.b, afterimageAlpha)), 1f, afterimageFade));
            }
            yield return null;
        }
        cardTransform.position = endPos;
        cardTransform.localRotation = Quaternion.Euler(0f, 0f, endAngle);
        cardTransform.localScale = endScale;

        // LANDING: thud sounds, shake, particles, a pulse, the other card gets knocked, a split second freeze
        AudioManager.instance.PlaySFX("CardPlaySFX", 1f, 0.85f);
        AudioManager.instance.PlaySFX("HitSFX", 0.35f, 1.3f);
        GameManager.instance.mainCamera.Shake(entranceShake, 0.2f);
        ParticleManager.instance.SpawnVFX(endPos, "HitVFX", true);
        StartCoroutine(FadeGhost(card.CreateGhost(new Color(ghostColor.r, ghostColor.g, ghostColor.b, 0.8f)), shockwaveScale, shockwaveTime));
        foreach (Card other in homeParents.Keys) if (other != card && other != null && other.onDisplay) Animations.instance.ShakeAnim(other.gameObject, otherCardKnock, 0.25f);

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(landHitStop);
        Time.timeScale = 1f;

        // squash and a tilt wobble that settles (the fight goes on meanwhile)
        StartCoroutine(Animations.instance.SquashStretch(cardTransform, endScale, landSquash, landWobbleTime));
        StartCoroutine(LandWobble(card, endAngle));
    }

    /// <summary>
    /// Damped tilt wobble around the end angle after the card lands (stops if the card leaves the display)
    /// </summary>
    IEnumerator LandWobble(Card card, float endAngle)
    {
        float t = 0;
        while (t < landWobbleTime && card != null && card.onDisplay)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / landWobbleTime);
            float wobble = landWobbleAngle * Mathf.Sin(clampedT * Mathf.PI * 3f) * (1 - clampedT) * (1 - clampedT);
            card.transform.localRotation = Quaternion.Euler(0f, 0f, endAngle + wobble);
            yield return null;
        }
        if (card != null && card.onDisplay) card.transform.localRotation = Quaternion.Euler(0f, 0f, endAngle);
    }

    /// <summary>
    /// Afterimage / pulse: the ghost grows to the given scale while fading out, then it's gone
    /// </summary>
    IEnumerator FadeGhost(Image ghost, float growTo, float time)
    {
        Vector3 startScale = ghost.transform.localScale;
        Color startColor = ghost.color;
        float t = 0;
        while (t < time && ghost != null)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / time);
            float coolT = 1 - Mathf.Pow(1 - clampedT, 2);
            ghost.transform.localScale = Vector3.Lerp(startScale, startScale * growTo, coolT);
            ghost.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1 - coolT));
            yield return null;
        }
        if (ghost != null) Destroy(ghost.gameObject);
    }

    float EaseOutBack(float t)
    {
        float c = entranceOvershoot;
        return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2);
    }

    /// <summary>
    /// After the fight the card goes back to its place in the row (knocked out cards just leave the display)
    /// </summary>
    IEnumerator ReturnCard(Card card)
    {
        if (card == null || !homeParents.TryGetValue(card, out Transform home)) yield break;
        homeParents.Remove(card);

        // destroyed cards (Bomb, Beast) are gone, knocked out ones are hidden in the discard (back to normal size for next round)
        if (!card.player.cardsOnField.Contains(card))
        {
            card.onDisplay = false;
            card.transform.SetParent(home, true);
            card.transform.localScale = card.defaultScale;
            card.transform.localRotation = Quaternion.identity;
            yield break;
        }

        // swings back down the way it came in (softer), then glides to its place in the row
        // (both run on the card, so they stop if the card is destroyed meanwhile)
        yield return card.StartCoroutine(CardExit(card));
        card.onDisplay = false;
        card.transform.SetParent(home, true);
        GameManager.instance.handManager.UpdateHandVisuals(card.player);
        yield return card.StartCoroutine(card.EaseIntoHandLayout(cardReturnTime));
    }

    /// <summary>
    /// The entrance backwards with a softer swing: the card goes back along the same arc towards the entrance start point,
    /// tilting and shrinking a bit, no overshoot
    /// </summary>
    IEnumerator CardExit(Card card)
    {
        Transform cardTransform = card.transform;
        bool mirrored = card.player != GameManager.instance.player;
        RectTransform spot = mirrored ? opponentCardSpot : playerCardSpot;
        Vector2 offset = mirrored ? new Vector2(-entranceStartOffset.x, entranceStartOffset.y) : entranceStartOffset;
        Vector3 entranceStart = spot.position + spot.parent.TransformVector(offset);

        Vector3 displayPos = cardTransform.position;
        Vector3 startScale = cardTransform.localScale;
        Vector3 endScale = Vector3.one * displayScale * exitScale;
        float startAngle = Mathf.DeltaAngle(0f, cardTransform.localEulerAngles.z);
        float endAngle = (mirrored ? -entranceStartAngle : entranceStartAngle) * exitTilt;

        float t = 0;
        while (t < exitTime)
        {
            t += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / exitTime));

            // same arc as the entrance, travelled backwards (only partly, then the glide to the row takes over)
            float pathT = 1 - smoothT * exitDistance;
            float sideT = 1 - Mathf.Pow(1 - pathT, 2);
            float upT = 1 - Mathf.Pow(1 - pathT, 3);
            cardTransform.position = new Vector3(Mathf.Lerp(entranceStart.x, displayPos.x, sideT), Mathf.Lerp(entranceStart.y, displayPos.y, upT), displayPos.z);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, endAngle, smoothT));
            cardTransform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }
    }

    IEnumerator MoveCard(Transform card, Vector3 targetPos, Vector3 targetScale, float targetAngle = 0f)
    {
        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;
        Quaternion startRotation = card.localRotation;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        float t = 0;
        while (t < cardMoveTime)
        {
            t += Time.deltaTime;
            float coolT = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / cardMoveTime), 3);
            card.position = Vector3.Lerp(startPos, targetPos, coolT);
            card.localScale = Vector3.Lerp(startScale, targetScale, coolT);
            card.localRotation = Quaternion.Slerp(startRotation, targetRotation, coolT);
            yield return null;
        }
        card.position = targetPos;
        card.localScale = targetScale;
        card.localRotation = targetRotation;
    }

    readonly Dictionary<Card, Transform> homeParents = new Dictionary<Card, Transform>(); // row each displayed card came from

    // ===============
    // FIGHT
    // ===============

    IEnumerator Fight(Card attackingCard, Card target)
    {
        fighting = true;
        GameManager gm = GameManager.instance;
        ExecuteManager executeManager = gm.executeManager;
        gm.managerUI.NewHint("");

        // a guard next to the target takes the attack instead
        Card guard = FindGuard(target);
        if (guard != null)
        {
            yield return StickerMessage<GuardNeighbourEffect>(guard, guard.cardName + " guards " + target.cardName + "!");
            target = guard;
        }

        // Joker swaps the attacker for another random card of the attacking player (the old one goes back to its spot)
        List<Card> otherAttackers = attackingCard.player.cardsOnField.FindAll(card => card != attackingCard);
        Card joker = (otherAttackers.Count > 0) ? FindSwappingJoker(target) : null;
        if (joker != null)
        {
            yield return Reveal(joker);
            yield return StickerMessage<SwapAttackerEffect>(joker, joker.cardName + " swaps the attacker!");
            attacker.RemoveFromBoard();
            attackingCard.currentPower = 0;
            StartCoroutine(ReturnCard(attackingCard));
            attackingCard = otherAttackers[GameRandom.Range(0, otherAttackers.Count)];
            yield return Reveal(attackingCard);
            yield return BringOut(attackingCard, true, unit => attacker = unit);
        }

        // Seer: a right guess of a face down target gives the attacker power
        int guessPower = 0;
        if (target.faceDown) yield return GuessTarget(attackingCard.player, target, amount => guessPower = amount);

        // both cards are revealed, the defender comes out
        yield return Reveal(attackingCard);
        yield return Reveal(target);
        yield return BringOut(target, false, unit => defender = unit);
        yield return Animations.instance.SkippablePause(afterSpawnPause);

        // both cards roll a d6 for their base power at the same time, stickers only give power after both dice landed
        yield return RollBasePower();

        // stickers give power (attacker's first, then the defender's)
        if (guessPower > 0) yield return AddPower(attacker, guessPower);
        QueueSideCardPower(attacker);
        QueueSideCardPower(defender);
        executeManager.abilityRunner.QueueFightTrigger(EffectTrigger.OnFightStart);
        yield return executeManager.TriggerEffects();
        yield return UseSavedPowerUps(attacker);
        yield return UseSavedPowerUps(defender);
        yield return Animations.instance.SkippablePause(beforeResultPause);

        // stronger one knocks the other out, the attacker wins a tie
        int attackerPower = attacker.card.currentPower;
        int defenderPower = defender.card.currentPower;
        Debug.Log("FightManager: " + attacker.card.cardName + " attacks " + defender.card.cardName + " with " + attackerPower + " vs " + defenderPower + " power");
        Unit winner = (attackerPower >= defenderPower) ? attacker : defender;
        Unit loser = (winner == attacker) ? defender : attacker;
        Card attackerCard = attacker.card;
        Card defenderCard = defender.card;
        Card loserCard = loser.card;

        // the power counters show who is stronger (the attacker on a tie), then both units lift up and fly at each other
        StartCoroutine(loser.ShowFightResult(false));
        yield return winner.ShowFightResult(true);
        yield return ClashUnits(attacker, defender);

        StartCoroutine(RecoilUnit(winner));
        yield return KnockOutOrRecoil(loser);
        winner.card.player.lostLastFight = false;
        loserCard.player.lostLastFight = true;

        // Beast destroys the card it beat for good (only when it attacks)
        if (winner == attacker) yield return DestroyBeatenCard(winner.card, loserCard);

        // winner's side cheers
        foreach (Card card in winner.card.player.cardsOnField) card.Cheer();

        // after fight stickers of the units still standing (knock itself out, hide, power-up for the next card)
        executeManager.abilityRunner.QueueFightTrigger(EffectTrigger.OnFightEnd);
        yield return executeManager.TriggerEffects();

        // cards that destroy themselves after a fight
        DiscardSelfIfSticker(attackerCard);
        DiscardSelfIfSticker(defenderCard);

        // whoever is still standing goes back to its place on the field (not if it knocked itself out or was destroyed)
        if (winner != null && !winner.knockedOut && winner.card.player.cardsOnField.Contains(winner.card)) ReturnToField(winner);
        if (loser != null && !loser.knockedOut && loser.card.player.cardsOnField.Contains(loser.card)) ReturnToField(loser);
        StartCoroutine(ReturnCard(attackerCard));
        StartCoroutine(ReturnCard(defenderCard));

        // Phantom: copies of knocked out cards go away, the owner picks again which one is the phantom
        yield return UpdatePhantom(attackerCard);
        yield return UpdatePhantom(defenderCard);

        yield return Animations.instance.SkippablePause(afterFightPause);

        attacker = null;
        defender = null;
        fighting = false;

        // round is over when someone has no cards left, otherwise the turn ends (Scouts reveal a card) and the other player attacks
        if (gm.player.cardsOnField.Count == 0 || gm.opponent.cardsOnField.Count == 0) gm.battleManager.EndBattle();
        else
        {
            yield return ScoutReveals(attackingCard.player);
            attackingCard.player.EndTurn();
        }
    }

    /// <summary>
    /// Both fighting units roll their base power die, each player clicks to stop their own (bot stops by itself)
    /// </summary>
    IEnumerator RollBasePower()
    {
        // results are rolled up front (attacker's dice, then the defender's), so online both games get the same numbers whichever die stops first
        int[] attackerRolls = RollBaseDice(attacker);
        int[] defenderRolls = RollBaseDice(defender);

        GameManager.instance.managerUI.NewHint("Click to roll your power!");
        Coroutine attackerDie = StartCoroutine(attacker.RollBasePower(attackerRolls));
        Coroutine defenderDie = StartCoroutine(defender.RollBasePower(defenderRolls));
        yield return attackerDie;
        yield return defenderDie;
        GameManager.instance.managerUI.NewHint("");
    }

    /// <summary>
    /// d6 results for the unit's base power dice (special cards roll 2)
    /// </summary>
    int[] RollBaseDice(Unit unit)
    {
        int[] results = new int[unit.BaseDiceCount];
        for (int i = 0; i < results.Length; i++) results[i] = GameRandom.Range(1, 7);
        return results;
    }

    /// <summary>
    /// Stickers call this when they give power: the gain shows on the power counter, then a short pause
    /// </summary>
    public IEnumerator AddPower(Unit unit, int amount)
    {
        if (unit == null || amount == 0) yield break;
        unit.card.GainPower(amount);
        yield return Animations.instance.SkippablePause(powerGainPause);
    }

    /// <summary>
    /// The unit fighting against this one
    /// </summary>
    public Unit GetOpposing(Unit unit) => (unit == attacker) ? defender : attacker;

    // ===============
    // FIELD POSITIONS
    // ===============

    /// <summary>
    /// Card next to this one on its field, side -1 = left, 1 = right. Null if there is none
    /// </summary>
    public Card GetNeighbour(Card card, int side)
    {
        List<Card> field = card.player.cardsOnField;
        int index = field.IndexOf(card) + side;
        return (index >= 0 && index < field.Count && field.IndexOf(card) >= 0) ? field[index] : null;
    }

    /// <summary>
    /// Leftmost or rightmost card of its field
    /// </summary>
    public bool IsSideCard(Card card)
    {
        List<Card> field = card.player.cardsOnField;
        int index = field.IndexOf(card);
        return index == 0 || index == field.Count - 1;
    }

    /// <summary>
    /// Guard right of the target protects it (a guard always guards the card on its left), or null
    /// </summary>
    Card FindGuard(Card target)
    {
        Card neighbour = GetNeighbour(target, 1);
        return (neighbour != null && neighbour.HasEffect<GuardNeighbourEffect>()) ? neighbour : null;
    }

    /// <summary>
    /// A fighting card on the side of its field gets power from every Cheerer on that field (itself included)
    /// </summary>
    void QueueSideCardPower(Unit unit)
    {
        if (!IsSideCard(unit.card)) return;

        foreach (Card card in unit.card.player.cardsOnField)
        {
            foreach ((Ability strip, NeighbourPowerUpEffect effect) in card.GetEffects<NeighbourPowerUpEffect>())
            {
                GameManager.instance.executeManager.abilityRunner.QueueOnUnit(unit, strip, effect);
            }
        }
    }

    /// <summary>
    /// Joker of the attacked player that swaps the attacker, or null. A Joker that is the target always swaps,
    /// every other Joker on the field swaps with its chance (GameRandom, so online both games roll the same)
    /// </summary>
    Card FindSwappingJoker(Card target)
    {
        if (target.HasEffect<SwapAttackerEffect>()) return target;

        foreach (Card card in target.player.cardsOnField)
        {
            foreach ((Ability strip, SwapAttackerEffect effect) in card.GetEffects<SwapAttackerEffect>())
            {
                if (GameRandom.Range(0, 100) < effect.chancePercent) return card;
            }
        }
        return null;
    }

    /// <summary>
    /// End of a turn: every Scout of the player whose turn it was reveals a random face down card of the opponent
    /// (field order, so online both games reveal the same cards)
    /// </summary>
    IEnumerator ScoutReveals(Player turnPlayer)
    {
        Player opponent = GameManager.instance.GetOpponentOfPlayer(turnPlayer);
        foreach (Card scout in new List<Card>(turnPlayer.cardsOnField))
        {
            foreach ((Ability strip, RevealRandomOnTurnEndEffect effect) in scout.GetEffects<RevealRandomOnTurnEndEffect>())
            {
                List<Card> hidden = opponent.cardsOnField.FindAll(card => card.faceDown);
                if (hidden.Count == 0) continue;

                Card revealed = hidden[GameRandom.Range(0, hidden.Count)];
                EffectNotifier notifier = GameManager.instance.managerUI.effectNotifier;
                yield return notifier.Show(strip, "Revealing a card");
                Animations.instance.PopAnim(scout.gameObject, 0.3f, 0.25f);
                Animations.instance.PopAnim(revealed.gameObject, 0.3f, 0.3f);
                yield return Reveal(revealed);
                yield return ShowMessage(scout.cardName + " scouts " + revealed.cardName + "!");
                notifier.Hide();
            }
        }
    }

    // ===============
    // STICKER ACTIONS
    // ===============

    /// <summary>
    /// Turns the card face up. Mixer stickers mix up the opponent's field when they get revealed
    /// </summary>
    public IEnumerator Reveal(Card card)
    {
        // the opponent knows this card from now on (Seer guesses)
        GameManager.instance.GetOpponentOfPlayer(card.player).seenCards.Add(card.cardData);

        if (!card.faceDown) yield break;
        card.SetFaceDown(false);

        // Phantom: revealing one of the pair reveals the other one too
        if (card.twin != null && card.twin.faceDown) yield return Reveal(card.twin);

        if (!card.HasEffect<ShuffleOpponentOnRevealEffect>()) yield break;
        EffectNotifier notifier = GameManager.instance.managerUI.effectNotifier;
        yield return notifier.Show(card.GetEffects<ShuffleOpponentOnRevealEffect>()[0].strip);

        // shuffled with GameRandom, so online both games get the same order
        List<Card> field = GameManager.instance.GetOpponentOfPlayer(card.player).cardsOnField;
        for (int i = field.Count - 1; i > 0; i--)
        {
            int j = GameRandom.Range(0, i + 1);
            (field[i], field[j]) = (field[j], field[i]);
        }
        GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.GetOpponentOfPlayer(card.player));
        AudioManager.instance.PlaySFX("RerollSFX");
        yield return ShowMessage(card.cardName + " mixes up the cards!");
        notifier.Hide();
    }

    /// <summary>
    /// Scout: cards next to the target are revealed
    /// </summary>
    public IEnumerator RevealNeighbours(Card target)
    {
        foreach (int side in new int[] { -1, 1 })
        {
            Card neighbour = GetNeighbour(target, side);
            if (neighbour == null) continue;

            Animations.instance.PopAnim(neighbour.gameObject, 0.3f, 0.3f);
            yield return Reveal(neighbour);
        }
    }

    /// <summary>
    /// Momentum: power-ups saved for this player's next fighting card
    /// </summary>
    IEnumerator UseSavedPowerUps(Unit unit)
    {
        Player owner = unit.card.player;
        if (owner.nextFightPowerUps <= 0) yield break;

        int saved = owner.nextFightPowerUps;
        owner.nextFightPowerUps = 0;
        EffectContext ctx = new EffectContext { self = unit };
        yield return EffectPresenter.Show(ctx, "Momentum!");
        yield return AddPower(unit, saved);
        yield return EffectPresenter.Hide(ctx);
    }

    /// <summary>
    /// Sneaky: the card turns face down and can swap places with another face down card of its owner
    /// </summary>
    public IEnumerator HideAndSwap(Card card)
    {
        card.SetFaceDown(true);

        List<Card> hidden = card.player.cardsOnField.FindAll(other => other != card && other.faceDown);
        if (hidden.Count == 0) yield break;

        // the bot swaps half of the time
        yield return ChooseCard(card.player, hidden, "Click a face down card to swap places with (right click to stay)", true);
        if (chosenCard == null) yield break;

        List<Card> field = card.player.cardsOnField;
        int a = field.IndexOf(card), b = field.IndexOf(chosenCard);
        (field[a], field[b]) = (field[b], field[a]);
        GameManager.instance.handManager.UpdateHandVisuals(card.player);
        AudioManager.instance.PlaySFX("UnitMoveSFX");
    }

    /// <summary>
    /// The player picks one of the cards, the result is in chosenCard (null = skipped).
    /// Local player clicks (right click skips if allowed), the bot picks at random (skips half of the time if allowed)
    /// </summary>
    IEnumerator ChooseCard(Player chooser, List<Card> options, string hint, bool canSkip)
    {
        chosenCard = null;
        if (options.Count == 0) yield break;

        // bot picks at random (skips half of the time if it can)
        if (chooser.isAI)
        {
            if (!canSkip || Random.value < 0.5f) chosenCard = options[Random.Range(0, options.Count)];
            yield break;
        }

        // local player clicks, online the pick comes back as a Choice command (also the other player's pick)
        pendingOptions = options;
        choiceAnswered = false;
        choiceSent = false;
        bool local = chooser == GameManager.instance.player;
        if (local)
        {
            choiceOptions = options;
            foreach (Card option in options) option.HighlightCard(true);
        }
        GameManager.instance.managerUI.NewHint(local ? hint : chooser.playerName + " is choosing...");

        while (!choiceAnswered)
        {
            if (local && canSkip && Input.GetMouseButtonDown(1) && Card.hoveredCard == null) PickChoice(-1);
            yield return null;
        }

        foreach (Card option in options) if (option != null) option.HighlightCard(false);
        choiceOptions = null;
        pendingOptions = null;
        GameManager.instance.managerUI.NewHint("");
    }

    List<Card> pendingOptions; // options of the current question (local or the other player's)
    bool choiceAnswered;
    bool choiceSent;

    /// <summary>
    /// Online: a Choice command can only be used while this game is asking the question too
    /// </summary>
    public bool WaitingForChoice => pendingOptions != null && !choiceAnswered;

    /// <summary>
    /// Local player picked an option (-1 = skipped). Online it goes to the server first
    /// </summary>
    void PickChoice(int index)
    {
        if (choiceSent) return;

        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            choiceSent = true;
            MatchNetwork.instance.SendChoice(index);
            return;
        }
        AnswerChoice(index);
    }

    /// <summary>
    /// The pick is in (offline right away, online when the Choice command comes)
    /// </summary>
    public void AnswerChoice(int index)
    {
        if (pendingOptions == null) return;
        chosenCard = (index >= 0 && index < pendingOptions.Count) ? pendingOptions[index] : null;
        choiceAnswered = true;
    }

    // ===============
    // SPECIALS
    // ===============

    /// <summary>
    /// Start of the battle: Phantoms get their copies, then Dragons knock out the cards in their position, then the first attack
    /// </summary>
    public void StartBattle()
    {
        StartCoroutine(StartBattleSequence());
    }

    IEnumerator StartBattleSequence()
    {
        GameManager gm = GameManager.instance;
        Player first = gm.firstPlayer;
        Player second = gm.GetOpponentOfPlayer(first);

        // Phantom copies appear in a random spot (GameRandom, same on both games online)
        foreach (Player p in new Player[] { first, second })
        {
            foreach (Card card in new List<Card>(p.cardsOnField))
            {
                if (card.HasEffect<PhantomCopyEffect>() && !card.isPhantom) yield return CreatePhantom(card);
            }
        }

        // Dragons: targets are picked first, then knocked out one after another (two Dragons can knock each other out)
        List<(Card dragon, Card target)> dragonHits = new List<(Card, Card)>();
        foreach (Player p in new Player[] { first, second })
        {
            List<Card> enemyField = gm.GetOpponentOfPlayer(p).cardsOnField;
            foreach (Card card in p.cardsOnField)
            {
                if (!card.HasEffect<KnockOutSamePositionEffect>() || enemyField.Count == 0) continue;

                // same position, or the closest one (the rightmost) if the opponent has fewer cards
                int position = Mathf.Min(p.cardsOnField.IndexOf(card), enemyField.Count - 1);
                dragonHits.Add((card, enemyField[position]));
            }
        }
        foreach ((Card dragon, Card target) in dragonHits)
        {
            yield return Reveal(dragon);
            yield return StickerMessage<KnockOutSamePositionEffect>(dragon, dragon.cardName + " breathes fire on " + target.cardName + "!");
            if (target.player.cardsOnField.Contains(target)) yield return KnockOutCard(target);
        }

        // a Dragon can end the round before it starts
        if (gm.player.cardsOnField.Count == 0 || gm.opponent.cardsOnField.Count == 0) gm.battleManager.EndBattle();
        else gm.StartFirstAttack();
    }

    /// <summary>
    /// Phantom: a copy of the card appears in a random spot on its field. It starts as the phantom
    /// </summary>
    IEnumerator CreatePhantom(Card card)
    {
        Player owner = card.player;
        EffectNotifier notifier = GameManager.instance.managerUI.effectNotifier;
        yield return notifier.Show(card.GetEffects<PhantomCopyEffect>()[0].strip);

        Card copy = GameManager.instance.cardGenerator.CreateLooseCard(card.cardData, owner, card.transform.parent);
        copy.isPhantom = true;
        copy.twin = card;
        card.twin = copy;
        copy.SetFaceDown(card.faceDown);

        owner.cardsOnField.Insert(GameRandom.Range(0, owner.cardsOnField.Count + 1), copy);
        GameManager.instance.handManager.UpdateHandVisuals(owner);
        Animations.instance.PopAnim(copy.gameObject, 0.4f, 0.3f);
        AudioManager.instance.PlaySFX("CardPlaySFX");
        yield return ShowMessage(owner.playerName + "'s Phantom appears!");
        notifier.Hide();
    }

    /// <summary>
    /// After a fight: a knocked out phantom just disappears, a knocked out main card takes its phantom with it.
    /// If both are still on the field they are revealed and the owner picks again which one is the phantom
    /// </summary>
    IEnumerator UpdatePhantom(Card card)
    {
        if (card == null || card.twin == null) yield break;

        Card twin = card.twin;
        bool onField = card.player.cardsOnField.Contains(card);
        if (!onField)
        {
            RemovePhantom(card.isPhantom ? card : twin);
            yield break;
        }
        if (!twin.player.cardsOnField.Contains(twin)) yield break;

        yield return Reveal(twin);
        yield return ChooseCard(card.player, new List<Card> { card, twin }, "Click the card that should be the phantom", false);

        Card phantom = (chosenCard != null) ? chosenCard : card;
        Card main = phantom.twin;
        phantom.isPhantom = true;
        main.isPhantom = false;
        phantom.Refresh();
        main.Refresh();
    }

    /// <summary>
    /// Phantom copy leaves the game (it never goes to the discard)
    /// </summary>
    void RemovePhantom(Card phantom)
    {
        if (phantom == null) return;

        Player owner = phantom.player;
        owner.cardsOnField.Remove(phantom);
        owner.cardsInDiscard.Remove(phantom);
        if (phantom.twin != null) phantom.twin.twin = null;
        phantom.twin = null;
        Destroy(phantom.gameObject);
        GameManager.instance.handManager.UpdateHandVisuals(owner);
    }

    /// <summary>
    /// End of the round: phantoms disappear before damage is counted
    /// </summary>
    public void RemoveAllPhantoms()
    {
        foreach (Player p in new Player[] { GameManager.instance.player, GameManager.instance.opponent })
        {
            foreach (Card card in new List<Card>(p.cardsOnField)) if (card.isPhantom) RemovePhantom(card);
            foreach (Card card in new List<Card>(p.cardsInDiscard)) if (card.isPhantom) RemovePhantom(card);
        }
    }

    /// <summary>
    /// Knocks a field card out without a fight (Dragon)
    /// </summary>
    IEnumerator KnockOutCard(Card card)
    {
        Animations.instance.ShakeAnim(card.gameObject, 12f, 0.4f);
        AudioManager.instance.PlaySFX("HitSFX");
        yield return Animations.instance.SkippablePause(0.4f);

        if (card.isPhantom) { RemovePhantom(card); yield break; }
        if (card.twin != null) RemovePhantom(card.twin);
        GameManager.instance.handManager.AddCardToDiscarded(card, card.player);
    }

    /// <summary>
    /// Beast: the card it beat is destroyed for good (Lucky loses its sticker instead), its owner gets a star in the workshop
    /// </summary>
    IEnumerator DestroyBeatenCard(Card winner, Card loser)
    {
        if (!winner.HasEffect<DestroyOnWinEffect>() || loser.isPhantom) yield break;

        Sticker lucky = loser.GetStickerWith<ProtectFromDiscardEffect>();
        if (lucky != null)
        {
            yield return StickerMessage<ProtectFromDiscardEffect>(loser, loser.cardName + " is lucky and loses its Lucky sticker instead!");
            loser.RemoveSticker(lucky);
            yield break;
        }

        yield return StickerMessage<DestroyOnWinEffect>(winner, winner.cardName + " destroys " + loser.cardName + "!");
        winner.destroyedCardThisRound = true;
        loser.player.compensationStars++;
        if (loser.twin != null) RemovePhantom(loser.twin);
        loser.DestroyCard();
    }

    /// <summary>
    /// Seer: if the attacking player has a Seer on the field, they guess the face down target from the opponent cards they have seen
    /// </summary>
    IEnumerator GuessTarget(Player guesser, Card target, System.Action<int> setPowerUps)
    {
        List<(Ability strip, GuessTargetEffect effect)> seers = new List<(Ability, GuessTargetEffect)>();
        foreach (Card card in guesser.cardsOnField) seers.AddRange(card.GetEffects<GuessTargetEffect>());
        if (seers.Count == 0) yield break;

        // every known card once (a phantom and its card are the same card)
        List<CreatureObj> known = new List<CreatureObj>();
        foreach (Card card in target.player.cardsOnField) if (guesser.seenCards.Contains(card.cardData) && !known.Contains(card.cardData)) known.Add(card.cardData);
        foreach (Card card in target.player.cardsInDiscard) if (guesser.seenCards.Contains(card.cardData) && !known.Contains(card.cardData)) known.Add(card.cardData);
        if (known.Count == 0) yield break;

        EffectNotifier notifier = GameManager.instance.managerUI.effectNotifier;
        yield return notifier.Show(seers[0].strip);

        // the options are shown as cards (only to the guessing player, online the other game waits for the pick)
        guessPanel.SetActive(guesser == GameManager.instance.player);
        List<Card> options = new List<Card>();
        foreach (CreatureObj cardData in known) options.Add(GameManager.instance.cardGenerator.CreateLooseCard(cardData, target.player, guessRow));
        yield return ChooseCard(guesser, options, "Seer: guess which card it is!", false);
        CreatureObj guess = (chosenCard != null) ? chosenCard.cardData : null;
        foreach (Card option in options) Destroy(option.gameObject);
        guessPanel.SetActive(false);

        if (guess == target.cardData)
        {
            setPowerUps(seers[0].effect.amount);
            yield return ShowMessage("Right guess! Power-up!");
        }
        else yield return ShowMessage("Wrong guess!");
        notifier.Hide();
    }

    /// <summary>
    /// Bomb: the card is destroyed for good after its fight
    /// </summary>
    void DiscardSelfIfSticker(Card card)
    {
        if (card == null || !card.HasEffect<DiscardSelfAfterFightEffect>()) return;

        if (card.unit != null && !card.unit.knockedOut) card.unit.RemoveFromBoard();
        Player owner = card.player;
        card.DestroyCard();
        GameManager.instance.handManager.UpdateHandVisuals(owner);
    }

    /// <summary>
    /// A sticker of a card that isn't fighting does something: the notifier shows the card and the sticker first, then the message
    /// </summary>
    IEnumerator StickerMessage<T>(Card card, string message) where T : AbilityEffect
    {
        EffectNotifier notifier = GameManager.instance.managerUI.effectNotifier;
        List<(Ability strip, T effect)> effects = card.GetEffects<T>();
        if (effects.Count > 0) yield return notifier.Show(effects[0].strip);
        yield return ShowMessage(message);
        notifier.Hide();
    }

    /// <summary>
    /// Text on the hint line for a moment
    /// </summary>
    IEnumerator ShowMessage(string message)
    {
        GameManager.instance.managerUI.NewHint(message);
        yield return Animations.instance.SkippablePause(stickerMessagePause);
        GameManager.instance.managerUI.NewHint("");
    }

    // ===============
    // ANIMATION
    // ===============

    /// <summary>
    /// Both fighting units lift into the air, pull back and fly at each other. Ends at the hit
    /// </summary>
    IEnumerator ClashUnits(Unit first, Unit second)
    {
        clashRest.Clear();
        first.PinPowerCounter();
        second.PinPowerCounter();
        Transform a = first.transform;
        Transform b = second.transform;
        Vector3 aRest = a.position, bRest = b.position;
        Vector3 aRestScale = a.localScale, bRestScale = b.localScale;
        clashRest[first] = (aRest, aRestScale);
        clashRest[second] = (bRest, bRestScale);
        Vector3 lift = Vector3.up * clashLiftHeight;
        Vector3 aToB = (bRest - aRest).normalized;

        // lifting up
        float t = 0;
        while (t < clashLiftTime)
        {
            t += Time.deltaTime;
            float coolT = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / clashLiftTime), 3);
            a.position = aRest + lift * coolT;
            b.position = bRest + lift * coolT;
            a.localScale = Vector3.Lerp(aRestScale, aRestScale * clashLiftScale, coolT);
            b.localScale = Vector3.Lerp(bRestScale, bRestScale * clashLiftScale, coolT);
            yield return null;
        }

        // pulling back: squashing down and shaking harder and harder, like a spring getting loaded
        Vector3 aLifted = aRest + lift, bLifted = bRest + lift;
        Vector3 aLiftedScale = aRestScale * clashLiftScale, bLiftedScale = bRestScale * clashLiftScale;
        Vector3 aBack = aLifted - aToB * clashWindupDistance, bBack = bLifted + aToB * clashWindupDistance;
        t = 0;
        while (t < clashWindupTime + clashWindupHold)
        {
            t += Time.deltaTime;
            float pullT = Mathf.Clamp01(t / clashWindupTime);
            float coolT = 1 - Mathf.Pow(1 - pullT, 3);
            float tremble = clashWindupTremble * Mathf.Clamp01(t / (clashWindupTime + clashWindupHold));
            a.position = Vector3.Lerp(aLifted, aBack, coolT) + (Vector3)(Random.insideUnitCircle * tremble);
            b.position = Vector3.Lerp(bLifted, bBack, coolT) + (Vector3)(Random.insideUnitCircle * tremble);
            Animations.instance.SetSquash(a, aLiftedScale, clashWindupSquash * coolT);
            Animations.instance.SetSquash(b, bLiftedScale, clashWindupSquash * coolT);
            yield return null;
        }

        // letting go: flying at each other, stretched out and speeding up until they hit in the middle
        Vector3 middle = (aLifted + bLifted) / 2f;
        Vector3 aHit = middle - aToB * clashHitGap / 2f;
        Vector3 bHit = middle + aToB * clashHitGap / 2f;
        t = 0;
        while (t < clashFlyTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / clashFlyTime);
            float coolT = clampedT * clampedT * clampedT;
            a.position = Vector3.Lerp(aBack, aHit, coolT);
            b.position = Vector3.Lerp(bBack, bHit, coolT);
            // wide and flat towards the flying direction
            float stretch = Mathf.Lerp(clashWindupSquash, clashFlyStretch, Mathf.Clamp01(clampedT * 3f));
            Animations.instance.SetSquash(a, aLiftedScale, stretch);
            Animations.instance.SetSquash(b, bLiftedScale, stretch);
            yield return null;
        }

        // hit: both squash flat, burst, shake, sound and a split second freeze
        a.position = aHit;
        b.position = bHit;
        Animations.instance.SetSquash(a, aLiftedScale, -clashHitSquash);
        Animations.instance.SetSquash(b, bLiftedScale, -clashHitSquash);
        ParticleManager.instance.SpawnVFX(middle, "HitVFX");
        GameManager.instance.mainCamera.Shake(0.06f, 0.3f);
        AudioManager.instance.PlaySFX("HitSFX");
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(clashHitStop);
        Time.timeScale = 1f;
        // wobbling back into shape (done before the winner starts going back to its spot)
        float wobbleTime = winnerRecoilTime;
        StartCoroutine(Animations.instance.SquashStretch(a, aLiftedScale, -clashHitSquash, wobbleTime));
        StartCoroutine(Animations.instance.SquashStretch(b, bLiftedScale, -clashHitSquash, wobbleTime));
    }

    /// <summary>
    /// Winner gets knocked back from the hit in a little hop and stays there (still big from the clash)
    /// </summary>
    IEnumerator RecoilUnit(Unit unit)
    {
        if (unit == null || !clashRest.TryGetValue(unit, out (Vector3 position, Vector3 scale) rest)) yield break;
        Transform unitTransform = unit.transform;
        Vector3 startPos = unitTransform.position;
        Vector3 away = rest.position - startPos;
        away.y = 0f;
        Vector3 recoilPos = startPos + away.normalized * winnerRecoilDistance;

        float t = 0;
        while (t < winnerRecoilTime && unit != null)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / winnerRecoilTime);
            float coolT = 1 - Mathf.Pow(1 - clampedT, 3);
            unitTransform.position = Vector3.Lerp(startPos, recoilPos, coolT) + Vector3.up * winnerRecoilHop * Mathf.Sin(clampedT * Mathf.PI);
            yield return null;
        }
        if (unit != null) unitTransform.position = recoilPos;
    }

    readonly Dictionary<Unit, (Vector3 position, Vector3 scale)> clashRest = new Dictionary<Unit, (Vector3, Vector3)>(); // where each fighting unit stood before the clash

    /// <summary>
    /// Losing unit gets knocked out from the hit (it flies away), if a sticker saves it, it recoils like a winner
    /// </summary>
    IEnumerator KnockOutOrRecoil(Unit unit)
    {
        yield return unit.KnockOut();
        if (unit != null && !unit.knockedOut) yield return RecoilUnit(unit);
    }

    /// <summary>
    /// Fighting unit leaves, its card goes back to its place on the field
    /// </summary>
    void ReturnToField(Unit unit)
    {
        Card card = unit.card;
        unit.RemoveFromBoard();
        card.currentPower = 0;
        card.auraPower = 0;
    }
}
