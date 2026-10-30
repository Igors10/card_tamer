using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes power from every other unit on the board and adds it to this unit (units without power have nothing to give)
/// </summary>
[System.Serializable]
public class StealPowerFromAllEffect : AbilityEffect
{
    public int amountPerUnit = 1;
    [Tooltip("Pause between each unit losing power")]
    public float pauseBetweenSteals = 0.15f;

    public override bool CanTrigger(EffectContext ctx) => FindVictims(ctx).Count > 0;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        int stolen = 0;
        foreach (Unit victim in FindVictims(ctx))
        {
            // power never goes below 0, so only what the unit has can be taken
            int amount = Mathf.Min(amountPerUnit, victim.card.currentPower);
            victim.card.GainPower(-amount);
            stolen += amount;
            yield return new WaitForSeconds(pauseBetweenSteals);
        }

        if (stolen > 0) ctx.self.card.GainPower(stolen);
    }

    List<Unit> FindVictims(EffectContext ctx)
    {
        // knocked out units don't count
        return ctx.GetUnits(TargetSide.Anyone).FindAll(unit => !unit.stunned && unit.card.currentPower > 0);
    }
}
