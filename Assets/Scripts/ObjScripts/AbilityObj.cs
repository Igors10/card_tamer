using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ability")]
public class AbilityObj : ScriptableObject
{
    public int speed;
    public int power;
    public int block;
    public string abilityDescription;
    public string abilityName;
    public bool isPassive;
}
