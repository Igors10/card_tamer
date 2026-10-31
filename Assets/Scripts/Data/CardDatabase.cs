using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "card database", menuName = "Card Database")]
public class CardDatabase : ScriptableObject
{
    // CARD SETS 
    // =====================================================
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

    /// <summary>
    /// Every unit preset in one list (basic and special units of every set). Online, cards send the preset's place in it
    /// </summary>
    public List<UnitPreset> GetAllPresets()
    {
        List<UnitPreset> allPresets = new List<UnitPreset>();
        foreach (OpponentIndentity cardSet in cardSets)
        {
            allPresets.AddRange(cardSet.basicUnits);
            allPresets.AddRange(cardSet.specialUnits);
        }
        return allPresets;
    }

    // COLORS
    // ========================================================
    public List<Color> allPlayerColors;
}
