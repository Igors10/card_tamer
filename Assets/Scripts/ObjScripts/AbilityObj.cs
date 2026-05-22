using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ability")]
public class AbilityObj : ScriptableObject
{
    public int power;
    public string abilityDescription;

    public GameObject effect;
}
