using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature")]
public class CreatureObj : ScriptableObject
{
    public string creature_name;
    public string health;
    public Sprite creature_sprite;
    public FoodType foot_type;
    public int cost;
    public AbilityObj[] ability = new AbilityObj[2];
}

public enum FoodType
{
    MEAT,
    FISH,
    BERRIES
}
