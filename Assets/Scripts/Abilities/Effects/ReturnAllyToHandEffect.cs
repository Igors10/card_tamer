using System.Collections;

/// <summary>
/// Returns a chosen ally back to its owner's hand, the card's power resets like at the end of a round
/// </summary>
[System.Serializable]
public class ReturnAllyToHandEffect : AbilityEffect
{
    public string targetHint = "Choose an ally to return to your hand";
    public bool canTargetSelf = false;

    public override bool CanTrigger(EffectContext ctx)
    {
        return ctx.GetUnits(TargetSide.Allies, canTargetSelf).Count > 0;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return ctx.ChooseTarget(ctx.GetUnits(TargetSide.Allies, canTargetSelf), targetHint);

        if (ctx.chosenTarget != null) yield return ctx.chosenTarget.ReturnToHand();
    }
}
