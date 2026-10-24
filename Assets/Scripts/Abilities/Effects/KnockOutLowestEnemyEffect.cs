using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Knocks out the enemy unit with the lowest power (random pick between ties)
/// </summary>
[System.Serializable]
public class KnockOutLowestEnemyEffect : AbilityEffect
{
    [Tooltip("Pause after the knock out starts, so the unit is seen flying off before the next effect")]
    public float pauseAfterKnockOut = 0.4f;

    public override bool CanTrigger(EffectContext ctx) => FindLowest(ctx) != null;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        Unit lowest = FindLowest(ctx);
        if (lowest == null) yield break;

        // not waiting for the whole knock out flight, only for the start of it
        lowest.StartCoroutine(lowest.KnockOut());
        yield return new WaitForSeconds(pauseAfterKnockOut);
    }

    Unit FindLowest(EffectContext ctx)
    {
        // units that are already knocked out don't count
        List<Unit> options = ctx.EnemyUnits.FindAll(unit => !unit.stunned && !unit.knockedOut);
        if (options.Count == 0) return null;

        int minPower = int.MaxValue;
        foreach (Unit unit in options) minPower = Mathf.Min(minPower, unit.card.currentPower);

        List<Unit> lowestUnits = options.FindAll(unit => unit.card.currentPower == minPower);
        return lowestUnits[Random.Range(0, lowestUnits.Count)];
    }
}
