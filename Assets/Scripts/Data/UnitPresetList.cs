using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New unit preset list", menuName = "UnitPresetList")]
public class UnitPresetList : ScriptableObject
{
    public List<UnitPreset> presets = new List<UnitPreset>();
}
