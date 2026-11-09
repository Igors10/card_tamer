using System.Collections;
using UnityEngine;

// new values go at the end, the stickers save the condition as a number
public enum PowerUpCondition
{
    Always,
    WhenAttacking,
    WhenDefending,
    FewerCards,              // you have fewer cards on the field than the opponent
    LessHP,                  // you have less HP than the opponent
    FaceDownAlly,            // another card on your field is face down
    PreviousFightLost,       // your side lost its previous fight
    AgainstSideCard,         // the opposing card is the leftmost or rightmost on its field
    OpponentHasMorePowerUps, // old power-up rules, never true now
    NoCardDestroyedThisRound, // this card hasn't destroyed a card this round
    IsPhantom,               // this card is the phantom copy (Phantom)
    OpponentNoFaceDown,      // the opponent has no face down cards on the field (Scout)
    AttackingSideCard        // attacking the leftmost or rightmost card of the opponent's field (Flanker)
}

/// <summary>
/// Gives the unit power for this fight when the condition is met. A max above the amount gives a random amount in between
/// </summary>
[System.Serializable]
public class PowerUpEffect : AbilityEffect
{
    [Tooltip("When the card gets the power")]
    public PowerUpCondition condition = PowerUpCondition.Always;
    [Tooltip("How much power")]
    public int amount = 1;
    [Tooltip("Above the amount: random power from amount to this (like 0-3)")]
    public int maxAmount = 0;

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
            case PowerUpCondition.OpponentHasMorePowerUps: return false;
            case PowerUpCondition.NoCardDestroyedThisRound: return !ctx.self.card.destroyedCardThisRound;
            case PowerUpCondition.IsPhantom: return ctx.self.card.isPhantom;
            case PowerUpCondition.OpponentNoFaceDown: return !ctx.Opponent.cardsOnField.Exists(card => card.faceDown);
            case PowerUpCondition.AttackingSideCard: return ctx.IsAttacking && fight.IsSideCard(fight.GetOpposing(ctx.self).card);
            default: return true;
        }
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        // random amounts come from GameRandom, so online both games get the same power
        int power = (maxAmount > amount) ? GameRandom.Range(amount, maxAmount + 1) : amount;
        yield return FightManager.instance.AddPower(ctx.self, power);
    }
}
