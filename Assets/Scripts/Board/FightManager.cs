using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battle phase: players take turns attacking one opponent card with one of their own.
/// Attack: card dragged up -> unit spawns -> arrow -> target picked -> both revealed -> power-ups rolled -> stronger one wins
/// </summary>
public class FightManager : MonoBehaviour
{
    public static FightManager instance;

    [Header("refs")]
    [SerializeField] Transform playerSpot; // where the local player's fighting unit stands
    [SerializeField] Transform opponentSpot;
    [SerializeField] TargetArrow targetArrow;

    [Header("input")]
    [SerializeField] float attackDropHeight = 0.35f; // card has to be dropped above this part of the screen to attack

    [Header("timing")]
    [SerializeField] float afterSpawnPause = 0.6f;
    [SerializeField] float beforeRollPause = 0.4f;
    [SerializeField] float beforeResultPause = 0.6f;
    [SerializeField] float dashTime = 0.18f;
    [SerializeField] float dashReturnTime = 0.3f;
    [SerializeField] float afterFightPause = 0.8f;

    // current fight
    [HideInInspector] public Unit attacker;
    [HideInInspector] public Unit defender;
    readonly Dictionary<Unit, int> powerUps = new Dictionary<Unit, int>();
    Card choosingFor; // local player's card waiting for a target
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
        return gm.currentState == GameState.BATTLING && !fighting && choosingFor == null
            && (p == gm.player) == gm.yourTurn && !gm.gameOver;
    }

    /// <summary>
    /// Local player dropped a field card: attacks if it was dragged high enough. Returns false if nothing happened
    /// </summary>
    public bool TryStartAttack(Card card)
    {
        if (!CanAttack(card.player) || Input.mousePosition.y < Screen.height * attackDropHeight) return false;

        choosingFor = card;
        attacker = SpawnFighter(card);
        targetArrow.Show(attacker.transform);
        GameManager.instance.managerUI.NewHint("Choose an opponent card to attack (right click to cancel)");
        return true;
    }

    public bool IsTarget(Card card) => choosingFor != null && card.player != choosingFor.player && card.player.cardsOnField.Contains(card);

    /// <summary>
    /// Local player clicked a card while choosing a target
    /// </summary>
    public bool TryPickTarget(Card card)
    {
        if (!IsTarget(card)) return false;

        card.HighlightCard(false);
        Card attackingCard = choosingFor;
        choosingFor = null;
        targetArrow.Hide();

        StartCoroutine(Fight(attackingCard, card));
        return true;
    }

    void Update()
    {
        // right click puts the attacker back
        if (choosingFor != null && Input.GetMouseButtonDown(1)) CancelAttack();
    }

    void CancelAttack()
    {
        targetArrow.Hide();
        attacker.RemoveFromBoard();
        attacker = null;

        Card card = choosingFor;
        choosingFor = null;
        card.gameObject.SetActive(true);
        GameManager.instance.handManager.UpdateHandVisuals(card.player);
        GameManager.instance.managerUI.NewHint(GameManager.instance.GetState().defaultHintText);
    }

    /// <summary>
    /// Bot (and later the online opponent) attacks straight away
    /// </summary>
    public void Attack(Card attackingCard, Card target)
    {
        attacker = SpawnFighter(attackingCard);
        StartCoroutine(Fight(attackingCard, target));
    }

    /// <summary>
    /// Card leaves its place on the field (the place waits for it) and its unit comes out
    /// </summary>
    Unit SpawnFighter(Card card)
    {
        Transform spot = (card.player == GameManager.instance.player) ? playerSpot : opponentSpot;
        card.gameObject.SetActive(false);
        Unit unit = GameManager.instance.fieldManager.SpawnFightUnit(card, spot.position);
        card.GetStartingPower();
        return unit;
    }

    // ===============
    // FIGHT
    // ===============

    IEnumerator Fight(Card attackingCard, Card target)
    {
        fighting = true;
        GameManager gm = GameManager.instance;
        gm.managerUI.NewHint("");

        // both cards are revealed, the defender comes out
        attackingCard.SetFaceDown(false);
        target.SetFaceDown(false);
        defender = SpawnFighter(target);
        AudioManager.instance.PlaySFX("NextCardSFX");
        yield return Animations.instance.SkippablePause(afterSpawnPause);

        // stickers decide how many power-ups each one gets
        powerUps.Clear();
        powerUps[attacker] = 0;
        powerUps[defender] = 0;
        gm.BroadcastOnFightStart();
        yield return gm.executeManager.TriggerEffects();

        attacker.ShowPowerUpDice(powerUps[attacker]);
        defender.ShowPowerUpDice(powerUps[defender]);
        yield return Animations.instance.SkippablePause(beforeRollPause);

        // attacker rolls first, then the defender
        if (powerUps[attacker] > 0) gm.managerUI.NewHint(RollHint(attacker));
        yield return attacker.RollPowerUps(powerUps[attacker]);
        if (powerUps[defender] > 0) gm.managerUI.NewHint(RollHint(defender));
        yield return defender.RollPowerUps(powerUps[defender]);
        gm.managerUI.NewHint("");
        yield return Animations.instance.SkippablePause(beforeResultPause);

        // stronger one knocks the other out, on a tie both are knocked out
        int attackerPower = attacker.card.currentPower;
        int defenderPower = defender.card.currentPower;
        Unit winner = (attackerPower > defenderPower) ? attacker : (defenderPower > attackerPower) ? defender : null;

        if (winner == null)
        {
            StartCoroutine(Dash(attacker, defender));
            yield return Dash(defender, attacker);
            StartCoroutine(attacker.KnockOut());
            yield return defender.KnockOut();
        }
        else
        {
            Unit loser = (winner == attacker) ? defender : attacker;
            yield return Dash(winner, loser);
            yield return loser.KnockOut();

            // winner's side cheers, winner goes back to the field face up
            foreach (Card card in winner.card.player.cardsOnField) card.Cheer();
            ReturnToField(winner);
        }

        yield return Animations.instance.SkippablePause(afterFightPause);

        attacker = null;
        defender = null;
        fighting = false;

        // round is over when someone has no cards left, otherwise the other player attacks
        if (gm.player.cardsOnField.Count == 0 || gm.opponent.cardsOnField.Count == 0) gm.battleManager.EndBattle();
        else attackingCard.player.EndTurn();
    }

    string RollHint(Unit unit)
    {
        return (unit.card.player == GameManager.instance.player) ? "Click to roll your power-ups!" : unit.card.player.playerName + " is rolling...";
    }

    /// <summary>
    /// Stickers call this when they give power-ups
    /// </summary>
    public void AddPowerUps(Unit unit, int amount)
    {
        if (!powerUps.ContainsKey(unit)) return;
        powerUps[unit] += amount;
    }

    /// <summary>
    /// Unit rushes at the other one and comes back
    /// </summary>
    IEnumerator Dash(Unit unit, Unit target)
    {
        Vector3 startPos = unit.transform.position;
        Vector3 hitPos = Vector3.Lerp(startPos, target.transform.position, 0.75f);

        float t = 0;
        while (t < dashTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / dashTime);
            unit.transform.position = Vector3.Lerp(startPos, hitPos, clampedT * clampedT);
            yield return null;
        }

        GameManager.instance.mainCamera.Shake(0.02f, 0.2f);
        StartCoroutine(DashBack(unit, hitPos, startPos));
    }

    IEnumerator DashBack(Unit unit, Vector3 from, Vector3 to)
    {
        float t = 0;
        while (t < dashReturnTime && unit != null)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / dashReturnTime);
            unit.transform.position = Vector3.Lerp(from, to, 1 - (1 - clampedT) * (1 - clampedT));
            yield return null;
        }
    }

    /// <summary>
    /// Fighting unit leaves, its card goes back to its place on the field (face up)
    /// </summary>
    void ReturnToField(Unit unit)
    {
        Card card = unit.card;
        unit.RemoveFromBoard();
        card.currentPower = 0;
        card.auraPower = 0;
        card.gameObject.SetActive(true);
        GameManager.instance.handManager.UpdateHandVisuals(card.player);
    }
}
