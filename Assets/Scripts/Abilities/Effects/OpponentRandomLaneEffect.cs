using System.Collections;

/// <summary>
/// The opponent's next card is played on a random free lane instead of the one they chose
/// </summary>
[System.Serializable]
public class OpponentRandomLaneEffect : AbilityEffect
{
    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.Opponent.playsOnRandomLane = true;
        ctx.Opponent.playerUI.Refresh();
        yield break;
    }
}
