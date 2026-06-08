using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "card database", menuName = "Card Database")]
public class CardDatabase : ScriptableObject
{
    public List<OpponentIndentity> cardSets;

    public OpponentIndentity GetRandomCardSet()
    {
        return cardSets[Random.Range(0, cardSets.Count)];
    }

    public UnitPreset GetRandomBasicPreset()
    {
        OpponentIndentity cardSet = GetRandomCardSet();

        return cardSet.basicUnits[Random.Range(0, cardSet.basicUnits.Count)];
    }

    public UnitPreset GetRandomSpecialPreset()
    {
        OpponentIndentity cardSet = GetRandomCardSet();

        return cardSet.specialUnits[Random.Range(0, cardSet.specialUnits.Count)];
    }
}
