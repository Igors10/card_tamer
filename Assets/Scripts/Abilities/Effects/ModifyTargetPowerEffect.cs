using System.Collections;

/// <summary>
/// Lets the player choose a unit and changes its power (negative amount = debuff)
/// </summary>
[System.Serializable]
public class ModifyTargetPowerEffect : AbilityEffect
{
    public TargetSide targets = TargetSide.Enemies;
    public int amount = -2;
    public string targetHint = "Choose a unit";
    public bool canTargetSelf = false;

    public override bool CanTrigger(EffectContext ctx) => ctx.GetUnits(targets, canTargetSelf).Count > 0;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return ctx.ChooseTarget(ctx.GetUnits(targets, canTargetSelf), targetHint);

        if (ctx.chosenTarget != null) ctx.chosenTarget.card.GainPower(amount);
    }
}
