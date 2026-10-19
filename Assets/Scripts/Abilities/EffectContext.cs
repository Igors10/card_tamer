using System.Collections;
using System.Collections.Generic;

public enum TargetSide
{
    Enemies,
    Allies,
    Anyone
}

/// <summary>
/// Everything an effect needs to know when it runs, plus shortcuts for common board queries
/// </summary>
public class EffectContext
{
    public Unit self;                   // unit that owns the effect
    public Ability ability;             // ability the effect belongs to
    public Card triggeringCard;         // card that caused the trigger (played card, card that rolled), can be null
    public int diceResult;              // rolled value for OnDiceRolled
    public Dictionary<string, int> memory; // per unit + effect storage, cleared at the end of each round
    public Unit chosenTarget;           // result of ChooseTarget

    // players
    public Player Owner => self.card.player;
    public Player Opponent => GameManager.instance.GetOpponentOfPlayer(Owner);

    // fields
    public FieldManager Fields => GameManager.instance.fieldManager;
    public int FieldID => Fields.GetFieldID(self.currentField);
    public Field OwnField => self.currentField;
    public Field OpposingField => Opponent.fields[FieldID];
    public bool IsOnCurrentBattleLine => FieldID == GameManager.instance.battleManager.currentLine;

    // units
    public List<Unit> EnemyUnits => Fields.GetAllUnits(false, Opponent);
    public List<Unit> AllyUnits => Fields.GetAllUnits(false, Owner);

    /// <summary>
    /// Returns units of the chosen side, the effect owner is left out unless includeSelf is true
    /// </summary>
    public List<Unit> GetUnits(TargetSide side, bool includeSelf = false)
    {
        List<Unit> units;
        switch (side)
        {
            case TargetSide.Enemies: units = EnemyUnits; break;
            case TargetSide.Allies: units = AllyUnits; break;
            default: units = Fields.GetAllUnits(); break;
        }

        if (!includeSelf) units.Remove(self);
        return units;
    }

    /// <summary>
    /// Highlights options and waits until the player (or AI) chooses one. Result is stored in chosenTarget
    /// </summary>
    public IEnumerator ChooseTarget(List<Unit> options, string hint)
    {
        chosenTarget = null;
        if (options.Count == 0) yield break;

        ExecuteManager executeManager = GameManager.instance.executeManager;
        yield return executeManager.ShowTargets(options, hint);
        chosenTarget = executeManager.GetTarget();
    }
}
