using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Listens to game events and queues matching effects of units on the board into the ExecuteManager effect stacks.
/// Owned by ExecuteManager, effects never subscribe to events themselves
/// </summary>
public class AbilityRunner
{
    readonly ExecuteManager executeManager;
    readonly Dictionary<(Unit, AbilityEffect), Dictionary<string, int>> memories = new Dictionary<(Unit, AbilityEffect), Dictionary<string, int>>();
    readonly HashSet<(Unit, AbilityEffect)> usedThisRound = new HashSet<(Unit, AbilityEffect)>();

    public AbilityRunner(ExecuteManager executeManager)
    {
        this.executeManager = executeManager;
    }

    public void Subscribe()
    {
        GameManager.OnCardPlayed += HandleCardPlayed;
        GameManager.OnCardPlayResolved += HandleCardPlayResolved;
        GameManager.OnDiceRolled += HandleDiceRolled;
        GameManager.OnLineResolved += HandleLineResolved;
        GameManager.OnRoundEnd += HandleRoundEnd;
    }

    public void Unsubscribe()
    {
        GameManager.OnCardPlayed -= HandleCardPlayed;
        GameManager.OnCardPlayResolved -= HandleCardPlayResolved;
        GameManager.OnDiceRolled -= HandleDiceRolled;
        GameManager.OnLineResolved -= HandleLineResolved;
        GameManager.OnRoundEnd -= HandleRoundEnd;
    }

    // ===============
    // EVENTS
    // ===============

    void HandleCardPlayed(Card card)
    {
        if (card.unit != null) Queue(card.unit, EffectTrigger.OnPlay, executeManager.effectStack, card);

        foreach (Unit unit in GetBoardUnits()) Queue(unit, EffectTrigger.OnAnyCardPlayed, executeManager.effectStack, card);
    }

    void HandleCardPlayResolved(Card card)
    {
        foreach (Unit unit in GetBoardUnits()) Queue(unit, EffectTrigger.AfterAnyCardPlayed, executeManager.effectStack, card);
    }

    void HandleDiceRolled(Card card, int result)
    {
        foreach (Unit unit in GetBoardUnits()) Queue(unit, EffectTrigger.OnDiceRolled, executeManager.rollEffectStack, card, result);
    }

    void HandleLineResolved()
    {
        foreach (Unit unit in GetBoardUnits()) Queue(unit, EffectTrigger.OnOwnLineResolved, executeManager.battleOverEffectStack);
    }

    void HandleRoundEnd()
    {
        // units are removed from the board at the end of round, so all per unit data can go
        memories.Clear();
        usedThisRound.Clear();
    }

    /// <summary>
    /// Called by Unit.KnockOut. Adds the unit's OnWouldBeKnockedOut effects to the list and returns true if one of them prevents the knock out
    /// </summary>
    public bool QueueKnockOutEffects(Unit unit, List<IEnumerator> effects)
    {
        List<AbilityEffect> queued = Queue(unit, EffectTrigger.OnWouldBeKnockedOut, effects);
        return queued.Exists(effect => effect.PreventsKnockOut);
    }

    // ===============
    // QUEUEING
    // ===============

    /// <summary>
    /// Copy of all units on board, so effects can safely add or remove units while we loop
    /// </summary>
    List<Unit> GetBoardUnits() => new List<Unit>(GameManager.instance.fieldManager.unitList);

    /// <summary>
    /// Adds the unit's effects with the given trigger to the stack. Returns the effects that were added
    /// </summary>
    List<AbilityEffect> Queue(Unit unit, EffectTrigger trigger, List<IEnumerator> stack, Card triggeringCard = null, int diceResult = 0)
    {
        List<AbilityEffect> queued = new List<AbilityEffect>();

        // knocked out units don't trigger effects
        if (unit == null || unit.card == null || unit.stunned) return queued;

        foreach (Ability ability in unit.card.abilities)
        {
            if (ability == null || ability.abilityData == null) continue;

            foreach (AbilityEffect effect in ability.abilityData.effects)
            {
                if (effect == null || effect.trigger != trigger) continue;

                (Unit, AbilityEffect) key = (unit, effect);
                if (effect.oncePerRound && usedThisRound.Contains(key)) continue;

                EffectContext ctx = new EffectContext
                {
                    self = unit,
                    ability = ability,
                    triggeringCard = triggeringCard,
                    diceResult = diceResult,
                    memory = GetMemory(key)
                };

                // line effects only care about the line that was just resolved
                if (trigger == EffectTrigger.OnOwnLineResolved && !ctx.IsOnCurrentBattleLine) continue;
                if (!effect.CanTrigger(ctx)) continue;

                if (effect.oncePerRound) usedThisRound.Add(key);
                stack.Add(effect.Run(ctx));
                queued.Add(effect);
            }
        }
        return queued;
    }

    Dictionary<string, int> GetMemory((Unit, AbilityEffect) key)
    {
        if (!memories.TryGetValue(key, out Dictionary<string, int> memory))
        {
            memory = new Dictionary<string, int>();
            memories[key] = memory;
        }
        return memory;
    }
}
