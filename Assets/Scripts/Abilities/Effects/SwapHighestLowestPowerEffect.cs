using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Swaps the power of the strongest and the weakest unit (random pick between ties)
/// </summary>
[System.Serializable]
public class SwapHighestLowestPowerEffect : AbilityEffect
{
    public TargetSide units = TargetSide.Anyone;
    public bool canTargetSelf = false;

    public override bool CanTrigger(EffectContext ctx) => FindHighestAndLowest(ctx, out _, out _);

    protected override IEnumerator Execute(EffectContext ctx)
    {
        if (!FindHighestAndLowest(ctx, out Unit highest, out Unit lowest)) yield break;

        int difference = highest.card.currentPower - lowest.card.currentPower;
        highest.card.GainPower(-difference);
        lowest.card.GainPower(difference);
    }

    /// <summary>
    /// Returns false if there is nothing to swap (less than 2 units or all have the same power)
    /// </summary>
    bool FindHighestAndLowest(EffectContext ctx, out Unit highest, out Unit lowest)
    {
        highest = null;
        lowest = null;

        // knocked out units don't count
        List<Unit> options = ctx.GetUnits(units, canTargetSelf).FindAll(unit => !unit.stunned);
        if (options.Count < 2) return false;

        int maxPower = int.MinValue;
        int minPower = int.MaxValue;
        foreach (Unit unit in options)
        {
            maxPower = Mathf.Max(maxPower, unit.card.currentPower);
            minPower = Mathf.Min(minPower, unit.card.currentPower);
        }
        if (maxPower == minPower) return false;

        List<Unit> highestUnits = options.FindAll(unit => unit.card.currentPower == maxPower);
        List<Unit> lowestUnits = options.FindAll(unit => unit.card.currentPower == minPower);
        highest = highestUnits[Random.Range(0, highestUnits.Count)];
        lowest = lowestUnits[Random.Range(0, lowestUnits.Count)];
        return true;
    }
}
