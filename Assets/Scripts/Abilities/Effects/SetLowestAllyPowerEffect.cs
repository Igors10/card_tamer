using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sets the power of your weakest unit to a fixed value (random pick between ties)
/// </summary>
[System.Serializable]
public class SetLowestAllyPowerEffect : AbilityEffect
{
    public int newPower = 6;
    public bool canTargetSelf = false;

    // skipped if the weakest ally is already at (or above) the new power
    public override bool CanTrigger(EffectContext ctx) => FindLowest(ctx) != null;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        Unit lowest = FindLowest(ctx);
        if (lowest == null) yield break;

        lowest.card.GainPower(newPower - lowest.card.currentPower);
    }

    Unit FindLowest(EffectContext ctx)
    {
        // knocked out units don't count
        List<Unit> options = ctx.GetUnits(TargetSide.Allies, canTargetSelf).FindAll(unit => !unit.stunned);
        if (options.Count == 0) return null;

        int minPower = int.MaxValue;
        foreach (Unit unit in options) minPower = Mathf.Min(minPower, unit.card.currentPower);
        if (minPower >= newPower) return null;

        List<Unit> lowestUnits = options.FindAll(unit => unit.card.currentPower == minPower);
        return lowestUnits[GameRandom.Range(0, lowestUnits.Count)];
    }
}
