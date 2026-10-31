using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new opponent identity", menuName = "OpponentIdentity")]
public class OpponentIndentity : ScriptableObject
{
    public string opponentName;
    public List<UnitPreset> basicUnits = new List<UnitPreset>();
    public List<UnitPreset> specialUnits = new List<UnitPreset>();
}
