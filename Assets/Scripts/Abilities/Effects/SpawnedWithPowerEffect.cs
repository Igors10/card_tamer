using System.Collections;

public enum PowerReceiver
{
    SpawnedUnit,    // the unit that just entered play
    Self            // the unit that owns this effect
}

public enum PowerCheck
{
    Exactly,
    OrLess,
    OrMore
}

/// <summary>
/// When a unit enters play with [neededPower] starting power (base or rolled), changes the power of that unit or of this unit
/// </summary>
[System.Serializable]
public class SpawnedWithPowerEffect : AbilityEffect
{
    public int neededPower = 4;
    public PowerCheck check = PowerCheck.Exactly;
    public TargetSide spawnedBy = TargetSide.Enemies;
    public PowerReceiver receiver = PowerReceiver.SpawnedUnit;
    public int amount = -2;

    public SpawnedWithPowerEffect()
    {
        trigger = EffectTrigger.AfterAnyCardPlayed;
    }

    public override bool CanTrigger(EffectContext ctx)
    {
        Card spawned = ctx.triggeringCard;

        // this unit entering play doesn't count
        if (spawned == null || spawned == ctx.self.card) return false;
        if (spawned.unit == null || spawned.unit.stunned) return false;

        if (spawnedBy == TargetSide.Enemies && spawned.player != ctx.Opponent) return false;
        if (spawnedBy == TargetSide.Allies && spawned.player != ctx.Owner) return false;

        switch (check)
        {
            case PowerCheck.OrLess: return spawned.startingPower <= neededPower;
            case PowerCheck.OrMore: return spawned.startingPower >= neededPower;
            default: return spawned.startingPower == neededPower;
        }
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        Card cardToChange = (receiver == PowerReceiver.Self) ? ctx.self.card : ctx.triggeringCard;
        cardToChange.GainPower(amount);
        yield break;
    }
}
