using System.Collections;

/// <summary>
/// Blocks the opposing enemy field for some rounds, so no units can be placed there
/// </summary>
[System.Serializable]
public class BlockOpposingFieldEffect : AbilityEffect
{
    public int rounds = 2;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.OpposingField.roundsBlocked += rounds;
        ctx.OpposingField.RefreshFieldVisuals();
        yield break;
    }
}
