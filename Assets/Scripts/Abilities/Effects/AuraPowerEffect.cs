using System.Collections;
using UnityEngine;

/// <summary>
/// Passive: units on nearby lanes have extra power while this unit is on the board (and not knocked out).
/// The bonus itself is calculated by FieldManager.RefreshAuras, the trigger only shows the presentation
/// </summary>
[System.Serializable]
public class AuraPowerEffect : AbilityEffect
{
    public TargetSide affects = TargetSide.Allies;
    public int amount = 1;
    [Tooltip("How many lanes to each side are affected (1 = neighbouring lanes)")]
    public int laneRange = 1;
    public bool includeOwnLane = false;

    /// <summary>
    /// Returns true if the target gets the bonus from this aura
    /// </summary>
    public bool Affects(Unit source, int sourceLane, Unit target, int targetLane)
    {
        if (source == target) return false;

        bool isAlly = source.card.player == target.card.player;
        if (affects == TargetSide.Allies && !isAlly) return false;
        if (affects == TargetSide.Enemies && isAlly) return false;

        int laneDistance = Mathf.Abs(sourceLane - targetLane);
        if (laneDistance == 0) return includeOwnLane;
        return laneDistance <= laneRange;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.Fields.RefreshAuras();
        yield break;
    }
}
