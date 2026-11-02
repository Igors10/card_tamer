using System.Collections;
using UnityEngine;

public enum PowerUpCondition
{
    Always,
    WhenAttacking,
    WhenDefending,
    FewerCards,              // you have fewer cards on the field than the opponent
    LessHP,                  // you have less HP than the opponent
    FaceDownAlly,            // another card on your field is face down
    PreviousFightLost,       // your side lost its previous fight (a tie counts as lost)
    AgainstSideCard,         // the opposing card is the leftmost or rightmost on its field
    OpponentHasMorePowerUps, // use with the OnPowerUpsCounted trigger
    NoCardDestroyedThisRound, // this card hasn't destroyed a card this round (Beast)
    IsPhantom                // this card is the phantom copy (Phantom)
}

/// <summary>
/// Gives the unit power-ups (+1d6 each) for this fight when the condition is met
/// </summary>
[System.Serializable]
public class PowerUpEffect : AbilityEffect
{
    [Tooltip("When the card gets the power-ups")]
    public PowerUpCondition condition = PowerUpCondition.Always;
    [Tooltip("How many power-ups (each one is a d6)")]
    public int amount = 1;

    public PowerUpEffect()
    {
        trigger = EffectTrigger.OnFightStart;
    }

    public override bool CanTrigger(EffectContext ctx)
    {
        FightManager fight = FightManager.instance;
        switch (condition)
        {
            case PowerUpCondition.WhenAttacking: return ctx.IsAttacking;
            case PowerUpCondition.WhenDefending: return ctx.IsDefending;
            case PowerUpCondition.FewerCards: return ctx.Owner.cardsOnField.Count < ctx.Opponent.cardsOnField.Count;
            case PowerUpCondition.LessHP: return ctx.Owner.health < ctx.Opponent.health;
            case PowerUpCondition.FaceDownAlly: return ctx.Owner.cardsOnField.Exists(card => card != ctx.self.card && card.faceDown);
            case PowerUpCondition.PreviousFightLost: return ctx.Owner.lostLastFight;
            case PowerUpCondition.AgainstSideCard: return fight.IsSideCard(fight.GetOpposing(ctx.self).card);
            case PowerUpCondition.OpponentHasMorePowerUps: return fight.CountedPowerUps(fight.GetOpposing(ctx.self)) > fight.CountedPowerUps(ctx.self);
            case PowerUpCondition.NoCardDestroyedThisRound: return !ctx.self.card.destroyedCardThisRound;
            case PowerUpCondition.IsPhantom: return ctx.self.card.isPhantom;
            default: return true;
        }
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        FightManager.instance.AddPowerUps(ctx.self, amount);
        yield break;
    }
}
