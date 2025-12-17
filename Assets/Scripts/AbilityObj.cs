using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ability")]
public class AbilityObj : ScriptableObject
{
    public int speed;
    public int power;
    public string ability_description;
    public string ability_name;
    public bool is_passive;
}
