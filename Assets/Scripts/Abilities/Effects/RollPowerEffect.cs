using System.Collections;

/// <summary>
/// Rolls a die and gains the rolled amount of power (basic Roll ability)
/// </summary>
[System.Serializable]
public class RollPowerEffect : AbilityEffect
{
    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return ctx.self.RollPower();
    }
}
