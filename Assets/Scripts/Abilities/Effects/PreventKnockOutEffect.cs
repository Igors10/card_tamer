using System.Collections;

/// <summary>
/// This unit can't be knocked out. The presentation shows every time a knock out is blocked
/// </summary>
[System.Serializable]
public class PreventKnockOutEffect : AbilityEffect
{
    public PreventKnockOutEffect()
    {
        trigger = EffectTrigger.OnWouldBeKnockedOut;
    }

    public override bool PreventsKnockOut => true;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        // the hit still lands, it just doesn't knock out
        AudioManager.instance.PlaySFX("HitSFX");
        yield break;
    }
}
