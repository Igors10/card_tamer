using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Knocks out the unit with the highest power on each side, this unit is left out (random pick between ties)
/// </summary>
[System.Serializable]
public class KnockOutHighestEachSideEffect : AbilityEffect
{
    [Tooltip("Pause after the knock outs start, so the units are seen flying off before the next effect")]
    public float pauseAfterKnockOut = 0.4f;

    public override bool CanTrigger(EffectContext ctx) => FindHighest(ctx, TargetSide.Enemies) != null || FindHighest(ctx, TargetSide.Allies) != null;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        // picking both first, so one knock out can't change who the other target is
        Unit highestEnemy = FindHighest(ctx, TargetSide.Enemies);
        Unit highestAlly = FindHighest(ctx, TargetSide.Allies);
        if (highestEnemy == null && highestAlly == null) yield break;

        // not waiting for the whole knock out flight, only for the start of it
        if (highestEnemy != null) highestEnemy.StartCoroutine(highestEnemy.KnockOut());
        if (highestAlly != null) highestAlly.StartCoroutine(highestAlly.KnockOut());
        yield return new WaitForSeconds(pauseAfterKnockOut);
    }

    Unit FindHighest(EffectContext ctx, TargetSide side)
    {
        // units that are already knocked out don't count
        List<Unit> options = ctx.GetUnits(side).FindAll(unit => !unit.stunned && !unit.knockedOut);
        if (options.Count == 0) return null;

        int maxPower = int.MinValue;
        foreach (Unit unit in options) maxPower = Mathf.Max(maxPower, unit.card.currentPower);

        List<Unit> highestUnits = options.FindAll(unit => unit.card.currentPower == maxPower);
        return highestUnits[Random.Range(0, highestUnits.Count)];
    }
}
