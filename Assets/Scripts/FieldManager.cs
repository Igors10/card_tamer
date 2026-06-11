using FishNet.Editing;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] ParticleSystem spawnVFX;

    [Header("prefabs")]
    [SerializeField] GameObject unitPrefab;

    [Header("unit moving")]
    [SerializeField] float moveTriggerThreshold;

    Field[] GetCurrentPlayerFields()
    {
        return GameManager.instance.yourTurn ? GameManager.instance.player.fields : GameManager.instance.opponent.fields;
    }

    public List<Unit> GetStunnedUnits(Player player)
    {
        List<Unit> unitsToReturn = new List<Unit>();

        for (int i = 0; i < player.fields.Length; i++)
        {
            List<Unit> fieldUnits = player.fields[i].GetFieldUnits();
            foreach (Unit unit in fieldUnits) if (unit.stunned) unitsToReturn.Add(unit);
        }

        return unitsToReturn;
    }

    /// <summary>
    /// Making chosen spawnslot visible indicating that a card can be placed there
    /// </summary>
    /// <param name="spawnSlot"></param>
    public void EnableSpawnSlots(int spawnSlot = 1)
    {
        Field[] fields = GetCurrentPlayerFields();

        //if (!GameManager.instance.yourTurn) return;

        for (int i = 0;  i < fields.Length; i++)
        {
            fields[i].EnableSpawnSlot(spawnSlot);
        }
    }

    /// <summary>
    /// Shows fields that a creature can move to during its turn
    /// </summary>
    /// <param name="fieldStart"></param>
    /// <param name="movingRange"></param>
    public void EnableMoveSlots(Field fieldStart, int movingRange)
    {
        Field[] fields = GetCurrentPlayerFields();

        // resetting the slots
        DisableAllSlots();

        int fieldStartID = 0;

        for (int i = 0; i < fields.Length; i++) // finding the correct field in the field list
        {
            if (fieldStart == fields[i]) { fieldStartID = i; break; }
        }

        // deciding ids of first and last fields to highlight in the field array
        int firstAvailableField = (fieldStartID - movingRange < 0) ? 0 : fieldStartID - movingRange;
        int lastAvailableField = (fieldStartID + movingRange >= fields.Length) ? fields.Length - 1 : fieldStartID + movingRange;

        // highlighting and making available to move to chosen array of fields
        for (int i = firstAvailableField; i <= lastAvailableField; i++)
        {
            if(fields[i] != fieldStart) fields[i].EnableSpawnSlot(); // enables next available spawnslot if any
            fields[i].MoveHighlightField(true);
        }
    }

    /// <summary>
    /// Disabling all spawnslots on all fields
    /// </summary>
    public void DisableAllSlots()
    {
        Field[] fields = GetCurrentPlayerFields();

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].DisableAllSlots();
        }
    }

    // ================
    // VISUALS
    // ================

    public void Refresh()
    {
        // FIELD COLOR
        // ==================
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;

        // Highlighting the winning player field in each line
        for (int i = 0; i < player.fields.Length; i++)
        {
            Field playerField = player.fields[i];
            Field opponentField = opponent.fields[i];

            // comparing power
            bool lineTie = playerField.GetFieldPower() == opponentField.GetFieldPower();
            bool playerWins = playerField.GetFieldPower() > opponentField.GetFieldPower() && !lineTie;

            
            // player field color
            Color playerFieldColor = (playerField.GetFieldPower() != 0) ? Colors.instance.BlendColor(player.playerColor, 0.5f) : Color.white;
            if (!playerWins && !lineTie && playerField.GetFieldPower() != 0) playerFieldColor = Colors.instance.BlendColor(player.playerColor, 0.3f);
            playerField.defaultColor = playerFieldColor;

            // opponent field color
            Color opponentFieldColor = (opponentField.GetFieldPower() != 0) ? Colors.instance.BlendColor(opponent.playerColor, 0.5f) : Color.white;
            if (playerWins && !lineTie && opponentField.GetFieldPower() != 0) opponentFieldColor = Colors.instance.BlendColor(opponent.playerColor, 0.3f);
            opponentField.defaultColor = opponentFieldColor;

            // applying colors
            playerField.RefreshFieldVisuals();
            opponentField.RefreshFieldVisuals();
        }
    }

    // ================
    // SPAWNING UNITS
    // ================

    /// <summary>
    /// Find all lines with empty backlines
    /// </summary>
    /// <param name="potentialFields"></param>
    /// <returns></returns>
    public List<Field> FindEmptyFields(Field[] potentialFields)
    {
        List<Field> fieldsToReturn = new List<Field>();
        for (int i = 0; i < potentialFields.Length; i++)
        {
            if (potentialFields[i].units[1] == null && potentialFields[i].roundsBlocked == 0) fieldsToReturn.Add(potentialFields[i]);
        }

        return fieldsToReturn;
    }

    /// <summary>
    /// Places a unit on a field from played card
    /// </summary>
    public void PlayCard(Card cardToPlay, Player player)
    {
        Field[] fields = GetCurrentPlayerFields();

        for (int i = 0;i < fields.Length;i++)
        {
            if (fields[i].PlayCard(cardToPlay, player))
            {
                break;
            }
        }
        
        DisableAllSlots();
    }

    public void SpawnUnit(Card cardToSpawn, Field fieldToSpawnOn)
    {
        // checking if field is full already
        if (fieldToSpawnOn.units[1] != null) { Debug.Log("Field: cannot spawn unit, field is already full"); return; }

        int nextEmptySlot = (fieldToSpawnOn.units[0] == null) ? 0 : 1; // chooses next availavble slot to spawn unit on
        GameObject newUnitObj = Instantiate(unitPrefab, fieldToSpawnOn.unitSlots[nextEmptySlot].transform.position, Quaternion.Euler(60f, 0f, 0f), this.gameObject.transform);
        Unit newUnit = newUnitObj.GetComponent<Unit>();
        fieldToSpawnOn.units[nextEmptySlot] = newUnit;
        newUnit.InitUnit(cardToSpawn, fieldToSpawnOn);

        // Plays spawning "poof" VFX
        GameManager.instance.VFXmanager.PlayVFX(newUnitObj.transform.position, spawnVFX);

        // playing soundeffect
        AudioManager.instance.PlaySFX("CardPlaySFX");
    }

    public int GetUnitSlot(Unit unit)
    {
        return (unit.currentField.units[0] == unit) ? 0 : 1;
    }

    public int GetFieldID(Field field)
    {
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;

        for (int i = 0; i < player.fields.Length; i++)
        {
            if (player.fields[i] == field || opponent.fields[i] == field) { Debug.Log("FieldManager: Requested field ID is " + i); return i; }
        }

        Debug.Log("FieldManager: field ID was not found");
        return 0;
    }

    public bool IsFieldEmpty(Field field, Unit unitExeption = null)
    {
        for (int i = 0; i < field.units.Length; i++)
        {
            // checking player's field
            if (field.units[i] != null && field.units[i] != unitExeption) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns another unit from the same field as passed unit
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public Unit GetAnotherUnit(Unit unit)
    {
        for (int i = 0; i < unit.currentField.units.Length; i++)
        {
            if (unit.currentField.units[i] != unit) return unit.currentField.units[i];
        }

        return null;
    }

    // ================
    // MOVIING UNITS
    // ================

    /// <summary>
    /// Moving a unit to a specified slot on a field
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="targetField"></param>
    /// <param name="targetSlot"></param>
    public void MoveUnit(Unit unit, Field targetField, int targetSlot, bool activatePrevSlot = false)
    {
        // play soundeffect
        AudioManager.instance.PlaySFX("UnitMoveSFX");

        // making prev unit slot empty
        unit.currentField.unitSlots[GetUnitSlot(unit)].gameObject.SetActive(activatePrevSlot);
        unit.currentField.units[GetUnitSlot(unit)] = null;

        // moving the unit to a new field slot
        unit.transform.position = targetField.unitSlots[targetSlot].transform.position;
        unit.currentField = targetField;
        targetField.units[targetSlot] = unit;
        targetField.unitSlots[targetSlot].gameObject.SetActive(false);

        // refreshing visuals
        Refresh();
    }

    /// <summary>
    /// Checking if player dragged unit close to any potential unit slot, returns true if unit was moved to a new slot
    /// </summary>
    /// <param name="movingUnit"></param>
    public bool CheckUnitMove(Unit movingUnit)
    {
        Field[] fields = GetCurrentPlayerFields();

        Debug.Log("FieldManager: checking if "+movingUnit.name+" is dropped on one of the unit slots");
        foreach (Field field in fields)
        {
            for (int i = 0; i < field.unitSlots.Length; i++)
            {
                if (Vector3.Distance(movingUnit.transform.position, field.unitSlots[i].transform.position) < moveTriggerThreshold 
                    && field.units[i] == null && field.unitSlots[i].gameObject.activeSelf)
                {
                    MoveUnit(movingUnit, field, i);
                    Debug.Log("FieldManager: " + movingUnit.name + " was dropped on one of the unit slots");

                    return true;
                }
            }
        }

        Debug.Log("FieldManager: " + movingUnit.name + " wasn't close to any of the unit slots");
        return false;
    }
}
