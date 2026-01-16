using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Creature", menuName = "Creature")]
public class CreatureObj : ScriptableObject
{
    public string creatureName;
    public int health;
    public Sprite cardSprite;
    public Sprite unitSprite;
    public FoodType foodType;
    public int cost;
    public AbilityObj[] ability = new AbilityObj[2];
}

public enum FoodType
{
    MEAT,
    FISH,
    BERRIES
}
