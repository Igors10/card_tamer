using System.Collections;
using UnityEngine;

public enum PowerUpCondition
{
    Always,
    WhenAttacking,
    WhenDefending
}

/// <summary>
/// Gives the unit power-ups (+1d6 each) for this fight when the condition is met
/// </summary>
[System.Serializable]
public class PowerUpEffect : AbilityEffect
{
    [Tooltip("When the card gets the power-ups")]
    public PowerUpCondition condition = PowerUpCondition.Always;
    [Tooltip("How many power-ups (each one is a d6)")]
    public int amount = 1;

    public PowerUpEffect()
    {
        trigger = EffectTrigger.OnFightStart;
    }

    public override bool CanTrigger(EffectContext ctx)
    {
        switch (condition)
        {
            case PowerUpCondition.WhenAttacking: return ctx.IsAttacking;
            case PowerUpCondition.WhenDefending: return ctx.IsDefending;
            default: return true;
        }
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        FightManager.instance.AddPowerUps(ctx.self, amount);
        yield break;
    }
}
