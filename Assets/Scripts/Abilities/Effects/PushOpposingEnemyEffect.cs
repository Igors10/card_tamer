using System.Collections;
using UnityEngine;

/// <summary>
/// Pushes the front enemy on the opposing field to a free slot on an adjacent field (random side if both are free)
/// </summary>
[System.Serializable]
public class PushOpposingEnemyEffect : AbilityEffect
{
    public override bool CanTrigger(EffectContext ctx) => !ctx.Fields.IsFieldEmpty(ctx.OpposingField);

    protected override IEnumerator Execute(EffectContext ctx)
    {
        Field[] enemyFields = ctx.Opponent.fields;
        int fieldID = ctx.FieldID;
        Field opposingField = enemyFields[fieldID];
        Unit enemy = opposingField.GetFieldUnits()[0];

        // finding free slots on both sides
        int leftSlot = GetFreeSlot(enemyFields, fieldID - 1);
        int rightSlot = GetFreeSlot(enemyFields, fieldID + 1);
        if (leftSlot < 0 && rightSlot < 0) yield break; // nowhere to push

        // choosing which side to push to: left -1, right +1
        int side;
        if (leftSlot >= 0 && rightSlot >= 0) side = (Random.value > 0.5f) ? -1 : 1;
        else side = (leftSlot >= 0) ? -1 : 1;

        // moving the enemy, and the unit behind it to the front
        Unit backUnit = ctx.Fields.GetAnotherUnit(enemy);
        ctx.Fields.MoveUnit(enemy, enemyFields[fieldID + side]);
        if (backUnit != null) ctx.Fields.MoveUnit(backUnit, opposingField);
    }

    /// <summary>
    /// Returns first free slot of the field, or -1 if the field doesn't exist or is full
    /// </summary>
    static int GetFreeSlot(Field[] fields, int fieldID)
    {
        if (fieldID < 0 || fieldID >= fields.Length) return -1;

        for (int i = 0; i < fields[fieldID].units.Length; i++)
        {
            if (fields[fieldID].units[i] == null) return i;
        }
        return -1;
    }
}
