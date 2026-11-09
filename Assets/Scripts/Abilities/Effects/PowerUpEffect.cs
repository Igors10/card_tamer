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
        return ConditionMet(ctx.self.card, ctx.IsAttacking, FightManager.instance.GetOpposing(ctx.self).card);
    }

    /// <summary>
    /// The bonus depends on which card it fights (Flanker), so before the fight it's only a maybe
    /// </summary>
    public bool DependsOnOpposingCard => condition == PowerUpCondition.AgainstSideCard || condition == PowerUpCondition.AttackingSideCard;

    /// <summary>
    /// Before a fight: could the card get this bonus if it fought now (bonuses about the opposing card count as possible)
    /// </summary>
    public bool CouldTrigger(Card self, bool attacking)
    {
        switch (condition)
        {
            case PowerUpCondition.AgainstSideCard: return true;
            case PowerUpCondition.AttackingSideCard: return attacking;
            default: return ConditionMet(self, attacking, null);
        }
    }

    /// <summary>
    /// The condition for a card that would fight (attacking or defending). Without the opposing card (null, the card
    /// shows its possible bonuses before a fight) the conditions about the opposing card are false
    /// </summary>
    public bool ConditionMet(Card self, bool attacking, Card opposing)
    {
        FightManager fight = FightManager.instance;
        Player owner = self.player;
        Player opponent = GameManager.instance.GetOpponentOfPlayer(owner);
        switch (condition)
        {
            case PowerUpCondition.WhenAttacking: return attacking;
            case PowerUpCondition.WhenDefending: return !attacking;
            case PowerUpCondition.FewerCards: return owner.cardsOnField.Count < opponent.cardsOnField.Count;
            case PowerUpCondition.LessHP: return owner.health < opponent.health;
            case PowerUpCondition.FaceDownAlly: return owner.cardsOnField.Exists(card => card != self && card.faceDown);
            case PowerUpCondition.PreviousFightLost: return owner.lostLastFight;
            case PowerUpCondition.AgainstSideCard: return opposing != null && fight.IsSideCard(opposing);
            case PowerUpCondition.OpponentHasMorePowerUps: return false;
            case PowerUpCondition.NoCardDestroyedThisRound: return !self.destroyedCardThisRound;
            case PowerUpCondition.IsPhantom: return self.isPhantom;
            case PowerUpCondition.OpponentNoFaceDown: return !opponent.cardsOnField.Exists(card => card.faceDown);
            case PowerUpCondition.AttackingSideCard: return attacking && opposing != null && fight.IsSideCard(opposing);
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
