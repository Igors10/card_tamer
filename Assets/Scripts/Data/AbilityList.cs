using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New ability list", menuName = "AbilityList")]
public class AbiltiyList : ScriptableObject
{
    public List<AbilityObj> abilityList = new List<AbilityObj>();
}
