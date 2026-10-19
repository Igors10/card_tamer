using System.Collections;

/// <summary>
/// Knocks this unit out (by default after the battle on its line)
/// </summary>
[System.Serializable]
public class KnockSelfOutEffect : AbilityEffect
{
    public KnockSelfOutEffect()
    {
        trigger = EffectTrigger.OnOwnLineResolved;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.self.StartCoroutine(ctx.self.KnockOut());
        yield break;
    }
}
