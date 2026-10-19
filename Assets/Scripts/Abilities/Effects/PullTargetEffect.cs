using System.Collections;
using Unity.VisualScripting;

/// <summary>
/// Rolls a die and gains the rolled amount of power (basic Roll ability)
/// </summary>
[System.Serializable]
public class PullTargetEffect : AbilityEffect
{
    public TargetSide targets = TargetSide.Enemies;
    public string targetHint = "Choose a unit to pull";
    public bool canTargetSelf = false;

    public override bool CanTrigger(EffectContext ctx)
    {
        // checking if there is space on the lane
        bool isOpponentFieldAvailable =
            ctx.Opponent.fields[GameManager.instance.fieldManager.GetFieldID(ctx.self.currentField)].GetFieldUnits().Count < ctx.self.currentField.units.Length;
        bool isPlayerFieldAvailable = ctx.self.currentField.GetFieldUnits().Count < ctx.self.currentField.units.Length;

        // checking if there are units to choose from
        bool unitsExist = ctx.GetUnits(targets, canTargetSelf).Count > 0;

        bool toReturn = (targets == TargetSide.Allies) ? unitsExist && isPlayerFieldAvailable : unitsExist && isOpponentFieldAvailable;
        return toReturn;
    }

    protected override IEnumerator Execute(EffectContext ctx)
    {
        yield return ctx.ChooseTarget(ctx.GetUnits(targets, canTargetSelf), targetHint);

        if (ctx.chosenTarget != null)
        {
            Field fieldToPullTo = ctx.self.currentField;
            // pulls to opponent's lane when choosing an enemy
            if (targets == TargetSide.Enemies) fieldToPullTo = ctx.chosenTarget.card.player.fields[GameManager.instance.fieldManager.GetFieldID(ctx.self.currentField)];
            GameManager.instance.fieldManager.MoveUnit(ctx.chosenTarget, fieldToPullTo);
        }
    }
}

