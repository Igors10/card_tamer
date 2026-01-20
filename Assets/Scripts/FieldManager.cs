using UnityEngine;

public class FieldManager : MonoBehaviour
{
    [Header("refs")]
    public Field[] fields = new Field[4];

    [Header("prefabs")]
    [SerializeField] GameObject unitPrefab;

    /// <summary>
    /// Making chosen spawnslot visible indicating that a card can be placed there
    /// </summary>
    /// <param name="spawnSlot"></param>
    public void EnableSpawnSlots(int spawnSlot = 1)
    {
        if (!GameManager.instance.yourTurn) return;

        for (int i = 0;  i < fields.Length; i++)
        {
            fields[i].EnableSpawnSlot(spawnSlot);
        }
    }

    /// <summary>
    /// Disabling all spawnslots on all fields
    /// </summary>
    public void DisableSpawnSlots()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].DisableSpawnSlots();
        }
    }

    /// <summary>
    /// Places a unit on a field from played card
    /// </summary>
    public void PlayCard(Card cardToPlay)
    {
        for (int i = 0;i < fields.Length;i++)
        {
            fields[i].PlayCard(cardToPlay);
        }
        DisableSpawnSlots();
    }

    public void SpawnUnit(Card cardToSpawn, Field field)
    {
        // checking if field is full already
        if (field.units[1] != null) { Debug.Log("Field: cannot spawn unit, field is already full"); return; }

        int nextEmptySlot = 1; // always spawns units at the back slot
        GameObject newUnitObj = Instantiate(unitPrefab, field.unitSlots[nextEmptySlot].transform.position, Quaternion.identity, this.gameObject.transform);
        Unit newUnit = newUnitObj.GetComponent<Unit>();
        field.units[nextEmptySlot] = newUnit;
        newUnit.InitUnit(cardToSpawn, field);
    }

    public int GetUnitSlot(Unit unit)
    {
        return (unit.currentField.units[0] == unit) ? 0 : 1;
    }
}
