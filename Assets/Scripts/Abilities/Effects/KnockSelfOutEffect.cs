using System.Collections;

/// <summary>
/// Knocks this unit out (by default after its fight)
/// </summary>
[System.Serializable]
public class KnockSelfOutEffect : AbilityEffect
{
    public KnockSelfOutEffect()
    {
        trigger = EffectTrigger.OnFightEnd;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return ctx.self.StartCoroutine(ctx.self.KnockOut());
    }
}
