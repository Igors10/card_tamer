using System.Collections;

/// <summary>
/// The opponent's next card has to be their special card (only triggers if they have it in hand)
/// </summary>
[System.Serializable]
public class OpponentMustPlaySpecialEffect : AbilityEffect
{
    public override bool CanTrigger(EffectContext ctx) => ctx.Opponent.GetSpecialInHand() != null;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.Opponent.mustPlaySpecial = true;
        ctx.Opponent.playerUI.Refresh();
        yield break;
    }
}
