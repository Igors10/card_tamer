using System.Collections;
using UnityEngine;

// Stickers that work from a card's place on the field or at a specific moment.
// They aren't queued by events (trigger Manual), FightManager / DiscardManager look for them on the card.

/// <summary>
/// Neighbour on the sticker's side gets power-ups when it fights (Cheerer). Runs on the fighting neighbour
/// </summary>
[System.Serializable]
public class NeighbourPowerUpEffect : AbilityEffect
{
    public int amount = 1;

    public NeighbourPowerUpEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        FightManager.instance.AddPowerUps(ctx.self, amount);
        yield break;
    }
}

/// <summary>
/// Attacks on the neighbour on the sticker's side hit this card instead (Guard)
/// </summary>
[System.Serializable]
public class GuardNeighbourEffect : AbilityEffect
{
    public GuardNeighbourEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// Card isn't destroyed by the discard, this sticker is lost instead (Lucky)
/// </summary>
[System.Serializable]
public class ProtectFromDiscardEffect : AbilityEffect
{
    public ProtectFromDiscardEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// When this card is revealed, the opponent's field cards get mixed up (Mixer)
/// </summary>
[System.Serializable]
public class ShuffleOpponentOnRevealEffect : AbilityEffect
{
    public ShuffleOpponentOnRevealEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// Card is destroyed for good after the fight, whatever the result (Bomb)
/// </summary>
[System.Serializable]
public class DiscardSelfAfterFightEffect : AbilityEffect
{
    public DiscardSelfAfterFightEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

// ===== specials =====

/// <summary>
/// Cards this one beats are destroyed for good, their owner gets a star in the workshop (Beast)
/// </summary>
[System.Serializable]
public class DestroyOnWinEffect : AbilityEffect
{
    public DestroyOnWinEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// When this card is attacked, the attacker is swapped for another random card of the attacking player (Joker)
/// </summary>
[System.Serializable]
public class SwapAttackerEffect : AbilityEffect
{
    public SwapAttackerEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// At the start of the battle the card gets a phantom copy in a random spot (Phantom)
/// </summary>
[System.Serializable]
public class PhantomCopyEffect : AbilityEffect
{
    [Tooltip("Base power of the phantom copy")]
    public int phantomPower = 4;

    public PhantomCopyEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// While this card is on the field, attacking a face down card lets you guess it, a right guess gives a power-up (Seer)
/// </summary>
[System.Serializable]
public class GuessTargetEffect : AbilityEffect
{
    public int amount = 1;

    public GuessTargetEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// At the start of the battle knocks out the opponent card in the same position (Dragon)
/// </summary>
[System.Serializable]
public class KnockOutSamePositionEffect : AbilityEffect
{
    public KnockOutSamePositionEffect()
    {
        trigger = EffectTrigger.Manual;
    }

    protected override IEnumerator Execute(EffectContext ctx) { yield break; }
}

/// <summary>
/// On attack, the cards next to the target are revealed (Scout)
/// </summary>
[System.Serializable]
public class RevealTargetNeighboursEffect : AbilityEffect
{
    public RevealTargetNeighboursEffect()
    {
        trigger = EffectTrigger.OnFightStart;
    }

    public override bool CanTrigger(EffectContext ctx) => ctx.IsAttacking;

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return FightManager.instance.RevealNeighbours(FightManager.instance.defender.card);
    }
}

/// <summary>
/// After winning, the next card of yours to fight gets power-ups (Momentum)
/// </summary>
[System.Serializable]
public class NextCardPowerUpEffect : AbilityEffect
{
    public int amount = 1;

    public NextCardPowerUpEffect()
    {
        trigger = EffectTrigger.OnFightEnd;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.Owner.nextFightPowerUps += amount;
        yield break;
    }
}

/// <summary>
/// After a fight the card turns face down and can swap places with another face down card (Sneaky)
/// </summary>
[System.Serializable]
public class HideSelfEffect : AbilityEffect
{
    public HideSelfEffect()
    {
        trigger = EffectTrigger.OnFightEnd;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return FightManager.instance.HideAndSwap(ctx.self.card);
    }
}
