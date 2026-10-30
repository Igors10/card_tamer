using System.Collections;
using UnityEngine;

/// <summary>
/// Knocks out the other ally on this unit's field and gains power for it
/// </summary>
[System.Serializable]
public class KnockOutFieldAllyEffect : AbilityEffect
{
    public int powerGain = 3;
    [Tooltip("Pause after the knock out starts, so the unit is seen flying off before the power gain")]
    public float pauseAfterKnockOut = 0.4f;

    public override bool CanTrigger(EffectContext ctx) => FindFieldAlly(ctx) != null;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        Unit ally = FindFieldAlly(ctx);
        if (ally == null) yield break;

        // not waiting for the whole knock out flight, only for the start of it
        ally.StartCoroutine(ally.KnockOut());
        yield return new WaitForSeconds(pauseAfterKnockOut);

        // the knock out could have been prevented, the power is gained either way
        ctx.self.card.GainPower(powerGain);
    }

    Unit FindFieldAlly(EffectContext ctx)
    {
        // units that are already knocked out don't count
        return ctx.OwnField.GetFieldUnits().Find(unit => unit != ctx.self && !unit.stunned && !unit.knockedOut);
    }
}
